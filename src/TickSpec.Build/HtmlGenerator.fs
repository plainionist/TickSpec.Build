module TickSpec.Build.HtmlGenerator

open System
open System.IO
open System.Xml.Linq
open TickSpec
open System.Xml

[<AutoOpen>]
module private Impl = 
    type Scenario =
        | Simple of ScenarioSource
        | Outline of string * ScenarioSource list

    module Scenario = 
        let Name = function
            | Simple x -> x.Name
            | Outline(n,_) -> n
        let Tags = function
            | Simple x -> x.Tags
            | Outline(_,x) -> x |> Seq.collect(fun y -> y.Tags) |> Seq.distinct |> Array.ofSeq
        let Steps = function
            | Simple x -> x.Steps
            | Outline(_,x) -> 
                // an outline must have at least one "scenario" and each scenario is expected
                // to have exactly same steps
                x |> Seq.head |> fun x -> x.Steps
        let Parameters = function
            | Simple x -> x.Parameters |> List.singleton
            | Outline(_,x) -> x |> List.map(fun y -> y.Parameters)

    let generateStep (stepType:StepType, line:LineSource) =
        let doc = new XElement("div")

        let text = line.Text.Trim()
        let keyword = text.Substring(0,text.IndexOf(' '))
        let statement = text.Substring(keyword.Length + 1)

        doc.Add(new XElement("span", [|
            new XAttribute("class", "gherkin-step-type") :> obj
            keyword |] ))

        doc.Add(new XElement("span", statement))

        doc

    let generateExamples (rows:(string*string) array list) =
        let header = rows |> List.head |> Array.map fst |> Array.sort

        let doc = new XElement("table",
            new XElement("thead",
                new XElement("tr", 
                    header |> Seq.map(fun k -> new XElement("td", k))
                )
            ),
            new XElement("tbody",
                rows 
                |> Seq.map(fun r -> 
                    new XElement("tr",
                        r 
                        |> Seq.sortBy fst // ensure all rows have columns in same order
                        |> Seq.map(fun (_,v) -> new XElement("td",v))))
            ))

        doc


    let generateScenario (scenario:Scenario) =
        let doc = new XElement("div")

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            scenario |> Scenario.Name :> obj |]))

        match scenario |> Scenario.Tags with
        | [||] -> ()
        | tags ->
            doc.Add(new XElement("span", "Tags:"))
            doc.Add(new XElement("span", String.Join(", ", tags)))

        scenario
        |> Scenario.Steps
        |> Seq.map generateStep 
        |> Seq.iter doc.Add

        match scenario |> Scenario.Parameters with
        | [] -> ()
        | h::_ as rows -> 
            doc.Add(new XElement("span", "Examples:"))
            doc.Add(generateExamples rows)

        doc

    // TickSpec create ScenarioSource per "Example" for a "Scenario Outline".
    // In the documentation we want to bring those together again
    let createScenarios =
        let tryCreateOutlineSample x =
            if x.Name.StartsWith("Scenario Outline:") then
                let name = x.Name.Substring("Scenario Outline:".Length, x.Name.LastIndexOf('(') - "Scenario Outline:".Length).Trim()
                Some("Scenario: " + name, x)
            else
                None

        let outline (name,scenarios) = Outline(name, scenarios |> List.rev)

        Seq.mapFold(fun collected scenario ->
            match collected, scenario |> tryCreateOutlineSample with
            | None, None -> [Simple(scenario)], None
            | Some y, None -> [outline(y); scenario |> Simple], None
            | None, Some(n,y) -> [], Some(n,[y])
            | Some(n1, y1), Some(n2, y2) when n1 = n2 -> [], Some(n1, y2::y1)
            | Some(n1, y1), Some(n2, y2) -> [outline(n1,y1)], Some(n2, [y2])
        ) None
        >> fun (scenarios, collected) -> 
            collected 
            |> function | None -> [] | Some x -> [outline(x)]
            |> Seq.append (scenarios |> Seq.collect id)
        
    let generateFeature (feature:FeatureSource) =
        let doc = new XElement("article")

        doc.Add(new XElement("h2", [|
            new XAttribute("class", "gherkin-feature-title") :> obj
            feature.Name |]))

        feature.Scenarios
        |> createScenarios
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

