using DIDAOperator;
using DIDAWorker;
using Storage;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Worker
{
    public class StorageProxy : IDIDAStorage
    {
        Dictionary<string, StorageService.StorageServiceClient> clients = new Dictionary<string, StorageService.StorageServiceClient>();

        Dictionary<string, Grpc.Net.Client.GrpcChannel> channels = new Dictionary<string, Grpc.Net.Client.GrpcChannel>();

        DIDAWorker.DIDAMetaRecord meta;

        SortedDictionary<ulong, StorageService.StorageServiceClient> replicaIdToClient = new SortedDictionary<ulong, StorageService.StorageServiceClient>();


        // The constructor of a storage proxy.
        // The storageNodes parameter lists the nodes that this storage proxy needs to be aware of to perform
        // read, write and updateIfValueIs operations.
        // The metaRecord identifies the request being processed by this storage proxy object
        // and allows the storage proxy to request data versions previously accessed by the request
        // and to inform operators running on the following (downstream) workers of the versions it Baccessed.
        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAWorker.DIDAMetaRecord metaRecord)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            foreach (DIDAStorageNode n in storageNodes)
            {
                channels[n.serverId] = Grpc.Net.Client.GrpcChannel.ForAddress(n.host + ":" + n.port);
                clients[n.serverId] = new StorageService.StorageServiceClient(channels[n.serverId]);

                byte[] encodedReplicaId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(n.serverId));
                var hashedReplicaId = BitConverter.ToUInt64(encodedReplicaId, 0);

                Console.WriteLine("Replica with ID " + n.serverId + " has hash " + hashedReplicaId.ToString());

                replicaIdToClient.Add(hashedReplicaId, clients[n.serverId]);
            }

            meta = metaRecord;
        }

        private SortedDictionary<ulong, StorageService.StorageServiceClient> LocateStorageNodesWithRecord(string id)
        {
            byte[] encodedRecordId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(id));
            var hashedRecordId = BitConverter.ToUInt64(encodedRecordId, 0);

            Console.WriteLine("Record with ID " + id + " has hash " + hashedRecordId.ToString());

            SortedDictionary<ulong, StorageService.StorageServiceClient> nodes = new SortedDictionary<ulong, StorageService.StorageServiceClient>();

            foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in replicaIdToClient)
            {
                if (pair.Key > hashedRecordId)
                {
                    nodes.Add(pair.Key, pair.Value);
                    //TODO: When replication gets implemented in the storage nodes, this method has to read more than one record
                    break;
                }

            }

            if (nodes.Count == 0)
            {
                foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in replicaIdToClient)
                {
                    //Get the first nodes (loop around the Consistent Hashing ring of storage servers)
                    nodes.Add(pair.Key, pair.Value);
                    //TODO: When replication gets implemented in the storage nodes, this method has to read more than one record
                    break;
                }
            }

            return nodes;
        }

        // THE FOLLOWING 3 METHODS ARE THE ESSENCE OF A STORAGE PROXY
        // IN THIS EXAMPLE THEY ARE JUST CALLING THE STORAGE 
        // IN THE COMLPETE IMPLEMENTATION THEY NEED TO:
        // 1) LOCATE THE RIGHT STORAGE SERVER,
        // 2) DEAL WITH FAILED STORAGE SERVERS
        // 3) CHECK IN THE METARECORD WHICH ARE THE PREVIOUSLY READ VERSIONS OF DATA 
        // 4) RECORD ACCESSED DATA INTO THE METARECORD

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            SortedDictionary<ulong, StorageService.StorageServiceClient> storagesWithRecord = LocateStorageNodesWithRecord(r.Id);

            //Modify r (or not) according to the metadata in this.meta

            //Tolerate faults when calling gRPC methods on the storage nodes; delete crashed nodes from this.clients

            ReadStorageReply reply = null;

            foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in storagesWithRecord)
            {
                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                //ASSUMING ONLY ONE REPLICA
                reply = pair.Value.ReadStorage(new ReadStorageRequest { Id = r.Id, DidaVersion = new DidaVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
            }

            Console.WriteLine(
                "Read - the record is: ID: " + reply.DidaRecord.Id + " Version Number: " + reply.DidaRecord.DidaVersion.VersionNumber +
                " Replica ID: " + reply.DidaRecord.DidaVersion.ReplicaId + " Val: " + reply.DidaRecord.Val
            );

            return new DIDAWorker.DIDARecordReply { Id = "1", Val = "1", Version = { VersionNumber = 1, ReplicaId = 1 } };
        }

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            SortedDictionary<ulong, StorageService.StorageServiceClient> storagesWithRecord = LocateStorageNodesWithRecord(r.Id);

            //Modify r (or not) according to the metadata in this.meta

            //Tolerate faults when calling gRPC methods on the storage nodes; delete crashed nodes from this.clients

            WriteStorageReply reply = null;

            foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in storagesWithRecord)
            {
                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                //ASSUMING ONLY ONE REPLICA
                reply = pair.Value.WriteStorage(new WriteStorageRequest { Id = r.Id, Val = r.Val });
            }

            Console.WriteLine(
                "Write - new version is: Version Number: " + reply.DidaVersion.VersionNumber +
                " Replica ID: " + reply.DidaVersion.ReplicaId
            );

            return new DIDAWorker.DIDAVersion { VersionNumber = reply.DidaVersion.VersionNumber, ReplicaId = reply.DidaVersion.ReplicaId };
        }

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            SortedDictionary<ulong, StorageService.StorageServiceClient> storagesWithRecord = LocateStorageNodesWithRecord(r.Id);

            //Modify r (or not) according to the metadata in this.meta

            //Tolerate faults when calling gRPC methods on the storage nodes; delete crashed nodes from this.clients

            UpdateIfReply reply = null;

            foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in storagesWithRecord)
            {
                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                //ASSUMING ONLY ONE REPLICA
                reply = pair.Value.UpdateIf(new UpdateIfRequest { Id = r.Id, NewValue = r.Newvalue, OldValue = r.Oldvalue });
            }

            Console.WriteLine(
                "UpdateIf - new version is: Version Number: " + reply.DidaVersion.VersionNumber +
                " Replica ID: " + reply.DidaVersion.ReplicaId
            );

            return new DIDAWorker.DIDAVersion { VersionNumber = reply.DidaVersion.VersionNumber, ReplicaId = reply.DidaVersion.ReplicaId };
        }
    }
}
