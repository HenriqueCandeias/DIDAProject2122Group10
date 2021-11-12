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

        public StorageServer(int gossip_delay)
        {
            domain = new StorageInterface(gossip_delay);
        }

        public override Task<SendNodesURLReply> SendNodesURL(SendNodesURLRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<SendNodesURLReply>(domain.SendNodesURL(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<UpdateIfReply> UpdateIf(UpdateIfRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<UpdateIfReply>(domain.UpdateIf(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<WriteStorageReply> WriteStorage(WriteStorageRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<WriteStorageReply>(domain.WriteStorage(request));
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<ReadStorageReply> ReadStorage(ReadStorageRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<ReadStorageReply>(domain.ReadStorage(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<StatusReply> Status(StatusRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<StatusReply>(domain.Status(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<ListReply> List(ListRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<ListReply>(domain.ListObjects());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<CrashReply> Crash(CrashRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<CrashReply>(domain.Crash());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<PopulateReply> Populate(PopulateRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<PopulateReply>(domain.Populate(request));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<GossipReply> RequestLog(GossipRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<GossipReply>(domain.RequestLog(request));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<CrashRepReply> CrashReport(CrashRepRequests request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<CrashRepReply>(domain.CrashReport(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

    }
}