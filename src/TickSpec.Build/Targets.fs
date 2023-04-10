module TickSpec.Build.Targets

open System.IO

let GenerateTestFixtures (output:string) = 
    printfn $"Generating test fixtures '{output}' ..."

    let featuresFolder = Path.GetDirectoryName(output)

    let features = 
        featuresFolder 
        |> GherkinParser.FindAllFeatureFiles
        |> List.map GherkinParser.Read

    if features.Length = 0 then
        printfn "No feature files found in %s" featuresFolder
    else
        use writer = new StreamWriter(output)
        TestFixtureGenerator.Generate writer features

let GenerateHtmlDocs tocFormat (input:string) (output:string) =
    printfn $"Generating documenation for '{input}' ..."

    let generate (feature:Feature) =
        let parts =
            [
                [output]
                feature.Location.Folders
                [feature.Name + ".html"]
            ]
            |> List.concat
            |> Array.ofList

        let file = Path.Combine(parts)

        let folder = Path.GetDirectoryName(file)
        if folder |> Directory.Exists |> not then
            Directory.CreateDirectory(folder) |> ignore

        use writer = new StreamWriter(file)
        HtmlGenerator.GenerateArticle writer feature

    let features =
        input
        |> GherkinParser.FindAllFeatureFiles
        |> List.map GherkinParser.Read

    if features |> Seq.isEmpty |> not then
        features
        |> Seq.iter generate

        tocFormat
        |> Option.iter(fun f -> HtmlGenerator.GenerateToC f features output)

        printfn $"Documentation generated to '{output}'"
    else
        printfn $"No feature files found"

