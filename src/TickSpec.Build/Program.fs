module TickSpec.Build.Program

open System
open System.IO

[<EntryPoint>]
let main argv =
    try
        if argv.Length <> 1 then
            failwithf "Exactly one argument required: file the 'code behind' test fixtures should be generated into"

        let output = argv.[0];

        printfn $"Generating '{output}'"

        let featuresFolder = Path.GetDirectoryName(output)

        let features = featuresFolder |> GherkinParser.ReadAllFeatureFiles

        if features.Length = 0 then
            printfn "No feature files found in %s" featuresFolder
        else
            use writer = new StreamWriter(output)
            TestFixtureGenerator.Generate writer features

        0
    with
        | ex -> 
            Console.Error.WriteLine($"ERROR: {ex}")
            1
