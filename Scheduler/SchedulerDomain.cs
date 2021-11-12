using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Worker;
using Storage;

namespace Scheduler
{
    class SchedulerDomain
    {
        private int executionId = 0;

        private const float replicationFactor = 0.5f;

        private Queue<string> workersId = new Queue<string>();

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<int, string> storagesIdToURL = new Dictionary<int, string>();

        private Dictionary<string, WorkerService.WorkerServiceClient> workersIdToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        private Dictionary<string, WorkerService.WorkerServiceClient> workersURLToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            //Configure workersIdToURL

            foreach (string key in request.Workers.Keys)
            {
                workersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));
                Console.WriteLine("Worker: " + key + " URL: " + workersIdToURL.GetValueOrDefault(key));
                workersId.Enqueue(key);
            }

            //Configure storagesIdToURL

            int highestReplicaId = -1;

            foreach (string key in request.Storages.Keys)
            {
                storagesIdToURL.Add(++highestReplicaId, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key + " Id: " + highestReplicaId + " URL: " + storagesIdToURL.GetValueOrDefault(highestReplicaId));
            }

            //Create and store gRPC clients for the workers

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            GrpcChannel channel;
            WorkerService.WorkerServiceClient workerClient;

            foreach (KeyValuePair<string, string> pair in workersIdToURL)
            {
                channel = GrpcChannel.ForAddress(pair.Value);
                workerClient = new WorkerService.WorkerServiceClient(channel);

                workersIdToClient.Add(pair.Key, workerClient);
                workersURLToClient.Add(pair.Value, workerClient);
            }

            //Inform the worker nodes about all nodes and the replicationFactor

            Worker.SendNodesURLRequest sendNodesToWorkersRequest = new Worker.SendNodesURLRequest();
            sendNodesToWorkersRequest.ReplicationFactor = replicationFactor;

            foreach(KeyValuePair<int, string> pair in storagesIdToURL)
            {
                sendNodesToWorkersRequest.Storages.Add(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, string> pair in workersIdToURL)
            {
                sendNodesToWorkersRequest.Workers.Add(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, WorkerService.WorkerServiceClient> pair in workersIdToClient)
            {
                pair.Value.SendNodesURL(sendNodesToWorkersRequest);
            }

            //Inform the storage nodes about all nodes and the replicationFactor

            Storage.SendNodesURLRequest sendNodesToStoragesRequest = new Storage.SendNodesURLRequest();
            sendNodesToStoragesRequest.ReplicationFactor = replicationFactor;

            foreach (KeyValuePair<int, string> pair in storagesIdToURL)
            {
                sendNodesToStoragesRequest.Storages.Add(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, string> pair in workersIdToURL)
            {
                sendNodesToStoragesRequest.Workers.Add(pair.Key, pair.Value);
            }

            StorageService.StorageServiceClient storageClient;

            //Send the prepared request to each storage node
            foreach (KeyValuePair<int, string> pair in storagesIdToURL)
            {
                Console.WriteLine("ReplicaId: " + pair.Key + " ReplicaURL: " + pair.Value);
                channel = GrpcChannel.ForAddress(pair.Value);
                storageClient = new StorageService.StorageServiceClient(channel);

                sendNodesToStoragesRequest.ReplicaId = pair.Key;
                storageClient.SendNodesURL(sendNodesToStoragesRequest);
            }

            return new SendNodesURLReply();
        }

        public StartAppReply StartApp(StartAppRequest request)
        {
            List<DIDAAssignment> chain = GenerateChain(request.Operators);
            
            Worker.StartAppRequest startAppRequest = new Worker.StartAppRequest()
            {
                DidaRequest = new DIDARequest()
                {
                    DidaMetaRecord = new DIDAMetaRecord()
                    {
                        Id = executionId,
                    },
                    Input = request.Input,
                    Next = 0,
                    ChainSize = chain.Count,
                }
            };

            startAppRequest.DidaRequest.Chain.Add(chain);

            string firstWorkerURL = startAppRequest.DidaRequest.Chain[0].Host + ":" + startAppRequest.DidaRequest.Chain[0].Port.ToString();

            Worker.StartAppReply reply = workersURLToClient.GetValueOrDefault(firstWorkerURL).StartApp(startAppRequest); //workersURLToClient.GetValueOrDefault(firstWorkerURL).StartAppAsync(startAppRequest);

            Console.WriteLine("Sent DIDARequest to worker.");

            executionId++;

            return new StartAppReply
            {
                DebugMessage = reply.DebugMessage,
            };
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
    }
}
