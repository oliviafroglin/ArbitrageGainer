module PnLCalculationCore

open System

type TransactionType = Buy | Sell
type CompletedTransaction = {
    TransactionType: TransactionType
    BuyPrice: decimal
    SellPrice: decimal
    Amount: decimal
    TransactionDate: DateTime
}

type ArbitrageOpportunity = {
    CryptoCurrencyPair: string
    ExchangeToBuyFrom: string
    BuyPrice: decimal
    BuyQuantity: decimal
    ExchangeToSellTo: string
    SellPrice: decimal
    SellQuantity: decimal
}

type DateRange = {
    StartDate: DateTime
    EndDate: DateTime
}

type ProfitLoss = Profit of decimal | Loss of decimal

let calculateProfitLoss transaction =
    let netResult = 
        match transaction.TransactionType with
        | Buy -> (transaction.SellPrice - transaction.BuyPrice) * transaction.Amount
        | Sell -> (transaction.SellPrice - transaction.BuyPrice) * transaction.Amount
    match netResult with
    | n when n >= 0m -> 
        Profit n
    | n -> 
        Loss n


let calculateProfitLossForOpportunity opportunity =
    let profit = opportunity.BuyPrice * opportunity.BuyQuantity - opportunity.SellPrice * opportunity.SellQuantity
    Profit profit
