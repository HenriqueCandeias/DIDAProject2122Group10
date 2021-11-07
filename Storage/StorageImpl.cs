using DIDAStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Storage
{
    public class StorageImpl : DIDAStorage.IDIDAStorage
    {
        private int gossipDelay;

        private int replicaId;

        private const int maxVersions = 3;

        public Dictionary<string, List<DIDARecord>> recordIdToRecords = new Dictionary<string, List<DIDARecord>>();

        public static readonly DIDARecord nullDIDARecord = new DIDARecord
        {
            id = "",
            version = new DIDAVersion
            {
                versionNumber = -1,
                replicaId = -1,
            },
            val = "",
        };

        private static readonly DIDAVersion nullDIDAVersion = new DIDAVersion
        {
            versionNumber = -1,
            replicaId = -1,
        };

        public StorageImpl(int gossip_delay, int replica_id)
        {
            
            gossipDelay = gossip_delay;
            replicaId = replica_id;
        }

        public DIDARecord read(string id, DIDAVersion version)
        {
            List<DIDARecord> didaRecords = recordIdToRecords.GetValueOrDefault(id);

            if(didaRecords != null)
            {
                if(version.Equals(nullDIDAVersion))
                {
                    return GetMostRecentRecord(id);
                }

                else
                {
                    //Get the requested version
                    foreach (DIDARecord record in didaRecords)
                    {
                        if (record.version.versionNumber == version.versionNumber && record.version.replicaId == version.replicaId)
                            return record;
                    }
                }
            }

            return nullDIDARecord;
        }

        public DIDAVersion updateIfValueIs(string id, string oldValue, string newValue)
        {
            List<DIDARecord> didaRecords = recordIdToRecords.GetValueOrDefault(id);

            if (didaRecords != null)
            {
                DIDARecord mostRecentRecord = GetMostRecentRecord(id);

                if (mostRecentRecord.val == oldValue)
                {
                    DIDAVersion newVersion = new DIDAVersion
                    {
                        replicaId = replicaId,
                        versionNumber = mostRecentRecord.version.versionNumber + 1,
                    };

                    DIDARecord newRecord = new DIDARecord
                    {
                        id = id,
                        version = newVersion,
                        val = newValue,
                    };

                    recordIdToRecords.GetValueOrDefault(id).Add(newRecord);

                    Console.WriteLine(
                        "UpdateIfValueIs - new record is: ID: " + newRecord.id + " Version Number: " + newVersion.versionNumber +
                        " Replica ID: " + newVersion.replicaId + " Val: " + newRecord.val
                    );

                    if (recordIdToRecords.GetValueOrDefault(id).Count > maxVersions)
                    {
                        Console.WriteLine(
                            "Remove old record:" +
                            " ID: " + recordIdToRecords.GetValueOrDefault(id)[0].id +
                            " VersionNumber: " + recordIdToRecords.GetValueOrDefault(id)[0].version.versionNumber +
                            " ReplicaId: " + recordIdToRecords.GetValueOrDefault(id)[0].version.replicaId +
                            " Val: " + recordIdToRecords.GetValueOrDefault(id)[0].val
                        );
                        recordIdToRecords.GetValueOrDefault(id).RemoveAt(0);
                    }
                    return newVersion;
                }
            }

            return nullDIDAVersion;
        }

        public DIDAVersion write(string id, string val)
        {
            DIDARecord mostRecentRecord = GetMostRecentRecord(id);

            if (mostRecentRecord.Equals(nullDIDARecord))
                recordIdToRecords.Add(id, new List<DIDARecord>());

            DIDAVersion newVersion = new DIDAVersion
            {
                versionNumber = mostRecentRecord.version.versionNumber + 1,
                replicaId = replicaId,
            };

            DIDARecord newRecord = new DIDARecord
            {
                id = id,
                version = newVersion,
                val = val,
            };

            recordIdToRecords.GetValueOrDefault(id).Add(newRecord);

            Console.WriteLine(
                "Write - new record is: ID: " + newRecord.id + " Version Number: " + newVersion.versionNumber +
                " Replica ID: " + newVersion.replicaId + " Val: " + newRecord.val
            );

            if (recordIdToRecords.GetValueOrDefault(id).Count > maxVersions)
            {
                Console.WriteLine(
                    "Remove old record:" +
                    " ID: " + recordIdToRecords.GetValueOrDefault(id)[0].id +
                    " VersionNumber: " + recordIdToRecords.GetValueOrDefault(id)[0].version.versionNumber +
                    " ReplicaId: " + recordIdToRecords.GetValueOrDefault(id)[0].version.replicaId +
                    " Val: " + recordIdToRecords.GetValueOrDefault(id)[0].val
                );
                recordIdToRecords.GetValueOrDefault(id).RemoveAt(0);
            }

            return newVersion;
        }

        private DIDARecord GetMostRecentRecord(string id)
        {
            List<DIDARecord> didaRecords = recordIdToRecords.GetValueOrDefault(id);

            if (didaRecords == null)
                return nullDIDARecord;

            DIDARecord mostRecentRecord = didaRecords[0];

            foreach (DIDARecord record in didaRecords)
            {
                //IMPORTANT: It is assumed that two different replicas with records that have
                //the same id and the same versionNumber cannot have different values.
                //This might not be true.
                if (record.version.versionNumber > mostRecentRecord.version.versionNumber)
                    mostRecentRecord = record;
            }

            return mostRecentRecord;
        }
    }
}