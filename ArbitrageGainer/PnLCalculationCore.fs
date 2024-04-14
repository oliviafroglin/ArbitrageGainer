module PnLCalculationCore

open System

type TransactionType = 
    | Buy
    | Sell

type CompletedTransaction = {
    TransactionType: TransactionType
    Price: decimal
    Amount: decimal
    TransactionDate: DateTime
}

type DateRange = {
    StartDate: DateTime
    EndDate: DateTime
}

type ProfitLoss = Profit of decimal | Loss of decimal

let calculateProfitLoss transaction =
    let netResult = 
        match transaction.TransactionType with
        | Buy -> 
            -transaction.Amount * transaction.Price
        | Sell -> 
            transaction.Amount * transaction.Price
    match netResult with
    | n when n >= 0m -> 
        Profit n
    | n -> 
        Loss n
