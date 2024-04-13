module PnLCalculationCore

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

type ProfitLoss = Profit of decimal | Loss of decimal

    let calculateProfitLoss transaction =
    let profit = match transaction.TransactionType with
    | Buy -> (transaction.SalePrice - transaction.PurchasePrice) * transaction.Amount
    | Sell -> (transaction.PurchasePrice - transaction.SalePrice) * transaction.Amount
Profit profit