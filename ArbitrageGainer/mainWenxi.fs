module Main

open Suave
open Suave.Filters
open Suave.Operators
// open PnLCalculationInfra
open ManagePnLThresholdInfra
// open AnnualizedReturnCalculationInfra

let app: WebPart =
    choose [
        // GET >=> choose [
        //     path "/pnl/historical" >=> pnlHandler
        //     path "/annualized-return" >=> annualizedReturnHandler
        // ]
        POST >=> choose [
            path "/pnl/threshold" >=> updateThresholdHandler
        ]
    ]

[<EntryPoint>]
let main argv =
    startWebServer defaultConfig app
    0 // Return an exit code of 0
