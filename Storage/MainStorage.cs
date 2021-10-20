using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace Storage
{
    //This is acts as a server
    public class Storage : StorageService.StorageServiceBase
    {
        public override Task<pingSWReply> pingSW(pingSWRequest request, ServerCallContext context)
        {
            return Task.FromResult<pingSWReply>(pingImpl(request));
        }

        private pingSWReply pingImpl(pingSWRequest request)
        {
            return new pingSWReply
            {
                Ok = 1,
            };
        }
    }
    class MainWorker
    {
        static void Main(string[] args)
        {
            int Port = 10004;

            Server server = new Server
            {
                Services = { StorageService.BindService(new Storage()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) },
            };

            server.Start();

            Console.WriteLine("ChatServer server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
