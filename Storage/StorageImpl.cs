using DIDAStorage;
using System;
using System.Collections.Generic;
using System.Text;

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
}
