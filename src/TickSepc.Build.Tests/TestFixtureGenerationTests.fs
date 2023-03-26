module TickSepc.Build.Tests.TestFixtureGenerationTests

open NUnit.Framework
open TickSpec.Build
open FsUnit


[<Test>]
let ``Single scenario``() =
    [
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
    ]
    |> TestApi.GenerateTestFixtures
    |> dump
    |> should haveSubstringIgnoringWhitespaces  """
        namespace Specification

        open System.Reflection
        open NUnit.Framework
        open TickSpec.CodeGen

        [<TestFixture>]
        type ``First feature``() = 
            inherit AbstractFeature()

            let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), "Dummy.feature")

            [<Test>]
            member this.``One``() =
        #line 5 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario: One")
            """

