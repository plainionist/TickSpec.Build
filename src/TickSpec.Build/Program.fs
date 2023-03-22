module TickSpec.Build.Program

open System

[<AutoOpen>]
module private Impl = 
    let (|EqualsI|_|) (a:string) b = 
        match a.Equals(b, StringComparison.OrdinalIgnoreCase) with
        | true -> Some a
        | false -> None

    let usage() =
        printfn "usage: TickSpec.Build [command] <options>"
        printfn ""
        printfn "Gobal options:"
        printfn "  -h        - prints this help"
        printfn ""
        printfn "Commands:"
        printfn "  fixtures             - generates code behind for test fixtures"
        printfn "    <file>             - F# file the fixtures shall be generated into"
        printfn "                         (feature files are collected from this folder)"
        printfn "  doc                  - generates HTML documentation of the feature files"
        printfn "    <input>            - folder to search for *.feature files"
        printfn "    <output>           - folder to generate the HTML files to"

[<EntryPoint>]
let main argv =
    try
        match argv |> List.ofArray with
        | [] -> usage()
        | (EqualsI "-h" _)::_ -> usage()
        | (EqualsI "fixtures" _)::t -> 
            match t with
            | [x] -> Targets.GenerateTestFixtures x
            | x -> failwithf "Missing or unknown arguments: %A" x
        | (EqualsI "doc" _)::t -> 
            match t with
            | [i;o] -> Targets.GenerateHtmlDocs i o
            | x -> failwithf "Missing or unknown arguments: %A" x
        | x -> failwithf "Unknown arguments: %A" x

        0
    with
        | ex -> 
            Console.Error.WriteLine($"ERROR: {ex}")
            1
