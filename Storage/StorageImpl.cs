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

        private Dictionary<string, List<DIDARecord>> recordIdToRecords = new Dictionary<string, List<DIDARecord>>();

        private static readonly DIDARecord nullDIDARecord = new DIDARecord
        {
            id = "",
            version = nullDIDAVersion,
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
                    //Get the most recent version
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

                    recordIdToRecords.GetValueOrDefault(id).Add(new DIDARecord
                    {
                        id = id,
                        version = newVersion,
                        val = newValue,
                    });

                    if (recordIdToRecords.GetValueOrDefault(id).Count > maxVersions)
                        recordIdToRecords.GetValueOrDefault(id).RemoveAt(0);

                    return newVersion;
                }
            }

            return nullDIDAVersion;
        }

        public DIDAVersion write(string id, string val)
        {
            Console.WriteLine("Going to write a new record.");

            DIDARecord mostRecentRecord = GetMostRecentRecord(id);

            Console.WriteLine("Got the most recent record");

            if (!mostRecentRecord.Equals(nullDIDARecord))
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

            if (recordIdToRecords.GetValueOrDefault(id).Count > maxVersions)
                recordIdToRecords.GetValueOrDefault(id).RemoveAt(0);

            Console.WriteLine("Write - new record is:");
            Console.WriteLine(newRecord.ToString());

            return newVersion;
        }

        private DIDARecord GetMostRecentRecord(string id)
        {
            List<DIDARecord> didaRecords = recordIdToRecords.GetValueOrDefault(id);

            if (didaRecords == null)
                return nullDIDARecord;

            DIDARecord mostRecentRecord = new DIDARecord
            {
                version = new DIDAVersion
                {
                    versionNumber = 0,
                },
            };

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