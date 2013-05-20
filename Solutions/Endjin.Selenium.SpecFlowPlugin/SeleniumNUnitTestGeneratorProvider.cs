namespace Endjin.Selenium.SpecFlowPlugin
{
    #region Using Directives

    using System;
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
        private ProjectSettings projectSettings;

        private bool scenarioSetupMethodsAdded;
        private bool enableSauceLabs;
        private SauceLabsSection sauceLabSettings;

        private readonly CustomConfigResolver customConfigResolver;

        public SeleniumNUnitTestGeneratorProvider(CodeDomHelper codeDomHelper, ProjectSettings projectSettings)
        {
            this.codeDomHelper = codeDomHelper;
            this.projectSettings = projectSettings;
            this.customConfigResolver = new CustomConfigResolver();
        }

        public bool SupportsRowTests
        {
            get { return true; }
        }

        public bool SupportsAsyncTests
        {
            get { return false; }
        }

        public void SetTestMethodCategories(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> scenarioCategories)
        {
            this.codeDomHelper.AddAttributeForEachValue(testMethod, CategoryAttr, scenarioCategories);
        }

        public void SetRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> arguments, IEnumerable<string> tags, bool isIgnored)
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

        public void SetTestClass(TestClassGenerationContext generationContext, string featureTitle, string featureDescription)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClass, TestfixtureAttr);
            this.codeDomHelper.AddAttribute(generationContext.TestClass, DescriptionAttr, featureTitle);

            if (generationContext.Feature.Tags != null)
            {
                this.enableSauceLabs = generationContext.Feature.Tags.Any(x => x.Name == "EnableSauceLabs");
            }

            if (this.enableSauceLabs)
            {
                this.sauceLabSettings = this.GetSauceLabsConfiguration();

                generationContext.TestClass.Members.Add(new CodeMemberField("Endjin.Selenium.SpecFlowPlugin.RemoteWebDriver", "driver"));
                generationContext.TestClass.Members.Add(new CodeMemberField("Endjin.Selenium.SpecFlowPlugin.SauceRest", "sauceRest"));

                CreateInitializeSeleniumMethod(generationContext);
                CreateInitializeSeleniumOverloadMethod(generationContext);

                CreateUpdateSauceLabsStatusMethod(generationContext);
                UpdateSauceLabsStatus(generationContext);
                CleanUpSeleniumContext(generationContext);
            }
        }

        public void SetTestClassCategories(TestClassGenerationContext generationContext, IEnumerable<string> featureCategories)
        {
            this.codeDomHelper.AddAttributeForEachValue(generationContext.TestClass, CategoryAttr, featureCategories);
        }

        public void SetTestClassCleanupMethod(TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClassCleanupMethod, TestfixtureteardownAttr);
        }

        public void SetTestClassIgnore(TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClass, IgnoreAttr);
        }

        public void SetTestClassInitializeMethod(TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClassInitializeMethod, TestfixturesetupAttr);
        }

        public void SetTestCleanupMethod(TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestCleanupMethod, TestteardownAttr);
        }

        public void SetTestInitializeMethod(TestClassGenerationContext generationContext)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestInitializeMethod, TestsetupAttr);

            if (this.enableSauceLabs)
            {
                if (!this.scenarioSetupMethodsAdded)
                {
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("            if(this.driver != null)"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("                ScenarioContext.Current.Add(\"Driver\", this.driver);"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement(string.Format("            this.sauceRest = new SauceRest(\"{0}\", \"{1}\", \"{2}\");", this.sauceLabSettings.Credentials.UserName, this.sauceLabSettings.Credentials.AccessKey, this.sauceLabSettings.Credentials.RestUrl)));
                    this.scenarioSetupMethodsAdded = true;
                } 
            }
        }

        public void SetTestMethod(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle)
        {
            this.SetTestMethod(generationContext, testMethod, scenarioTitle, false);
        }

        private void SetTestMethod(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle, bool isRowTest)
        {
            this.codeDomHelper.AddAttribute(testMethod, TestAttr);
            this.codeDomHelper.AddAttribute(testMethod, DescriptionAttr, scenarioTitle);

            if (this.enableSauceLabs && !isRowTest)
            {
                this.SetSauceAttributes(testMethod);
                this.SetInitializeSeleniumMethodInScenario(testMethod);
            }
        }

        public void SetTestMethodIgnore(TestClassGenerationContext generationContext, CodeMemberMethod testMethod)
        {
            this.codeDomHelper.AddAttribute(testMethod, IgnoreAttr);
        }

        public void SetRowTest(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle)
        {
            this.SetTestMethod(generationContext, testMethod, scenarioTitle, true);

            if (this.enableSauceLabs)
            {
                this.SetInitializeSeleniumMethodInScenario(testMethod);
            }
        }

        public void SetTestMethodAsRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle, string exampleSetName, string variantName, IEnumerable<KeyValuePair<string, string>> arguments)
        {
        }

        public void FinalizeTestClass(TestClassGenerationContext generationContext)
        {
            this.projectSettings = null;
            this.enableSauceLabs = false;
            this.scenarioSetupMethodsAdded = false;
            this.sauceLabSettings = null;
        }

        private static void CreateInitializeSeleniumMethod(TestClassGenerationContext generationContext)
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

        private static void CreateInitializeSeleniumOverloadMethod(TestClassGenerationContext generationContext)
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

        private static void CreateUpdateSauceLabsStatusMethod(TestClassGenerationContext generationContext)
        {
            var updateSauceLabsStatus = new CodeMemberMethod
            {
                Name = "UpdateSauceLabsStatus"
            };

            updateSauceLabsStatus.Parameters.Add(new CodeParameterDeclarationExpression("System.Boolean", "passed"));
            updateSauceLabsStatus.Statements.Add(new CodeSnippetStatement("             var jobId = this.driver.GetSessionId();"));
            updateSauceLabsStatus.Statements.Add(new CodeSnippetStatement("             if(passed) this.sauceRest.SetJobPassed(jobId);"));
            updateSauceLabsStatus.Statements.Add(new CodeSnippetStatement("             else this.sauceRest.SetJobFailed(jobId);"));

            generationContext.TestClass.Members.Add(updateSauceLabsStatus);
        }

        private static void CleanUpSeleniumContext(TestClassGenerationContext generationContext)
        {
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            try { System.Threading.Thread.Sleep(50); this.driver.Quit(); } catch (System.Exception) {}"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            this.driver = null;"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            ScenarioContext.Current.Remove(\"Driver\");"));
        }

        private static void UpdateSauceLabsStatus(TestClassGenerationContext generationContext)
        {
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            bool passed = true;"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            if (ScenarioContext.Current.TestError != null) { passed = false; }"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            UpdateSauceLabsStatus(passed);"));
        }

        private SauceLabsSection GetSauceLabsConfiguration()
        {
            var appConfigPath = string.Format(@"{0}\{1}.config", this.projectSettings.ProjectFolder, "App");

            if (!File.Exists(appConfigPath))
            {
                throw new FileNotFoundException(string.Format("Could not find a valid configuration file at location '{0}'", appConfigPath));
            }

            var configDefiningAssemblyPath = Assembly.GetAssembly(typeof(SeleniumNUnitTestGeneratorProvider)).Location;

            return this.customConfigResolver.GetCustomConfig<SauceLabsSection>(configDefiningAssemblyPath , appConfigPath, "sauceLabsSection");
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

            if (this.sauceLabSettings == null)
            {
                return;
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
                .Concat(new[] { new CodeAttributeArgument("TestName", new CodePrimitiveExpression(testName)) })
                .ToArray();

                this.codeDomHelper.AddAttribute(testMethod, RowAttr, withBrowserArgs);
            }

                
        }

        private void SetInitializeSeleniumMethodInScenario(CodeMemberMethod testMethod)
        {
            testMethod.Statements.Insert(0,
                                         new CodeSnippetStatement(
                                             "            InitializeSeleniumSauce(browser, version, platform, testName, url);"));
            testMethod.Parameters.Insert(0, new CodeParameterDeclarationExpression("System.string", "browser"));
            testMethod.Parameters.Insert(1, new CodeParameterDeclarationExpression("System.string", "version"));
            testMethod.Parameters.Insert(2, new CodeParameterDeclarationExpression("System.string", "platform"));
            testMethod.Parameters.Insert(3, new CodeParameterDeclarationExpression("System.string", "url"));
            testMethod.Parameters.Insert(4, new CodeParameterDeclarationExpression("System.string", "testName"));
        }
    }
}