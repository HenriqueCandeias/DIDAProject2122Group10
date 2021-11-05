using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Worker
{
    class WorkerServer : WorkerService.WorkerServiceBase
    {
        WorkerDomain domain;

        public WorkerServer(int gossip_delay, string puppet_master_URL)
        {
            domain = new WorkerDomain(gossip_delay, puppet_master_URL);
        }

        public override Task<SendNodesURLReply> SendNodesURL(SendNodesURLRequest request, ServerCallContext context)
        {
            try {
                return Task.FromResult<SendNodesURLReply>(domain.SendNodesURL(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<StartAppReply> StartApp(StartAppRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<StartAppReply>(domain.StartApp(request));
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
                return Task.FromResult<StatusReply>(domain.Status());
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
    }
}
