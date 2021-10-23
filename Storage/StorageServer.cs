using DIDAStorage;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Storage
{
    class StorageServer : StorageService.StorageServiceBase
    {
        public StorageImpl Mstorage = new StorageImpl();

        private Dictionary<int, string> WorkersIdToURL = new Dictionary<int, string>();

        private Dictionary<int, string> StoragesIdToURL = new Dictionary<int, string>();

        public override Task<SendNodesURLReply> SendNodesURL(SendNodesURLRequest request, ServerCallContext context)
        {
            return Task.FromResult<SendNodesURLReply>(SendNodesURLImpl(request));
        }

        public SendNodesURLReply SendNodesURLImpl(SendNodesURLRequest request)
        {
            foreach (int key in request.Workers.Keys)
            {
                WorkersIdToURL.Add(key, request.Workers.GetValueOrDefault(key));
                Console.WriteLine("Worker: " + key.ToString() + " URL: " + WorkersIdToURL.GetValueOrDefault(key));
            }

            foreach (int key in request.Storages.Keys)
            {
                StoragesIdToURL.Add(key, request.Storages.GetValueOrDefault(key));
                Console.WriteLine("Storage: " + key.ToString() + " URL: " + StoragesIdToURL.GetValueOrDefault(key));
            }

            return new SendNodesURLReply();
        }

        public override Task<UpdateIfReply> UpdateIf(UpdateIfRequest request, ServerCallContext context)
        {
            return Task.FromResult<UpdateIfReply>(UpdateIfImpl(request));
        }

        private UpdateIfReply UpdateIfImpl(UpdateIfRequest request)
        {
            DIDAVersion reply = Mstorage.updateIfValueIs(request.Id, request.OldValue, request.NewValue);

            return new UpdateIfReply
            {
                DidaVersion = new DidaVersion
                {
                    VersionNumber = reply.versionNumber,
                    ReplicaId = reply.replicaId,
                },
            };

        }

        public override Task<WriteStorageReply> WriteStorage(WriteStorageRequest request, ServerCallContext context)
        {
            return Task.FromResult<WriteStorageReply>(WriteStorageImpl(request));
        }

        private WriteStorageReply WriteStorageImpl(WriteStorageRequest request)
        {
            DIDAVersion reply = Mstorage.write(request.Id, request.Val);

            return new WriteStorageReply
            {
                DidaVersion = new DidaVersion
                {
                    VersionNumber = reply.versionNumber,
                    ReplicaId = reply.replicaId,
                },
            };
        }

        public override Task<ReadStorageReply> ReadStorage(ReadStorageRequest request, ServerCallContext context)
        {
            return Task.FromResult<ReadStorageReply>(ReadStorageImpl(request));
        }

        private ReadStorageReply ReadStorageImpl(ReadStorageRequest request)
        {
            DIDAVersion didaversion = new DIDAVersion();

            didaversion.replicaId = request.DidaVersion.ReplicaId;
            didaversion.versionNumber = request.DidaVersion.VersionNumber;

            DIDARecord reply = Mstorage.read(request.Id, didaversion);

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

        public override Task<PingWSReply> PingWS(PingWSRequest request, ServerCallContext context)
        {
            return Task.FromResult<PingWSReply>(PingImpl(request));
        }

        private PingWSReply PingImpl(PingWSRequest request)
        {
            return new PingWSReply
            {
                Ok = 1,
            };
        }
    }
}