using Grpc.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class PuppetMasterGUI : Form
    {
        public PuppetMasterInitializer initializer;
        public Server server;

        public PuppetMasterGUI()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            int Port = 10001;

            server = new Server
            {
                Services = { PuppetMasterService.BindService(new PuppetMasterServer()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            initializer = new PuppetMasterInitializer();
            initializer.Start();
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            initializer.Execute(CommandsInputBox.Text);
            CommandsInputBox.Clear();
        }
    }
}
