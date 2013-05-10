[assembly: TechTalk.SpecFlow.Infrastructure.RuntimePlugin(typeof(Endjin.SpecFlow.Selenium.RuntimePlugin))]

namespace Endjin.SpecFlow.Selenium
{
    using TechTalk.SpecFlow.Infrastructure;
    using TechTalk.SpecFlow.UnitTestProvider;

    public class RuntimePlugin : IRuntimePlugin
    {
        public void RegisterConfigurationDefaults(TechTalk.SpecFlow.Configuration.RuntimeConfiguration runtimeConfiguration)
        {
        }

        public void RegisterCustomizations(BoDi.ObjectContainer container, TechTalk.SpecFlow.Configuration.RuntimeConfiguration runtimeConfiguration)
        {
        }

        public void RegisterDependencies(BoDi.ObjectContainer container)
        {
            var runtimeProvider = new NUnitRuntimeProvider();

            container.RegisterInstanceAs<IUnitTestRuntimeProvider>(runtimeProvider, "SeleniumNUnit");
        }
    }
}