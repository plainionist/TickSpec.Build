module TickSepc.Build.Tests.HtmlGenerationTests

open NUnit.Framework
open TickSpec.Build
open FsUnit

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
    |> should haveSubstringIgnoringWhitespaces  """
      <div class="gherkin-scenario">
        <h3 class="gherkin-scenario-title">Scenario: One</h3>
        <div class="gherkin-scenario-body">
          <div>
            <div>GIVEN some environment</div>
            <div> AND with following setting</div>
            <div>WHEN some event happens</div>
            <div>THEN the system should be in this state</div>
            <div>AND behave like this</div>
          </div>
        </div>
      </div>"""
