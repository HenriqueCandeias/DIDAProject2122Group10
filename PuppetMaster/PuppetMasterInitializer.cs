using System;
using System.Diagnostics;
using System.IO;

namespace PuppetMaster
{
    public class PuppetMasterInitializer
    {

        private bool debug = false;

        private int Port = 10002;

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

            p_info.Arguments = server_id + " " + URL + " " + Port.ToString();

            Process.Start(p_info);
        }

        public void startWorker(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Worker\\bin\\Debug\\netcoreapp3.1\\Worker.exe";

            p_info.Arguments = server_id + " " + URL + " " + Port.ToString();

            Process.Start(p_info);
        }

        public void startStorage(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;  
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Storage\\bin\\Debug\\netcoreapp3.1\\Storage.exe";

            p_info.Arguments = server_id + " " + URL + " " + Port.ToString();

            Process.Start(p_info);
        }

        public void start()
        {
            string systemConfigurationFile = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\systemConfiguration.txt";
            
            foreach(string line in System.IO.File.ReadLines(systemConfigurationFile))
            {
                execute(line);
            }
        }
    }
}
