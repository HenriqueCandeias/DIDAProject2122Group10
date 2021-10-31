﻿using DIDAStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Storage
{
    class StorageInterface
    {
        public StorageImpl mStorage = new StorageImpl();

        private Dictionary<string, string> workersIdToURL = new Dictionary<string, string>();

        private Dictionary<string, string> storagesIdToURL = new Dictionary<string, string>();

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
            DIDAVersion reply = mStorage.updateIfValueIs(request.Id, request.OldValue, request.NewValue);

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
            DIDAVersion reply = mStorage.write(request.Id, request.Val);

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
                versionNumber = request.DidaVersion.VersionNumber
            };

            DIDARecord reply = mStorage.read(request.Id, didaversion);

            return new ReadStorageReply
            {
                DidaRecord = new DidaRecord
                {
                    Id = reply.id,
                    DidaVersion = new DidaVersion
                    {
                        VersionNumber = reply.version.versionNumber,
                        ReplicaId = reply.version.replicaId,
                    },
                    Val = reply.val
                }
            };

        }

        public PingWSReply Ping(PingWSRequest request)
        {
            return new PingWSReply
            {
                Ok = 1,
            };
        }

        public StatusReply Status()
        {
            //TODO display necessary info
            return new StatusReply();
        }
    }
}