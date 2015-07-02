using System;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

namespace WindowsService.Models
{
    [DataContract]
    class OTSettings
    {
        [DataMember]
        public string login { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public int otLogLevel { get; set; }
        [DataMember]
        public int barcodeStartPos { get; set; }
        [DataMember]
        public int barcodeEndPos { get; set; }
    }
}
