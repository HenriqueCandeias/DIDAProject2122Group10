using Grpc.Core;
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

        private bool debug;

        private string puppetMasterURL;

        private Dictionary<int, string> storagesIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> crashedStoragesIdToURL = new Dictionary<int, string>();

        private Dictionary<string, WorkerService.WorkerServiceClient> workersIdToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        public WorkerDomain(int worker_delay, string puppet_master_URL, bool debug)
        {
            workerDelay = worker_delay;
            puppetMasterURL = puppet_master_URL;
            this.debug = debug;
        }

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            foreach (int key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                if (debug)
                {
                    Console.WriteLine("Storage: " + key + " URL: " + storagesIdToURL.GetValueOrDefault(key));
                }
            }

            GrpcChannel newWorkerChannel;

            foreach (string key in request.Workers.Keys)
            {
                newWorkerChannel = GrpcChannel.ForAddress(request.Workers[key]);
                workersIdToClient.Add(key, new WorkerService.WorkerServiceClient(newWorkerChannel));
                if (debug)
                {
                    Console.WriteLine("Worker: " + key + " URL: " + request.Workers[key]);
                }
            }

            replicationFactor = request.ReplicationFactor;

            return new SendNodesURLReply();
        }

        public StartAppReply StartApp(StartAppRequest request)
        {
            if(debug)
            {
            Console.WriteLine("Received a DIDARequest:");
            Console.WriteLine(request.DidaRequest.ToString());
            }

            //Load the operator mentioned in the request by reflection

            DIDAAssignment didaAssignment = request.DidaRequest.Chain[request.DidaRequest.Next];

            DIDAOperatorID didaOperatorID = didaAssignment.Operator;

            IDIDAOperator myOperator = LoadByReflection(didaOperatorID.Classname);

            if (myOperator == null)
            {
                if(debug)
                {
                    Console.WriteLine("Failed to load operator by reflection: " + didaOperatorID.Classname);
                }
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

            myOperator.ConfigureStorage(new StorageProxy(storagesURL.ToArray(), metaRecordConsistent, debug));

            string previousOutput = "";
            if (request.DidaRequest.Next - 1 >= 0)
                previousOutput = request.DidaRequest.Chain[request.DidaRequest.Next - 1].Output;

            string output = myOperator.ProcessRecord(metaRecordConsistent, request.DidaRequest.Input, previousOutput);

            //Check if the StorageProxy detected that an operator failed. If so, warn all other workers

            if(metaRecordConsistent.failedReplicasIds.Count > 0)
            {
                CrashRepRequests crashReportRequest;

                foreach (int replicaId in metaRecordConsistent.failedReplicasIds)
                {
                    crashedStoragesIdToURL.Add(replicaId, storagesIdToURL[replicaId]);
                    storagesIdToURL.Remove(replicaId);

                    crashReportRequest = new CrashRepRequests
                    {
                        Id = replicaId,
                    };

                    foreach (WorkerService.WorkerServiceClient client in workersIdToClient.Values)
                        client.CrashReport(crashReportRequest);
                }
            }

            //If the app is inconsistent, it should terminate
            
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

                if(debug)
                {
                    Console.WriteLine("Going to send to the worker "
                        + request.DidaRequest.Chain[request.DidaRequest.Next].Host + ":" + request.DidaRequest.Chain[request.DidaRequest.Next].Port + " the following request: ");
                    Console.WriteLine(request);
                }
                Thread.Sleep(workerDelay);

                nextWorkerClient.StartApp(request);  //nextWorkerClient.StartAppAsync(request);

                if(debug)
                {
                    Console.WriteLine("Request sent.");
                }
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
            Console.WriteLine("\r\nWORKER STATUS\r\n");

            foreach (KeyValuePair<int, string> pair in storagesIdToURL)
                Console.WriteLine("Active Storage - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("");

            foreach (KeyValuePair<int, string> pair in crashedStoragesIdToURL)
                Console.WriteLine("Crashed Storage - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("\r\nEND OF STATUS\r\n");

            return new StatusReply();
        }

        public CrashReply Crash()
        {
            if(debug)
                Console.WriteLine("Crashing...");
            Task.Delay(1000).ContinueWith(t => System.Environment.Exit(1));
            return new CrashReply();
        }
    }
}
