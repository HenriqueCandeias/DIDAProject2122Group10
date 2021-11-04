using DIDAOperator;
using DIDAWorker;
using Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Worker
{
    public class StorageProxy : IDIDAStorage
    {
        Dictionary<string, StorageService.StorageServiceClient> clients = new Dictionary<string, StorageService.StorageServiceClient>();

        Dictionary<string, Grpc.Net.Client.GrpcChannel> channels = new Dictionary<string, Grpc.Net.Client.GrpcChannel>();

        DIDAWorker.DIDAMetaRecord meta;

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
                Console.WriteLine("Going to create a client for the following node: serverId=" + n.serverId + " host=" + n.host + " port= " + n.port);
                channels[n.serverId] = Grpc.Net.Client.GrpcChannel.ForAddress(n.host + ":" + n.port);
                clients[n.serverId] = new StorageService.StorageServiceClient(channels[n.serverId]);
            }
            Console.WriteLine("Dictionary: " + clients.ToString());
            meta = metaRecord;
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
            var res = clients["s1"].ReadStorage(new ReadStorageRequest { Id = r.Id, DidaVersion = new DidaVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
            return new DIDAWorker.DIDARecordReply { Id = "1", Val = "1", Version = { VersionNumber = 1, ReplicaId = 1 } };
        }

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            Console.WriteLine("Entered proxy.write");
            var cenas = clients["s1"];
            Console.WriteLine("TEST");
            var res = clients["s1"].WriteStorage(new WriteStorageRequest { Id = r.Id, Val = r.Val });
            Console.WriteLine("Got this res: " + res.ToString());
            return new DIDAWorker.DIDAVersion { VersionNumber = res.DidaVersion.VersionNumber, ReplicaId = res.DidaVersion.ReplicaId };
        }

        // this dummy solution assumes there is a single storage server called "s1"
        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            var res = clients["s1"].UpdateIf(new UpdateIfRequest { Id = r.Id, NewValue = r.Newvalue, OldValue = r.Oldvalue });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.DidaVersion.VersionNumber, ReplicaId = res.DidaVersion.ReplicaId };
        }
    }
}
