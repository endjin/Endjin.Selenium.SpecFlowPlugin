namespace Endjin.Selenium.SpecFlowPlugin
{
    #region Using Directives

    using System.CodeDom;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Endjin.Selenium.SpecFlowPlugin.Configuration;
    using TechTalk.SpecFlow.Generator;
    using TechTalk.SpecFlow.Generator.UnitTestProvider;
    using TechTalk.SpecFlow.Utils;
    using TechTalk.SpecFlow.Generator.Interfaces;

    #endregion 

    public class SeleniumNUnitTestGeneratorProvider : IUnitTestGeneratorProvider
    {
        private const string TestfixtureAttr = "NUnit.Framework.TestFixtureAttribute";
        private const string TestAttr = "NUnit.Framework.TestAttribute";
        private const string RowAttr = "NUnit.Framework.TestCaseAttribute";
        private const string CategoryAttr = "NUnit.Framework.CategoryAttribute";
        private const string TestsetupAttr = "NUnit.Framework.SetUpAttribute";
        private const string TestfixturesetupAttr = "NUnit.Framework.TestFixtureSetUpAttribute";
        private const string TestfixtureteardownAttr = "NUnit.Framework.TestFixtureTearDownAttribute";
        private const string TestteardownAttr = "NUnit.Framework.TearDownAttribute";
        private const string IgnoreAttr = "NUnit.Framework.IgnoreAttribute";
        private const string DescriptionAttr = "NUnit.Framework.DescriptionAttribute";

        private readonly CodeDomHelper codeDomHelper;
        private readonly ProjectSettings projectSettings;

        private bool scenarioSetupMethodsAdded;
        private bool enableSauceLabs;
        private SauceLabsSection sauceLabSettings;

        public SeleniumNUnitTestGeneratorProvider(CodeDomHelper codeDomHelper, ProjectSettings projectSettings)
        {
            this.codeDomHelper = codeDomHelper;
            this.projectSettings = projectSettings;
        }

        public bool SupportsRowTests
        {
            get { return true; }
        }

        public bool SupportsAsyncTests
        {
            get { return false; }
        }

        public void SetTestMethodCategories(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> scenarioCategories)
        {
            this.codeDomHelper.AddAttributeForEachValue(testMethod, CategoryAttr, scenarioCategories);
        }

        public void SetRow(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> arguments, IEnumerable<string> tags, bool isIgnored)
        {
            var args = arguments.Select(arg => new CodeAttributeArgument(new CodePrimitiveExpression(arg))).ToList();

            // addressing ReSharper bug: TestCase attribute with empty string[] param causes inconclusive result - https://github.com/techtalk/SpecFlow/issues/116
            var exampleTagExpressionList = tags.Select(t => new CodePrimitiveExpression(t)).ToArray();
            
            CodeExpression exampleTagsExpression = exampleTagExpressionList.Length == 0 ? (CodeExpression)new CodePrimitiveExpression(null) : new CodeArrayCreateExpression(typeof(string[]), exampleTagExpressionList);
            
            args.Add(new CodeAttributeArgument(exampleTagsExpression));

            if (isIgnored)
            {
                args.Add(new CodeAttributeArgument("Ignored", new CodePrimitiveExpression(true)));
            }

            if (this.enableSauceLabs)
            {
                this.SetSauceAttributes(testMethod, args);
            }
            else
            {
                this.codeDomHelper.AddAttribute(testMethod, RowAttr, args.ToArray());
            }
        }

        private void SetSauceAttributes(CodeMemberMethod testMethod, List<CodeAttributeArgument> args = null)
        {
            var argsString = string.Empty;

            if (args != null && args.Any())
            {
                argsString = " with: " + string.Concat(args.Take(args.Count - 1).Select(arg => string.Format("\"{0}\" ,", ((CodePrimitiveExpression)arg.Value).Value)));
                argsString = argsString.TrimEnd(' ', ',');
            }

            foreach (var codeAttributeDeclaration in testMethod.CustomAttributes.Cast<CodeAttributeDeclaration>().Where(attr => attr.Name == RowAttr && attr.Arguments.Count == 3).ToList())
            {
                testMethod.CustomAttributes.Remove(codeAttributeDeclaration);
            }

            foreach (CapabilityElement capability in this.sauceLabSettings.Capabilities)
            {
                var testName = string.Format("{0} on {1} version {2} on {3}{4}", testMethod.Name, capability.Browser, capability.BrowserVersion, capability.Platform, argsString);

                var withBrowserArgs = new[]
                    {
                        new CodeAttributeArgument(new CodePrimitiveExpression(capability.Browser)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(capability.BrowserVersion)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(capability.Platform)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(this.sauceLabSettings.Credentials.Url)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(testName))
                    }
                .Concat(args ?? new List<CodeAttributeArgument>())
                .Concat(new[] 
                    {
                        //new CodeAttributeArgument("Category", new CodePrimitiveExpression(capability.Browser)),
                        new CodeAttributeArgument("TestName", new CodePrimitiveExpression(testName))
                    })
                .ToArray();

                this.codeDomHelper.AddAttribute(testMethod, RowAttr, withBrowserArgs);
            }

            testMethod.Statements.Insert(0, new CodeSnippetStatement("            InitializeSeleniumSauce(browser, version, platform, name, url);"));
            testMethod.Parameters.Insert(0, new CodeParameterDeclarationExpression("System.string", "browser"));
            testMethod.Parameters.Insert(1, new CodeParameterDeclarationExpression("System.string", "version"));
            testMethod.Parameters.Insert(2, new CodeParameterDeclarationExpression("System.string", "platform"));
            testMethod.Parameters.Insert(3, new CodeParameterDeclarationExpression("System.string", "url"));
            testMethod.Parameters.Insert(4, new CodeParameterDeclarationExpression("System.string", "testName"));
        }

        public void SetTestClass(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, string featureTitle, string featureDescription)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClass, TestfixtureAttr);
            this.codeDomHelper.AddAttribute(generationContext.TestClass, DescriptionAttr, featureTitle);

            this.enableSauceLabs = generationContext.Feature.Tags.Any(x => x.Name == "EnableSauceLabs");
            
            if (this.enableSauceLabs)
            {
                var configuration = this.GetConfiguration();

                this.sauceLabSettings = configuration.GetSection("sauceLabsSection") as SauceLabsSection;

                if (this.sauceLabSettings == null)
                {
                    Debug.WriteLine("Configuration Section 'sauceLabsSection' could not be found.");
                }

                generationContext.TestClass.Members.Add(new CodeMemberField("OpenQA.Selenium.IWebDriver", "driver"));

                if (!this.scenarioSetupMethodsAdded)
                {
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("            if(this.driver != null)"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("                ScenarioContext.Current.Add(\"Driver\", this.driver);"));
                    this.scenarioSetupMethodsAdded = true;
                }

                CreateInitializeSeleniumMethod(generationContext);
                CreateInitializeSeleniumOverloadMethod(generationContext);

                CleanUpSeleniumContext(generationContext); 
            }
        }

        private System.Configuration.Configuration GetConfiguration()
        {
            var configPath = string.Empty;
            var configFileTemplate = @"{0}\{1}.config";

            var appConfig = string.Format(configFileTemplate, this.projectSettings.ProjectFolder, "App");
            var webConfig = string.Format(configFileTemplate, this.projectSettings.ProjectFolder, "Web");

            if (File.Exists(appConfig))
            {
                configPath = appConfig;
            }
            else if (File.Exists(webConfig))
            {
                configPath = webConfig;
            }

            var exeConfig = new ExeConfigurationFileMap
            {
                ExeConfigFilename = configPath
            };

            return ConfigurationManager.OpenMappedExeConfiguration(exeConfig, ConfigurationUserLevel.None);
        }

        public void SetTestClassCategories(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, IEnumerable<string> featureCategories)
        {
            this.codeDomHelper.AddAttributeForEachValue(generationContext.TestClass, CategoryAttr, featureCategories);
        }

        public void SetTestClassCleanupMethod(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClassCleanupMethod, TestfixtureteardownAttr);
        }

        public void SetTestClassIgnore(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClass, IgnoreAttr);
        }

        public void SetTestClassInitializeMethod(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClassInitializeMethod, TestfixturesetupAttr);
        }

        public void SetTestCleanupMethod(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestCleanupMethod, TestteardownAttr);
        }

        public void SetTestInitializeMethod(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestInitializeMethod, TestsetupAttr);
        }

        public void SetTestMethod(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle)
        {
            this.codeDomHelper.AddAttribute(testMethod, TestAttr);
            this.codeDomHelper.AddAttribute(testMethod, DescriptionAttr, scenarioTitle);

            if (this.enableSauceLabs)
            {
                this.SetSauceAttributes(testMethod);
            }
        }

        public void SetTestMethodIgnore(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod)
        {
            this.codeDomHelper.AddAttribute(testMethod, IgnoreAttr);
        }

        public void SetRowTest(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle)
        {
            this.SetTestMethod(generationContext, testMethod, scenarioTitle);

            if (this.enableSauceLabs)
            {
                this.SetSauceAttributes(testMethod);
            }
        }

        public void SetTestMethodAsRow(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle, string exampleSetName, string variantName, IEnumerable<KeyValuePair<string, string>> arguments)
        {
        }

        public void FinalizeTestClass(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
        }

        private static void CreateInitializeSeleniumMethod(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            var initializeSelenium = new CodeMemberMethod
            {
                Name = "InitializeSelenium"
            };
            
            initializeSelenium.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "browser"));
            initializeSelenium.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "url"));
            initializeSelenium.Statements.Add(new CodeSnippetStatement("            this.driver = new Endjin.Selenium.SpecFlowPlugin.RemoteWebDriver(url, browser);"));

            generationContext.TestClass.Members.Add(initializeSelenium);
        }

        private static void CreateInitializeSeleniumOverloadMethod(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            var initializeSelenium = new CodeMemberMethod
            {
                Name = "InitializeSeleniumSauce"
            };

            initializeSelenium.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "browser"));
            initializeSelenium.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "version"));
            initializeSelenium.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "platform"));
            initializeSelenium.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "testName"));
            initializeSelenium.Parameters.Add(new CodeParameterDeclarationExpression("System.String", "url"));
            initializeSelenium.Statements.Add(new CodeSnippetStatement("            this.driver = new Endjin.Selenium.SpecFlowPlugin.RemoteWebDriver(url, browser, version, platform, testName, true);"));

            generationContext.TestClass.Members.Add(initializeSelenium);
        }

        private static void CleanUpSeleniumContext(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            try { System.Threading.Thread.Sleep(50); this.driver.Quit(); } catch (System.Exception) {}"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            this.driver = null;"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            ScenarioContext.Current.Remove(\"Driver\");"));
        }
    }
}