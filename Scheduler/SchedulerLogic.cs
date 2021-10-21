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

    //this acts as a client for the worker
    public class SchedulerWorkerchatService
    {
        private readonly GrpcChannel channel;
        private readonly SchedulerService.SchedulerServiceClient client;

        private string serverHostname = "localhost";
        private int serverPort = 10004;

        public SchedulerWorkerchatService()
        {
            AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

            client = new SchedulerService.SchedulerServiceClient(channel);
        }

        public void PingWorker()
        {

            pingSHWReply reply = client.pingSHW(new pingSHWRequest
            {

            });

            Console.WriteLine(reply);
            Console.WriteLine("worker main");
        }
    }

    class SchedulerLogic
    {

        static void Main(string[] args)
        {
            int Port = Int32.Parse(args[2]);

            //Console.WriteLine(Port);

            Server server = new Server
            {
                Services = { SchedulerDebugService.BindService(new Scheduler())},
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) },
            };

            server.Start();

            
            PuppetMasterSchedulerService p = new PuppetMasterSchedulerService();
            Console.WriteLine("Press any key to send ping to PuppetMaster...");
            Console.ReadKey();
            p.Ping();
            
            
            SchedulerWorkerchatService SW = new SchedulerWorkerchatService();
            Console.WriteLine("Press any key to send ping to Worker...");
            Console.ReadKey();
            SW.PingWorker();
            

            Console.WriteLine("Press any key to stop the server Scheduler...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
