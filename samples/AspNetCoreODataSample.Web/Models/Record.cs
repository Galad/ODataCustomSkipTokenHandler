using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace AspNetCoreODataSample.Web.Models
{
    public class Record
    {
        [IgnoreDataMember]
        public int HiddenId { get; set; }
    }
}
