using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Worker;

namespace Scheduler
{
    class SchedulerServer : SchedulerService.SchedulerServiceBase
    {

        SchedulerDomain domain = new SchedulerDomain();

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
    }
}
