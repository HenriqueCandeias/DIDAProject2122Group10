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
            int port = Int32.Parse(args[1].Split(':')[2]);

            Console.WriteLine("Starting Server on Port: " + port);

            Server server = new Server
            {
                Services = { WorkerService.BindService(new WorkerServer()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) },
            };

            server.Start();

            Console.WriteLine("Started Server on Port: " + port);

            Console.WriteLine("Press any key to stop the server Worker...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
