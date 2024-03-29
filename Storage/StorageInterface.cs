﻿using DIDAStorage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Storage
{
    class StorageInterface
    {
        private int gossipDelay;

        public StorageImpl storageImpl;

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

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
        }

        public SendNodesURLReply SendNodesURL(SendNodesURLRequest request)
        {
            foreach (string key in request.Workers.Keys)
            {
                workersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));
                Console.WriteLine("Worker: " + key + " URL: " + workersIdToURL.GetValueOrDefault(key));
            }

            foreach (string key in request.Storages.Keys)
            {
                storagesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key + " URL: " + storagesIdToURL.GetValueOrDefault(key));
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
