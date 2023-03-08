module TickSpec.Build.TestFixtureGenerator

open System.IO

[<AutoOpen>]
module private Impl =
    let writeHeader (writer:TextWriter) =
        writer.WriteLine("namespace Specification");
        writer.WriteLine()
        writer.WriteLine("open System.Reflection")
        writer.WriteLine("open NUnit.Framework")
        writer.WriteLine("open TickSpec.CodeGen")
        writer.WriteLine()

    let writeTestCase (writer:TextWriter) featureFile scenario =
        writer.WriteLine($"    [<Test>]")
        writer.WriteLine($"    member this.``{scenario.Title}``() =")
        writer.WriteLine($"#line {scenario.StartsAtLine} \"{featureFile}\"")
        writer.WriteLine($"        this.RunScenario(scenarios, \"{scenario.Name}\")")
        writer.WriteLine()

    let writeTestFixture (writer:TextWriter) feature =
        writer.WriteLine($"[<TestFixture>]")
        writer.WriteLine($"type ``{feature.Name}``() = ")
        writer.WriteLine($"    inherit AbstractFeature()")
        writer.WriteLine()
        writer.WriteLine($"    let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), \"{feature.Filename}\")")
        writer.WriteLine()

        feature.Scenarios
        |> Seq.iter (writeTestCase writer feature.Filename)

let Generate (writer:TextWriter) features =
    
    writeHeader writer

    features
    |> Seq.iter (writeTestFixture writer)
