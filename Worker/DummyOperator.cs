using System;
using System.Collections.Generic;
using System.Text;
using DIDAWorker;

namespace Worker
{
    class DummyOperator : IDIDAOperator
    {
        IDIDAStorage storageProxy;
        public void ConfigureStorage(IDIDAStorage storageProxy)
        {
            this.storageProxy = storageProxy;
        }

        public string ProcessRecord(DIDAWorker.DIDAMetaRecord meta, string input, string previousOperatorOutput)
        {
            string myOutput = (Int32.Parse(previousOperatorOutput) + 1).ToString();

            return myOutput;
        }
    }
}
