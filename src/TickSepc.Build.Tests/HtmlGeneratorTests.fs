namespace TickSepc.Build.Tests

open NUnit.Framework
open TickSpec.Build
open FsUnit

module HtmlGeneratorTests =
    let dump x =
        printfn "%A" x
        x

    [<Test>]
    let FeatureTitleIsRendered () =
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
        |> TestApi.GenerateHtmlDoc
        |> dump
        |> should haveSubstring  """<h2 class="gherkin-feature-title">First feature</h2>"""
