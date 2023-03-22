module TickSpec.Build.HtmlGenerator

open System
open System.IO
open System.Xml.Linq
open TickSpec
open System.Xml

[<AutoOpen>]
module private Impl = 

    let generateStep (stepType:StepType, line:LineSource) =
        let doc = new XElement("div")

        let text = line.Text.Trim()
        let keyword = text.Substring(0,text.IndexOf(' '))
        let statement = text.Substring(keyword.Length + 1)

        doc.Add(new XElement("span", [|
            new XAttribute("style", "font-weight:bold") :> obj
            keyword |] ))

        doc.Add(new XElement("span", statement))

        doc

    let generateScenario (scenario:ScenarioSource) =
        let doc = new XElement("div")

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            scenario.Name |]))

        if scenario.Tags.Length > 0 then
            doc.Add(new XElement("span", "Tags:"))
            doc.Add(new XElement("span", String.Join(", ", scenario.Tags)))

        // TODO: consider "scenario outline" by grouping scenarios together again
        // and write "Examples" section

        scenario.Steps
        |> Seq.map generateStep 
        |> Seq.iter doc.Add

        doc

    let generateFeature (feature:FeatureSource) =
        let doc = new XElement("article")

        doc.Add(new XElement("h2", [|
            new XAttribute("class", "gherkin-feature-title") :> obj
            feature.Name |]))

        feature.Scenarios
        |> Seq.map generateScenario
        |> Seq.iter doc.Add

        doc

    let write (writer:TextWriter) (doc:XElement) =
        let settings = new XmlWriterSettings()
        settings.Indent <- true

        use xmlWriter = XmlWriter.Create(writer, settings)
        doc.WriteTo(xmlWriter)

let GenerateArticle (writer:TextWriter) (feature:FeatureSource) =
    feature
    |> generateFeature
    |> write writer

