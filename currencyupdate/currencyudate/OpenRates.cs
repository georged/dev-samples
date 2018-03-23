using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AlexaCRM.Currency
{
    [DataContract]
    public class OpenRates
    {
        [DataMember]
        public string disclaimer { get; set; }

        [DataMember]
        public string license { get; set; }

        [DataMember]
        public int timestamp { get; set; }
        public DateTime stamp
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
                    .AddSeconds(timestamp);
            }
        }

        [DataMember(Name = "base")]
        public string basecode { get; set; }

        [DataMember]
        public Dictionary<string, decimal> rates;

        internal static OpenRates GetRates()
        {
            string url = "https://openexchangerates.org/api/latest.json?app_id=f000b7225ed4413c9c3b911b0ed98b36";
            string rates;
            using (WebClient wc = new WebClient())
            {
                rates = wc.DownloadString(url);
            }

            if (!string.IsNullOrWhiteSpace(rates))
            {
                using (Stream str = new MemoryStream(Encoding.UTF8.GetBytes(rates)))
                {
                    var deserializer = new DataContractJsonSerializer(
                        typeof(OpenRates),
                        new DataContractJsonSerializerSettings()
                        {
                            UseSimpleDictionaryFormat = true
                        });

                    return (OpenRates)deserializer.ReadObject(str);
                }
            }
            return null;
        }
    }
}

