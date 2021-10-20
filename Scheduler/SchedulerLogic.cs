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

    //This is acts as a server
    public class Scheduler : SchedulerDebugService.SchedulerDebugServiceBase
    {
        public override Task<DebugReply> Debug(DebugRequest request, ServerCallContext context)
        {
            return Task.FromResult<DebugReply>(debugImpl(request));
        }

        public DebugReply debugImpl(DebugRequest request)
        {
            return new DebugReply
            {
                Ok = true,
            };
        }
    }

    //this acts as a client
    public class PuppetMasterSchedulerService
    {
        private readonly GrpcChannel channel;
        private readonly PuppetMasterService.PuppetMasterServiceClient client;

        private string serverHostname = "localhost";
        private int serverPort = 10000;

        public PuppetMasterSchedulerService()
        {
            AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

            client = new PuppetMasterService.PuppetMasterServiceClient(channel);
        }

        public void Ping()
        {

            pingReply reply = client.ping(new pingRequest
            {

            });

            Console.WriteLine(reply);
        }
    }

    class SchedulerLogic
    {

        static void Main(string[] args)
        {
            int Port = 10002;

            Server server = new Server
            {
                Services = { SchedulerDebugService.BindService(new Scheduler())},
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) },
            };

            server.Start();

            PuppetMasterSchedulerService p = new PuppetMasterSchedulerService();
            p.Ping();

            server.ShutdownAsync().Wait();
        }
    }
}
