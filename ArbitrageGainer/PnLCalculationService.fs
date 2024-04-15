module PnLCalculationService

open PnLCalculationCore

let calculateTotalPnL transactions =
    transactions |> List.map calculateProfitLoss |> List.sumBy (function Profit p -> p | Loss l -> l)

let calculatePnL (transaction:CompletedTransaction) =
    calculateProfitLoss transaction

let calculateHistoricalPnL transactions dateRange =
    match transactions with
    | [] -> Error "No transactions found in the given date range."
    | _ ->
        let filteredTransactions = transactions |> List.filter (fun t -> t.TransactionDate >= dateRange.StartDate && t.TransactionDate <= dateRange.EndDate)
        match filteredTransactions with
        | [] -> Error "No transactions found in the given date range."
        | _ ->
            let totalPnL = calculateTotalPnL filteredTransactions
            match totalPnL with
            | pnl when pnl >= 0M -> Ok (Profit pnl)
            | pnl -> Ok (Loss (-pnl))

