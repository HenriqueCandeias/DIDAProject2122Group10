using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Pcs;

namespace PCS
{
    class PCSDomain
    {
        public StartSchedulerReply StartScheduler(StartSchedulerRequest request)
        {
            Console.WriteLine("Creating Scheduler");

            ProcessStartInfo p_info = new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardError = true,
                FileName = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Scheduler\\bin\\Debug\\netcoreapp3.1\\Scheduler.exe",

                Arguments = request.ServerId + " " + request.Url,
            };

            Process.Start(p_info);

            Console.WriteLine("Scheduler created. URL: " + request.Url + " Id: " + request.ServerId + ".");
            
            return new StartSchedulerReply();
        }

        public StartWorkerReply StartWorker(StartWorkerRequest request)
        {
            ProcessStartInfo p_info = new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardError = true,
                FileName = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Worker\\bin\\Debug\\netcoreapp3.1\\Worker.exe",

                Arguments = request.ServerId + " " + request.Url + " " + request.GossipDelay,
            };

            if (request.DebugActive)
                p_info.Arguments += " " + request.PuppetMasterURL;

            Process.Start(p_info);

            Console.WriteLine("Worker created. URL: " + request.Url + " Id: " + request.ServerId + 
                " Gossip Delay: " + request.GossipDelay + " Debug Active: " + request.DebugActive.ToString() + ".");

            return new StartWorkerReply();
        }

        public StartStorageReply StartStorage(StartStorageRequest request)
        {
            ProcessStartInfo p_info = new ProcessStartInfo
            {
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardError = true,
                FileName = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\Storage\\bin\\Debug\\netcoreapp3.1\\Storage.exe",

                Arguments = request.ServerId + " " + request.Url + " " + request.GossipDelay + " " + request.ReplicaId,
            };

            Process.Start(p_info);

            Console.WriteLine(
                "Storage created. URL: " + request.Url + " Id: " + request.ServerId + " Gossip Delay: " + request.GossipDelay + " Replica Id:" + request.ReplicaId + "."
            );

            return new StartStorageReply();
        }
    }
}
