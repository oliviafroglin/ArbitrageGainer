module ArbitrageModels

type Quote = {
    CryptoPair: string
    BidPrice: decimal
    AskPrice: decimal
    BidQuantity: decimal
    AskQuantity: decimal
    ExchangeId: int
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

type TradingCommand =
    | Start
    | Stop
    | CheckStatus of AsyncReplyChannel<bool>
    
type ConfigMessage =
    | UpdateConfig of int * decimal * decimal * decimal * decimal
    | GetConfig of AsyncReplyChannel<(int * decimal * decimal * decimal * decimal)>

type TradingConfig = {
    MinimalPriceSpread: decimal
    MinimalProfit: decimal
    MaximalTotalTransactionValue: decimal
    MaximalTradingValue: decimal
}

type Result<'Success,'Failure> =
    | Success of 'Success
    | Failure of 'Failure