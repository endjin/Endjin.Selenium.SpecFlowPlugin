namespace Endjin.SpecFlow.Selenium
{
    using System;
    using System.Configuration;

    using OpenQA.Selenium;
    using OpenQA.Selenium.Remote;

    public class RemoteWebDriver : OpenQA.Selenium.Remote.RemoteWebDriver
    {
        public RemoteWebDriver(string url, string browser) : base(new Uri(url), GetCapabilities(browser))
        {
        }

        public RemoteWebDriver(string url, string browser, string version, string platform, string testName = "", bool sauceLabs = false) : base(new Uri(url), GetCapabilities(browser, version, platform, testName, sauceLabs))
        {
        }

        private static ICapabilities GetCapabilities(string browserName, string version, string platform, string testName = "", bool sauceLabs = false)
        {
            DesiredCapabilities capabilities;

            switch (browserName)
            {
                case "InternetExplorer":
                    capabilities = DesiredCapabilities.InternetExplorer();
                    break;
                case "Chrome":
                    capabilities = DesiredCapabilities.Chrome();
                    break;
                case "Firefox":
                    capabilities = DesiredCapabilities.Firefox();
                    break;
                default:
                    throw new InvalidOperationException(string.Format("{0} is not a valid browser type", browserName));
            }

            capabilities.SetCapability(CapabilityType.Version, version);
            capabilities.SetCapability(CapabilityType.Platform, platform);

            if (sauceLabs)
            {
                var userName = ConfigurationManager.AppSettings["sauceLabsUserName"];
                var accessKey = ConfigurationManager.AppSettings["sauceLabsAccessKey"];
                capabilities.SetCapability("username", userName);
                capabilities.SetCapability("accessKey", accessKey);
                capabilities.SetCapability("name", testName); 
            }

            return capabilities;
        }

        /// <summary>
        /// Uses reflection to create an instance of <see cref="DesiredCapabilities"/>
        /// </summary>
        /// <param name="browserName">
        /// Name of the browser to use for testing
        /// </param>
        /// <returns>
        /// Instance of DesiredCapabilities describing the browser
        /// </returns>
        private static DesiredCapabilities GetCapabilities(string browserName)
        {
            var capabilityCreationMethod = typeof(DesiredCapabilities).GetMethod(browserName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (capabilityCreationMethod == null)
            {
                throw new NotSupportedException("Can't find DesiredCapabilities with name " + browserName);
            }

            var capabilities = capabilityCreationMethod.Invoke(null, null) as DesiredCapabilities;

            if (capabilities == null)
            {
                throw new NotSupportedException("Can't find DesiredCapabilities with name " + browserName);
            }

            return capabilities;
        }
    }
}