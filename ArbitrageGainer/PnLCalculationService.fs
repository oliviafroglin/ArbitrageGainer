module PnLCalculationService

open PnLCalculationCore

let calculateTotalPnL transactions =
    transactions |> List.map calculateProfitLoss |> List.sumBy (function Profit p -> p | Loss l -> l)

let calculatePnL opportunity =
    calculateProfitLossForOpportunity opportunity

let calculateHistoricalPnL transactions dateRange =
    if List.isEmpty transactions then
        Error "No transactions found in the given date range."
    else
        let filteredTransactions = transactions |> List.filter (fun t -> t.TransactionDate >= dateRange.StartDate && t.TransactionDate <= dateRange.EndDate)
        if List.isEmpty filteredTransactions then
            Error "No transactions found in the given date range."
        else
            let totalPnL = calculateTotalPnL filteredTransactions
            if totalPnL >= 0M then Ok (Profit totalPnL)
            else Ok (Loss (-totalPnL))
