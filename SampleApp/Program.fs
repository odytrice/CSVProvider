// Learn more about F# at http://fsharp.org

open System

open FSharpConf

type PeopleCsvFile = CsvProvider<"../people.csv">

let file = PeopleCsvFile()

file.Rows
|> Seq.filter(fun r -> r.Age > 5.)
|> Seq.iter(fun r -> printfn "%s %s" r.FirstName r.LastName)

[<EntryPoint>]
let main argv =


    0 // return an integer exit code
