module ManagePnLThresholdInfra

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open ManagePnLThresholdService
open ManagePnLThresholdCore
open System.Globalization
open System


let thresholdAgent = PnLThresholdAgent()

let parseDecimal (value: string) : option<decimal> =
    let tryParse = 
        fun () ->
            let mutable tempResult = 0m
            match Decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, &tempResult) with
            | true -> Some tempResult
            | _ -> None
    tryParse ()

let updateThresholdHandler (ctx: HttpContext) : Async<HttpContext option> =
    async {
        let maybeNewThreshold = ctx.request.queryParam "newThreshold"
        let parseQueryParam (param: Choice<string, string>) =
            match param with
            | Choice1Of2 value -> parseDecimal value
            | Choice2Of2 _ -> None

        match parseQueryParam maybeNewThreshold with
        | Some newThreshold ->
            match! thresholdAgent.SetThreshold(newThreshold) with
            | Ok _ -> return! CREATED "Threshold updated successfully" ctx
            | Error err -> return! BAD_REQUEST ("Error updating threshold: " + err) ctx
        | None ->
            return! BAD_REQUEST "Invalid or missing 'newThreshold' parameter" ctx
    }

let getThreshold(ctx: HttpContext) : Async<HttpContext option> =
    async {
        let! threshold = thresholdAgent.GetThreshold()
        return! OK (threshold.ToString()) ctx
    }



