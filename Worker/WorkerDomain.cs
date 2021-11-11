﻿using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DIDAWorker;
using DIDAStorage;
using System.IO;
using System.Reflection;
using Grpc.Net.Client;
using Worker;
using DIDAOperator;
using System.Threading;

namespace Worker
{
    class WorkerDomain
    {
        private int workerDelay;

        private float replicationFactor;

        private string puppetMasterURL;

        private Dictionary<int, string> storagesIdToURL = new Dictionary<int, string>();

        public WorkerDomain(int worker_delay, string puppet_master_URL)
        {
            workerDelay = worker_delay;
            puppetMasterURL = puppet_master_URL;
        }

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            foreach (int key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key + " URL: " + storagesIdToURL.GetValueOrDefault(key));
            }

            replicationFactor = request.ReplicationFactor;

            return new SendNodesURLReply();
        }

        public StartAppReply StartApp(StartAppRequest request)
        {
            Console.WriteLine("Received a DIDARequest:");
            Console.WriteLine(request.DidaRequest.ToString());

            //Load the operator mentioned in the request by reflection

            DIDAAssignment didaAssignment = request.DidaRequest.Chain[request.DidaRequest.Next];

            DIDAOperatorID didaOperatorID = didaAssignment.Operator;

            IDIDAOperator myOperator = LoadByReflection(didaOperatorID.Classname);

            if (myOperator == null)
            {
                Console.WriteLine("Failed to load operator by reflection: " + didaOperatorID.Classname);
                return new StartAppReply();
            }

            //Create the DIDAMetaRecord to assign to the StorageProxy 

            DIDAMetaRecordConsistent metaRecordConsistent = new DIDAMetaRecordConsistent();
            metaRecordConsistent.Id = request.DidaRequest.DidaMetaRecord.Id;
            metaRecordConsistent.replicationFactor = replicationFactor;

            foreach (string recordId in request.DidaRequest.DidaMetaRecord.RecordIdToConsistentVersion.Keys)
            {
                metaRecordConsistent.RecordIdToConsistentVersion.Add(recordId, new DIDAWorker.DIDAVersion
                {
                    VersionNumber = request.DidaRequest.DidaMetaRecord.RecordIdToConsistentVersion[recordId].VersionNumber,
                    ReplicaId = request.DidaRequest.DidaMetaRecord.RecordIdToConsistentVersion[recordId].ReplicaId,
                });
            }

            //Create the list of StorageNodes to assign to the StorageProxy
                
            List<DIDAStorageNode> storagesURL = new List<DIDAStorageNode>();
            foreach (KeyValuePair<int, string> pair in storagesIdToURL)
            {
                storagesURL.Add(new DIDAStorageNode
                {
                    serverId = pair.Key.ToString(),
                    host = pair.Value.Split(':')[0] + ":" + pair.Value.Split(':')[1],
                    port = Int32.Parse(pair.Value.Split(':')[2]),
                });
            }

            //Configure and run the operator

            myOperator.ConfigureStorage(new StorageProxy(storagesURL.ToArray(), metaRecordConsistent));

            string previousOutput = "";
            if (request.DidaRequest.Next - 1 >= 0)
                previousOutput = request.DidaRequest.Chain[request.DidaRequest.Next - 1].Output;

            string output = myOperator.ProcessRecord(metaRecordConsistent, request.DidaRequest.Input, previousOutput);

            if (metaRecordConsistent.appIsInconsistent)
                return new StartAppReply();

            //Modify the received DIDARequest and send it to the next worker in the chain

            StartAppRequest nextWorkerRequest = new StartAppRequest()
            {
                DidaRequest = new DIDARequest()
                {
                    DidaMetaRecord = new DIDAMetaRecord()
                    {
                        Id = request.DidaRequest.DidaMetaRecord.Id,
                    },
                    Input = request.DidaRequest.Input,
                    Next = request.DidaRequest.Next++,
                    ChainSize = request.DidaRequest.ChainSize,
                },
            };

            nextWorkerRequest.DidaRequest.Chain.Add(request.DidaRequest.Chain);
            nextWorkerRequest.DidaRequest.Chain[request.DidaRequest.Next - 1].Output = output;
            foreach (KeyValuePair<string, DIDAWorker.DIDAVersion> pair in metaRecordConsistent.RecordIdToConsistentVersion)
            {
                nextWorkerRequest.DidaRequest.DidaMetaRecord.RecordIdToConsistentVersion.Add(pair.Key, new DIDAVersion
                {
                    VersionNumber = pair.Value.VersionNumber,
                    ReplicaId = pair.Value.ReplicaId,
                });
            }

            if (request.DidaRequest.Next < request.DidaRequest.ChainSize)
            {
                GrpcChannel nextWorkerChannel = GrpcChannel.ForAddress(
                    request.DidaRequest.Chain[request.DidaRequest.Next].Host + ":" + request.DidaRequest.Chain[request.DidaRequest.Next].Port);
                WorkerService.WorkerServiceClient nextWorkerClient = new WorkerService.WorkerServiceClient(nextWorkerChannel);

                Console.WriteLine("Going to send to the worker "
                    + request.DidaRequest.Chain[request.DidaRequest.Next].Host + ":" + request.DidaRequest.Chain[request.DidaRequest.Next].Port + " the following request: ");
                Console.WriteLine(request);

                Thread.Sleep(workerDelay);

                nextWorkerClient.StartApp(request);  //nextWorkerClient.StartAppAsync(request);
                Console.WriteLine("Request sent.");
            }

            return new StartAppReply();
        }

        static IDIDAOperator LoadByReflection(string className)
        {
            IDIDAOperator _objLoadedByReflection;
            try 
            {
                foreach (string filename in Directory.EnumerateFiles(
                    Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Worker\\bin\\Debug\\netcoreapp3.1\\"))
                {
                    if (filename.EndsWith(".dll"))
                    {
                        Assembly _dll = Assembly.LoadFrom(filename);
                        Type[] _typeList = _dll.GetTypes();

                        foreach (Type type in _typeList)
                        {
                            if (type.Name == className)
                            {
                                Console.WriteLine("Found type to load dynamically: " + className);
                                _objLoadedByReflection = (IDIDAOperator)Activator.CreateInstance(type);

                                return _objLoadedByReflection;
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        public StatusReply Status()
        {
            //TODO display necessary info
            Console.WriteLine("Status: I'm alive");
            return new StatusReply();
        }

        public CrashReply Crash()
        {
            Task.Delay(1000).ContinueWith(t => System.Environment.Exit(1));
            return new CrashReply();
        }
    }
}
