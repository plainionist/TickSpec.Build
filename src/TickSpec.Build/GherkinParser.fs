module TickSpec.Build.GherkinParser

open System
open System.IO
open TickSpec

[<AutoOpen>]
module private Impl = 
    let parseLines (text:string)=
        let lines = text.Split(Environment.NewLine)

        // indent which exists for all non-empty lines
        let globalIndent = 
            lines
            |> Seq.filter (String.IsNullOrWhiteSpace >> not)
            |> Seq.map(fun x -> x |> Seq.takeWhile Char.IsWhiteSpace |> Seq.length)
            |> Seq.min

        let normalize (line:string) =
            if line |> String.IsNullOrWhiteSpace then
                String.Empty
            else
                line.Substring(globalIndent).TrimEnd()

        lines
        // start counting lines with 1 as in any editor
        |> Seq.mapi(fun i l -> i + 1, l |> normalize)
        |> List.ofSeq

    let (|Title|_|) (keyword:string) (line:string) =
        let prefix = keyword + ":"
        if line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
            line.Substring(prefix.Length).Trim() |> Some
         else
            None

    let trimEmptyLines =
        Seq.skipWhile(fun (_,line) -> line |> String.IsNullOrWhiteSpace)
        >> Seq.rev
        >> Seq.skipWhile(fun (_,line) -> line |> String.IsNullOrWhiteSpace)
        >> Seq.rev

let Parse filename (feature:string) =
    let linesWithLineNo = feature |> parseLines

    let featureName =
        linesWithLineNo
        |> Seq.map snd
        |> Seq.choose(function | Title "Feature:" x -> x |> Some | _ -> None)
        |> Seq.exactlyOne

    let scenarios = 
        linesWithLineNo
        |> Seq.mapFold(fun scenarioIdx (i, line) ->
            match line with
            | Title "Scenario:" x
            | Title "Scenario Outline:" x -> (scenarioIdx + 1, (i, x)), scenarioIdx + 1
            | x -> (scenarioIdx, (i, line)), scenarioIdx) 0
        |> fst
        |> Seq.groupBy fst
        |> Seq.map snd
        |> Seq.map(fun lines -> 
            lines
            |> Seq.map snd // remove scenario index
            |> Seq.sortBy fst // sort by line numbers
            |> trimEmptyLines
            |> List.ofSeq)
        |> Seq.map(fun lines ->
            let titleLine = lines |> Seq.head
            { 
                Name = titleLine |> snd
                Title = titleLine |> snd
                StartsAtLine = (titleLine |> fst) + 1 // skip scenario title
                Body = lines |> Seq.skip 1 |> Seq.map snd |> List.ofSeq
            })
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
