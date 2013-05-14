@EnableSauceLabs
Feature: SpecFlowFeature1
	In order to avoid silly mistakes
	As a math idiot
	I want to be told the sum of two numbers


Scenario: Add two numbers
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	When I press add
	Then the result should be 120 on the screen

@SomeTag
Scenario: Add three numbers
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	And I have enetered 20 into the calculator
	When I press add
	Then the result should be 140 on the screen

Scenario Outline: Add Two Numbers
	Given I navigated to /
	And I have entered <SummandOne> into summandOne calculator
	And I have entered <SummandTwo> into summandTwo calculator
	When I press add
	Then the result should be <Result> on the screen
	Scenarios: 
		| SummandOne | SummandTwo | Result |       
		| 50         | 70         | 120    | 
		| 1          | 10         | 11     |
