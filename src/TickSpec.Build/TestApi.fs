﻿module TickSpec.Build.TestApi

open System.IO

let GenerateHtmlDoc (featureText:string) =
    use writer = new StringWriter()

    featureText
    |> GherkinParser.ParseAST
    |> HtmlGenerator.GenerateArticle writer

    writer.ToString()        

let GenerateTestFixtures (featureText:string) = 
    use writer = new StringWriter()

    featureText 
    |> GherkinParser.Parse "Dummy.feature"
    |> List.singleton
    |> TestFixtureGenerator.Generate writer

    writer.ToString()        
