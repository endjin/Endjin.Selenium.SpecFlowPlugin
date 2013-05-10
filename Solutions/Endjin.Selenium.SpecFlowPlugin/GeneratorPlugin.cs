using Endjin.Selenium.SpecFlowPlugin;

[assembly: TechTalk.SpecFlow.Infrastructure.GeneratorPlugin(typeof(GeneratorPlugin))]

namespace Endjin.Selenium.SpecFlowPlugin
{
    using TechTalk.SpecFlow.Generator.Interfaces;
    using TechTalk.SpecFlow.Generator.Plugins;
    using TechTalk.SpecFlow.Generator.UnitTestProvider;
    using TechTalk.SpecFlow.Utils;

    public class GeneratorPlugin : IGeneratorPlugin
    {
        public void RegisterConfigurationDefaults(TechTalk.SpecFlow.Generator.Configuration.SpecFlowProjectConfiguration specFlowConfiguration)
        {
        }

        public void RegisterCustomizations(BoDi.ObjectContainer container, TechTalk.SpecFlow.Generator.Configuration.SpecFlowProjectConfiguration generatorConfiguration)
        {
        }

        public void RegisterDependencies(BoDi.ObjectContainer container)
        {
            var projectSettings = container.Resolve<ProjectSettings>();

            var codeDomHelper = container.Resolve<CodeDomHelper>(projectSettings.ProjectPlatformSettings.Language);

            var generatorProvider = new SeleniumNUnitTestGeneratorProvider(codeDomHelper, projectSettings);
            
            container.RegisterInstanceAs<IUnitTestGeneratorProvider>(generatorProvider, "SeleniumNUnit");
        }
    }
}