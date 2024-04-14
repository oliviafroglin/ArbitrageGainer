open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open ArbitrageInfra
open Newtonsoft.Json
open HistoricalDataAnalysisInfra
open IdentifyCrossTradedPairsInfra

let app : WebPart =
    choose [
        POST >=> choose [
            path "/update-config" >=> updateConfig
            path "/start-trading" >=> startTrading
            path "/stop-trading" >=> stopTrading
        ]
        GET >=> choose [
            path "/get-historical-data" >=> (fun (ctx: HttpContext) ->
                async {
                    let opportunities = HistoricalDataAnalysisInfra.getHistoricalSpread()
                    return! Successful.OK (JsonConvert.SerializeObject(opportunities)) ctx
                })
            path "/get-cross-traded-pairs" >=> (fun (ctx: HttpContext) ->
                async {
                    let task = IdentifyCrossTradedPairsInfra.identifyCrossTradedPairs () |> Async.StartAsTask
                    let json = JsonConvert.SerializeObject(task.Result)
                    return! Successful.OK json ctx
                })
        ]
    ]
[<EntryPoint>]
let main args  =

    startWebServer defaultConfig app // start the web server
    
    0 // return an integer exit code