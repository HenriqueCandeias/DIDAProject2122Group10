using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grpc.Core;
using Grpc.Net.Client;

namespace PuppetMaster
{   

    //This acts as a server
    class PuppetMasterServer : PuppetMasterService.PuppetMasterServiceBase
    {
        public override Task<pingReply> ping(pingRequest request, ServerCallContext context)
        {
            return Task.FromResult<pingReply>(pingImpl(request));
        }

        public pingReply pingImpl(pingRequest request)
        {
            return new pingReply
            {
                Ok = 1,
            };
        }
    }

    //This acts as a client
    public class SchedulerServiceClient
    {
        private readonly GrpcChannel channel;
        private readonly SchedulerDebugService.SchedulerDebugServiceClient client;

        private string serverHostname = "localhost";
        private int serverPort = 10002;

        public SchedulerServiceClient()
        {
            AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

            client = new SchedulerDebugService.SchedulerDebugServiceClient(channel);
        }

        public void Debug()
        {
            DebugReply reply = client.Debug(new DebugRequest
            {

            });

            //This needs to be implemented on the GUI
            Console.WriteLine(reply);
        }

    }

    static class PuppetMasterProgram    
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            int Port = 10000;

            Server server = new Server
            {
                Services = { PuppetMasterService.BindService(new PuppetMasterServer()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            /*
            PuppetMasterInitializer initializer = new PuppetMasterInitializer();
            initializer.startScheduler();

            SchedulerServiceClient s = new SchedulerServiceClient();
            s.Debug();*/

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            server.ShutdownAsync().Wait();
        }
    }
}
