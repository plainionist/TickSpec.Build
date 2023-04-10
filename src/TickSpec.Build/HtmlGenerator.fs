module TickSpec.Build.HtmlGenerator

open System
open System.IO
open System.Xml.Linq
open System.Xml

type TocFormat =
    | Html
    | Json

[<AutoOpen>]
module private Impl = 

    let (|Keyword|_|) (keyword:string) (line:string) =
        if line.TrimStart().StartsWith(keyword + " ", StringComparison.OrdinalIgnoreCase) then
            let indent = line |> Seq.takeWhile Char.IsWhiteSpace |> Seq.length
            (keyword.PadLeft(indent, ' '), line.Substring(keyword.Length + indent)) |> Some
         else
            None

    let generateStep (line:string) =
        match line with
        | Keyword "Given" (k,l) 
        | Keyword "When" (k,l)  
        | Keyword "Then" (k,l) 
        | Keyword "And" (k,l)  
        | Keyword "But" (k,l) -> 
            [
                new XElement("span", new XAttribute("class", "gherkin-keyword"), k) :> obj
                l
                Environment.NewLine
            ]
        | _ -> 
            [
                line
                Environment.NewLine
            ]

    let generateScenarioBody (lines:string list) =
        new XElement("pre", 
            new XAttribute("class", "gherkin-scenario-body"), 
            new XElement("code",
                lines
                |> Seq.map generateStep))

    let generateScenario (scenario:Scenario) =
        let doc = new XElement("div", new XAttribute("class", "gherkin-scenario"))

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            scenario.Title :> obj |]))

        match scenario.Tags with
        | [] -> ()
        | tags ->
            new XElement("div", 
                new XElement("span", new XAttribute("class", "gherkin-tags"), "Tags:"), 
                String.Join(", ", tags))
            |> doc.Add

        match scenario.Description with
        | "" -> ()
        | text ->
            doc.Add(new XElement("div", new XAttribute("class", "gherkin-description"), text))

        doc.Add(generateScenarioBody scenario.Body)

        doc
        
    let generateBackground (lines:string list) =
        let doc = new XElement("div", new XAttribute("class", "gherkin-scenario"))

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            "Background" :> obj |]))

        doc.Add(generateScenarioBody lines)

        doc

    let generateFeature (feature:Feature) =
        let doc = new XElement("article")

        doc.Add(new XElement("h2", [|
            new XAttribute("class", "gherkin-feature-title") :> obj
            feature.Name |]))

        match feature.Background with
        | [] -> ()
        | x -> doc.Add(generateBackground x)

        feature.Scenarios
        |> Seq.map generateScenario
        |> Seq.iter doc.Add

        doc

    let write (writer:TextWriter) (doc:XElement) =
        let settings = new XmlWriterSettings()
        // explicitly disable so that <pre/> formatting is kept
        settings.Indent <- false

        use xmlWriter = XmlWriter.Create(writer, settings)
        doc.WriteTo(xmlWriter)

    let generateHtmlToc (features:Feature list) (output:string) =
        ()

    let generateJsonToc (features:Feature list) (output:string) =
        ()

let GenerateArticle (writer:TextWriter) (feature:Feature) =
    feature
    |> generateFeature
    |> write writer

let GenerateToC tocFormat (features:Feature list) (output:string) =
    match tocFormat with
    | Html -> generateHtmlToc features output
    | Json -> generateJsonToc features output
