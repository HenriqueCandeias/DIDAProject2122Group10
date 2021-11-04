using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Storage
{
    class StorageServer : StorageService.StorageServiceBase
    {
        private StorageInterface domain;

        public StorageServer(int gossip_delay, int replica_id)
        {
            domain = new StorageInterface(gossip_delay, replica_id);
        }

        public override Task<SendNodesURLReply> SendNodesURL(SendNodesURLRequest request, ServerCallContext context)
        {
            return Task.FromResult<SendNodesURLReply>(domain.SendNodesURL(request));
        }

        public override Task<UpdateIfReply> UpdateIf(UpdateIfRequest request, ServerCallContext context)
        {
            return Task.FromResult<UpdateIfReply>(domain.UpdateIf(request));
        }

        public override Task<WriteStorageReply> WriteStorage(WriteStorageRequest request, ServerCallContext context)
        {
            Console.WriteLine("StorageServer.WriteStorage");
            try
            {
                return Task.FromResult<WriteStorageReply>(domain.WriteStorage(request));
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public override Task<ReadStorageReply> ReadStorage(ReadStorageRequest request, ServerCallContext context)
        {
            return Task.FromResult<ReadStorageReply>(domain.ReadStorage(request));
        }

        public override Task<StatusReply> Status(StatusRequest request, ServerCallContext context)
        {
            return Task.FromResult<StatusReply>(domain.Status());
        }

        public override Task<ListReply> List(ListRequest request, ServerCallContext context)
        {
            return Task.FromResult<ListReply>(domain.ListObjects());
        }

        public override Task<CrashReply> Crash(CrashRequest request, ServerCallContext context)
        {
            return Task.FromResult<CrashReply>(domain.Crash());
        }

    }
}