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

let Parse filename (feature:string) =
    let linesWithLineNo = feature |> parseLines

    let grep prefix =
        linesWithLineNo 
        |> Seq.filter(fun (_,x) -> x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        |> Seq.map(fun (i,x) -> i, x.Trim(), x.Substring(prefix.Length).Trim())

    {
        Name = grep "Feature:" |> Seq.exactlyOne |> fun (_,_,x) -> x
        Filename = filename
        Scenarios = 
            grep "Scenario:" 
            |> Seq.append (grep "Scenario Outline:") 
            |> Seq.map(fun (lineNo, name, title) -> 
                { 
                    Name = name
                    Title = title
                    StartsAtLine = lineNo + 1 // skip scenario title
                })
            |> List.ofSeq
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
