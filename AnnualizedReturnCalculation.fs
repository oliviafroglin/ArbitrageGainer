module AnnualizedReturnCalculation

open System

type CashFlowError = 
    | InvalidDuration of string
    | InvalidInitialInvestment of string

type AnnualizedReturnResult = 
    | Success of decimal
    | Failure of CashFlowError

let calculateAnnualizedReturn (initialInvestment: decimal) (totalReturn: decimal) (startTime: DateTime) : AnnualizedReturnResult =
    if initialInvestment <= 0M then
        Failure (InvalidInitialInvestment "Initial investment must be greater than zero.")
    else
        let endTime = DateTime.UtcNow
        let duration = endTime - startTime
        let yearsElapsed = duration.TotalDays / 365.25
        if yearsElapsed <= 0.0 then
            Failure (InvalidDuration "Duration must be greater than zero.")
        else
            let annualizedReturn = Math.Pow(double (totalReturn / initialInvestment + 1M), 1.0 / double yearsElapsed) - 1.0 |> decimal
            Success annualizedReturn
