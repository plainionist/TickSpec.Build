module TickSpec.Build.BindingVerifier

open System
open System.IO
open System.Reflection
open System.Runtime.Loader
open TickSpec
open TickSpec.CodeGen

type private VerificationLoadContext(assemblyPath:string) =
    inherit AssemblyLoadContext(isCollectible = true)

    let resolver = AssemblyDependencyResolver(assemblyPath)
    let sharedAssemblies =
        [|
            typeof<StepDefinitions>.Assembly
            typeof<unit>.Assembly
            typeof<AbstractFeature>.Assembly
        |]

    override this.Load(assemblyName) =
        // Binding attributes and F# types must have the same identity as the resolver's types.
        match sharedAssemblies |> Array.tryFind(fun x -> AssemblyName.ReferenceMatchesDefinition(x.GetName(), assemblyName)) with
        | Some assembly -> assembly
        | None ->
            match resolver.ResolveAssemblyToPath(assemblyName) with
            | null -> null
            | path -> this.LoadFromAssemblyPath(path)

    override this.LoadUnmanagedDll(name) =
        match resolver.ResolveUnmanagedDllToPath(name) with
        | null -> IntPtr.Zero
        | path -> this.LoadUnmanagedDllFromPath(path)

let Verify (assemblyPath:string) =
    let path = Path.GetFullPath(assemblyPath)
    if File.Exists(path) |> not then
        raise (FileNotFoundException($"Assembly to verify not found: '{path}'", path))

    printfn $"Verifying step bindings in '{path}' ..."

    let context = new VerificationLoadContext(path)
    try
        let assembly = context.LoadFromAssemblyPath(path)
        let featureCount, scenarioCount = AbstractFeature.VerifyBindings(assembly)
        printfn $"Verified bindings for {scenarioCount} scenarios in {featureCount} feature files."
    finally
        context.Unload()
