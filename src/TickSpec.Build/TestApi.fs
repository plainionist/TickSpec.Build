module TickSpec.Build.TestApi

open System.IO

let GenerateHtmlDoc (featureText:string) =
    use writer = new StringWriter()

    featureText
    |> GherkinParser.Parse "Dummy.feature"
    |> HtmlGenerator.GenerateArticle writer

    writer.ToString()        

let GenerateTestFixtures file (featureText:string list) = 
    use writer = new StringWriter()

    featureText 
    |> List.map (GherkinParser.Parse file)
    |> TestFixtureGenerator.Generate writer

    writer.ToString()        
