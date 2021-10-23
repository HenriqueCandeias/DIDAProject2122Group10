using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using Storage;
using DIDAWorker;
using DIDAStorage;

namespace Worker
{
    class WorkerMain
    {
        //this acts as a client for storage
        public class StorageWorkerService
        {
            private readonly GrpcChannel channel;
            private readonly StorageService.StorageServiceClient client;

            private string serverHostname = "localhost";
            public StorageWorkerService(string storagePort)
            {
                AppContext.SetSwitch(
                        "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + storagePort);

                client = new StorageService.StorageServiceClient(channel);
            }

            public void Update(string id, string oldValue, string newValue)
            {
                UpdateIfReply updatereply = client.UpdateIf(new UpdateIfRequest 
                { 
                    Id = id,
                    OldValue = oldValue,
                    NewValue = newValue,
                });

                Console.WriteLine(updatereply);
                Console.WriteLine("Storage update reply");
            }

            public void Write(string id, string val)
            {
                WriteStorageReply Writereply = client.WriteStorage(new WriteStorageRequest
                {
                    Id = id,
                    Val = val,
                });

                Console.WriteLine(Writereply);
                Console.WriteLine("Storage write reply");
            }

            public void Read(DIDAVersion didaversion)
            {
                ReadStorageReply Readreply = client.ReadStorage(new ReadStorageRequest
                {
                    Id = "teste",
                    DidaVersion = new DidaVersion
                    {
                        VersionNumber = didaversion.versionNumber,
                        ReplicaId = didaversion.replicaId,

                    }
                });

                Console.WriteLine(Readreply);
                Console.WriteLine("Storage read reply");
            }

            public void Ping()
            {
                
                PingWSReply reply = client.PingWS(new PingWSRequest
                {

                });

                Console.WriteLine(reply);
                Console.WriteLine("ping storage");
            }
        }

        static void LoadByReflection(string className)
        {
            string _dllNameTermination = ".dll";
            string _currWorkingDir = Directory.GetCurrentDirectory();
            IDIDAOperator _objLoadedByReflection;


            Console.WriteLine("Current working directory (cwd): " + _currWorkingDir);
            foreach (string filename in Directory.EnumerateFiles(_currWorkingDir))
            {
                Console.WriteLine("file in cwd: " + Path.GetFileName(filename));
                if (filename.EndsWith(_dllNameTermination))
                {
                    Console.WriteLine("File is a dll...Let's look at it's contained types...");
                    Assembly _dll = Assembly.LoadFrom(filename);
                    Type[] _typeList = _dll.GetTypes();
                    foreach (Type type in _typeList)
                    {
                        Console.WriteLine("type contained in dll: " + type.Name);
                        if (type.Name == className)
                        {
                            Console.WriteLine("Found type to load dynamically: " + className);
                            _objLoadedByReflection = (IDIDAOperator)Activator.CreateInstance(type);
                            foreach (MethodInfo method in type.GetMethods())
                            {
                                Console.WriteLine("method from class " + className + ": " + method.Name);
                            }
                            //_objLoadedByReflection. function of class
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            int port = Int32.Parse(args[2]);

            Console.WriteLine("Starting Server on Port: " + port);

            Server server = new Server
            {
                Services = { WorkerService.BindService(new WorkerServer()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) },
            };

            server.Start();

            Thread.Sleep(1000);

            Console.WriteLine("Started Server on Port: " + port);

            ////StorageWorkerService WS = new StorageWorkerService(args[3]);
            Console.WriteLine("Press any key to send ping to storage...");
            Console.ReadKey();

            // testing

            DIDAVersion versionDida = new DIDAVersion();
            DIDARecord recordDida = new DIDARecord();

            versionDida.replicaId = 14;
            versionDida.versionNumber = 8;

            recordDida.version = versionDida;

            //WS.Read(versionDida);

            //WS.Write(recordDida.id, "testing write");

            //WS.Update(recordDida.id, "testing write", "testing update");

            ////WS.Ping();

            // testing 

            Console.WriteLine("Press any key to stop the server Worker...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
