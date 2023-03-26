module TickSpec.Build.TestApi

open System.IO

let GenerateHtmlDoc (featureText:string) =
    use writer = new StringWriter()

    featureText
    |> GherkinParser.Parse "Dummy.feature"
    |> HtmlGenerator.GenerateArticle writer

    writer.ToString()        

let GenerateTestFixtures (featureText:string list) = 
    use writer = new StringWriter()

    featureText 
    |> List.map (GherkinParser.Parse "Dummy.feature")
    |> TestFixtureGenerator.Generate writer

    writer.ToString()        
