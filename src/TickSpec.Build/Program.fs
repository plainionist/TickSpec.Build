module TickSpec.Build.Program

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Text.Json
open CommandLine

module Unions =
    open System.Reflection
    open Microsoft.FSharp.Reflection

    let private bindingFlags = BindingFlags.Public ||| BindingFlags.NonPublic
    
    let fromString<'a> (s:string) =
        try
            match FSharpType.GetUnionCases(typeof<'a>, bindingFlags) |> Array.filter (fun case -> case.Name.Equals(s,StringComparison.OrdinalIgnoreCase)) with
            |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
            |_ -> None
        with
            | _ -> None

[<Verb("fixtures", HelpText = "Generates code behind for test fixtures")>]
type FixturesOptions = {
    [<Value(0, MetaName="file", HelpText = "F# file the fixtures shall be generated into (feature files are collected from this folder)")>] File : string
}

[<Verb("verify-bindings", HelpText = "Verifies step bindings in a compiled test assembly without executing scenarios")>]
type VerifyBindingsOptions = {
    [<Value(0, MetaName="assembly", HelpText = "Compiled test assembly containing embedded feature files")>] Assembly : string
}

[<Verb("doc", HelpText = "Generates HTML documentation of the feature files")>]
type DocOptions = {
    [<Value(0, MetaName="input", HelpText = "Folder to search for *.feature files")>] Input : string
    [<Value(1, MetaName="output", HelpText = "Folder to generate the HTML files to")>] Output : string
    [<Option('t', "toc", Required = false, HelpText = "Generate table of contents in given format (HTML, Json)")>] TocFormat : string option
}

[<AutoOpen>]
module private Impl =
    let hasRuntimeFrameworks runtimeConfig =
        use document = JsonDocument.Parse(File.ReadAllText(runtimeConfig))
        let options = document.RootElement.GetProperty("runtimeOptions")
        fst (options.TryGetProperty("framework")) || fst (options.TryGetProperty("frameworks"))

    let printException (error:Exception) =
        match error with
        | :? ReflectionTypeLoadException as ex ->
            Console.Error.WriteLine("ERROR: Unable to load step-binding types.")
            ex.LoaderExceptions
            |> Seq.choose Option.ofObj
            |> Seq.distinctBy(fun x -> x.Message)
            |> Seq.iter(fun x -> Console.Error.WriteLine($"ERROR: {x.Message}"))
        | _ ->
            Console.Error.WriteLine($"ERROR: {error.Message}")

    let runFixtures opts =
        if String.IsNullOrEmpty(opts.File) then
            failwith "Name of the file to generate missing"

        Targets.GenerateTestFixtures opts.File

        0

    let runVerifyBindings opts =
        if String.IsNullOrWhiteSpace(opts.Assembly) then
            failwith "Assembly to verify missing"

        let assemblyPath = Path.GetFullPath(opts.Assembly)
        let runtimeConfig = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json")
        let runtimeMarker = "TICKSPEC_BUILD_VERIFY_RUNTIME"
        let hostedAssembly = Environment.GetEnvironmentVariable(runtimeMarker)

        if hostedAssembly <> assemblyPath && File.Exists(runtimeConfig) && hasRuntimeFrameworks runtimeConfig then
            let host =
                match Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") with
                | null | "" -> "dotnet"
                | path -> path
            let startInfo = ProcessStartInfo(host)
            startInfo.UseShellExecute <- false
            for argument in [
                "exec"; "--runtimeconfig"; runtimeConfig
                Assembly.GetExecutingAssembly().Location; "verify-bindings"; assemblyPath
            ] do
                startInfo.ArgumentList.Add(argument)
            // The child must use the consumer's frameworks, but the verifier's own dependency graph.
            startInfo.Environment[runtimeMarker] <- assemblyPath

            use child = new Process()
            child.StartInfo <- startInfo
            if child.Start() |> not then
                failwith "Could not start binding verification under the test assembly's runtime configuration"
            child.WaitForExit()
            child.ExitCode
        else
            BindingVerifier.Verify assemblyPath
            0

    let runDoc opts = 
        if String.IsNullOrEmpty(opts.Input) then
            failwith "Folder to scan for feature files missing"

        if String.IsNullOrEmpty(opts.Output) then
            failwith "Output folder missing"

        let tocFormat = opts.TocFormat |> Option.bind Unions.fromString<HtmlGenerator.TocFormat>
        Targets.GenerateHtmlDocs tocFormat opts.Input opts.Output
        0

    let printErrors (errors:Error seq) =
        let help = new Text.HelpText()
        errors
        |> Seq.filter(fun x -> x.Tag <> ErrorType.HelpRequestedError)
        |> Seq.filter(fun x -> x.Tag <> ErrorType.HelpVerbRequestedError)
        |> Seq.filter(fun x -> x.Tag <> ErrorType.VersionRequestedError)
        |> Seq.map(fun x -> x |> printf "%A"; help.SentenceBuilder.FormatError.Invoke(x))
        |> Seq.iter Console.Error.WriteLine
        1

[<EntryPoint>]
let main args =

    try
        let parser = new Parser(fun settings ->
            settings.AutoHelp <- true
            settings.AutoVersion <- false
            settings.CaseSensitive <- false
            settings.CaseInsensitiveEnumValues <- true
            settings.HelpWriter <- Console.Out
        )

        let result = parser.ParseArguments<FixturesOptions, DocOptions, VerifyBindingsOptions> args
        result.MapResult(runFixtures, runDoc, runVerifyBindings, printErrors)
    with
        | ex -> 
            printException ex
            1
