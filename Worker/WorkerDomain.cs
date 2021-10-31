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

namespace Worker
{
    class WorkerDomain
    {
        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

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

            myOperator.ConfigureStorage(new StorageProxy(storagesURL.ToArray(), didaMetaRecord));

            string output = myOperator.ProcessRecord(didaMetaRecord, request.DidaRequest.Input, request.DidaRequest.Chain[request.DidaRequest.Next - 1].Output);
            request.DidaRequest.Chain[request.DidaRequest.Next].Output = output;

            request.DidaRequest.Next = request.DidaRequest.Next++;
            
            if (request.DidaRequest.Next < request.DidaRequest.ChainSize)
            {
                //TALVEZ SEJA PRECISO USAR O ARG. ORDER PARA OBTER OS DIDAASSIGNMENT CERTOS
                GrpcChannel nextWorkerChannel = GrpcChannel.ForAddress(
                    request.DidaRequest.Chain[request.DidaRequest.Next].Host + ":" + request.DidaRequest.Chain[request.DidaRequest.Next].Port);
                WorkerService.WorkerServiceClient nextWorkerClient = new WorkerService.WorkerServiceClient(nextWorkerChannel);

                //TODO: O WORKER NAO PODE BLOQUEAR AQUI. TALVEZ USAR TASK!
                Console.WriteLine("I'm going to send the following request to the next worker:");
                Console.WriteLine(request);
                nextWorkerClient.StartApp(request);
            }

            return new StartAppReply();
        }

        static IDIDAOperator LoadByReflection(string className)
        {
            IDIDAOperator _objLoadedByReflection;

            foreach (string filename in Directory.EnumerateFiles(Directory.GetCurrentDirectory()))
            {
                Console.WriteLine("file in cwd: " + Path.GetFileName(filename));
                if (filename.EndsWith(".dll"))
                {
                    Console.WriteLine("File is a dll...Let's look at it's contained types...");
                    Assembly _dll = Assembly.LoadFrom(filename);
                    Type[] _typeList = _dll.GetTypes();
                    foreach (Type type in _typeList)
                    {
                        Console.WriteLine("type contained in dll: " + type.Name);
                        if (type.Name == className)
                        {
                            Console.WriteLine("Found type to load dynamically: " + className);
                            _objLoadedByReflection = (IDIDAOperator)Activator.CreateInstance(type);
                            foreach (MethodInfo method in type.GetMethods())
                            {
                                Console.WriteLine("method from class " + className + ": " + method.Name);
                            }
                            return _objLoadedByReflection;
                        }
                    }
                }
            }
            return null;
        }

        public StatusReply Status()
        {
            //TODO display necessary info
            return new StatusReply();
        }
    }
}
