using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsService.Models
{
    public class OTCSRequestResult
    {

        public OTCSRequestResult()
        {
            this.ok = false;            
        }

        public bool ok { get; set; }
        public object value { get; set; }
        public string errMsg { get; set; }

        public OTCSRequestResult setError(string errorText)
        {
            this.ok = false;
            this.errMsg = errorText;
            return this;
        }
    }
}
