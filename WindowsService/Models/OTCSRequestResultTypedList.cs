using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;


namespace WindowsService.Models
{

    [DataContract]
    public class OTCSRequestResultTypedList<T>
    {
        public OTCSRequestResultTypedList()
        {
            this.value = new List<T>();
        }

        [DataMember]
        public bool ok { get; set; }
        [DataMember]
        public List<T> value { get; set; }
        [DataMember]
        public string errMsg { get; set; }

        public OTCSRequestResultTypedList<T> setError(string errorText)
        {
            this.ok = false;
            this.errMsg = this.errMsg + " " + errorText;
            return this;
        }
    }
}
