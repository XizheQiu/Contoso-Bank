using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace Contoso_Bank.DataModels
{
    public class xizhesContosoBank
    {
        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime createdAt { get; set; }

        [JsonProperty(PropertyName = "userName")]
        public string userName { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string password { get; set; }

        [JsonProperty(PropertyName = "savings")]
        public double savings { get; set; }

        [JsonProperty(PropertyName = "address")]
        public string address { get; set; }

        [JsonProperty(PropertyName = "phone")]
        public string phone { get; set; }
    }
}