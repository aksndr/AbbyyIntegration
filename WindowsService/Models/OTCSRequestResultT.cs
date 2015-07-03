using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace WindowsService.Models
{
    [DataContract]
    class OTCSRequestResultT<T>
    {
        public OTCSRequestResultT() {}

        [DataMember]
        public bool ok { get; set; }
        [DataMember]
        public T value { get; set; }
        [DataMember]
        public string errMsg { get; set; }

        public OTCSRequestResultT<T> setError(string errorText)
        {
            this.ok = false;
            this.errMsg = this.errMsg + " " + errorText;
            return this;
        }

    }
}
