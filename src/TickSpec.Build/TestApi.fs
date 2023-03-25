module TickSpec.Build.TestApi

open System.IO

let GenerateHtmlDoc (featureText:string) =

    use writer = new StringWriter()

    featureText
    |> GherkinParser.ParseAST
    |> HtmlGenerator.GenerateArticle writer

    writer.ToString()        
