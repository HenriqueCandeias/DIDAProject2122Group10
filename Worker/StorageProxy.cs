using DIDAOperator;
using DIDAWorker;
using Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    // this is an example of what an implementation of a Worker's StorageProxy could be
    // Each request running on a specific worker node must have a separate instance of a Operator object
    // and a separate instance of the storage proxy
    public class StorageProxy : IDIDAStorage
    {
        // dictionary with storage gRPC client objects for all storage nodes
        Dictionary<string, StorageService.StorageServiceClient> _clients = new Dictionary<string, StorageService.StorageServiceClient>();

        // dictionary with storage gRPC channel objects for all storage nodes
        Dictionary<string, Grpc.Net.Client.GrpcChannel> _channels = new Dictionary<string, Grpc.Net.Client.GrpcChannel>();

        // metarecord for the request that this storage proxy is handling
        DIDAWorker.DIDAMetaRecord _meta;

        // The constructor of a storage proxy.
        // The storageNodes parameter lists the nodes that this storage proxy needs to be aware of to perform
        // read, write and updateIfValueIs operations.
        // The metaRecord identifies the request being processed by this storage proxy object
        // and allows the storage proxy to request data versions previously accessed by the request
        // and to inform operators running on the following (downstream) workers of the versions it accessed.
        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAWorker.DIDAMetaRecord metaRecord)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            foreach (DIDAStorageNode n in storageNodes)
            {
                _channels[n.serverId] = Grpc.Net.Client.GrpcChannel.ForAddress("http://" + n.host + ":" + n.port);
                _clients[n.serverId] = new StorageService.StorageServiceClient(_channels[n.serverId]);
            }
            _meta = metaRecord;
        }

        // THE FOLLOWING 3 METHODS ARE THE ESSENCE OF A STORAGE PROXY
        // IN THIS EXAMPLE THEY ARE JUST CALLING THE STORAGE 
        // IN THE COMLPETE IMPLEMENTATION THEY NEED TO:
        // 1) LOCATE THE RIGHT STORAGE SERVER
        // 2) DEAL WITH FAILED STORAGE SERVERS
        // 3) CHECK IN THE METARECORD WHICH ARE THE PREVIOUSLY READ VERSIONS OF DATA 
        // 4) RECORD ACCESSED DATA INTO THE METARECORD

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            var res = _clients["s1"].ReadStorage(new ReadStorageRequest { Id = r.Id, DidaVersion = new DidaVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
            return new DIDAWorker.DIDARecordReply { Id = "1", Val = "1", Version = { VersionNumber = 1, ReplicaId = 1 } };
        }

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            var res = _clients["s1"].WriteStorage(new WriteStorageRequest { Id = r.Id, Val = r.Val });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.DidaVersion.VersionNumber, ReplicaId = res.DidaVersion.ReplicaId };
        }

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            var res = _clients["s1"].UpdateIf(new UpdateIfRequest { Id = r.Id, NewValue = r.Newvalue, OldValue = r.Oldvalue });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.DidaVersion.VersionNumber, ReplicaId = res.DidaVersion.ReplicaId };
        }
    }
}
