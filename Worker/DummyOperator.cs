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
                    VersionNumber = 3,
                    ReplicaId = 0,
                }
            });

            storageProxy.read(new DIDAReadRequest
            {
                Id = "noSuchRecord",
                Version = new DIDAVersion
                {
                    VersionNumber = 3,
                    ReplicaId = 0,
                }
            });

            storageProxy.updateIfValueIs(new DIDAUpdateIfRequest
            {
                Id = "myRecord",
                Oldvalue = "3",
                Newvalue = "5",
            });

            Console.WriteLine("DummyOperator: Wrote my output successfully.");

            return myOutput;
        }
    }
}
