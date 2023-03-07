module TickSpec.Build.Program

open System
open System.IO

module Gherkin =
    type Scenario = {
        Name : string
        Title : string
        StartsAtLine : int
    }

    type Feature = {
        Name : string
        Filename : string
        Scenarios : Scenario list
    }

    let readFeature featureFile =

        let lines = 
            File.ReadAllLines(featureFile) 
            |> Seq.mapi(fun i l -> i,l)
            |> List.ofSeq

        let grep prefix =
            lines 
            |> Seq.filter(fun (_,x) -> x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            |> Seq.map(fun (i,x) -> i, x.Trim(), x.Substring(prefix.Length).Trim())

        {
            Name = grep "Feature:" |> Seq.exactlyOne |> fun (_,_,x) -> x
            Filename = Path.GetFileName(featureFile)
            Scenarios = 
                grep "Scenario:" 
                |> Seq.append (grep "Scenario Outline:") 
                |> Seq.map(fun (lineNo, name, title) -> 
                    { 
                        Name = name
                        Title = title
                        StartsAtLine = lineNo + 2 // start counting with 1 and skip scenario title
                    })
                |> List.ofSeq
        }

    let getFeatures folder =
        Directory.GetFiles(folder, "*.feature")
        |> Seq.map readFeature
        |> List.ofSeq

    let generateScenario (writer:TextWriter) featureFile s =
        writer.WriteLine($"    [<Test>]")
        writer.WriteLine($"    member this.``{s.Title}``() =")
        writer.WriteLine($"#line {s.StartsAtLine} \"{featureFile}\"")
        writer.WriteLine($"        this.RunScenario(scenarios, \"{s.Name}\")")
        writer.WriteLine()

    let generateFeature (writer:TextWriter) f =
        writer.WriteLine($"[<TestFixture>]")
        writer.WriteLine($"type ``{f.Name}``() = ")
        writer.WriteLine($"    inherit AbstractFeature()")
        writer.WriteLine()
        writer.WriteLine($"    let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), \"{f.Filename}\")")
        writer.WriteLine()

        f.Scenarios
        |> Seq.iter (generateScenario writer f.Filename)

    let generate (writer:TextWriter) features =
        writer.WriteLine("namespace Specification");
        writer.WriteLine()
        writer.WriteLine("open System.Reflection")
        writer.WriteLine("open NUnit.Framework")
        writer.WriteLine("open TickSpec.CodeGen")
        writer.WriteLine()

        features
        |> Seq.iter (generateFeature writer)

[<EntryPoint>]
let main argv =
    try
        let output = argv.[0];

        printfn $"Generating '{output}'"

        let features = Path.GetDirectoryName(output) |> Gherkin.getFeatures

        if features.Length <> 0 then
            use writer = new StreamWriter(output)
            Gherkin.generate writer features

        0
    with
        | ex -> 
            Console.Error.WriteLine($"ERROR: {ex}")
            1
