using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Pcs;

namespace PCS
{
    class PCSServer : PCSService.PCSServiceBase
    {
        PCSDomain domain = new PCSDomain();
        public override Task<StartSchedulerReply> StartScheduler(StartSchedulerRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<StartSchedulerReply>(domain.StartScheduler(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<StartWorkerReply> StartWorker(StartWorkerRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<StartWorkerReply>(domain.StartWorker(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }

        public override Task<StartStorageReply> StartStorage(StartStorageRequest request, ServerCallContext context)
        {
            try
            {
                return Task.FromResult<StartStorageReply>(domain.StartStorage(request));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }
    }
}
