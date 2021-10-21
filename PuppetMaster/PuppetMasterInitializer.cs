using System;
using System.Diagnostics;
using System.IO;

namespace PuppetMaster
{
    class PuppetMasterInitializer
    {
        public void startScheduler(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Scheduler\\bin\\Debug\\netcoreapp3.1\\Scheduler.exe";

            Process.Start(p_info);
        }

        public void startWorker(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Worker\\bin\\Debug\\netcoreapp3.1\\Worker.exe";

            Process.Start(p_info);
        }

        public void startStorage(string server_id, string URL)
        {
            ProcessStartInfo p_info = new ProcessStartInfo();
            p_info.UseShellExecute = true;
            p_info.CreateNoWindow = false;
            p_info.WindowStyle = ProcessWindowStyle.Normal;
            p_info.FileName = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Storage\\bin\\Debug\\netcoreapp3.1\\Storage.exe";

            Process.Start(p_info);
        }

        public void start()
        {
            string systemConfigurationFile = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName + "\\systemConfiguration.txt";

            string[] words;
            string server_id;
            string URL;
            foreach(string line in System.IO.File.ReadLines(systemConfigurationFile))
            {
                words = line.Split(' ');

                server_id = words[1];
                URL = words[2];

                switch (words[0])
                {
                    case "scheduler":
                        startScheduler(server_id, URL);
                        break;
                    case "worker":
                        startWorker(server_id, URL);
                        break;
                    case "storage":
                        startStorage(server_id, URL);
                        break;
                }
            }
        }
    }
}
