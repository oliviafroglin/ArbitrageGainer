module ManagePnLThresholdInfra

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open ManagePnLThresholdService
open System.Globalization

let app = 
    let thresholdAgent = PnLThresholdAgent()

    let thresholdHandler =
        PUT >=> path "/threshold" >=> bindJson<Decimal> (fun newThreshold ->
            async {
                match! thresholdAgent.SetThreshold(newThreshold) with
                | Ok _ -> return! CREATED "Threshold updated successfully"
                | Error err -> return! BAD_REQUEST ("Error updating threshold: " + err)
            })

    choose [
        path "/threshold" >=> thresholdHandler
    ]