﻿using System;
using System.Runtime.Serialization;

namespace WindowsService.Models
{
    [DataContract]
    public class Record
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int objectId { get; set; }
        [DataMember]
        public int versionNum { get; set; }
        public byte[] content { get; set; }
        [DataMember]
        public string fileName { get; set; }
        [DataMember]
        public int workTypeId { get; set; }

        public byte[] recognizedContent { get; set; }

        public override string ToString()
        {
            return "Record ID: " +this.ID+ " "+ "Object ID: " + this.objectId + ".";
        }
    }
}
