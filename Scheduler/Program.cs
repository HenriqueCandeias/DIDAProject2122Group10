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
    public class PuppetMasterSchedulerService
    {
        private readonly GrpcChannel channel;
        private readonly PuppetMasterService.PuppetMasterServiceClient client;
        private Server server;

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

    class Program
    {

        static void Main(string[] args)
        {
            PuppetMasterSchedulerService p = new PuppetMasterSchedulerService();
            p.Ping();
        }
    }
}
