﻿#region License

//-------------------------------------------------------------------------------------------------
// <auto-generated> 
// Marked as auto-generated so StyleCop will ignore BDD style tests
// </auto-generated>
//-------------------------------------------------------------------------------------------------



#pragma warning disable 169
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

#endregion

namespace Endjin.SpecFlow.Selenium.Specs
{
    #region Using Directives

    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    using Machine.Specifications;

    using TechTalk.SpecFlow.Generator;
    using TechTalk.SpecFlow.Generator.Configuration;
    using TechTalk.SpecFlow.Parser;
    using TechTalk.SpecFlow.Parser.SyntaxElements;
    using TechTalk.SpecFlow.Utils;

    #endregion

    [Subject(typeof(SeleniumNUnitTestGeneratorProvider))]
    public class when_the_selenium_nunit_test_generator_is_asked_to_generate_tests
    {
        private const string SampleFeatureFile = @"
            Feature: Sample feature file for a custom generator provider
            
            Scenario: Simple scenario
				Given there is something
				When I do something
				Then something should happen

            @mytag
			Scenario Outline: Simple Scenario Outline
				Given there is something
                    """"""
                      long string
                    """"""
				When I do <what>
                    | foo | bar |
                    | 1   | 2   |
				Then something should happen
			Examples: 
				| what           |
				| something      |
				| somethign else |";

        Establish context = () =>
        {
            var parser = new SpecFlowLangParser(new CultureInfo("en-US"));

            using (var reader = new StringReader(SampleFeatureFile))
            {
                Feature feature = parser.Parse(reader, null);

                var seleniumNUnitTestGeneratorProvider = new SeleniumNUnitTestGeneratorProvider(new CodeDomHelper(CodeDomProviderLanguage.CSharp));

                var codeDomHelper = new CodeDomHelper(CodeDomProviderLanguage.CSharp);

                var converter = new UnitTestFeatureGenerator(seleniumNUnitTestGeneratorProvider, codeDomHelper, new GeneratorConfiguration { AllowRowTests = true, AllowDebugGeneratedFiles = true }, new DecoratorRegistryStub());
                    
                CodeNamespace code = converter.GenerateUnitTestFixture(feature, "TestClassName", "Target.Namespace");
            }
        };

        Because of = () => { };

        It should_ = () => { };
    }
}