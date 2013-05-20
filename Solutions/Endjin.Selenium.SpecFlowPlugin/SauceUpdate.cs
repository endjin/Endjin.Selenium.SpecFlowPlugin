namespace Endjin.Selenium.SpecFlowPlugin
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class SauceUpdate
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string name { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] tags { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string @public { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? passed { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? build { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> customData { get; set; }
    }
}