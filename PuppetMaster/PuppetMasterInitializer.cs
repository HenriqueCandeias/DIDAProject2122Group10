using Grpc.Core;
using Grpc.Net.Client;
using Pcs;
using Scheduler;
using Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Worker;

namespace PuppetMaster
{
    public class PuppetMasterInitializer
    {
        //Communication

        private string puppetMasterHost = "http://localhost";
        
        private const int puppetMasterPort = 10001;

        private const int pcsServerPort = 10000;

        //Id to URL

        private Dictionary<string, string> schedulerIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

        //Id to gRPC client

        private SchedulerService.SchedulerServiceClient schedulerClient;

        private Dictionary<string, WorkerService.WorkerServiceClient> workersIdToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        private Dictionary<string, StorageService.StorageServiceClient> storagesIdToClient = new Dictionary<string, StorageService.StorageServiceClient>();

        //Other

        private bool debugActive = false;

        private bool nodesAreInformed = false;

        private int highestReplicaId = -1;

        public enum Action
        {
            List,
            Crash,
        }

        public PuppetMasterInitializer()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        public void StartPCS()
        {
            ProcessStartInfo p_info = new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\PCS\\bin\\Debug\\netcoreapp3.1\\PCS.exe",
            };

            Process.Start(p_info);
        }

        private void InformNodesAboutURL()
        {
            //Inform Scheduler about URLs

            Scheduler.SendNodesURLRequest schedulerRequest = new Scheduler.SendNodesURLRequest();

            schedulerRequest.Workers.Add(workersIdToURL);
            schedulerRequest.Storages.Add(storagesIdToURL);

            schedulerClient.SendNodesURL(schedulerRequest);

            //Inform Workers about URLs
    
            Worker.SendNodesURLRequest workersRequest = new Worker.SendNodesURLRequest();

            workersRequest.Workers.Add(workersIdToURL);
            workersRequest.Storages.Add(storagesIdToURL);

            foreach (WorkerService.WorkerServiceClient worker in workersIdToClient.Values)
                worker.SendNodesURL(workersRequest);

            //Inform Storages about URLs

            Storage.SendNodesURLRequest storagesRequest = new Storage.SendNodesURLRequest();

            storagesRequest.Workers.Add(workersIdToURL);
            storagesRequest.Storages.Add(storagesIdToURL);

            foreach (StorageService.StorageServiceClient storage in storagesIdToClient.Values)
                storage.SendNodesURL(storagesRequest);
        }

        public void Execute(string command)
        {
            string[] words = command.Split(' ');
            switch (words[0])
            {
                case "debug":
                    StartDebugging();
                    return;

                case "scheduler":
                    StartScheduler(words[1], words[2]);
                    return;

                case "worker":
                    StartWorker(words[1], words[2], words[3]);
                    return;

                case "storage":
                    StartStorage(words[1], words[2], words[3]);
                    return;
            }

            if (!nodesAreInformed)
            {
                InformNodesAboutURL();
                nodesAreInformed = true;
            }

            switch (words[0])
            {
                case "client":
                    StartApp(words[1], words[2]);
                    break;

                case "populate":
                    break;

                case "status":
                    Status();
                    break;

                case "listServer":
                    ServerFinder(words[1], Action.List);
                    break;

                case "listGlobal":
                    ServerGlobal(Action.List);
                    break;

                case "crash":
                    ServerFinder(words[1], Action.Crash);
                    break;

                case "crashAll":
                    ServerGlobal(Action.Crash);
                    break;

                case "wait":
                    Thread.Sleep(Int32.Parse(words[1]));
                    break;
            }
        }

        private void StartDebugging()
        {
            Server server = new Server
            {
                Services = { PuppetMasterService.BindService(new PuppetMasterServer()) },
                Ports = { new ServerPort(puppetMasterHost, puppetMasterPort, ServerCredentials.Insecure) }
            };
            server.Start();

            debugActive = true;
        }

        public void StartScheduler(string server_id, string URL)
        {
            schedulerIdToURL.Clear();
            schedulerIdToURL.Add(server_id, URL);

            StartSchedulerRequest request = new StartSchedulerRequest()
            {
                ServerId = server_id,
                Url = URL,
            };
            
            GrpcChannel pcsChannel = GrpcChannel.ForAddress(URL.Split(':')[0] + ":" + URL.Split(':')[1] + ":" + pcsServerPort.ToString());
            PCSService.PCSServiceClient pcsClient = new PCSService.PCSServiceClient(pcsChannel);

            pcsClient.StartScheduler(request);

            GrpcChannel schedulerChannel = GrpcChannel.ForAddress(URL);
            schedulerClient = new SchedulerService.SchedulerServiceClient(schedulerChannel);
        }

