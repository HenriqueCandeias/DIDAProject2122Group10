using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Worker
{
    class MainStorage
    {
        //this acts as a client
        public class StorageWorkerchatService
        {
            private readonly GrpcChannel channel;
            private readonly WorkerService.WorkerServiceClient client;

            private string serverHostname = "localhost";
            private int serverPort = 10006;

            public StorageWorkerchatService()
            {
                AppContext.SetSwitch(
                        "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

                client = new WorkerService.WorkerServiceClient(channel);
            }

            public void Ping()
            {

                pingWSReply reply = client.pingWS(new pingWSRequest
                {

                });

                Console.WriteLine(reply);
                Console.WriteLine("worker main");
            }
        }

        //This is acts as a server
        public class Worker : SchedulerService.SchedulerServiceBase
        {
            public override Task<pingSHWReply> pingSHW(pingSHWRequest request, ServerCallContext context)
            {
                return Task.FromResult<pingSHWReply>(pingImpl(request));
            }

            private pingSHWReply pingImpl(pingSHWRequest request)
            {
                return new pingSHWReply
                {
                    Ok = 1,
                };
            }
        }
        static void Main(string[] args)
        {
            int Port = 10004;

            Server server = new Server
            {
                Services = { SchedulerService.BindService(new Worker()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) },
            };

            server.Start();

            StorageWorkerchatService WS = new StorageWorkerchatService();
            Console.WriteLine("Press any key to send ping to storage...");
            Console.ReadKey();
            WS.Ping();

            Console.WriteLine("Press any key to stop the server Worker...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
