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
            return Task.FromResult<SendNodesURLReply>(domain.SendNodesURL(request));
        }

        public override Task<StartAppReply> StartApp(StartAppRequest request, ServerCallContext context)
        {
            return Task.FromResult<StartAppReply>(domain.StartApp(request));
        }

        public override Task<StatusReply> Status(StatusRequest request, ServerCallContext context)
        {
            return Task.FromResult<StatusReply>(domain.Status()); 
        }

        public override Task<ListReply> List(ListRequest request, ServerCallContext context)
        {
            return Task.FromResult<ListReply>(domain.ListObjects());
        }
    }
}
