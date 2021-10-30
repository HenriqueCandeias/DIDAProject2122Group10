using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;
using Grpc.Net.Client;

namespace Storage
{
    class StorageMain
    {
        static void Main(string[] args)
        {
            int port = Int32.Parse(args[1].Split(':')[2]);

            Console.WriteLine("Starting Storage Server on Port: " + port);

            Server server = new Server
            {
                Services = { StorageService.BindService(new StorageServer()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) },
            };

            server.Start();

            Console.WriteLine("Started Server on Port: " + port);

            Console.WriteLine("Press any key to stop the server Storage...");
            Console.ReadKey();
            
            server.ShutdownAsync().Wait();
        }
    }
}
