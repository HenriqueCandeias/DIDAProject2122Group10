using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PuppetMaster
{
    public class PuppetMasterInitializer
    {

        private bool debug = false;

        private SchedulerService.SchedulerServiceClient SchedulerClient;

        private Dictionary<int, string> SchedulerIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> WorkersIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> StoragesIdToURL = new Dictionary<int, string>();

        private int storageID;

        public void execute(string command)
        {
            string[] words = command.Split(' ');
            switch (words[0])
            {
                case "scheduler":
                    startScheduler(words[1], words[2]);
                    break;

                case "worker":
                    startWorker(words[1], words[2]);
                    break;

                case "storage":
                    startStorage(words[1], words[2]);
                    break;

                case "client":
                    startApp(words[1], words[2]);
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

        public void startScheduler(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Scheduler\\bin\\Debug\\netcoreapp3.1\\Scheduler.exe";

            p_info.Arguments = server_id + " " + URL.Split(':')[0] + " " + URL.Split(':')[1];

            Process.Start(p_info);

            GrpcChannel channel = GrpcChannel.ForAddress("http://" + URL);
            SchedulerClient = new SchedulerService.SchedulerServiceClient(channel);


            SchedulerIdToURL.Clear();
            SchedulerIdToURL.Add(Int32.Parse(server_id), URL);
        }

        public void startWorker(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Worker\\bin\\Debug\\netcoreapp3.1\\Worker.exe";

            p_info.Arguments = server_id + " " + URL.Split(':')[0] + " " + URL.Split(':')[1] + " " + StoragesIdToURL[storageID].Split(':')[1];

            Process.Start(p_info);

            WorkersIdToURL.Add(Int32.Parse(server_id), URL);
        }

        public void startStorage(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;  
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Storage\\bin\\Debug\\netcoreapp3.1\\Storage.exe";

            p_info.Arguments = server_id + " " + URL.Split(':')[0] + " " + URL.Split(':')[1];

            Process.Start(p_info);


            storageID = Int32.Parse(server_id);
            StoragesIdToURL.Add(Int32.Parse(server_id), URL);
        }

        private void startApp(string input, string app_file)
        {
            StartAppRequest request = new StartAppRequest();

            string appFileContent = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\" + app_file;

            foreach (string line in System.IO.File.ReadLines(appFileContent))
            {
                request.Operators.Add(line.Split(' ')[1]);
            }

            SchedulerClient.StartApp(request);
        }

        public void start()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            string systemConfigurationFile = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\systemConfiguration.txt";
            
            foreach(string line in System.IO.File.ReadLines(systemConfigurationFile))
            {
                execute(line);
            }

            //TODO send message to scheduler to inform about all the workers and storages URLs
            //TODO send messages to all workers to inform about all the storages URLs
            //TODO send messages to all storages to inform about all the storages URLs
            //All those nodes can only start working after receiving that message
        }
    }
}