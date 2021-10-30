using System;
using Grpc.Core;
using Pcs;

namespace PCS
{
    class PCSMain
    {
        public const int pcsPort = 10000;
        static void Main(string[] args)
        {
            Console.WriteLine("Starting PCS Server on Port: " + pcsPort);

            Server server = new Server
            {
                Services = { PCSService.BindService(new PCSServer()) },
                Ports = { new ServerPort("localhost", pcsPort, ServerCredentials.Insecure) },
            };

            server.Start();

            Console.WriteLine("Started PCS Server on Port: " + pcsPort);

            Console.WriteLine("Press any key to stop the server PCS...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
