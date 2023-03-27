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
          <div><span class="gherkin-keyword">Given</span> some environment</div>
          <div> <span class="gherkin-keyword">And</span> with following setting</div>
          <div><span class="gherkin-keyword">When</span> some event happens</div>
          <div><span class="gherkin-keyword">Then</span> the system should be in this state</div>
          <div><span class="gherkin-keyword">And</span> behave like this</div>
        </div>
      </div>"""
        
[<Test>]
let ``With Background``() =
    """
    Feature: First feature

    Background:
        GIVEN some additional environment

    Scenario: One
    GIVEN some environment
     AND with following setting
    WHEN some event happens
    THEN the system should be in this state
    AND behave like this
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
        <article>
          <h2 class="gherkin-feature-title">First feature</h2>
          <div class="gherkin-scenario">
            <h3 class="gherkin-scenario-title">Background</h3>
            <div class="gherkin-scenario-body">
              <div><span class="gherkin-keyword">Given</span> some additional environment</div>
            </div>
          </div>
          <div class="gherkin-scenario">"""

[<Test>]
let ``Step with multi line string``() =
    """
    Feature: First feature

    Scenario: One
    GIVEN some environment
     AND the following value
        \"\"\"
        line 1
        line 2
        \"\"\"
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
      <div class="gherkin-scenario">
        <h3 class="gherkin-scenario-title">Scenario: One</h3>
        <div class="gherkin-scenario-body">
          <div><span class="gherkin-keyword">Given</span> some environment</div>
          <div> <span class="gherkin-keyword">And</span> the following value</div>
          <div>    \"\"\"</div>
          <div>    line 1</div>
          <div>    line 2</div>
          <div>    \"\"\"</div>
          <div><span class="gherkin-keyword">When</span> some event happens</div>
          <div><span class="gherkin-keyword">Then</span> the system should be in this state</div>
        </div>
      </div>"""

[<Test>]
let ``Tags``() =
    """
    Feature: First feature

    @some-tag @one-more-tag
    Scenario: One
    GIVEN some environment
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
      <div class="gherkin-scenario">
        <h3 class="gherkin-scenario-title">Scenario: One</h3>
        <div class="gherkin-scenario-body">
          <div><span class="gherkin-tags">Tags:</span>some-tag, one-more-tag</div>
          <div><span class="gherkin-keyword">Given</span> some environment</div>
          <div><span class="gherkin-keyword">When</span> some event happens</div>
          <div><span class="gherkin-keyword">Then</span> the system should be in this state</div>
        </div>
      </div>"""
