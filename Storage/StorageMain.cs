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
            int Port = Int32.Parse(args[2]);

            Console.WriteLine("Starting Server on Port: " + Port);

            Server server = new Server
            {
                Services = { StorageService.BindService(new StorageServer()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) },
            };

            server.Start();

            Console.WriteLine("Started Server on Port: " + Port);

            Console.WriteLine("Press any key to stop the server Storage...");
            Console.ReadKey();
            
            server.ShutdownAsync().Wait();
        }
    }
}
