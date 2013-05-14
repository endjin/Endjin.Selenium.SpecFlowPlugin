namespace Endjin.Selenium.SpecFlowPlugin
{
    using System;
    using System.Configuration;
    using System.Reflection;

    public class CustomConfigResolver
    {
        private Assembly configurationDefiningAssembly;

        public CustomConfigResolver()
        {
        }

        public TConfig GetCustomConfig<TConfig>(
            string configDefiningAssemblyPath, string configFilePath, string sectionName)
            where TConfig : ConfigurationSection
        {
            AppDomain.CurrentDomain.AssemblyResolve += new
                ResolveEventHandler(ConfigResolveEventHandler);
            configurationDefiningAssembly = Assembly.LoadFrom(configDefiningAssemblyPath);

            var exeFileMap = new ExeConfigurationFileMap {ExeConfigFilename = configFilePath};
            var customConfig = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap,
                                                                               ConfigurationUserLevel.None);
            var returnConfig = customConfig.GetSection(sectionName) as TConfig;

            AppDomain.CurrentDomain.AssemblyResolve -= ConfigResolveEventHandler;

            return returnConfig;
        }

        public Assembly ConfigResolveEventHandler(object sender, ResolveEventArgs args)
        {
            return configurationDefiningAssembly;
        }
    }
}