type Exchange = Kraken | Bitstamp | Bitfinex
type CryptoCurrencyPair = string

type UserDefinedParameters = {
    MinimalPriceSpreadValue: decimal
    MinimalTransactionProfit: decimal
    MaximalTransactionValue: decimal
    MaximalTradingValue: decimal
    EmailForNotification: string
    ProfitThreshold: decimal option
}

type ArbitrageOpportunity = {
    CryptoCurrencyPair: CryptoCurrencyPair
    ExchangeToBuyFrom: Exchange
    ExchangeToSellTo: Exchange
}

type Quote = {
    CryptoPair: string
    BidPrice: decimal
    AskPrice: decimal
    BidSize: decimal  
    AskSize: decimal  
    Timestamp: int64
    ExchangeId: int
}
let notifyUser email message =
    printfn "Notifying user %s: %s" email message

let updateProfitAndCheckThreshold currentProfit profit userParams =
    let newTotalProfit = currentProfit + profit
    match userParams.ProfitThreshold with
    | Some threshold when newTotalProfit >= threshold -> 
        notifyUser userParams.EmailForNotification "Profit threshold reached."
    | _ -> printfn "Total Profit: %A" newTotalProfit
    newTotalProfit

type Error = QuoteNotFound of string

let simulateFetchQuote (exchange: Exchange) (cryptoPair: CryptoCurrencyPair) : Result<Quote, Error> =
    match exchange, cryptoPair with
    | Bitstamp, "BTC-USD" -> Ok { CryptoPair = "BTC-USD"; BidPrice = 33000m; AskPrice = 33020m; BidSize = 5m; AskSize = 4m; Timestamp = 1610462411115L; ExchangeId = 1 }
    | Kraken, "BTC-USD" -> Ok { CryptoPair = "BTC-USD"; BidPrice = 40000m; AskPrice = 41000m; BidSize = 6m; AskSize = 5m; Timestamp = 1610462411115L; ExchangeId = 2 }
    | Bitstamp, "ETH-USD" -> Ok { CryptoPair = "ETH-USD"; BidPrice = 2000m; AskPrice = 2002m; BidSize = 15m; AskSize = 10m; Timestamp = 1610462411115L; ExchangeId = 1 }
    | Kraken, "ETH-USD" -> Ok { CryptoPair = "ETH-USD"; BidPrice = 2500m; AskPrice = 2510m; BidSize = 20m; AskSize = 15m; Timestamp = 1610462411115L; ExchangeId = 2 }
    | Kraken, "LTC-USD" -> Ok { CryptoPair = "LTC-USD"; BidPrice = 130m; AskPrice = 131m; BidSize = 12m; AskSize = 11m; Timestamp = 1610462411115L; ExchangeId = 2 }
    | Bitfinex, "LTC-USD" -> Ok { CryptoPair = "LTC-USD"; BidPrice = 132m; AskPrice = 133m; BidSize = 8m; AskSize = 7m; Timestamp = 1610462411115L; ExchangeId = 3 }
    | Bitfinex, "XRP-USD" -> Ok { CryptoPair = "XRP-USD"; BidPrice = 0.5m; AskPrice = 0.51m; BidSize = 500m; AskSize = 400m; Timestamp = 1610462411115L; ExchangeId = 3 }
    | Bitstamp, "XRP-USD" -> Ok { CryptoPair = "XRP-USD"; BidPrice = 0.52m; AskPrice = 0.53m; BidSize = 300m; AskSize = 300m; Timestamp = 1610462411115L; ExchangeId = 1 }
    | _, "BCH-USD" -> Ok { CryptoPair = "BCH-USD"; BidPrice = 500m; AskPrice = 502m; BidSize = 30m; AskSize = 25m; Timestamp = 1610462411115L; ExchangeId = if exchange = Kraken then 2 else 1 }
    | _ -> Error (QuoteNotFound "Quote not found for given exchange and crypto pair")

let simulateBuyTransaction opportunity amount price =
    printfn "Simulating buy transaction: %A, Amount: %A, Price: %A" opportunity.CryptoCurrencyPair amount price
    (true, amount) // Return: success status and the amount bought

let simulateSellTransaction opportunity amount price =
    // Randomly simulate full, partial, or no sell for demonstration purposes for now, will connect to API later
    let rnd = System.Random()
    let outcome = rnd.Next(0, 3) // 0 for fail, 1 for partial, 2 for full
    match outcome with
    | 0 -> (false, 0m) // Sell failed
    | 1 -> (true, amount * 0.9m) // Partial sell, assuming 90% of amount gets sold, will modify later with API
    | _ -> (true, amount) // Full sell

let logTransactionToDB transactionType opportunity amount price =
    printfn "Logging %s transaction to DB: %A, Amount: %A, Price: %A" transactionType opportunity.CryptoCurrencyPair amount price


