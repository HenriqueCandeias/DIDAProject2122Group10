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

        private Dictionary<int, string> workersIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> storagesIdToURL = new Dictionary<int, string>();

        private Dictionary<int, WorkerService.WorkerServiceClient> workersIdToClient = new Dictionary<int, WorkerService.WorkerServiceClient>();

        private Dictionary<string, WorkerService.WorkerServiceClient> workersURLToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            foreach (int key in request.Workers.Keys)
            {
                workersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));
                Console.WriteLine("Worker: " + key.ToString() + " URL: " + workersIdToURL.GetValueOrDefault(key));
            }

            foreach (int key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key.ToString() + " URL: " + storagesIdToURL.GetValueOrDefault(key));
            }

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            GrpcChannel channel;
            WorkerService.WorkerServiceClient client;
            foreach (KeyValuePair<int, string> pair in workersIdToURL)
            {
                channel = GrpcChannel.ForAddress("http://" + pair.Value);
                client = new WorkerService.WorkerServiceClient(channel);

                workersIdToClient.Add(pair.Key, client);
                workersURLToClient.Add(pair.Value, client);
            }

            return new SendNodesURLReply();
        }

        public StartAppReply StartApp(StartAppRequest request)
        {
            Console.WriteLine("Operators:");
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

            workersURLToClient.GetValueOrDefault(firstWorkerURL).StartApp(startAppRequest);

            Console.WriteLine("Sent DIDARequest to worker.");

            executionId++;

            return new StartAppReply();
        }

        private List<DIDAAssignment> GenerateChain(Google.Protobuf.Collections.MapField<int, string> operators)
        {
            //TODO Implement logic (Consistent Hashing)
            //This implementation assumes only one worker and one operator in the app

            List<DIDAAssignment> chain = new List<DIDAAssignment>();

            Console.WriteLine(operators);

            DIDAAssignment uniqueAssignment = new DIDAAssignment()
            {
                Operator = new DIDAOperatorID()
                {
                    Classname = operators[0],
                    Order = 0,
                },
                Host = workersIdToURL.GetValueOrDefault(2).Split(':')[0],
                Port = Int32.Parse(workersIdToURL.GetValueOrDefault(2).Split(':')[1]),
                Output = "",
            };

            chain.Add(uniqueAssignment);

            return chain;
        }
    }
}
