using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace Storage
{
    //This is acts as a server
    public class Storage : WorkerService.WorkerServiceBase
    {
        public override Task<pingWSReply> pingWS(pingWSRequest request, ServerCallContext context)
        {
            return Task.FromResult<pingWSReply>(pingImpl(request));
        }

        private pingWSReply pingImpl(pingWSRequest request)
        {
            return new pingWSReply
            {
                Ok = 1,
            };
        }
    }
    class MainWorker
    {
        static void Main(string[] args)
        {
            int Port = Int32.Parse(args[2]);

            Console.WriteLine("Starting Server on Port: " + Port);

            Server server = new Server
            {
                Services = { WorkerService.BindService(new Storage()) },
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
