module PnLCalculation

type TransactionType = Buy | Sell
type CompletedTransaction = {
    TransactionType: TransactionType
    PurchaseCost: decimal
    SaleRevenue: decimal
    Amount: decimal
}
type PnLCalculationError = | InvalidTransaction of string
type ProfitLoss = Profit of decimal | Loss of decimal

let calculateProfitLoss (transaction: CompletedTransaction) : Result<ProfitLoss, PnLCalculationError> =
    match transaction with
    | _ when transaction.Amount <= 0M || transaction.PurchaseCost < 0M || transaction.SaleRevenue < 0M ->
        Error (InvalidTransaction "Transaction contains invalid values.")
    | _ ->
        let profit = match transaction.TransactionType with
                     | Buy -> transaction.SaleRevenue - (transaction.PurchaseCost * transaction.Amount)
                     | Sell -> (transaction.SaleRevenue * transaction.Amount) - transaction.PurchaseCost
        Ok (Profit profit)

let calculateTotalPnL (transactions: CompletedTransaction list) : Result<ProfitLoss, PnLCalculationError> =
    transactions
    |> List.fold (fun acc transaction -> 
        match acc, calculateProfitLoss transaction with
        | Ok (Profit accP), Ok (Profit p) -> Ok (Profit (accP + p))
        | Error e, _ | _, Error e -> Error e
        | _, _ -> acc) (Ok (Profit 0M))
