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

type KrakenInnerRes = {
    order: string
}

type KrakenNestedRes = {
    descr: KrakenInnerRes
    txid: string[]
}

type KrakenDecodeRes = {
    error: string[]
    result: KrakenNestedRes
}

type KrakenStatusDescr = {
    Pair: string
    OrderType: string
    Price: string
    Price2: string
    Leverage: string
    Order: string
    Close: string
}

type KrakenNestedStatus = {
    Refid: string
    Userref: int
    Status: string
    Reason: string option
    Opentm: decimal
    Closetm: decimal
    Starttm: int
    Expiretm: int
    Descr: KrakenStatusDescr
    Vol: string
    Vol_exec: string
    Cost: string
    Fee: string
    Price: string
    Stopprice: string
    Limitprice: string
    Misc: string
    Oflags: string
    Trigger: string
    Trades: string[]
}

type KrakenStatusRes = {
    Error: string[]
    Result: Map<string, KrakenNestedStatus>
}

type BitstampTransaction = {
    Tid: string
    Price: string
    Executed: string
    USD: string
    Fee: string
    Datetime: string
    Type: int
}

type BitstampStatusRes = {
    Id: string
    Datetime: string
    OrderType: string
    Status: string
    Market: string
    Transactions: BitstampTransaction[]
    AmountRemaining: string
    ClientOrderId: string
}

type UnifiedStatusRes =
    | KrakenStatus of KrakenStatusRes
    | BitstampStatus of BitstampStatusRes
    | BitfinexStatus of decimal

type ApiResponse<'T> = {
    Error: string list
    Result: 'T
}

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