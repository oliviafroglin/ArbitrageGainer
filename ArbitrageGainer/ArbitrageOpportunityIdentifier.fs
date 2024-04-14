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
            // Calculate the price spreads for buying from one exchange and selling to the other
            let spreadBuyFromOther = quote.BidPrice - otherQuote.AskPrice
            let spreadSellToOther = otherQuote.BidPrice - quote.AskPrice
            // Check if the price spreads are greater than or equal to the minimal price spread
            match spreadBuyFromOther >= minimalPriceSpread, spreadSellToOther >= minimalPriceSpread with
            | true, _ | _, true ->
                // Determine the buy and sell prices, quantities, transaction value, and profit
                let buyPrice, sellPrice, quantity = 
                    // Buy from the exchange with the higher bid price and sell to the exchange with the lower ask price
                    match spreadBuyFromOther >= minimalPriceSpread with
                    | true -> otherQuote.AskPrice, quote.BidPrice, min otherQuote.AskQuantity quote.BidQuantity
                    | _ -> quote.AskPrice, otherQuote.BidPrice, min quote.AskQuantity otherQuote.BidQuantity
                let transactionValue = (buyPrice + sellPrice) * quantity
                let profit = (sellPrice - buyPrice) * quantity
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
