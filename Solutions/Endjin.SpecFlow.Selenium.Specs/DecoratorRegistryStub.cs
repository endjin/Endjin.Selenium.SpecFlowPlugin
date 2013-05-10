namespace Endjin.SpecFlow.Selenium.Specs
{
    #region Using Directives

    using System.CodeDom;
    using System.Collections.Generic;

    using TechTalk.SpecFlow.Generator;
    using TechTalk.SpecFlow.Generator.UnitTestConverter;
    using TechTalk.SpecFlow.Parser.SyntaxElements;

    #endregion 

    internal class DecoratorRegistryStub : IDecoratorRegistry
    {
        public void DecorateTestClass(TestClassGenerationContext generationContext, out List<string> unprocessedTags)
        {
            unprocessedTags = new List<string>();
        }

        public void DecorateTestMethod(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<Tag> tags, out List<string> unprocessedTags)
        {
            unprocessedTags = new List<string>();
        }
    }
}