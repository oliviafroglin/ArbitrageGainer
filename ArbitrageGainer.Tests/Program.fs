// Program.fs
open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Newtonsoft.Json
open IdentifyCrossTradedPairsInfra

let app =
    choose [
        path "/cross-traded-pairs" >=> GET >=> fun ctx ->
            async {
                let task = IdentifyCrossTradedPairsInfra.identifyCrossTradedPairs () |> Async.StartAsTask
                task.Result |> Set.iter (fun (pair1, pair2) -> printfn "%s-%s" pair1 pair2)
                // let! pairs = identifyCrossTradedPairs ()
                let json = JsonConvert.SerializeObject(task.Result)
                return! OK json ctx
            }
    ]

[<EntryPoint>]
let main argv =
    startWebServer defaultConfig app
    0 // Return an integer exit code