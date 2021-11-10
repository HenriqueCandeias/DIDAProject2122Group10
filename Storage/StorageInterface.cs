using DIDAStorage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace Storage
{
    class StorageInterface
    {
        private int replicaId;

        public StorageImpl storageImpl;

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

        private Dictionary<string, Grpc.Net.Client.GrpcChannel> channels = new Dictionary<string, Grpc.Net.Client.GrpcChannel>();
        private Dictionary<string, StorageService.StorageServiceClient> clients = new Dictionary<string, StorageService.StorageServiceClient>();


        private static readonly ReadStorageReply nullReadStorageReply = new ReadStorageReply
        {
            DidaRecord = new DidaRecord
            {
                DidaVersion = new DidaVersion
                {
                    VersionNumber = -1,
                    ReplicaId = -1,
                },
            },
        };

        public StorageInterface(int gossip_delay, int replica_id)
        {
            storageImpl = new StorageImpl(gossip_delay, replica_id);
            replicaId = replica_id;
            SetTimer(gossip_delay, Gossip);
        }

        private void Gossip(Object source, ElapsedEventArgs e)
        {
            GossipReply reply = null;

            foreach (KeyValuePair<string, StorageService.StorageServiceClient> pair in clients)
            {
                try
                {
                    reply = pair.Value.RequestLog(new GossipRequest
                    {
                        LogRequest = "resquest log from :" + replicaId,
                    });
                    Console.WriteLine(reply);


                    foreach (var items in reply.LogReply)
                    {
                        if (string.IsNullOrEmpty(items.OldVal))
                        {
                            storageImpl.updateIfValueIs(items.Id, items.OldVal, items.NewVal);
                        }
                        else
                        {
                            storageImpl.write(items.Id, items.NewVal);
                        }
                    }

                }
                catch
                {
                    Console.WriteLine("grpc connection to " + pair.Key + "has expired");
                }
            }
        }

        private static void SetTimer(int gossip_delay, ElapsedEventHandler Gossip)
        {
            Timer timer = new Timer(gossip_delay);
            timer.Elapsed += new ElapsedEventHandler(Gossip);
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public GossipReply RequestLog(GossipRequest request)
        {
            Console.WriteLine(request.LogRequest);

            var gossipreply = new GossipReply();
            gossipreply.LogReply.Add(storageImpl.GetLog());

            return gossipreply;
        }

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            foreach (string key in request.Workers.Keys)
            {
                workersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));
                Console.WriteLine("Worker: " + key + " URL: " + workersIdToURL.GetValueOrDefault(key));
            }

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            foreach (string key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key + " URL: " + storagesIdToURL.GetValueOrDefault(key));

                channels[key] = Grpc.Net.Client.GrpcChannel.ForAddress(storagesIdToURL.GetValueOrDefault(key));
                clients[key] = new StorageService.StorageServiceClient(channels[key]);

            }

            return new SendNodesURLReply();
        }

        public UpdateIfReply UpdateIf(UpdateIfRequest request)
        {
            DIDAVersion reply = storageImpl.updateIfValueIs(request.Id, request.OldValue, request.NewValue);

            return new UpdateIfReply
            {
                DidaVersion = new DidaVersion
                {
                    VersionNumber = reply.versionNumber,
                    ReplicaId = reply.replicaId,
                },
            };

        }

        public WriteStorageReply WriteStorage(WriteStorageRequest request)
        {
            DIDAVersion reply = storageImpl.write(request.Id, request.Val);

            return new WriteStorageReply
            {
                DidaVersion = new DidaVersion
                {
                    VersionNumber = reply.versionNumber,
                    ReplicaId = reply.replicaId,
                },
            };
        }

        public ReadStorageReply ReadStorage(ReadStorageRequest request)
        {
            DIDAVersion didaversion = new DIDAVersion
            {
                replicaId = request.DidaVersion.ReplicaId,
                versionNumber = request.DidaVersion.VersionNumber,
            };

            DIDARecord record = storageImpl.read(request.Id, didaversion);

            if (record.id.Equals("") && record.val.Equals(""))
                return nullReadStorageReply;

            else
                return new ReadStorageReply
                {
                    DidaRecord = new DidaRecord
                    {
                        Id = record.id,
                        DidaVersion = new DidaVersion
                        {
                            VersionNumber = record.version.versionNumber,
                            ReplicaId = record.version.replicaId,
                        },
                        Val = record.val,
                    },
                };

        }

        public StatusReply Status()
        {
            //TODO display necessary info
            Console.WriteLine("Status: I'm alive");
            return new StatusReply();
        }

        public ListReply ListObjects()
        {
            //TODO actually list the objects stored here
            foreach(KeyValuePair<string, List<DIDARecord>> recordList in storageImpl.recordIdToRecords)
            {
                foreach(DIDARecord record in recordList.Value)
                {
                    Console.WriteLine("ID:" + record.id + " Val:" + record.val + " Version:" + record.version);
                }
            }
            return new ListReply();
        }

        public CrashReply Crash()
        {
            Task.Delay(1000).ContinueWith(t => System.Environment.Exit(1));
            return new CrashReply();
        }

        public PopulateReply Populate(PopulateRequest request)
        {
            storageImpl.write(request.Id, request.Val);
            return new Storage.PopulateReply();
        }
    }
}
