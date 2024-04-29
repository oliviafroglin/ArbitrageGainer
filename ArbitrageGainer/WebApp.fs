open System.Net
open Suave
open Suave.Filters
open Suave.Operators
open ArbitrageInfra
open Newtonsoft.Json
open ManagePnLThresholdInfra
open PnLCalculationInfra
open AnnualizedReturnCalculationInfra
open OrderExecutionInfra
open IdentifyCrossTradedPairsInfra


let app : WebPart =
    choose [
        POST >=> choose [
            path "/config" >=> updateConfig
            path "/start-trading" >=> startTrading
            path "/stop-trading" >=> stopTrading
            path "/pnl/threshold" >=> updateThresholdHandler
            path "/email" >=> updateEmail // for sending email in order execution
            path "/auto-stop" >=> updateAutoStop
        ]
        GET >=> choose [
            path "/get-historical-data" >=> (fun (ctx: HttpContext) ->
                async {
                    let opportunities = HistoricalDataAnalysisInfra.getHistoricalSpread()
                    return! Successful.OK (JsonConvert.SerializeObject(opportunities)) ctx
                })
            path "/get-cross-traded-pairs" >=> identifyCrossTradedPairsHandler
            path "/pnl/historical" >=> pnlHandler
            path "/annualized-return" >=> annualizedReturnHandler
        ]
    ]
    
let startWebServer() =
    let ipAddress = IPAddress.Parse("0.0.0.0")
    let port = Sockets.Port.Parse("8080") // Convert an int to a Port
    let binding = HttpBinding.create HTTP ipAddress port
    let config = { defaultConfig with bindings = [ binding ] }
    startWebServer config app

[<EntryPoint>]
let main args  =
    startWebServer()
    
    0 // return an integer exit code
