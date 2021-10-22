using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler
{
    class SchedulerServer : SchedulerService.SchedulerServiceBase
    {
        
        public override Task<StartAppReply> StartApp(StartAppRequest request, ServerCallContext context)
        {
            return Task.FromResult<StartAppReply>(StartAppImpl(request));
        }

        public StartAppReply StartAppImpl(StartAppRequest request)
        {
            //TODO Logic

            return new StartAppReply();
        }
    }
}
