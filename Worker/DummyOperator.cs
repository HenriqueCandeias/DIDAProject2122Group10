using System;
using System.Collections.Generic;
using System.Text;
using DIDAWorker;

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
            Console.WriteLine("Entered ProcessRecord");
            int num = 0;

            if (!String.IsNullOrEmpty(previousOperatorOutput))
                num = Int32.Parse(previousOperatorOutput);

            Console.WriteLine("num=" + num);

            string myOutput = (num + 1).ToString();

            Console.WriteLine("myoutput calculated: " + myOutput);

            return myOutput;
        }
    }
}
