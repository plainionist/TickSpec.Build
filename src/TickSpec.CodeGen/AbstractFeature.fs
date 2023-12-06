namespace TickSpec.CodeGen

open System
open System.Reflection
open System.Runtime.ExceptionServices
open TickSpec
open NUnit.Framework

type AbstractFeature() =
    static member GetScenarios(assembly:Assembly, featureFilename) =
        // support step definitions in shared assemblies of "same project"
        let projectName = assembly.GetName().Name.Split('.') |> Seq.head
        let deps = 
            assembly.GetReferencedAssemblies()
            |> Seq.filter(fun x -> x.Name.StartsWith(projectName, StringComparison.OrdinalIgnoreCase))
            |> Seq.map Assembly.Load
            |> List.ofSeq
        let types = (assembly::deps) |> Seq.collect(fun x -> x.GetTypes()) |> Array.ofSeq
        let definitions = new StepDefinitions(types)

        let getScenarios (featureFile:string) =
            let feature = definitions.GenerateFeature(featureFile, assembly.GetManifestResourceStream(featureFile))
            feature.Scenarios

        assembly.GetManifestResourceNames()
        |> Seq.filter(fun x -> x.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
        |> Seq.filter(fun x -> x.EndsWith("." + featureFilename, StringComparison.OrdinalIgnoreCase))
        |> Seq.collect getScenarios
        |> List.ofSeq

    member __.RunScenario (scenarios:Scenario list, name:string) = 
        let run (scenario:Scenario) =
            if scenario.Tags |> Seq.exists ((=) "ignore") then
                raise (new IgnoreException("Ignored: " + scenario.ToString()))
            try
                scenario.Action.Invoke()
            with
            | :? TargetInvocationException as ex -> ExceptionDispatchInfo.Capture(ex.InnerException).Throw()

        let matching = 
            scenarios 
            // in case of "Scenario Outline" there are multiple scenarios starting with same name
            |> Seq.filter(fun x -> x.Name = name || x.Name.StartsWith(name + " ("))

        Assert.That(matching, Is.Not.Empty, "No matching scenarios found")

        matching
        |> Seq.iter run

