namespace Endjin.SpecFlow.Selenium
{
    #region Using Directives

    using System.CodeDom;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;

    using Endjin.SpecFlow.Selenium.Configuration;

    using TechTalk.SpecFlow.Generator.UnitTestProvider;
    using TechTalk.SpecFlow.Utils;

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

        private bool scenarioSetupMethodsAdded;

        private bool sauce;

        private SauceLabsSection sauceLabSettings;

        public SeleniumNUnitTestGeneratorProvider(CodeDomHelper codeDomHelper)
        {
            this.codeDomHelper = codeDomHelper;
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
            this.codeDomHelper.AddAttributeForEachValue(testMethod, CategoryAttr, scenarioCategories.Where(cat => !cat.StartsWith("Browser:")));

            var sauceLabConfigTag = scenarioCategories.SingleOrDefault(x => x == "SauceLabConfig");

            if (sauceLabConfigTag != null)
            {
                this.sauceLabSettings = (SauceLabsSection)ConfigurationManager.GetSection("sauceLabsSection");
                this.sauce = true;
            }

            if (this.sauce)
            {
                foreach (CapabilityElement capability in this.sauceLabSettings.Capabilities)
                {
                    testMethod.UserData.Add("Browser:" + capability.Browser, capability.Browser);

                    var testName = string.Format("{0} on {1} version {2} on {3}", testMethod.Name, capability.Browser, capability.BrowserVersion, capability.Platform);

                    var withBrowserArgs = new[]
                    {
                        new CodeAttributeArgument(new CodePrimitiveExpression(capability.Browser)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(capability.BrowserVersion)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(capability.Platform)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(this.sauceLabSettings.Credentials.Url)),
                        new CodeAttributeArgument(new CodePrimitiveExpression(testName))
                    }
                    .Concat(new[] 
                    {
                        new CodeAttributeArgument("Category", new CodePrimitiveExpression(capability.Browser)),
                        new CodeAttributeArgument("TestName", new CodePrimitiveExpression(testName))
                    })
                    .ToArray();

                    this.codeDomHelper.AddAttribute(testMethod, RowAttr, withBrowserArgs);
                }

                if (!this.scenarioSetupMethodsAdded)
                {
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("            if(this.driver != null)"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("                ScenarioContext.Current.Add(\"Driver\", this.driver);"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("            if(this.container != null)"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("                ScenarioContext.Current.Add(\"Container\", this.container);"));
                    this.scenarioSetupMethodsAdded = true;
                }

                testMethod.Statements.Insert(0, new CodeSnippetStatement("            InitializeSeleniumSauce(browser, version, platform, name, url);"));
                testMethod.Parameters.Insert(0, new CodeParameterDeclarationExpression("System.string", "browser"));
                testMethod.Parameters.Insert(1, new CodeParameterDeclarationExpression("System.string", "version"));
                testMethod.Parameters.Insert(2, new CodeParameterDeclarationExpression("System.string", "platform"));
                testMethod.Parameters.Insert(3, new CodeParameterDeclarationExpression("System.string", "url"));
                testMethod.Parameters.Insert(4, new CodeParameterDeclarationExpression("System.string", "testName"));

                return;
            }
            
            bool hasBrowser = false;

            foreach (var browser in scenarioCategories.Where(cat => cat.StartsWith("Browser:")).Select(cat => cat.Replace("Browser:", string.Empty)))
            {
                testMethod.UserData.Add("Browser:" + browser, browser);

                var withBrowserArgs = new[] { new CodeAttributeArgument(new CodePrimitiveExpression(browser)) }
                        .Concat(new[] 
                        {
                            new CodeAttributeArgument("Category", new CodePrimitiveExpression(browser)),
                            new CodeAttributeArgument("TestName", new CodePrimitiveExpression(string.Format("{0} on {1}", testMethod.Name, browser)))
                        })
                        .ToArray();

                this.codeDomHelper.AddAttribute(testMethod, RowAttr, withBrowserArgs);

                hasBrowser = true;
            }

            if (hasBrowser)
            {
                if (!this.scenarioSetupMethodsAdded)
                {
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("            if(this.driver != null)"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("                ScenarioContext.Current.Add(\"Driver\", this.driver);"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("            if(this.container != null)"));
                    generationContext.ScenarioInitializeMethod.Statements.Add(new CodeSnippetStatement("                ScenarioContext.Current.Add(\"Container\", this.container);"));
                    this.scenarioSetupMethodsAdded = true;
                }
                
                testMethod.Statements.Insert(0, new CodeSnippetStatement("            InitializeSelenium(browser);"));
                testMethod.Parameters.Insert(0, new CodeParameterDeclarationExpression("System.string" , "browser"));
            }
        }

        public void SetRow(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> arguments, IEnumerable<string> tags, bool isIgnored)
        {
            var args = arguments.Select(
              arg => new CodeAttributeArgument(new CodePrimitiveExpression(arg))).ToList();

            // addressing ReSharper bug: TestCase attribute with empty string[] param causes inconclusive result - https://github.com/techtalk/SpecFlow/issues/116
            var exampleTagExpressionList = tags.Select(t => new CodePrimitiveExpression(t)).ToArray();
            
            CodeExpression exampleTagsExpression = exampleTagExpressionList.Length == 0 ? (CodeExpression)new CodePrimitiveExpression(null) : new CodeArrayCreateExpression(typeof(string[]), exampleTagExpressionList);
            
            args.Add(new CodeAttributeArgument(exampleTagsExpression));

            if (isIgnored)
            {
                args.Add(new CodeAttributeArgument("Ignored", new CodePrimitiveExpression(true)));
            }

            var browsers = testMethod.UserData.Keys.OfType<string>()
                .Where(key => key.StartsWith("Browser:"))
                .Select(key => (string)testMethod.UserData[key]).ToArray();

            if (browsers.Any())
            {
                foreach (var codeAttributeDeclaration in testMethod.CustomAttributes.Cast<CodeAttributeDeclaration>().Where(attr => attr.Name == RowAttr && attr.Arguments.Count == 3).ToList())
                {
                    testMethod.CustomAttributes.Remove(codeAttributeDeclaration);
                }

                foreach (var browser in browsers)
                {
                    var argsString = string.Concat(args.Take(args.Count - 1).Select(arg => string.Format("\"{0}\" ,", ((CodePrimitiveExpression)arg.Value).Value)));
                    argsString = argsString.TrimEnd(' ', ',');

                    var withBrowserArgs = new[] { new CodeAttributeArgument(new CodePrimitiveExpression(browser)) }
                        .Concat(args)
                        .Concat(new[] 
                        {
                            new CodeAttributeArgument("Category", new CodePrimitiveExpression(browser)),
                            new CodeAttributeArgument("TestName", new CodePrimitiveExpression(string.Format("{0} on {1} with: {2}", testMethod.Name, browser, argsString)))
                        })
                        .ToArray();

                    this.codeDomHelper.AddAttribute(testMethod, RowAttr, withBrowserArgs);
                }
            }
            else
            {
                this.codeDomHelper.AddAttribute(testMethod, RowAttr, args.ToArray());
            }
        }

        public void SetTestClass(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, string featureTitle, string featureDescription)
        {
            this.codeDomHelper.AddAttribute(generationContext.TestClass, TestfixtureAttr);
            this.codeDomHelper.AddAttribute(generationContext.TestClass, DescriptionAttr, featureTitle);

            generationContext.Namespace.Imports.Add(new CodeNamespaceImport("Autofac"));
            generationContext.Namespace.Imports.Add(new CodeNamespaceImport("Autofac.Configuration"));
            generationContext.TestClass.Members.Add(new CodeMemberField("OpenQA.Selenium.IWebDriver", "driver"));
            generationContext.TestClass.Members.Add(new CodeMemberField("IContainer", "container"));

            CreateInitializeSeleniumMethod(generationContext);
            CreateInitializeSeleniumOverloadMethod(generationContext);

            CleanUpSeleniumContext(generationContext);
        }

        public void SetTestClassCategories(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, IEnumerable<string> featureCategories)
        {
            this.codeDomHelper.AddAttributeForEachValue(generationContext.TestClass, CategoryAttr, featureCategories);
            
            var sauceLabConfigTag = featureCategories.SingleOrDefault(x => x == "SauceLabConfig");

            if (sauceLabConfigTag != null)
            {
                this.sauceLabSettings = (SauceLabsSection)ConfigurationManager.GetSection("sauceLabsSection");
                this.sauce = true;
            }
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

            generationContext.TestClassInitializeMethod.Statements.Add(new CodeSnippetStatement("            var builder = new ContainerBuilder();"));
            generationContext.TestClassInitializeMethod.Statements.Add(new CodeSnippetStatement("            builder.RegisterModule(new ConfigurationSettingsReader());"));
            generationContext.TestClassInitializeMethod.Statements.Add(new CodeSnippetStatement("            this.container = builder.Build();"));
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
        }

        public void SetTestMethodIgnore(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod)
        {
            this.codeDomHelper.AddAttribute(testMethod, IgnoreAttr);
        }

        public void SetRowTest(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle)
        {
            this.SetTestMethod(generationContext, testMethod, scenarioTitle);
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
            initializeSelenium.Statements.Add(new CodeSnippetStatement("            this.driver = this.container.ResolveNamed<OpenQA.Selenium.IWebDriver>(browser);"));

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
            initializeSelenium.Statements.Add(new CodeSnippetStatement("            this.driver = new Baseclass.Contrib.SpecFlow.Selenium.NUnit.RemoteWebDriver(url, browser, version, platform, testName, true);"));

            generationContext.TestClass.Members.Add(initializeSelenium);
        }

        private static void CleanUpSeleniumContext(TechTalk.SpecFlow.Generator.TestClassGenerationContext generationContext)
        {
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            try { System.Threading.Thread.Sleep(50); this.driver.Quit(); } catch (System.Exception) {}"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            this.driver = null;"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            ScenarioContext.Current.Remove(\"Driver\");"));
            generationContext.ScenarioCleanupMethod.Statements.Add(new CodeSnippetStatement("            ScenarioContext.Current.Remove(\"Container\");"));
        }
    }
}