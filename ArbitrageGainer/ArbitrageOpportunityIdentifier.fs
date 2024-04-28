module ArbitrageOpportunityIdentifier

open ArbitrageModels

let convertExIdToName id =
    match id with
    | 2 -> "Bitfinex"
    | 6 -> "Bitstamp"
    | 23 -> "Kraken"
    | _ -> "Unknown"

// Function to identify arbitrage opportunities based on the market data cache, quote, and trading configuration
let identifyArbitrageOpportunity 
    (cache: (string * int * Quote) list)
    (quote: Quote)
    (minimalPriceSpread: decimal)
    (minimalProfit: decimal)
    (maximalTotalTransactionValue: decimal)
    (maximalTradingValue: decimal)
    (accTradingValue: decimal) =
    cache |> List.fold (fun (accTradingVal, arbOpportunity) (cryptoPair, exchangeId, otherQuote) ->
        // Check if it is the same crypto pair and different exchanges
        match cryptoPair = quote.CryptoPair && exchangeId <> quote.ExchangeId with
        | false -> (accTradingVal, arbOpportunity)
        | true ->
            // printfn "\n\n\nIdentifying arbitrage opportunity for crypto pair %s" quote.CryptoPair
            // printfn "current cache: %A" cache
            // printfn "incoming quote: %A" quote
            // printfn "Quote from exchange %s: %M %M" (convertExIdToName exchangeId) otherQuote.BidPrice otherQuote.AskPrice
            // Calculate the price spreads for buying from one exchange and selling to the other
            let spreadBuyFromOther = quote.BidPrice - otherQuote.AskPrice
            let spreadSellToOther = otherQuote.BidPrice - quote.AskPrice
            
            // Check if it is buying from other exchange or selling to other exchange
            let buyFromOther = spreadBuyFromOther >= minimalPriceSpread
            let sellToOther = spreadSellToOther >= minimalPriceSpread

            // Check if the price spreads are greater than or equal to the minimal price spread
            match buyFromOther, sellToOther with
            | true, _ | _, true ->
                // printfn "\n\n\n--- Spread diff found ---"
                // Determine the buy and sell prices, quantities, transaction value, and profit
                let buyPrice, sellPrice, quantity, exchangeToBuyFrom, exchangeToSellTo = 
                    // Buy from the exchange with the higher bid price and sell to the exchange with the lower ask price
                    match buyFromOther with
                    | true -> otherQuote.AskPrice, quote.BidPrice, min otherQuote.AskQuantity quote.BidQuantity, convertExIdToName exchangeId, convertExIdToName quote.ExchangeId
                    | _ -> quote.AskPrice, otherQuote.BidPrice, min quote.AskQuantity otherQuote.BidQuantity, convertExIdToName quote.ExchangeId, convertExIdToName exchangeId
                let transactionValue = (buyPrice + sellPrice) * quantity
                let profit = (sellPrice - buyPrice) * quantity
                // printfn "Quantity: %M, Transaction value: %M, Profit: %M" quantity transactionValue profit
                // printfn "Buy from %s at %M, Sell to %s at %M" exchangeToBuyFrom buyPrice exchangeToSellTo sellPrice

                // Check if the trading conditions are met
                match profit >= minimalProfit, transactionValue <= maximalTotalTransactionValue, (accTradingVal + transactionValue) <= maximalTradingValue with
                // Return the accumulated trading value and the arbitrage opportunity
                | true, true, true ->
                    let opportunity = {
                        CryptoCurrencyPair = quote.CryptoPair
                        ExchangeToBuyFrom = convertExIdToName exchangeId
                        BuyPrice = buyPrice
                        BuyQuantity = quantity
                        ExchangeToSellTo = convertExIdToName quote.ExchangeId
                        SellPrice = sellPrice
                        SellQuantity = quantity
                    }
                    (accTradingVal + transactionValue, Some opportunity)
                | _ -> (accTradingVal, arbOpportunity)
            | _ -> (accTradingVal, arbOpportunity)
    ) (accTradingValue, None)
