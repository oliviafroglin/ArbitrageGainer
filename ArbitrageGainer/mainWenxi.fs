module Main

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open ManagePnLThresholdInfra
open PnLCalculationInfra

let mainWebPart =
    choose [
        pathPrefix "/pnl" >=> choose [
            pathPrefix "/threshold" >=> ManagePnLThresholdInfra.app
            pathPrefix "/historical" >=> PnLCalculationInfra.app
        ]
        pathPrefix "/annualized-return" >=> AnnualizedReturnInfra.app
    ]

let configureAndStartServer () =
    let config = { defaultConfig with bindings = [ HttpBinding.create HTTP "localhost" 8080 ] }
    startWebServer config mainWebPart

[<EntryPoint>]
let main argv =
    configureAndStartServer ()
    0  // Return an exit code of 0
