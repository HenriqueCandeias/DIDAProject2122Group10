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

        SortedDictionary<int, StorageService.StorageServiceClient> replicaIdToClient = new SortedDictionary<int, StorageService.StorageServiceClient>();

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
                GrpcChannel channel = GrpcChannel.ForAddress(n.host + ":" + n.port);
                replicaIdToClient.Add(Int32.Parse(n.serverId), new StorageService.StorageServiceClient(channel));
            }

            this.metaRecord = metaRecord;
        }

        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            bool versionChangedAccordingToMetaRecord = false;

            List<int> orderOfReplicaIdToRead = GetOrderOfReplicasToReadFrom(r.Id);

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

            foreach (int replicaId in orderOfReplicaIdToRead)
            {
                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                try
                {
                    reply = replicaIdToClient.GetValueOrDefault(replicaId).ReadStorage(new ReadStorageRequest
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
                    Console.WriteLine("Replica with id " + replicaId + " crashed while performing read operation.");
                    replicaIdToClient.Remove(replicaId);
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

        private List<int> GetOrderOfReplicasToReadFrom(string recordId)
        {
            byte[] encodedRecordId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(recordId));
            int hashedRecordId = BitConverter.ToInt32(encodedRecordId, 0) % replicaIdToClient.Count;

            List<int> replicaIdOrdered = new List<int>();

            int currentReplicaId = (hashedRecordId + 1) % replicaIdToClient.Count;

            int amountOfReplicasToReadFrom = (int)(replicaIdToClient.Count * metaRecord.replicationFactor);

            for (int replicaCounter = 0; replicaCounter <= amountOfReplicasToReadFrom; replicaCounter++, currentReplicaId++)
            {
                currentReplicaId %= replicaIdToClient.Count;

                replicaIdOrdered.Add(currentReplicaId);
            }

            return replicaIdOrdered;
        }

        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            byte[] encodedRecordId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(r.Id));
            int hashedRecordId = BitConverter.ToInt32(encodedRecordId, 0) % replicaIdToClient.Count;

            int currentReplicaId = (hashedRecordId + 1) % replicaIdToClient.Count;
            int initialNumberOfStorages = replicaIdToClient.Count;

            WriteStorageReply reply = null;

            for(int attemptedReplicas = 0; attemptedReplicas <= initialNumberOfStorages; currentReplicaId++, attemptedReplicas++)
            {
                currentReplicaId %= initialNumberOfStorages;

                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                try
                {
                    StorageService.StorageServiceClient client = replicaIdToClient.GetValueOrDefault(currentReplicaId);
                    if (client == null)
                        continue;

                    reply = client.WriteStorage(new WriteStorageRequest
                    {
                        Id = r.Id,
                        Val = r.Val,
                    });
                }
                catch (Grpc.Core.RpcException)
                {
                    Console.WriteLine("Replica with id " + currentReplicaId + " crashed while performing write operation.");
                    replicaIdToClient.Remove(currentReplicaId);
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
            byte[] encodedRecordId = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(r.Id));
            int hashedRecordId = BitConverter.ToInt32(encodedRecordId, 0) % replicaIdToClient.Count;

            int currentReplicaId = (hashedRecordId + 1) % replicaIdToClient.Count;
            int initialNumberOfStorages = replicaIdToClient.Count;

            UpdateIfReply reply = null;

            for (int attemptedReplicas = 0; attemptedReplicas <= initialNumberOfStorages; currentReplicaId++, attemptedReplicas++)
            {
                currentReplicaId %= initialNumberOfStorages;

                //First check if the desired replica has the desired version. If not, try again with the next replicas. If no replica has the desired version, then it has already been
                //garbage-collected (maxVersions) or replica crashed. In that case, the app should terminate (no more workers in the chain will execute the remaining operators of the app).

                try
                {
                    StorageService.StorageServiceClient client = replicaIdToClient.GetValueOrDefault(currentReplicaId);
                    if (client == null)
                        continue;

                    reply = client.UpdateIf(new UpdateIfRequest
                    {
                        Id = r.Id,
                        NewValue = r.Newvalue,
                        OldValue = r.Oldvalue
                    });
                }
                catch (Grpc.Core.RpcException)
                {
                    Console.WriteLine("Replica with id " + currentReplicaId + " crashed while performing updateIf operation.");
                    replicaIdToClient.Remove(currentReplicaId);
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
