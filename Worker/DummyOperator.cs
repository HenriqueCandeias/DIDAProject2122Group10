using System;
using System.Collections.Generic;
using System.Text;
using DIDAWorker;
using Storage;

namespace DIDAOperator
{
    class DummyOperator : IDIDAOperator
    {
        IDIDAStorage storageProxy;
        void IDIDAOperator.ConfigureStorage(IDIDAStorage storageProxy)
        {
            this.storageProxy = storageProxy;
        }

        string IDIDAOperator.ProcessRecord(DIDAWorker.DIDAMetaRecord meta, string input, string previousOperatorOutput)
        {
            int num = 0;

            if (!String.IsNullOrEmpty(previousOperatorOutput))
                num = Int32.Parse(previousOperatorOutput);

            string myOutput = (num + 1).ToString();

            //Storage Tests
            
            storageProxy.write(new DIDAWriteRequest
            {
                Id = "myRecord",
                Val = myOutput,
            });

            storageProxy.read(new DIDAReadRequest
            {
                Id = "myRecord",
                Version = new DIDAVersion
                {
                    VersionNumber = -1,
                    ReplicaId = -1,
                }
            });

            /*
            DIDARecordReply reply = storageProxy.read(new DIDAReadRequest
            {
                Id = "myRecord",
                Version = new DIDAVersion
                {
                    VersionNumber = -1,
                    ReplicaId = -1,
                }
            });

            if (!reply.Val.Equals(myOutput))
                Console.WriteLine("\r\nTEST FAILED\r\n");
            
            storageProxy.read(new DIDAReadRequest
            {
                Id = "myRecord",
                Version = new DIDAVersion
                {
                    VersionNumber = 3,
                    ReplicaId = 0,
                }
            });
            
            DIDAVersion newVersion = storageProxy.updateIfValueIs(new DIDAUpdateIfRequest
            {
                Id = "myRecord",
                Oldvalue = "2",
                Newvalue = "10",
            });

            if (newVersion.VersionNumber != -1)
            {
                Console.WriteLine("UpdateIf SUCCESS");
                myOutput = "10";
            }
            */

            Console.WriteLine("DummyOperator: Wrote my output successfully.");

            return myOutput;
        }
    }
}
