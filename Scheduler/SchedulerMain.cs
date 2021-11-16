using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Scheduler
{

    class SchedulerMain
    {

        static void Main(string[] args)
        {
            try
            {
                int port = Int32.Parse(args[1].Split(':')[2]);

                Console.WriteLine("Starting Scheduler Server on Port: " + port);

                Server server = new Server
                {
                    Services = { SchedulerService.BindService(new SchedulerServer()) },
                    Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) },
                };

                server.Start();

                Console.WriteLine("Started Scheduler Server on Port: " + port);

                Console.WriteLine("Press any key to stop the server Scheduler...");
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
