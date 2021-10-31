using System;
using DIDAWorker;

namespace DIDAOperator {

    public class UpdateAndChainOperator : IDIDAOperator {
        IDIDAStorage _storageProxy;

        // this operator increments the storage record identified in the metadata record every time it is called.
        string IDIDAOperator.ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput) {
            var val = _storageProxy.updateIfValueIs(new DIDAUpdateIfRequest { Id = input, Newvalue = "success", Oldvalue = previousOperatorOutput });
            val = _storageProxy.updateIfValueIs(new DIDAUpdateIfRequest { Id = input, Newvalue = "failure", Oldvalue = previousOperatorOutput });

            Console.WriteLine("updated record:" + input );
            return "end";
        }

        void IDIDAOperator.ConfigureStorage(IDIDAStorage storageProxy) {
            _storageProxy = storageProxy;
        }
    }

    public class AddOperator : IDIDAOperator {
        IDIDAStorage _storageProxy;

        // this operator increments the storage record identified in the metadata record every time it is called.
        string IDIDAOperator.ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput) {
            Console.WriteLine("input string was: " + input);
            Console.Write("reading data record: " + input + " with value: ");
            var val = _storageProxy.read(new DIDAReadRequest { Id = input, Version = new DIDAVersion { VersionNumber = -1, ReplicaId = -1 } });
            string storedString = val.Val;
            Console.WriteLine(storedString);
            int requestCounter;
            string output;
            try {
                requestCounter = Int32.Parse(storedString);
                requestCounter++;
                output = requestCounter.ToString();
            } catch (Exception e) {
                output = "int_conversion_failed";
                Console.WriteLine(" operator expecting int but got chars: " + e.Message);
            }


            int oneAhead = Int32.Parse(input);
            oneAhead++;
            Console.Write("reading data record: " + oneAhead + " with value: ");
            _storageProxy.read(new DIDAReadRequest { Id = oneAhead.ToString(), Version = new DIDAVersion { VersionNumber = -1, ReplicaId = -1 } });
            storedString = val.Val;
            Console.WriteLine(storedString);


            _storageProxy.write(new DIDAWriteRequest { Id = input, Val = output });
            Console.WriteLine("writing data record:" + input + " with new value: " + output);
            return output;
        }

        void IDIDAOperator.ConfigureStorage(IDIDAStorage storageProxy) {
            _storageProxy = storageProxy;
            }
        }

}

public class IncrementOperator : IDIDAOperator {
    IDIDAStorage _storageProxy;

    public IncrementOperator() {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    // this operator increments the storage record identified in the metadata record every time it is called.
    string IDIDAOperator.ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput) {
        Console.WriteLine("input string was: " + input);
        Console.Write("reading data record: " + meta.Id + " with value: ");
        var val = _storageProxy.read(new DIDAReadRequest { Id = meta.Id.ToString(), Version = new DIDAVersion { VersionNumber = -1, ReplicaId = -1 } });
        string storedString = val.Val;
        Console.WriteLine(storedString);
        int requestCounter = Int32.Parse(storedString);

        requestCounter++;
        //requestCounter += Int32.Parse(previousOperatorOutput);

        _storageProxy.write(new DIDAWriteRequest { Id = meta.Id.ToString(), Val = requestCounter.ToString() });
        Console.WriteLine("writing data record:" + meta.Id + " with new value: " + requestCounter.ToString());
        return requestCounter.ToString();
    }

    void IDIDAOperator.ConfigureStorage(IDIDAStorage storageProxy) {
        _storageProxy = storageProxy;
    }
}
