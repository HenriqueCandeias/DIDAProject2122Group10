using Grpc.Net.Client;
using Scheduler;
using Worker;
using System;
using Storage;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PuppetMaster
{
    public class PuppetMasterInitializer
    {

        private bool debug = false;

        private Dictionary<int, string> SchedulerIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> WorkersIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> StoragesIdToURL = new Dictionary<int, string>();

        private SchedulerService.SchedulerServiceClient SchedulerClient;

        private Dictionary<int, WorkerService.WorkerServiceClient> WorkersIdToClient = new Dictionary<int, WorkerService.WorkerServiceClient>();

        private Dictionary<int, StorageService.StorageServiceClient> StoragesIdToClient = new Dictionary<int, StorageService.StorageServiceClient>();

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
                    this.debug = !debug;
                    break;

                case "crash":
                    break;

                case "wait":
                    System.Threading.Thread.Sleep(Int32.Parse(words[1]));
                    break;
            }
        }

        public void StartScheduler(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Scheduler\\bin\\Debug\\netcoreapp3.1\\Scheduler.exe",

                Arguments = server_id + " " + URL.Split(':')[0] + " " + URL.Split(':')[1]
            };

            Process.Start(p_info);

            SchedulerIdToURL.Clear();
            SchedulerIdToURL.Add(Int32.Parse(server_id), URL);

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + URL);
            SchedulerClient = new SchedulerService.SchedulerServiceClient(channel);
        }

        public void StartWorker(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Worker\\bin\\Debug\\netcoreapp3.1\\Worker.exe",

                Arguments = server_id + " " + URL.Split(':')[0] + " " + URL.Split(':')[1]
            };

            Process.Start(p_info);

            WorkersIdToURL.Add(Int32.Parse(server_id), URL);

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + URL);
            WorkersIdToClient.Add(Int32.Parse(server_id), new WorkerService.WorkerServiceClient(channel));
        }

        public void StartStorage(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Storage\\bin\\Debug\\netcoreapp3.1\\Storage.exe",

                Arguments = server_id + " " + URL.Split(':')[0] + " " + URL.Split(':')[1]
            };

            Process.Start(p_info);

            StoragesIdToURL.Add(Int32.Parse(server_id), URL);

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + URL);
            StoragesIdToClient.Add(Int32.Parse(server_id), new StorageService.StorageServiceClient(channel));
        }

        private void StartApp(string input, string app_file)
        {
            StartAppRequest request = new StartAppRequest()
            {
                Input = input
            };

            string appFileContent = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\" + app_file;

            foreach (string line in System.IO.File.ReadLines(appFileContent))
            {
                //TODO Colocar num Dictionary<int, string> primeiro e depois contruir a lista ordenada de strings
                request.Operators.Add(line.Split(' ')[1]);
            }

            SchedulerClient.StartApp(request);
        }

        public void Start()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            //Read the startup commands

            string systemConfigurationFile = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\systemConfiguration.txt";
            
            foreach(string line in System.IO.File.ReadLines(systemConfigurationFile))
            {
                Execute(line);
            }

            //Inform Scheduler about URLs

            Scheduler.SendNodesURLRequest schedulerRequest = new Scheduler.SendNodesURLRequest();
            
            schedulerRequest.Workers.Add(WorkersIdToURL);
            schedulerRequest.Storages.Add(StoragesIdToURL);

            SchedulerClient.SendNodesURL(schedulerRequest);

            //Inform Workers about URLs

            Worker.SendNodesURLRequest workersRequest = new Worker.SendNodesURLRequest();

            workersRequest.Workers.Add(WorkersIdToURL);
            workersRequest.Storages.Add(StoragesIdToURL);

            foreach(WorkerService.WorkerServiceClient worker in WorkersIdToClient.Values)
                worker.SendNodesURL(workersRequest);

            //Inform Storages about URLs
        
            Storage.SendNodesURLRequest storagesRequest = new Storage.SendNodesURLRequest();

            storagesRequest.Workers.Add(WorkersIdToURL);
            storagesRequest.Storages.Add(StoragesIdToURL);

            foreach (StorageService.StorageServiceClient storage in StoragesIdToClient.Values)
                storage.SendNodesURL(storagesRequest);
        

            //All those nodes can only start working after receiving that message
        }
    }
}