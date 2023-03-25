module TickSpec.Build.GherkinParser

open System
open System.IO
open TickSpec

let Read file =
    let linesWithLineNo = 
        File.ReadAllLines(file) 
        // start counting lines with 1 as in any editor
        |> Seq.mapi(fun i l -> i + 1, l)
        |> List.ofSeq

    let grep prefix =
        linesWithLineNo 
        |> Seq.filter(fun (_,x) -> x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        |> Seq.map(fun (i,x) -> i, x.Trim(), x.Substring(prefix.Length).Trim())

    {
        Name = grep "Feature:" |> Seq.exactlyOne |> fun (_,_,x) -> x
        Filename = Path.GetFileName(file)
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
