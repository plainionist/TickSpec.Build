module TickSpec.Build.GherkinParser

open System
open System.IO

[<AutoOpen>]
module private Impl = 
    let trimLine numSpaces (line:string) =
        if line |> String.IsNullOrWhiteSpace then
            String.Empty
        else
            line.Substring(numSpaces).TrimEnd()

    // indent which exists for all non-empty lines
    let detectGlobalIndent lines =
        lines
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> Seq.map(fun x -> x |> Seq.takeWhile Char.IsWhiteSpace |> Seq.length)
        |> Seq.min

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

    let trimEmptyLines =
        Seq.skipWhile String.IsNullOrWhiteSpace
        >> Seq.rev
        >> Seq.skipWhile String.IsNullOrWhiteSpace
        >> Seq.rev

    let parseTags (lines:(int*string) list) lineNo =
        if lineNo > 0 then
            let tagsLine = lines |> Seq.find(fun (x,_) -> x = lineNo - 1) |> snd |> fun x -> x.Trim()
            if tagsLine.StartsWith("@") then
                tagsLine.Split(' ',StringSplitOptions.RemoveEmptyEntries)
                |> Seq.map(fun x -> x.TrimStart('@'))
                |> List.ofSeq
            else
                []
        else
            []

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
                        Tags = parseTags linesWithLineNo lineNo
                    }
                scenario, newScenario |> Some
            | Some scenario, _ -> None, { scenario with Body = line::scenario.Body } |> Some
            | None, _ -> None, None // ignore lines outside scenario
            ) None
        |> fun (scenarios, scenario) -> scenario |> List.singleton |> Seq.append scenarios
        |> Seq.choose id
        |> Seq.map(fun x -> 
            let globalIndent = x.Body |> detectGlobalIndent
            { x with Body = x.Body |> Seq.map (trimLine globalIndent) |> trimEmptyLines |> Seq.rev |> List.ofSeq } )
        |> List.ofSeq

    let background = 
        linesWithLineNo
        |> Seq.map snd
        |> Seq.skipWhile ((function | Title "Background" _ -> true | _ -> false) >> not)
        |> Seq.takeWhile ((function | Title "Scenario" _ -> true | Title "Scenario Outline" _ -> true | _ -> false) >> not)
        |> trimEmptyLines
        |> List.ofSeq 
        |> function
            | [] -> []
            | h::t -> 
                let globalIndent = t |> detectGlobalIndent
                t |> List.map (trimLine globalIndent)
    
    {
        Name = featureName
        Background = background
        Filename = filename
        Scenarios = scenarios
    }

let Read file =
    file |> File.ReadAllText |> Parse (Path.GetFileName(file))

let FindAllFeatureFiles folder =
    let options = EnumerationOptions()
    options.RecurseSubdirectories <- true
    
    Directory.GetFiles(folder, "*.feature", options)
    |> List.ofArray
