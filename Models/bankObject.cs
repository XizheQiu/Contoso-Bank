using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso_Bank.Models
{
    public class bankObject
    {
        public class RootObject
        {
            public string id { get; set; }
            public string createdAt { get; set; }
            public string updatedAt { get; set; }
            public string version { get; set; }
            public bool deleted { get; set; }
            public string userName { get; set; }
            public string password { get; set; }
            public double savings { get; set; }
            public string address { get; set; }
            public string phone { get; set; }
        }
    }
}