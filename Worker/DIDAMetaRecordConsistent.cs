using System;
using System.Collections.Generic;
using System.Text;

namespace DIDAWorker
{
    public class DIDAMetaRecordConsistent : DIDAMetaRecord
    {
        public Dictionary<string, DIDAVersion> RecordIdToConsistentVersion = new Dictionary<string, DIDAVersion>();

        public bool appIsInconsistent = false;

        public float replicationFactor = 0;

        public List<int> failedReplicasIds = new List<int>();
    } 
}
