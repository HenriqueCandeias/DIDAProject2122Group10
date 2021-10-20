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
            private readonly StorageService.StorageServiceClient client;

            private string serverHostname = "localhost";
            private int serverPort = 10004;

            public StorageWorkerchatService()
            {
                AppContext.SetSwitch(
                        "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

                client = new StorageService.StorageServiceClient(channel);
            }

            public void Ping()
            {

                pingSWReply reply = client.pingSW(new pingSWRequest
                {

                });

                Console.WriteLine(reply);
                Console.WriteLine("worker main");
            }
        }
        static void Main(string[] args)
        {
            StorageWorkerchatService SW = new StorageWorkerchatService();
            Console.ReadKey();
            SW.Ping();
        }
    }
}
