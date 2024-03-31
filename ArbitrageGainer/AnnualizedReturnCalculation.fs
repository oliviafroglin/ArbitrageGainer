module AnnualizedReturnCalculation

open System

type CashFlowError = 
    | InvalidDuration of string
    | InvalidInitialInvestment of string

let calculateAnnualizedReturn (initialInvestment: double) (totalReturn: double) (startTime: DateTime) =
    match initialInvestment > 0.0, DateTime.UtcNow > startTime with
    | true, true ->
        let duration = DateTime.UtcNow - startTime
        let yearsElapsed = duration.TotalDays / 365.25
        let annualizedReturn = Math.Pow((totalReturn / initialInvestment + 1.0), 1.0 / yearsElapsed) - 1.0
        Ok annualizedReturn
    | false, _ -> Error (InvalidInitialInvestment "Initial investment must be greater than zero.")
    | _, false -> Error (InvalidDuration "Start time must be in the past.")

