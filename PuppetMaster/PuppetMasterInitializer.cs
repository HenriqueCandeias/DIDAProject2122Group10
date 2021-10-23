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

        private Dictionary<int, string> schedulerIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> workersIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> storagesIdToURL = new Dictionary<int, string>();

        private SchedulerService.SchedulerServiceClient schedulerClient;

        private Dictionary<int, WorkerService.WorkerServiceClient> workersIdToClient = new Dictionary<int, WorkerService.WorkerServiceClient>();

        private Dictionary<int, StorageService.StorageServiceClient> storagesIdToClient = new Dictionary<int, StorageService.StorageServiceClient>();

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
                    debug = !debug;
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

            schedulerIdToURL.Clear();
            schedulerIdToURL.Add(Int32.Parse(server_id), URL);

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + URL);
            schedulerClient = new SchedulerService.SchedulerServiceClient(channel);
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

            workersIdToURL.Add(Int32.Parse(server_id), URL);

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + URL);
            workersIdToClient.Add(Int32.Parse(server_id), new WorkerService.WorkerServiceClient(channel));
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

            storagesIdToURL.Add(Int32.Parse(server_id), URL);

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + URL);
            storagesIdToClient.Add(Int32.Parse(server_id), new StorageService.StorageServiceClient(channel));
        }

        private void StartApp(string input, string app_file)
        {
            StartAppRequest request = new StartAppRequest()
            {
                Input = input
            };

            Dictionary<int, string> operators = new Dictionary<int, string>();

            string appFileContent = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\" + app_file;

            foreach (string line in System.IO.File.ReadLines(appFileContent))
                operators.Add(Int32.Parse(line.Split(' ')[2]), line.Split(' ')[1]);

            for (int i = 0; i < operators.Count; i++)
                request.Operators.Add(operators.GetValueOrDefault(i));

            schedulerClient.StartApp(request);
        }

        public void Start()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            ReadStartupCommands();
            InformNodesAboutURL();
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

        private void ReadStartupCommands()
        {
            string systemConfigurationFile = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\systemConfiguration.txt";

            foreach (string line in System.IO.File.ReadLines(systemConfigurationFile))
            {
                Execute(line);
            }
        }
    }
}