        public void StartWorker(string server_id, string URL, string gossip_delay)
        {
            workersIdToURL.Add(server_id, URL);

            StartWorkerRequest request = new StartWorkerRequest()
            {
                ServerId = server_id,
                Url = URL,
                GossipDelay = Int32.Parse(gossip_delay),
                DebugActive = debugActive,
                PuppetMasterURL = puppetMasterHost + ":" + puppetMasterPort,
            };

            GrpcChannel pcsChannel = GrpcChannel.ForAddress(URL.Split(':')[0] + ":" + URL.Split(':')[1] + ":" + pcsServerPort.ToString());
            PCSService.PCSServiceClient pcsClient = new PCSService.PCSServiceClient(pcsChannel);

            pcsClient.StartWorker(request);

            GrpcChannel workerChannel = GrpcChannel.ForAddress(URL);
            workersIdToClient.Add(server_id, new WorkerService.WorkerServiceClient(workerChannel));
        }

        public void StartStorage(string server_id, string URL, string gossip_delay)
        {
            storagesIdToURL.Add(server_id, URL);

            highestReplicaId++;

            StartStorageRequest request = new StartStorageRequest()
            {
                ServerId = server_id,
                Url = URL,
                GossipDelay = Int32.Parse(gossip_delay),
                ReplicaId = highestReplicaId,
            };

            GrpcChannel pcsChannel = GrpcChannel.ForAddress(URL.Split(':')[0] + ":" + URL.Split(':')[1] + ":" + pcsServerPort.ToString());
            PCSService.PCSServiceClient pcsClient = new PCSService.PCSServiceClient(pcsChannel);

            pcsClient.StartStorage(request);

            GrpcChannel storageChannel = GrpcChannel.ForAddress(URL);
            storagesIdToClient.Add(server_id, new StorageService.StorageServiceClient(storageChannel));
        }

        private void StartApp(string input, string app_file)
        {
            StartAppRequest request = new StartAppRequest()
            {
                Input = input
            };

            Dictionary<int, string> operators = new Dictionary<int, string>();

            string appFileContent = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\" + app_file;

            try
            {
                foreach (string line in File.ReadLines(appFileContent))
                    operators.Add(Int32.Parse(line.Split(' ')[2]), line.Split(' ')[1]);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found: " + app_file);
            }

            request.Operators.Add(operators);

            schedulerClient.StartApp(request);
        }

        public void Status()
        {
            Scheduler.StatusRequest schedulerStatusRequest = new Scheduler.StatusRequest();

            schedulerClient.Status(schedulerStatusRequest);

            Worker.StatusRequest workerStatusRequest = new Worker.StatusRequest();

            foreach (WorkerService.WorkerServiceClient worker in workersIdToClient.Values)
                worker.Status(workerStatusRequest);

            Storage.StatusRequest storageStatusRequest = new Storage.StatusRequest();

            foreach (StorageService.StorageServiceClient storage in storagesIdToClient.Values)
                storage.Status(storageStatusRequest);

            //TODO print PuppterMaster status if necessary

        }

        /// Executes given action to all servers
        public void ServerGlobal(Action action)
        {
            foreach (String workerId in workersIdToClient.Keys)
            {
                if (action == Action.List)
                {
                    ListWorker(workerId);
                }
                else
                {
                    CrashWorker(workerId);
                }
            }

            foreach(String storageId in storagesIdToClient.Keys)
            {
                if (action == Action.List)
                {
                    ListStorage(storageId);
                }
                else
                {
                    CrashStorage(storageId);
                }
            }
        }

        /// Finds a server with a specific ID and executed the given action
        public void ServerFinder(string serverId, Action action)
        {
            if (workersIdToClient.ContainsKey(serverId))
            {
                if(action == Action.List)
                {
                    ListWorker(serverId);
                } 
                else
                {
                    CrashWorker(serverId);
                }

            } else if (storagesIdToClient.ContainsKey(serverId))
            {
                if (action == Action.List)
                {
                    ListStorage(serverId);
                }
                else
                {
                    CrashStorage(serverId);
                }
            }
                
        }

        //LIST COMMAND
        public void ListWorker(string serverId)
        {
            Worker.ListRequest listRequest = new Worker.ListRequest();
            workersIdToClient[serverId].List(listRequest);
        }

        public void ListStorage(string serverId)
        {
            Storage.ListRequest listRequest = new Storage.ListRequest();
            storagesIdToClient[serverId].List(listRequest);
        }

        //CRASH COMMAND
        public void CrashWorker(string serverId)
        {
            Worker.CrashRequest crashRequest = new Worker.CrashRequest();
            workersIdToClient[serverId].Crash(crashRequest);
        }

        public void CrashStorage(string serverId)
        {
            Storage.CrashRequest crashRequest = new Storage.CrashRequest();
            storagesIdToClient[serverId].Crash(crashRequest);
        }
    }
}