let rec executeTransaction opportunity userParams currentProfit cumulativeTradingValue =
    let buyResult = simulateFetchQuote opportunity.ExchangeToBuyFrom opportunity.CryptoCurrencyPair
    let sellResult = simulateFetchQuote opportunity.ExchangeToSellTo opportunity.CryptoCurrencyPair

    match buyResult, sellResult with
    | Ok buyQuote, Ok sellQuote ->
        let buyPrice = buyQuote.AskPrice
        let sellPrice = sellQuote.BidPrice
        let profitPerUnit = sellPrice - buyPrice

        let maxBuyAmountBasedOnPriceAndValue = userParams.MaximalTransactionValue / (buyPrice + sellPrice)
        let availableBuyAmount = min buyQuote.AskSize sellQuote.BidSize
        let orderQuantity = min availableBuyAmount (int maxBuyAmountBasedOnPriceAndValue)
        let transactionValue = (buyPrice * decimal(orderQuantity)) + (sellPrice * decimal(orderQuantity))
        let potentialNewCumulativeTradingValue = cumulativeTradingValue + transactionValue

        let isSpreadProfitable = profitPerUnit >= userParams.MinimalPriceSpreadValue
        let totalProfit = profitPerUnit * decimal(orderQuantity)
        let isProfitable = totalProfit >= userParams.MinimalTransactionProfit && transactionValue <= userParams.MaximalTransactionValue

        match (isSpreadProfitable, isProfitable, orderQuantity > 0, potentialNewCumulativeTradingValue <= userParams.MaximalTradingValue) with
        | (true, true, true, true) ->
            let (buySuccess, boughtAmount) = simulateBuyTransaction opportunity orderQuantity buyPrice
            match buySuccess with
            | true ->
                let (sellSuccess, soldAmount) = simulateSellTransaction opportunity boughtAmount sellPrice
                match sellSuccess with
                | true when soldAmount < boughtAmount ->
                    let remainingAmount = boughtAmount - soldAmount
                    let newSellPrice = sellPrice * 0.98m // Simulating a 2% decrease for the second sell order
                    let newSellValue = remainingAmount * newSellPrice
                    let newTransactionValue = transactionValue + newSellValue - (remainingAmount * sellPrice)
                    let newProfit = (soldAmount * profitPerUnit) + (remainingAmount * (newSellPrice - buyPrice))

                    logTransactionToDB "Sell" opportunity soldAmount sellPrice
                    logTransactionToDB "Sell" opportunity remainingAmount newSellPrice

                    notifyUser userParams.EmailForNotification $"Partial sell completed. New sell order for remaining amount placed at {newSellPrice}."
                    
                    let updatedTotalProfit = updateProfitAndCheckThreshold currentProfit newProfit userParams
                    let updatedCumulativeTradingValue = cumulativeTradingValue + newTransactionValue
                    (updatedTotalProfit, updatedCumulativeTradingValue)
                | true ->
                    logTransactionToDB "Sell" opportunity soldAmount sellPrice
                    let updatedTotalProfit = updateProfitAndCheckThreshold currentProfit totalProfit userParams
                    let updatedCumulativeTradingValue = cumulativeTradingValue + transactionValue
                    (updatedTotalProfit, updatedCumulativeTradingValue)
                | false ->
                    notifyUser userParams.EmailForNotification "Sell transaction failed. Nothing was sold."
                    (currentProfit, cumulativeTradingValue)
            | false ->
                notifyUser userParams.EmailForNotification "Buy transaction failed. No sell order placed."
                (currentProfit, cumulativeTradingValue)
        | _ ->
            printfn "Transaction does not meet profit criteria, trading limits exceeded, or cumulative trading value would be surpassed."
            (currentProfit, cumulativeTradingValue)
    | Error e, _ | _, Error e ->
        printfn "Failed to fetch quotes due to error: %A" e
        (currentProfit, cumulativeTradingValue)

let main =
    printfn "Starting order execution..."

    let userParams = {
        MinimalPriceSpreadValue = 10m
        MinimalTransactionProfit = 20m
        MaximalTransactionValue = 1000000m
        MaximalTradingValue = 10000000m
        EmailForNotification = "fakeuser@example.com"
        ProfitThreshold = Some 50000m
    }

    let opportunities = [
        { CryptoCurrencyPair = "BTC-USD"; ExchangeToBuyFrom = Bitstamp; ExchangeToSellTo = Kraken };
        { CryptoCurrencyPair = "LTC-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Bitfinex };
        { CryptoCurrencyPair = "ETH-USD"; ExchangeToBuyFrom = Bitstamp; ExchangeToSellTo = Kraken };
        { CryptoCurrencyPair = "XRP-USD"; ExchangeToBuyFrom = Bitfinex; ExchangeToSellTo = Bitstamp };
        { CryptoCurrencyPair = "BCH-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Bitstamp }
    ]

    let finalTotalProfit, _ = opportunities |> List.fold (fun (accProfit, accTradingValue) opp -> 
        executeTransaction opp userParams accProfit accTradingValue) (0m, 0m)

    printfn "Final total profit: %A" finalTotalProfit

main
