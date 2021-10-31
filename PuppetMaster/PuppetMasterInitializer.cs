using Grpc.Net.Client;
using Pcs;
using Scheduler;
using Worker;
using System;
using Storage;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PuppetMaster
{
    public class PuppetMasterInitializer
    {
        private const int pcsServerPort = 10000;

        private bool debug = false;

        private Dictionary<string, string> schedulerIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

        private SchedulerService.SchedulerServiceClient schedulerClient;

        private Dictionary<string, WorkerService.WorkerServiceClient> workersIdToClient = new Dictionary<string, WorkerService.WorkerServiceClient>();

        private Dictionary<string, StorageService.StorageServiceClient> storagesIdToClient = new Dictionary<string, StorageService.StorageServiceClient>();

        private bool nodesAreInformed = false;

        public PuppetMasterInitializer()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            //This function is only used to run the system in a single machine
            
            
            //InformNodesAboutURL();
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

        public void ReadStartupCommands()
        {
            string systemConfigurationFile = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\SystemConfig.txt";

            foreach (string line in System.IO.File.ReadLines(systemConfigurationFile))
            {
                Execute(line);
            }
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
                case "scheduler":
                    StartScheduler(words[1], words[2]);
                    break;

                case "worker":
                    StartWorker(words[1], words[2]);
                    break;

                case "storage":
                    StartStorage(words[1], words[2]);
                    break;
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
                    break;

                case "listServer":
                    break;

                case "listGlobal":
                    break;

                case "debug":
                    debug = !debug;
                    break;

                case "crash":
                    break;

                case "wait":
                    Thread.Sleep(Int32.Parse(words[1]));
                    break;
            }
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

        public void StartWorker(string server_id, string URL)
        {
            workersIdToURL.Add(server_id, URL);

            StartWorkerRequest request = new StartWorkerRequest()
            {
                ServerId = server_id,
                Url = URL,
            };

            GrpcChannel pcsChannel = GrpcChannel.ForAddress(URL.Split(':')[0] + ":" + URL.Split(':')[1] + ":" + pcsServerPort.ToString());
            PCSService.PCSServiceClient pcsClient = new PCSService.PCSServiceClient(pcsChannel);

            pcsClient.StartWorker(request);

            GrpcChannel workerChannel = GrpcChannel.ForAddress(URL);
            workersIdToClient.Add(server_id, new WorkerService.WorkerServiceClient(workerChannel));
        }

        public void StartStorage(string server_id, string URL)
        {
            storagesIdToURL.Add(server_id, URL);

            StartStorageRequest request = new StartStorageRequest()
            {
                ServerId = server_id,
                Url = URL,
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

            foreach (string line in File.ReadLines(appFileContent))
                operators.Add(Int32.Parse(line.Split(' ')[2]), line.Split(' ')[1]);

            request.Operators.Add(operators);

            schedulerClient.StartApp(request);
        }
    }
}