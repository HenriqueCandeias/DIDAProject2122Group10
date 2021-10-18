using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grpc.Core;

namespace PuppetMaster
{
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
    static class Program
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

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            server.ShutdownAsync().Wait();
        }
    }
}
