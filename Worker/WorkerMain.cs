using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using Storage;
using DIDAWorker;
using DIDAStorage;

namespace Worker
{

    class WorkerMain
    {
        static void Main(string[] args)
        {
            try {
                int port = Int32.Parse(args[1].Split(':')[2]);

                int worker_delay = Int32.Parse(args[2]);

                string puppet_master_URL = "";
                if (args.Length == 4)
                    puppet_master_URL = args[3];

                Console.WriteLine("Starting Server on Port: " + port);

                Server server = new Server
                {
                    Services = { WorkerService.BindService(new WorkerServer(worker_delay, puppet_master_URL)) },
                    Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) },
                };

                server.Start();

                Console.WriteLine("Started Server on Port: " + port);

                Console.WriteLine("Press any key to stop the server Worker...");
                Console.ReadKey();

                server.ShutdownAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in PCS:\r\n");
                Console.WriteLine(e.ToString());
            }
        }
    }
}
