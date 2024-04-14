open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open ArbitrageOpportunityInfra.fs

let app : WebPart =
    choose [
        POST >=> choose [
            path "/update-config" >=> updateConfig
            path "/start-trading" >=> startTrading
            path "/stop-trading" >=> stopTrading
        ]
    ]
[<EntryPoint>]
let main args  =

    startWebServer defaultConfig app // start the web server
    
    0 // return an integer exit code