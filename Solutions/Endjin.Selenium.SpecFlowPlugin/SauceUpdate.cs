namespace Endjin.Selenium.SpecFlowPlugin
{
    using System.Collections.Generic;

    public class SauceUpdate
    {
        public string Name { get; set; }

        public string[] Tags { get; set; }

        public string Public { get; set; }

        public bool? Passed { get; set; }

        public int? Build { get; set; }

        public IDictionary<string, string> CustomData { get; set; }
    }
}