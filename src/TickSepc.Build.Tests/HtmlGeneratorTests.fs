namespace TickSepc.Build.Tests

open NUnit.Framework
open TickSpec.Build
open FsUnit

module HtmlGeneratorTests =
    let dump x =
        printfn "%A" x
        x

    [<Test>]
    let ``Feature title is headline``() =
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
        |> TestApi.GenerateHtmlDoc
        |> should haveSubstringIgnoringWhitespaces  """<h2 class="gherkin-feature-title">First feature</h2>"""

    [<Test>]
    let ``Scenario title is headline``() =
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
        |> TestApi.GenerateHtmlDoc
        |> should haveSubstringIgnoringWhitespaces  """<h3 class="gherkin-scenario-title">Scenario: One</h3>"""
        
    [<Test>]
    let ``Steps rendered as separate <div/>s``() =
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        AND with following setting
        WHEN some event happens
        THEN the system should be in this state
        AND behave like this
        """
        |> TestApi.GenerateHtmlDoc
        |> dump
        |> should haveSubstringIgnoringWhitespaces  """
          <div class="gherkin-scenario">
            <h3 class="gherkin-scenario-title">Scenario: One</h3>
            <div class="gherkin-scenario-body">
              <div>
                <span class="gherkin-step-type">GIVEN</span>
                <span>some environment</span>
              </div>
              <div>
                <span class="gherkin-step-type">AND</span>
                <span>with following setting</span>
              </div>
              <div>
                <span class="gherkin-step-type">WHEN</span>
                <span>some event happens</span>
              </div>
              <div>
                <span class="gherkin-step-type">THEN</span>
                <span>the system should be in this state</span>
              </div>
              <div>
                <span class="gherkin-step-type">AND</span>
                <span>behave like this</span>
              </div>
            </div>
          </div>
          """
