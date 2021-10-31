using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Worker;

namespace Scheduler
{
    class SchedulerDomain
    {
        private int executionId = 0;

        private Queue<string> workersId = new Queue<string>();

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

        private Dictionary<string, WorkerService.WorkerServiceClient> workersIdToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        private Dictionary<string, WorkerService.WorkerServiceClient> workersURLToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            foreach (string key in request.Workers.Keys)
            {
                workersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));
                Console.WriteLine("Worker: " + key + " URL: " + workersIdToURL.GetValueOrDefault(key));
                workersId.Enqueue(key);
            }

            foreach (string key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key + " URL: " + storagesIdToURL.GetValueOrDefault(key));
            }

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            GrpcChannel channel;
            WorkerService.WorkerServiceClient client;
            foreach (KeyValuePair<string, string> pair in workersIdToURL)
            {
                channel = GrpcChannel.ForAddress(pair.Value);
                client = new WorkerService.WorkerServiceClient(channel);

                workersIdToClient.Add(pair.Key, client);
                workersURLToClient.Add(pair.Value, client);
            }

            return new SendNodesURLReply();
        }

        public StartAppReply StartApp(StartAppRequest request)
        {
            Console.WriteLine("Received the following operators:");
            foreach (KeyValuePair<int, string> pair in request.Operators)
                Console.WriteLine("operator " + pair.Value + " " + pair.Key);

            List<DIDAAssignment> chain = GenerateChain(request.Operators);
            
            Worker.StartAppRequest startAppRequest = new Worker.StartAppRequest()
            {
                DidaRequest = new DIDARequest()
                {
                    DidaMetaRecord = new DIDAMetaRecord()
                    {
                        Id = executionId,
                        //Configure meta information...
                    },
                    Input = request.Input,
                    Next = 0,
                    ChainSize = chain.Count,
                }
            };

            startAppRequest.DidaRequest.Chain.Add(chain);

            string firstWorkerURL = startAppRequest.DidaRequest.Chain[0].Host + ":" + startAppRequest.DidaRequest.Chain[0].Port.ToString();

            workersURLToClient.GetValueOrDefault(firstWorkerURL).StartApp(startAppRequest); //workersURLToClient.GetValueOrDefault(firstWorkerURL).StartAppAsync(startAppRequest);

            Console.WriteLine("Sent DIDARequest to worker.");

            executionId++;

            return new StartAppReply();
        }

        private List<DIDAAssignment> GenerateChain(Google.Protobuf.Collections.MapField<int, string> operators)
        {
            List<DIDAAssignment> chain = new List<DIDAAssignment>();

            Console.WriteLine(operators);

            string nextWorkerId;
            foreach (KeyValuePair<int, string> pair in operators)
            {
                nextWorkerId = workersId.Dequeue();

                DIDAAssignment didaAssignment = new DIDAAssignment()
                {
                    Operator = new DIDAOperatorID()
                    {
                        Classname = pair.Value,
                        Order = pair.Key,
                    },
                    Host = workersIdToURL.GetValueOrDefault(nextWorkerId).Split(':')[0] + ":" + workersIdToURL.GetValueOrDefault(nextWorkerId).Split(':')[1],
                    Port = Int32.Parse(workersIdToURL.GetValueOrDefault(nextWorkerId).Split(':')[2]),
                    Output = "",
                };

                workersId.Enqueue(nextWorkerId);

                chain.Add(didaAssignment);
            }

            return chain;
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
            return new ListReply();
        }

        public CrashReply Crash()
        {
            //TODO stop the server
            return new CrashReply();
        }

    }
}
