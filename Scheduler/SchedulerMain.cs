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
            int port = Int32.Parse(args[2]);

            Console.WriteLine("Starting Scheduler Server on Port: " + port);

            Server server = new Server
            {
                Services = { SchedulerService.BindService(new SchedulerServer())},
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) },
            };

            server.Start();

            Console.WriteLine("Started Scheduler Server on Port: " + port);

            Console.WriteLine("Press any key to stop the server Scheduler...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
