module AnnualizedReturnCalculationInfra

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open AnnualizedReturnCalculation
open System.Globalization

let annualizedReturnHandler =
    POST >=> path "/annualized-return" >=> bindJson<obj> (fun data ->
        async {
            match data with
            | { initialInvestment = iv; totalReturn = tr; startTime = st } ->
                match Double.TryParse(iv), Double.TryParse(tr), DateTime.TryParseExact(st, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | (true, initialInvestment), (true, totalReturn), (true, startTime) ->
                    match calculateAnnualizedReturn initialInvestment totalReturn startTime with
                    | Ok annualizedReturn -> return! CREATED (sprintf "Annualized Return: %f" annualizedReturn)
                    | Error err -> return! BAD_REQUEST (sprintf "Error: %A" err)
                | _ -> return! BAD_REQUEST "Invalid input formats."
            | _ -> return! BAD_REQUEST "Invalid JSON data."
        })

