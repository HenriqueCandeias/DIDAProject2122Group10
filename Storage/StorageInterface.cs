using DIDAStorage;
using Worker;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;

namespace Storage
{
    class StorageInterface
    {
        private int replicaId;

        private float replicationFactor;

        private int numReplicas;

        public List<int> clocks;

        public StorageImpl storageImpl;

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<int, string> storagesIdToURL = new Dictionary<int, string>();

        private Dictionary<string, WorkerService.WorkerServiceClient> workerClients = new Dictionary<string, WorkerService.WorkerServiceClient>();

        private SortedDictionary<int, StorageService.StorageServiceClient> storageClients = new SortedDictionary<int, StorageService.StorageServiceClient>();

        private Dictionary<int, string> crashedStoragesIdToURL = new Dictionary<int, string>();

        private List<int> replicasIds = new List<int>();
        
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

        public StorageInterface(int gossip_delay)
        {
            storageImpl = new StorageImpl();
            SetTimer(gossip_delay, Gossip);
        }

        private void Gossip(Object source, ElapsedEventArgs e)
        {
            GossipReply reply = null;

            List<LogStruct> allLogs = new List<LogStruct>();

            int currentReplicaIndex = replicasIds.FindIndex(a => a == replicaId);

            int currentReplica = replicasIds[currentReplicaIndex];

            for (int i = 0; i < numReplicas; i++)
            {
                currentReplicaIndex = currentReplicaIndex == 0 ? storageClients.Count() - 1 : --currentReplicaIndex;
                currentReplica = replicasIds[currentReplicaIndex];
                try
                {
                    reply = storageClients.GetValueOrDefault(currentReplica).RequestLog(new GossipRequest
                    {
                        LogRequest = "Request log from :" + replicaId,
                        Clock = clocks[currentReplica],
                    });

                    allLogs.AddRange(reply.LogReply);
                    clocks[currentReplica] += reply.LogReply.Count;
                    if(reply.LogReply.Any())
                    {
                        Console.WriteLine("received from: " + currentReplica);
                        Console.WriteLine(reply);
                    }
                }
                catch
                {
                    Console.WriteLine("grpc connection to " + currentReplica + " has expired");

                    if(crashedStoragesIdToURL.ContainsKey(currentReplica))
                    {
                        crashedStoragesIdToURL.Add(currentReplica, storagesIdToURL[currentReplica]);
                        storagesIdToURL.Remove(currentReplica);
                        storageClients.Remove(currentReplica);
                        replicasIds.Remove(currentReplica);
                    }


                    storageClients.AsParallel().ForAll(entry => entry.Value.CrashReport(new Storage.CrashRepRequests
                    {
                        Id = currentReplica,
                    }));

                    workerClients.AsParallel().ForAll(entry => entry.Value.CrashReport(new Worker.CrashRepRequests
                    {
                        Id = currentReplica,
                    }));

                    i--;
                    numReplicas = (int)Math.Ceiling((replicationFactor * storageClients.Count()) - 1);
                }

            }

            List<LogStruct> SortedList = allLogs.OrderBy(l => l.Id).ThenBy(l => l.DidaVersion.VersionNumber).ToList();

            foreach (var items in SortedList)
            {
                if (string.IsNullOrEmpty(items.OldVal))
                {
                    storageImpl.write(items.Id, items.NewVal, false);
                }
                else
                {
                    storageImpl.updateIfValueIs(items.Id, items.OldVal, items.NewVal, false);
                }
            }
        }

        public CrashRepReply CrashReport(CrashRepRequests request)
        {
            if (storagesIdToURL.ContainsKey(request.Id))
            {
                crashedStoragesIdToURL.Add(request.Id, storagesIdToURL[request.Id]);
                storagesIdToURL.Remove(request.Id);
                storageClients.Remove(request.Id);
                replicasIds.Remove(request.Id);

                Console.WriteLine("removed :" + request.Id);
            }
            return new CrashRepReply();
        }

        private static void SetTimer(int gossip_delay, ElapsedEventHandler Gossip)
        {
            if (gossip_delay == 0)
                return;

            Timer timer = new Timer(gossip_delay);
            timer.Elapsed += new ElapsedEventHandler(Gossip);
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public GossipReply RequestLog(GossipRequest request)
        {
            var gossipreply = new GossipReply();
            gossipreply.LogReply.Add(storageImpl.GetLog(request.Clock));

            return gossipreply;
        }

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Grpc.Net.Client.GrpcChannel channel;

            foreach (string key in request.Workers.Keys)
            {
                workersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));

                Console.WriteLine("Worker: " + key + " URL: " + workersIdToURL.GetValueOrDefault(key));

                channel = Grpc.Net.Client.GrpcChannel.ForAddress(workersIdToURL.GetValueOrDefault(key));
                workerClients[key] = new WorkerService.WorkerServiceClient(channel);
            }

            foreach (int key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                
                Console.WriteLine("Storage: " + key + " URL: " + storagesIdToURL.GetValueOrDefault(key));

                channel = Grpc.Net.Client.GrpcChannel.ForAddress(storagesIdToURL.GetValueOrDefault(key));
                storageClients[key] = new StorageService.StorageServiceClient(channel);
                replicasIds.Add(key);

            }

            replicaId = request.ReplicaId;
            storageImpl.replicaId = replicaId;
            replicationFactor = request.ReplicationFactor;
            numReplicas = (int)Math.Ceiling((replicationFactor * storageClients.Count()) - 1);

            clocks = new List<int>(new int[storageClients.Count()]);

            Console.WriteLine("I'm replicaId " + replicaId.ToString());
            return new SendNodesURLReply();
        }

        public UpdateIfReply UpdateIf(UpdateIfRequest request)
        {
            DIDAVersion reply = storageImpl.updateIfValueIs(request.Id, request.OldValue, request.NewValue, true);

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
            DIDAVersion reply = storageImpl.write(request.Id, request.Val, true);

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

        public StatusReply Status(StatusRequest request)
        {
            Console.WriteLine("\r\nSTORAGE STATUS\r\n");

            foreach (KeyValuePair<int, string> pair in storagesIdToURL)
                Console.WriteLine("Active Storage - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("");

            foreach (KeyValuePair<int, string> pair in crashedStoragesIdToURL)
                Console.WriteLine("Crashed Storage - Id: " + pair.Key + " URL: " + pair.Value);

            Console.WriteLine("\r\nEND OF STATUS\r\n");

            return new StatusReply();
        }

        public ListReply ListObjects()
        {
            //TODO actually list the objects stored here
            foreach(KeyValuePair<string, List<DIDARecord>> recordList in storageImpl.recordIdToRecords)
            {
                foreach(DIDARecord record in recordList.Value)
                {
                    Console.WriteLine("ID:" + record.id + " Val:" + record.val + " Version:" + record.version.versionNumber);
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
            byte[] encodedRecordId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(request.Id));
            int hashedRecordId = BitConverter.ToInt32(encodedRecordId, 0) % storageClients.Count;
            Console.WriteLine("Recieved Populate Requests");
            Console.WriteLine("ID: " + request.Id + " Value: " + request.Val);
            Console.WriteLine(hashedRecordId);
            if(hashedRecordId == replicaId)
            {
                Console.WriteLine("Writing");
                storageImpl.write(request.Id, request.Val, true);
            }
            else
            {
                Console.WriteLine("Not Writting");
            }

            return new Storage.PopulateReply();
        }
    }
}
