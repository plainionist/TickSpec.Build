namespace TickSpec.CodeGen

open System
open System.Reflection
open System.Runtime.ExceptionServices
open System.Runtime.Loader
open TickSpec
open NUnit.Framework

module private Bindings =
    let getDefinitions (assembly:Assembly) =
        // support step definitions in shared assemblies of "same project"
        let projectName = assembly.GetName().Name.Split('.') |> Seq.head
        let context = AssemblyLoadContext.GetLoadContext(assembly)
        let deps = 
            assembly.GetReferencedAssemblies()
            |> Seq.filter(fun x -> x.Name.StartsWith(projectName, StringComparison.OrdinalIgnoreCase))
            |> Seq.map context.LoadFromAssemblyName
            |> List.ofSeq
        let types = (assembly::deps) |> Seq.collect(fun x -> x.GetTypes()) |> Array.ofSeq
        new StepDefinitions(types)

    let getFeatureResources (assembly:Assembly) =
        assembly.GetManifestResourceNames()
        |> Array.filter(fun x -> x.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))

    let getScenarios (assembly:Assembly) (definitions:StepDefinitions) featureFile =
        use stream = assembly.GetManifestResourceStream(featureFile)
        definitions.GenerateFeature(featureFile, stream).Scenarios |> List.ofSeq

type AbstractFeature() =
    static member GetScenarios(assembly:Assembly, featureFilename) =
        let definitions = Bindings.getDefinitions assembly

        Bindings.getFeatureResources assembly
        |> Seq.filter(fun x -> x.EndsWith("." + featureFilename, StringComparison.OrdinalIgnoreCase))
        |> Seq.collect (Bindings.getScenarios assembly definitions)
        |> List.ofSeq

    static member VerifyBindings(assembly:Assembly) =
        let resources = Bindings.getFeatureResources assembly
        if resources.Length = 0 then
            invalidOp $"No embedded .feature resources found in '{assembly.Location}'"

        let definitions = Bindings.getDefinitions assembly
        let scenarioCount =
            resources
            |> Array.sumBy(fun resource ->
                try
                    Bindings.getScenarios assembly definitions resource |> List.length
                with
                | :? StepException as ex ->
                    raise (InvalidOperationException($"Binding verification failed for '{resource}': {ex.Message}", ex)))

        resources.Length, scenarioCount

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
