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
    if transaction.Amount <= 0M || transaction.PurchaseCost < 0M || transaction.SaleRevenue < 0M then
        Error (InvalidTransaction "Transaction contains invalid values.")
    else
        match transaction.TransactionType with
        | Buy -> Ok (Profit (transaction.SaleRevenue - (transaction.PurchaseCost * transaction.Amount)))
        | Sell -> Ok (Profit ((transaction.SaleRevenue * transaction.Amount) - transaction.PurchaseCost))

let calculateTotalPnL (transactions: CompletedTransaction list) : Result<ProfitLoss, PnLCalculationError> =
    let rec helper acc transactions =
        match transactions with
        | [] -> Ok acc
        | t::ts ->
            match calculateProfitLoss t with
            | Error e -> Error e
            | Ok (Profit p) -> 
                helper (match acc with
                        | Profit accP -> Profit (accP + p)
                        | Loss accL -> if accL > p then Loss (accL - p) else Profit (p - accL)) ts
            | Ok (Loss l) -> 
                helper (match acc with
                        | Profit accP -> if accP > l then Profit (accP - l) else Loss (l - accP)
                        | Loss accL -> Loss (accL + l)) ts
    helper (Profit 0M) transactions
