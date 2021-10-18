using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grpc.Core;
using Grpc.Net.Client;

namespace PuppetMaster
{

    //This acts as a server
    class PuppetMaster : PuppetMasterService.PuppetMasterServiceBase
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
    public class SchedulerPuppetMasterService
    {
        private readonly GrpcChannel channel;
        private readonly SchedulerService.SchedulerServiceClient client;

        private string serverHostname = "localhost";
        private int serverPort = 10002;

        public SchedulerPuppetMasterService()
        {
            AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

            client = new SchedulerService.SchedulerServiceClient(channel);
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
                Services = { PuppetMasterService.BindService(new PuppetMaster()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            SchedulerPuppetMasterService s = new SchedulerPuppetMasterService();
            s.Debug();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            server.ShutdownAsync().Wait();
        }
    }
}
