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
            Console.WriteLine("Entered ProcessRecord");
            int num = 0;

            if (!String.IsNullOrEmpty(previousOperatorOutput))
                num = Int32.Parse(previousOperatorOutput);

            Console.WriteLine("num=" + num);

            string myOutput = (num + 1).ToString();

            Console.WriteLine("myoutput calculated: " + myOutput);

            Console.WriteLine("Going to write my output in the only storage.");

            DIDAVersion newVersion = storageProxy.write(new DIDAWriteRequest
            {
                Id = "myRecord",
                Val = myOutput,
            });

            Console.Write("Wrote my output successfully. Got this DIDAVersion:");
            Console.Write(newVersion.ToString());

            return myOutput;
        }
    }
}
