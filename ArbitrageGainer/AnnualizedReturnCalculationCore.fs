module AnnualizedReturnCalculationCore

open System
open PnLCalculationCore

type InvestmentDetails = {
    StartDate: DateTime
    InitialInvestment: decimal
    TotalReturn: decimal
    DurationYears: float
}

let calculateInitialInvestment (transactions: CompletedTransaction list) =
    transactions
    |> List.fold (fun acc t -> 
        match t.TransactionType with
        | Buy -> acc - (t.Price * t.Amount)
        | Sell -> acc + (t.Price * t.Amount)
        ) 0m

let calculateAnnualizedReturn (investment: InvestmentDetails) =
    match investment.DurationYears with
    | 0.0 -> raise (DivideByZeroException "Cannot calculate return for zero duration.")
    | _ ->
        let ratio = investment.TotalReturn / investment.InitialInvestment
        let exponent = 1.0 / investment.DurationYears
        Math.Pow(Convert.ToDouble(ratio), exponent) - 1.0



