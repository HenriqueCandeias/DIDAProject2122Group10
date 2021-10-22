﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;
using Grpc.Net.Client;

namespace Storage
{
    public class StorageImpl : DIDAStorage.IDIDAStorage
    {
        public Dictionary<string, DIDARecord> valueStorage;

        public StorageImpl()
        {
            valueStorage = new Dictionary<string, DIDARecord>();
        }

        public DIDARecord read(string id, DIDAVersion version)
        {
            return valueStorage[id];
        }

        public DIDAVersion updateIfValueIs(string id, string oldvalue, string newvalue)
        {
            DIDARecord record = valueStorage[id];
            
            if (record.val.Equals(oldvalue))
            {
                record.val = newvalue;
                //TODO Add version incrementer

                valueStorage[id] = record;
            }

            return record.version;
        }

        public DIDAVersion write(string id, string val)
        {
            DIDARecord record = valueStorage[id];

            record.val = val;
            //TODO Add version incrementer

            valueStorage[id] = record;

            return record.version;

        }
    }


    //This is acts as a server for worker
    public class Storage : WorkerService.WorkerServiceBase
    {

        public StorageImpl Mstorage;

        public Storage()
        {
            Mstorage = new StorageImpl();
        }

        public override Task<updateIfReply> updateIf(updateIfRequest request, ServerCallContext context)
        {
            return Task.FromResult<updateIfReply>(updateIfImpl(request));
        }

        private updateIfReply updateIfImpl(updateIfRequest request)
        {
            DIDAVersion reply = Mstorage.updateIfValueIs(request.Id, request.OldValue, request.NewValue);

            return new updateIfReply
            {
                DidaVersion = new didaVersion
                {
                    VersionNumber = reply.versionNumber,
                    ReplicaId = reply.replicaId,
                },
            };

        }

        public override Task<writeStorageReply> writeStorage(writeStorageRequest request, ServerCallContext context)
        {
            return Task.FromResult<writeStorageReply>(writeStorageImpl(request));
        }

        private writeStorageReply writeStorageImpl(writeStorageRequest request)
        {
            DIDAVersion reply = Mstorage.write(request.Id, request.Val);

            return new writeStorageReply
            {
                DidaVersion = new didaVersion
                {
                    VersionNumber = reply.versionNumber,
                    ReplicaId = reply.replicaId,
                },
            };
        }

        public override Task<readStorageReply> readStorage(readStorageRequest request, ServerCallContext context)
        {
            return Task.FromResult<readStorageReply>(readStorageImpl(request));
        }

        private readStorageReply readStorageImpl(readStorageRequest request)
        {
            DIDAVersion didaversion = new DIDAVersion();

            didaversion.replicaId = request.DidaVersion.ReplicaId;
            didaversion.versionNumber = request.DidaVersion.VersionNumber;

            DIDARecord reply = Mstorage.read(request.Id, didaversion);

            return new readStorageReply
            {
                DidaRecord = new didaRecord
                {
                    Id = reply.id,
                    DidaVersion = new didaVersion
                    {
                        VersionNumber = reply.version.versionNumber,
                        ReplicaId = reply.version.replicaId,
                    },
                    Val = reply.val
                }                                
            };
                
        }

        public override Task<pingWSReply> pingWS(pingWSRequest request, ServerCallContext context)
        {
            return Task.FromResult<pingWSReply>(pingImpl(request));
        }

        private pingWSReply pingImpl(pingWSRequest request)
        {
            return new pingWSReply
            {
                Ok = 1,
            };
        }
    }
    class MainStorage
    {
        static void Main(string[] args)
        {
            //int Port = Int32.Parse(args[2]);
            int Port = 11111; //for testing

            Console.WriteLine("Starting Server on Port: " + Port);

            Server server = new Server
            {
                Services = { WorkerService.BindService(new Storage()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) },
            };

            server.Start();

            Console.WriteLine("Started Server on Port: " + Port);

            Console.WriteLine("Press any key to stop the server Storage...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
