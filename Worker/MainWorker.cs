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
using DIDAWorker;
using DIDAStorage;

namespace Worker
{
    class MainWorker
    {
        //this acts as a client for storage
        public class StorageWorkerchatService
        {
            private readonly GrpcChannel channel;
            private readonly WorkerService.WorkerServiceClient client;

            private string serverHostname = "localhost";
            private int serverPort = 10004;
            public StorageWorkerchatService()
            {
                AppContext.SetSwitch(
                        "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

                client = new WorkerService.WorkerServiceClient(channel);
            }

            public void Update(string id, string oldValue, string newValue)
            {
                updateIfReply updatereply = client.updateIf(new updateIfRequest 
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
                writeStorageReply Writereply = client.writeStorage(new writeStorageRequest
                {
                    Id = id,
                    Val = val,
                });

                Console.WriteLine(Writereply);
                Console.WriteLine("Storage write reply");
            }

            public void Read(DIDAVersion didaversion)
            {
                readStorageReply Readreply = client.readStorage(new readStorageRequest
                {
                    Id = "teste",
                    DidaVersion = new didaVersion
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
                
                pingWSReply reply = client.pingWS(new pingWSRequest
                {

                });

                Console.WriteLine(reply);
                Console.WriteLine("ping worker main");
            }
        }

        //This is acts as a server for scheduler
        public class Worker : SchedulerService.SchedulerServiceBase
        {
            public override Task<pingSHWReply> pingSHW(pingSHWRequest request, ServerCallContext context)
            {
                return Task.FromResult<pingSHWReply>(pingImpl(request));
            }

            private pingSHWReply pingImpl(pingSHWRequest request)
            {
                return new pingSHWReply
                {
                    Ok = 1,
                };
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
            int Port = Int32.Parse(args[2]);

            Console.WriteLine("Starting Server on Port: " + Port);

            Server server = new Server
            {
                Services = { SchedulerService.BindService(new Worker()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) },
            };

            server.Start();

            Thread.Sleep(1000);

            Console.WriteLine("Started Server on Port: " + Port);

            StorageWorkerchatService WS = new StorageWorkerchatService();
            Console.WriteLine("Press any key to send ping to worker...");
            Console.ReadKey();

            // testing

            DIDAVersion versionDida = new DIDAVersion();
            DIDARecord recordDida = new DIDARecord();

            versionDida.replicaId = 14;
            versionDida.versionNumber = 8;

            recordDida.version = versionDida;

            WS.Read(versionDida);

            WS.Write(recordDida.id, "testing write");

            WS.Update(recordDida.id, "testing write", "testing update");

            WS.Ping();

            // testing 

            Console.WriteLine("Press any key to stop the server Worker...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
