module PnLCalculationService

open PnLCalculationCore
open PnLCalculationInfra

let calculateTotalPnL transactions =
    transactions |> List.map PnLCalculationCore.calculateProfitLoss |> List.sumBy (function Profit p -> p | _ -> 0M)

let calculateHistoricalPnL transactions dateRange =
    transactions |> List.filter (fun t -> t.TransactionDate >= dateRange.StartDate && t.TransactionDate <= dateRange.EndDate)
                 |> calculateTotalPnL
