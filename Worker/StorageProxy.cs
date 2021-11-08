using DIDAWorker;
using Grpc.Net.Client;
using Storage;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Worker
{
    public class StorageProxy : IDIDAStorage
    {
        DIDAMetaRecordConsistent metaRecord;

        SortedDictionary<ulong, StorageService.StorageServiceClient> replicaIdToClient = new SortedDictionary<ulong, StorageService.StorageServiceClient>();

        public static readonly DIDARecordReply nullDIDARecordReply = new DIDARecordReply
        {
            Id = "",
            Version = new DIDAWorker.DIDAVersion
            {
                VersionNumber = -1,
                ReplicaId = -1,
            },
            Val = "",
        };

        private static readonly DIDAVersion nullDIDAVersion = new DIDAVersion
        {
            VersionNumber = -1,
            ReplicaId = -1,
        };

        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAMetaRecordConsistent metaRecord)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            foreach (DIDAStorageNode n in storageNodes)
            {
                byte[] encodedReplicaId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(n.serverId));
                var hashedReplicaId = BitConverter.ToUInt64(encodedReplicaId, 0);

                GrpcChannel channel = GrpcChannel.ForAddress(n.host + ":" + n.port);
                replicaIdToClient.Add(hashedReplicaId, new StorageService.StorageServiceClient(channel));
            }

            this.metaRecord = metaRecord;
        }

        private SortedDictionary<ulong, StorageService.StorageServiceClient> LocateStorageNodesWithRecord(string id)
        {
            byte[] encodedRecordId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(id));
            var hashedRecordId = BitConverter.ToUInt64(encodedRecordId, 0);

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

        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            SortedDictionary<ulong, StorageService.StorageServiceClient> storagesWithRecord = LocateStorageNodesWithRecord(r.Id);

            bool versionChangedAccordingToMetaRecord = false;

            if (r.Version.Equals(nullDIDAVersion))
            {
                //Check if any of the operators of the app has already modified the record.
                //If so, the current operator will only be able to read the record version obtained by those operators.

                foreach (string recordId in metaRecord.RecordIdToConsistentVersion.Keys)
                {
                    if (recordId.Equals(r.Id))
                    {
                        r.Version = metaRecord.RecordIdToConsistentVersion.GetValueOrDefault(recordId);
                        versionChangedAccordingToMetaRecord = true;
                        break;
                    }
                }
            }

            ReadStorageReply reply = null;

            foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in storagesWithRecord)
            {
                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                try
                {
                    reply = pair.Value.ReadStorage(new ReadStorageRequest
                    {
                        Id = r.Id,
                        DidaVersion = new DidaVersion
                        {
                            VersionNumber = r.Version.VersionNumber,
                            ReplicaId = r.Version.ReplicaId
                        }
                    });
                }
                catch (Grpc.Core.RpcException)
                {
                    Console.WriteLine("Replica with id " + pair.Key.ToString() + " crashed while performing read operation.");
                    replicaIdToClient.Remove(pair.Key);
                    continue;
                }
                
                if (reply.DidaRecord.DidaVersion.VersionNumber != -1 && reply.DidaRecord.DidaVersion.ReplicaId != -1)
                    break;
            }

            //Note: if a read operation is executed according to a record id that does not exist anywhere AND
            //versionChangedAccordingToMetaRecord, then the app will be considered inconsistent and terminate.

            if (reply.DidaRecord.DidaVersion.VersionNumber == -1 && reply.DidaRecord.DidaVersion.ReplicaId == -1 
                && versionChangedAccordingToMetaRecord)
            {
                metaRecord.appIsInconsistent = true;
                return nullDIDARecordReply;
            }

            //If a future operator of this app reads a record with the same record id,
            //it has to read the same version read by this operator.

            metaRecord.RecordIdToConsistentVersion[reply.DidaRecord.Id] = new DIDAWorker.DIDAVersion 
            {
                VersionNumber = reply.DidaRecord.DidaVersion.VersionNumber,
                ReplicaId = reply.DidaRecord.DidaVersion.ReplicaId,
            };

            Console.WriteLine(
                "Read - the record is: ID: " + reply.DidaRecord.Id + " Version Number: " + reply.DidaRecord.DidaVersion.VersionNumber +
                " Replica ID: " + reply.DidaRecord.DidaVersion.ReplicaId + " Val: " + reply.DidaRecord.Val
            );

            return new DIDAWorker.DIDARecordReply
            {
                Id = reply.DidaRecord.Id,
                Val = reply.DidaRecord.Val,
                Version =
                {
                    VersionNumber = reply.DidaRecord.DidaVersion.VersionNumber,
                    ReplicaId = reply.DidaRecord.DidaVersion.ReplicaId,
                },
            };
        }

        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            SortedDictionary<ulong, StorageService.StorageServiceClient> storagesWithRecord = LocateStorageNodesWithRecord(r.Id);

            WriteStorageReply reply = null;

            foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in storagesWithRecord)
            {
                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                try
                {
                    reply = pair.Value.WriteStorage(new WriteStorageRequest
                    {
                        Id = r.Id,
                        Val = r.Val
                    });
                }
                catch (Grpc.Core.RpcException)
                {
                    Console.WriteLine("Replica with id " + pair.Key.ToString() + " crashed while performing write operation.");
                    replicaIdToClient.Remove(pair.Key);
                    continue;
                }

                break;
            }

            //If a future operator of this app reads a record with the same record id,
            //it has to read the version corresponding to the "write" that was just performed.

            metaRecord.RecordIdToConsistentVersion[r.Id] = new DIDAWorker.DIDAVersion
            {
                VersionNumber = reply.DidaVersion.VersionNumber,
                ReplicaId = reply.DidaVersion.ReplicaId,
            };

            Console.WriteLine(
                "Write - new version is: Version Number: " + reply.DidaVersion.VersionNumber +
                " Replica ID: " + reply.DidaVersion.ReplicaId
            );

            return new DIDAWorker.DIDAVersion
            {
                VersionNumber = reply.DidaVersion.VersionNumber,
                ReplicaId = reply.DidaVersion.ReplicaId
            };
        }

        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            SortedDictionary<ulong, StorageService.StorageServiceClient> storagesWithRecord = LocateStorageNodesWithRecord(r.Id);

            UpdateIfReply reply = null;

            foreach (KeyValuePair<ulong, StorageService.StorageServiceClient> pair in storagesWithRecord)
            {
                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                try
                {
                    reply = pair.Value.UpdateIf(new UpdateIfRequest
                    {
                        Id = r.Id,
                        NewValue = r.Newvalue,
                        OldValue = r.Oldvalue
                    });
                }
                catch (Grpc.Core.RpcException)
                {
                    Console.WriteLine("Replica with id " + pair.Key.ToString() + " crashed while performing updateIfValueIs operation.");
                    replicaIdToClient.Remove(pair.Key);
                    continue;
                }

                break;
            }

            //If a future operator of this app reads a record with the same record id,
            //it has to read the version corresponding to the "write" that was just performed.

            metaRecord.RecordIdToConsistentVersion[r.Id] = new DIDAWorker.DIDAVersion
            {
                VersionNumber = reply.DidaVersion.VersionNumber,
                ReplicaId = reply.DidaVersion.ReplicaId,
            };

            Console.WriteLine(
                "UpdateIf - new version is: Version Number: " + reply.DidaVersion.VersionNumber +
                " Replica ID: " + reply.DidaVersion.ReplicaId
            );

            return new DIDAWorker.DIDAVersion
            {
                VersionNumber = reply.DidaVersion.VersionNumber,
                ReplicaId = reply.DidaVersion.ReplicaId
            };
        }
    }
}
