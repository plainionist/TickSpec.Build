module TickSpec.Build.HtmlGenerator

open System
open System.IO
open System.Xml.Linq
open System.Xml

[<AutoOpen>]
module private Impl = 

    let generateStep (line:string) =
        let doc = new XElement("div")

        doc.Add(line)

        doc

    let generateScenarioBody (scenario:Scenario)=
        let doc = new XElement("div", new XAttribute("class", "gherkin-scenario-body"))

        //match scenario |> Scenario.Tags with
        //| [||] -> ()
        //| tags ->
        //    doc.Add(new XElement("span", "Tags:"))
        //    doc.Add(new XElement("span", String.Join(", ", tags)))

        doc.Add(new XElement("div",
            scenario.Body
            |> Seq.map generateStep))

        doc

    let generateScenario (scenario:Scenario) =
        let doc = new XElement("div", new XAttribute("class", "gherkin-scenario"))

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            scenario.Name :> obj |]))

        doc.Add(generateScenarioBody scenario)

        doc
        
    let generateBackground (lines:string list) =
        let doc = new XElement("div", new XAttribute("class", "gherkin-scenario"))

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            "Background" :> obj |]))

        doc.Add(new XElement("div",
            lines
            |> Seq.map generateStep))

        doc

    let generateFeature (feature:Feature) =
        let doc = new XElement("article")

        doc.Add(new XElement("h2", [|
            new XAttribute("class", "gherkin-feature-title") :> obj
            feature.Name |]))

        doc.Add(generateBackground feature.Background)

        feature.Scenarios
        |> Seq.map generateScenario
        |> Seq.iter doc.Add

        doc

    let write (writer:TextWriter) (doc:XElement) =
        let settings = new XmlWriterSettings()
        settings.Indent <- true

        use xmlWriter = XmlWriter.Create(writer, settings)
        doc.WriteTo(xmlWriter)

let GenerateArticle (writer:TextWriter) (feature:Feature) =
    feature
    |> generateFeature
    |> write writer

