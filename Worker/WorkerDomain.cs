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

namespace Worker
{
    class WorkerDomain
    {
        private int gossipDelay;

        private string puppetMasterURL;

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

        public WorkerDomain(int gossip_delay, string puppet_master_URL)
        {
            gossipDelay = gossip_delay;
            puppetMasterURL = puppet_master_URL;
        }

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            foreach (string key in request.Workers.Keys)
            {
                workersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));
                Console.WriteLine("Worker: " + key + " URL: " + workersIdToURL.GetValueOrDefault(key));
            }

            foreach (string key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key + " URL: " + storagesIdToURL.GetValueOrDefault(key));
            }

            return new SendNodesURLReply();
        }

        public StartAppReply StartApp(StartAppRequest request)
        {
            Console.WriteLine("Received a DIDARequest:");
            Console.WriteLine(request.DidaRequest.ToString());

            DIDAAssignment didaAssignment = request.DidaRequest.Chain[request.DidaRequest.Next];

            DIDAOperatorID didaOperatorID = didaAssignment.Operator;

            IDIDAOperator myOperator = LoadByReflection(didaOperatorID.Classname);

            if (myOperator == null)
            {
                Console.WriteLine("Failed to load operator by reflection: " + didaOperatorID.Classname);
                return new StartAppReply();
            }

            DIDAWorker.DIDAMetaRecord didaMetaRecord = new DIDAWorker.DIDAMetaRecord();
            didaMetaRecord.Id = request.DidaRequest.DidaMetaRecord.Id;
            // other metadata to be specified by the students

            List<DIDAStorageNode> storagesURL = new List<DIDAStorageNode>();
            foreach (KeyValuePair<string, string> pair in storagesIdToURL)
            {
                storagesURL.Add(new DIDAStorageNode
                {
                    serverId = pair.Key,
                    host = pair.Value.Split(':')[0] + ":" + pair.Value.Split(':')[1],
                    port = Int32.Parse(pair.Value.Split(':')[2]),
                });
            }

            Console.WriteLine("created List<DIDAStorageNode>");

            myOperator.ConfigureStorage(new StorageProxy(storagesURL.ToArray(), didaMetaRecord));

            Console.WriteLine("ConfigureStorage");

            string previousOutput = "";
            if (request.DidaRequest.Next - 1 >= 0)
                previousOutput = request.DidaRequest.Chain[request.DidaRequest.Next - 1].Output;

            Console.WriteLine("previous output: " + previousOutput);

            string output = myOperator.ProcessRecord(didaMetaRecord, request.DidaRequest.Input, previousOutput);

            Console.WriteLine("ProcessRecord");

            StartAppRequest nextWorkerRequest = new StartAppRequest()
            {
                DidaRequest = new DIDARequest()
                {
                    DidaMetaRecord = new DIDAMetaRecord()
                    {
                        Id = request.DidaRequest.DidaMetaRecord.Id,
                        // other metadata to be specified by the students
                    },
                    Input = request.DidaRequest.Input,
                    Next = request.DidaRequest.Next++,
                    ChainSize = request.DidaRequest.ChainSize,
                },
            };

            nextWorkerRequest.DidaRequest.Chain.Add(request.DidaRequest.Chain);
            nextWorkerRequest.DidaRequest.Chain[request.DidaRequest.Next - 1].Output = output;

            //////request.DidaRequest.Chain[request.DidaRequest.Next].Output = output;

            Console.WriteLine("Created chain");

            //////request.DidaRequest.Next = request.DidaRequest.Next++;
            
            if (request.DidaRequest.Next < request.DidaRequest.ChainSize)
            {
                Console.WriteLine("GrpcChannel nextWorkerChannel");
                //TALVEZ SEJA PRECISO USAR O ARG. ORDER PARA OBTER OS DIDAASSIGNMENT CERTOS
                GrpcChannel nextWorkerChannel = GrpcChannel.ForAddress(
                    request.DidaRequest.Chain[request.DidaRequest.Next].Host + ":" + request.DidaRequest.Chain[request.DidaRequest.Next].Port);
                WorkerService.WorkerServiceClient nextWorkerClient = new WorkerService.WorkerServiceClient(nextWorkerChannel);

                Console.WriteLine("Going to send to the worker: " + request.DidaRequest.Chain[request.DidaRequest.Next].Host + ":" + request.DidaRequest.Chain[request.DidaRequest.Next].Port);

                //TODO: O WORKER NAO PODE BLOQUEAR AQUI. TALVEZ USAR TASK!
                Console.WriteLine("I'm going to send the following request to the next worker:");
                Console.WriteLine(request);
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

        public ListReply ListObjects()
        {
            //TODO actually list the objects stored here
            Console.WriteLine("Listing");
            return new ListReply();
        }

        public CrashReply Crash()
        {
            Task.Delay(1000).ContinueWith(t => System.Environment.Exit(1));
            return new CrashReply();
        }
    }
}
