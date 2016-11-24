using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso_Bank.Models
{
    public class analyticsObject
    {

        public class Document
        {
            public double score { get; set; }
            public string id { get; set; }
        }

        public class RootObject
        {
            public List<Document> documents { get; set; }
            public List<object> errors { get; set; }
        }

    }
}