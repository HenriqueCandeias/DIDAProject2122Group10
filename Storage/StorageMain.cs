﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;
using Grpc.Net.Client;

namespace Storage
{
    class StorageMain
    {
        static void Main(string[] args)
        {
            try {
                int port = Int32.Parse(args[1].Split(':')[2]);

                int gossip_delay = Int32.Parse(args[2]);

                int replica_id = Int32.Parse(args[3]);

                Console.WriteLine("Starting Storage Server on Port: " + port);

                Server server = new Server
                {
                    Services = { StorageService.BindService(new StorageServer(gossip_delay, replica_id)) },
                    Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) },
                };

                server.Start();

                Console.WriteLine("Started Server on Port: " + port);

                Console.WriteLine("Press any key to stop the server Storage...");
                Console.ReadKey();
            
                server.ShutdownAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in PCS:\r\n");
                Console.WriteLine(e.ToString());
            }
        }
    }
}
