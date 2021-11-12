﻿using DIDAStorage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Storage
{
    public class StorageImpl : IDIDAStorage
    {
        public int replicaId;

        private const int maxVersions = 3;

        public ConcurrentDictionary<string, List<DIDARecord>> recordIdToRecords = new ConcurrentDictionary<string, List<DIDARecord>>();

        private List<LogStruct> log = new List<LogStruct>();

        private readonly object logLock = new object();

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

        public DIDAVersion updateIfValueIs(string id, string oldValue, string newValue, bool needLog)
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

                    if (needLog)
                    {
                        lock(logLock)
                        {
                            log.Add(new LogStruct
                            {
                                Id = id,
                                OldVal = oldValue,
                                NewVal = newValue,
                                DidaVersion = new DidaVersion
                                {
                                    VersionNumber = newVersion.versionNumber,
                                    ReplicaId = newVersion.replicaId,
                                },
                            });
                        }
                    }

                    return newVersion;
                }
            }

            return nullDIDAVersion;
        }

        public DIDAVersion write(string id, string val, bool needLog)
        {
            DIDARecord mostRecentRecord = GetMostRecentRecord(id);

            if (mostRecentRecord.Equals(nullDIDARecord))
                recordIdToRecords.TryAdd(id, new List<DIDARecord>());

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

            if (needLog)
            {
                lock(logLock)
                {
                    log.Add(new LogStruct
                    {
                        Id = id,
                        NewVal = val,
                        DidaVersion = new DidaVersion
                        {
                            VersionNumber = newVersion.versionNumber,
                            ReplicaId = newVersion.replicaId,
                        },
                    });
                }
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

        public List<LogStruct> GetLog(int clock)
        {
            List<LogStruct> lastLog = new List<LogStruct>();

            lock (logLock)
            {
                for (int i = clock; i < log.Count; i++)
                {
                    Console.WriteLine(i);
                    lastLog.Add(log[i]);
                }
            }
            
            return lastLog;
        }
    }
}