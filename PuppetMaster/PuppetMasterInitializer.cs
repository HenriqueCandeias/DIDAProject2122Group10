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

        private ConcurrentDictionary<string, string> schedulerIdToURL = new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<string, string> workersIdToURL = new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<string, string> storagesIdToURL = new ConcurrentDictionary<string, string>();

        //Crashed nodes: Id to URL

        private ConcurrentDictionary<string, string> crashedStoragesIdToURL = new ConcurrentDictionary<string, string>();

        //Id to gRPC client

        private SchedulerService.SchedulerServiceClient schedulerClient;

        private ConcurrentDictionary<string, WorkerService.WorkerServiceClient> workersIdToClient = new ConcurrentDictionary<string, WorkerService.WorkerServiceClient>();

        private ConcurrentDictionary<string, StorageService.StorageServiceClient> storagesIdToClient = new ConcurrentDictionary<string, StorageService.StorageServiceClient>();

        //Other

        private bool debugActive = false;

        private bool nodesAreInformed = false;

        private PuppetMasterGUI GUI;

        public enum Action
        {
            List,
            Crash,
        }

        public PuppetMasterInitializer(PuppetMasterGUI GUI)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            this.GUI = GUI;
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
            SendNodesURLRequest schedulerRequest = new SendNodesURLRequest();

            schedulerRequest.Workers.Add(workersIdToURL);
            schedulerRequest.Storages.Add(storagesIdToURL);

            schedulerClient.SendNodesURL(schedulerRequest);
        }

        public void Execute(string command)
        {
            string[] words = command.Split(' ');
            switch (words[0])
            {
                case "debug":
                    debugActive = true;
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
                    Populate(words[1]);
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

                case "wait":
                    Thread.Sleep(Int32.Parse(words[1]));
                    break;
            }
        }

        public void StartScheduler(string server_id, string URL)
        {
            schedulerIdToURL.Clear();
            schedulerIdToURL.TryAdd(server_id, URL);

            StartSchedulerRequest request = new StartSchedulerRequest()
            {
                ServerId = server_id,
                Url = URL,
            };
            
            GrpcChannel pcsChannel = GrpcChannel.ForAddress(URL.Split(':')[0] + ":" + URL.Split(':')[1] + ":" + pcsServerPort.ToString());
            PCSService.PCSServiceClient pcsClient = new PCSService.PCSServiceClient(pcsChannel);

            pcsClient.StartSchedulerAsync(request);

            GrpcChannel schedulerChannel = GrpcChannel.ForAddress(URL);
            schedulerClient = new SchedulerService.SchedulerServiceClient(schedulerChannel);
        }

        public void StartWorker(string server_id, string URL, string worker_delay)
        {
            workersIdToURL.TryAdd(server_id, URL);

            StartWorkerRequest request = new StartWorkerRequest()
            {
                ServerId = server_id,
                Url = URL,
                WorkerDelay = Int32.Parse(worker_delay),
                DebugActive = debugActive,
                PuppetMasterURL = puppetMasterHost + ":" + puppetMasterPort,
            };

            GrpcChannel pcsChannel = GrpcChannel.ForAddress(URL.Split(':')[0] + ":" + URL.Split(':')[1] + ":" + pcsServerPort.ToString());
            PCSService.PCSServiceClient pcsClient = new PCSService.PCSServiceClient(pcsChannel);

            pcsClient.StartWorkerAsync(request);

            GrpcChannel workerChannel = GrpcChannel.ForAddress(URL);
            workersIdToClient.TryAdd(server_id, new WorkerService.WorkerServiceClient(workerChannel));
        }

        public void StartStorage(string server_id, string URL, string gossip_delay)
        {
            storagesIdToURL.TryAdd(server_id, URL);

            StartStorageRequest request = new StartStorageRequest()
            {
                Url = URL,
                GossipDelay = Int32.Parse(gossip_delay),
            };

            GrpcChannel pcsChannel = GrpcChannel.ForAddress(URL.Split(':')[0] + ":" + URL.Split(':')[1] + ":" + pcsServerPort.ToString());
            PCSService.PCSServiceClient pcsClient = new PCSService.PCSServiceClient(pcsChannel);

            pcsClient.StartStorageAsync(request);

            GrpcChannel storageChannel = GrpcChannel.ForAddress(URL);
            storagesIdToClient.TryAdd(server_id, new StorageService.StorageServiceClient(storageChannel));
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

            if (debugActive)
                GUI.PrintDebugMessage(schedulerClient.StartApp(request).DebugMessage);

            else
                schedulerClient.StartAppAsync(request);
        }

        public void Status()
        {
            Worker.StatusRequest workerStatusRequest = new Worker.StatusRequest();

            foreach (WorkerService.WorkerServiceClient worker in workersIdToClient.Values)
                worker.StatusAsync(workerStatusRequest);

            Storage.StatusRequest storageStatusRequest = new Storage.StatusRequest();

            foreach (StorageService.StorageServiceClient storage in storagesIdToClient.Values)
                storage.StatusAsync(storageStatusRequest);

            PrintPuppetMasterStatus();
        }

        private void PrintPuppetMasterStatus()
        {
            Console.WriteLine("PUPPET MASTER STATUS\r\n");

            foreach (KeyValuePair<string, string> pair in schedulerIdToURL)
                Console.WriteLine("Scheduler - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("");

            foreach (KeyValuePair<string, string> pair in workersIdToURL)
                Console.WriteLine("Worker - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("");

            foreach (KeyValuePair<string, string> pair in storagesIdToURL)
                Console.WriteLine("Active Storage - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("");

            foreach (KeyValuePair<string, string> pair in crashedStoragesIdToURL)
                Console.WriteLine("Crashed Storage - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("END OF STATUS\r\n");
        }

        /// Executes given action to all servers
        public void ServerGlobal(Action action)
        {
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
        public void ServerFinder(string server_id, Action action)
        {
            if (storagesIdToClient.ContainsKey(server_id))
            {
                if (action == Action.List)
                {
                    ListStorage(server_id);
                }
                else
                {
                    CrashStorage(server_id);
                }
            }
                
        }

        public void ListStorage(string server_id)
        {
            Storage.ListRequest listRequest = new Storage.ListRequest();
            storagesIdToClient[server_id].ListAsync(listRequest);
        }

        public void CrashStorage(string server_id)
        {
            Storage.CrashRequest crashRequest = new Storage.CrashRequest();
            storagesIdToClient[server_id].CrashAsync(crashRequest);
            crashedStoragesIdToURL.TryAdd(server_id, storagesIdToURL[server_id]);

            string removeString;
            StorageService.StorageServiceClient removeClient;
            storagesIdToURL.TryRemove(server_id, out removeString);
            storagesIdToClient.TryRemove(server_id, out removeClient);
        }

        public void Populate(string data_file)
        {
            string dataPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\" + data_file;
            foreach(string line in File.ReadAllLines(dataPath))
            {
                string[] data = line.Split(',');
                Storage.PopulateRequest populateRequest = new Storage.PopulateRequest()
                {
                    Id = data[0],
                    Val = data[1],
                };
                foreach(StorageService.StorageServiceClient client in storagesIdToClient.Values)
                {
                    client.PopulateAsync(populateRequest);
                }
            }

        }
    }
}