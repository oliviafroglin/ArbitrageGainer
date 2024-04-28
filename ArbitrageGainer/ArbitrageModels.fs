module ArbitrageModels

open System

type Quote = {
    CryptoPair: string
    BidPrice: decimal
    AskPrice: decimal
    BidQuantity: decimal
    AskQuantity: decimal
    ExchangeId: int
}

// type ArbitrageOpportunity = {
//     CryptoCurrencyPair: string
//     ExchangeToBuyFrom: string
//     BuyPrice: decimal
//     BuyQuantity: decimal
//     ExchangeToSellTo: string
//     SellPrice: decimal
//     SellQuantity: decimal
// }
type Exchange = Kraken | Bitstamp | Bitfinex | Unknown

type ArbitrageOpportunity = {
    CryptoCurrencyPair: string
    ExchangeToBuyFrom: Exchange
    ExchangeToSellTo: Exchange
    BuyPrice: decimal
    BuyQuantity: decimal
    SellPrice: decimal
    SellQuantity: decimal
}

type TradingCommand =
    | Start
    | Stop
    | CheckStatus of AsyncReplyChannel<bool>
    
type ConfigMessage =
    | UpdateConfig of int * decimal * decimal * decimal * decimal
    | GetConfig of AsyncReplyChannel<(int * decimal * decimal * decimal * decimal)>

type TradingValue =
    | UpdateTradingValue of decimal
    | GetTradingValue of AsyncReplyChannel<decimal>
    
type TradingConfig = {
    MinimalPriceSpread: decimal
    MinimalProfit: decimal
    MaximalTotalTransactionValue: decimal
    MaximalTradingValue: decimal
}

type Result<'Success,'Failure> =
    | Success of 'Success
    | Failure of 'Failure

type CryptoCurrencyPair = string

type UserDefinedParameters = {
    ProfitThreshold: decimal option
}

type TransactionType = Buy | Sell

type OrderDetails = {
    Pair: string
    Size: decimal
    Price: decimal
    OrderType: TransactionType  // Buy or Sell
}
type OrderResponse = {
    Id: string
    Market: string
    DateTime: string
    Type: string
    Price: decimal
    Amount: decimal
    ClientOrderId: string
    Status: string
    Remaining: decimal
}

type ApiResponse<'T> = {
    Error: string list
    Result: 'T
}

// type Result<'TSuccess, 'TFailure> = 
//     | Success of 'TSuccess 
//     | Failure of 'TFailure

type CompletedTransaction = {
    TransactionType: TransactionType
    Price: decimal
    Amount: decimal
    TransactionDate: DateTime
}

type ProfitMessage =
    | GetProfit of AsyncReplyChannel<decimal>
    | SetProfit of decimal
    | AddProfit of decimal

type EmailMessage =
    | GetEmail of AsyncReplyChannel<string>
    | SetEmail of string

type AutoStopMessage =
    | GetAutoStop of AsyncReplyChannel<bool>
    | SetAutoStop of bool