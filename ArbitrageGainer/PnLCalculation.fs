module PnLCalculation

type TransactionType = Buy | Sell
type CompletedTransaction = {
    TransactionType: TransactionType
    PurchasePrice: decimal
    SalePrice: decimal
    Amount: decimal
    TransactionDate: DateTime
}
type DateRange = {
    StartDate: DateTime
    EndDate: DateTime
}
type PnLCalculationError = | InvalidTransaction of string
type ProfitLoss = Profit of decimal | Loss of decimal

let calculateProfitLoss (transaction: CompletedTransaction) : Result<ProfitLoss, PnLCalculationError> =
    match transaction with
    | _ when transaction.Amount <= 0M || transaction.PurchaseCost < 0M || transaction.SaleRevenue < 0M ->
        Error (InvalidTransaction "Transaction contains invalid values.")
    | _ ->
        let profit = match transaction.TransactionType with
                     | Buy -> (transaction.SalePrice - transaction.PurchasePrice) * transaction.Amount
                     | Sell -> (transaction.PurchasePrice - transaction.SalePrice )* transaction.Amount
        Ok (Profit profit)

let calculateTotalPnL (transactions: CompletedTransaction list) : Result<ProfitLoss, PnLCalculationError> =
    transactions
    |> List.fold (fun acc transaction -> 
        match acc, calculateProfitLoss transaction with
        | Ok (Profit accP), Ok (Profit p) -> Ok (Profit (accP + p))
        | Error e, _ | _, Error e -> Error e
        | _, _ -> acc) (Ok (Profit 0M))


let calculateHistoricalPnL (transactions: CompletedTransaction list) (dateRange: DateRange) : Result<ProfitLoss, PnLCalculationError> =
    if dateRange.StartDate >= dateRange.EndDate then
        Error (InvalidDateRange "Start date must be before end date.")
    else
        transactions
        |> List.filter (fun t -> t.TransactionDate >= dateRange.StartDate && t.TransactionDate <= dateRange.EndDate)
        |> calculateTotalPnL