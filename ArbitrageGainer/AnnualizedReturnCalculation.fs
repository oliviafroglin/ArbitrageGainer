module AnnualizedReturnCalculation

open System

type CashFlowError = 
    | InvalidDuration of string
    | InvalidInitialInvestment of string

let calculateAnnualizedReturn (initialInvestment: decimal) (totalReturn: decimal) (startTime: DateTime) =
    match initialInvestment > 0M, DateTime.UtcNow > startTime with
    | true, true ->
        let duration = DateTime.UtcNow - startTime
        let yearsElapsed = duration.TotalDays / 365.25
        if yearsElapsed > 0.0 then
            let annualizedReturn = Math.Pow(double (totalReturn / initialInvestment + 1M), 1.0 / double yearsElapsed) - 1.0 |> decimal
            Ok annualizedReturn
        else
            Error (InvalidDuration "Duration must be greater than zero.")
    | false, _ -> Error (InvalidInitialInvestment "Initial investment must be greater than zero.")
    | _, false -> Error (InvalidDuration "Start time must be in the past.")
