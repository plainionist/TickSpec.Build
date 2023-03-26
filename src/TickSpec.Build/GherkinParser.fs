module TickSpec.Build.GherkinParser

open System
open System.IO
open TickSpec

[<AutoOpen>]
module private Impl = 
    let trimLine numSpaces (line:string) =
        if line |> String.IsNullOrWhiteSpace then
            String.Empty
        else
            line.Substring(numSpaces).TrimEnd()

        // indent which exists for all non-empty lines
    let detectGlobalIndent = 
        Seq.filter (String.IsNullOrWhiteSpace >> not)
        >> Seq.map(fun x -> x |> Seq.takeWhile Char.IsWhiteSpace |> Seq.length)
        >> Seq.min

    // Line numbers start at 1 as in any editor
    let parseLines (text:string)=
        let lines = text.Split(Environment.NewLine)

        let globalIndent = lines |> detectGlobalIndent

        lines
        |> Seq.mapi(fun i l -> i + 1, l |> trimLine globalIndent)
        |> List.ofSeq

    let (|Title|_|) (keyword:string) (line:string) =
        let prefix = keyword + ":"
        if line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
            line.Substring(prefix.Length).Trim() |> Some
         else
            None

let Parse filename (feature:string) =
    let linesWithLineNo = feature |> parseLines

    let featureName =
        linesWithLineNo
        |> Seq.map snd
        |> Seq.choose(function | Title "Feature" x -> x |> Some | _ -> None)
        |> Seq.exactlyOne

    let scenarios = 
        linesWithLineNo
        |> Seq.mapFold(fun scenario (lineNo, line) ->
            match scenario, line with
            | _, Title "Scenario" x
            | _, Title "Scenario Outline" x ->
                let newScenario =
                    { 
                        Name = line
                        Title = x
                        StartsAtLine = lineNo + 1 // skip scenario title
                        Body = []
                    }
                scenario, newScenario |> Some
            | Some scenario, _ -> None, { scenario with Body = line::scenario.Body } |> Some
            | None, _ -> None, None // ignore lines outside scenario
            ) None
        |> fun (scenarios, scenario) -> scenario |> List.singleton |> Seq.append scenarios
        |> Seq.choose id
        |> Seq.map(fun x -> 
            // trim empty lines and reverse
            let lines = 
                x.Body
                |> Seq.skipWhile String.IsNullOrWhiteSpace
                |> Seq.rev
                |> Seq.skipWhile String.IsNullOrWhiteSpace
                |> List.ofSeq

            { x with Body = lines })
        |> List.ofSeq

    {
        Name = featureName
        Filename = filename
        Scenarios = scenarios
    }

let Read file =
    file |> File.ReadAllText |> Parse (Path.GetFileName(file))

let ReadAST file =
    let lines = File.ReadAllLines(file)
    FeatureParser.parseFeature(lines)

let ParseAST (text:string) =
    text.Split(Environment.NewLine)
    |> FeatureParser.parseFeature

let FindAllFeatureFiles folder =
    let options = EnumerationOptions()
    options.RecurseSubdirectories <- true
    
    Directory.GetFiles(folder, "*.feature", options)
    |> List.ofArray
