using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Worker
{
    class WorkerServer : WorkerService.WorkerServiceBase
    {
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
    }
}
