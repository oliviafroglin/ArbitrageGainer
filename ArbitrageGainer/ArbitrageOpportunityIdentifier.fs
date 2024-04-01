module ArbitrageOpportunitiesIdentifier

// Define the quote type
type Quote = {
    CryptoPair: string
    BidPrice: decimal
    AskPrice: decimal
    Timestamp: int64
    ExchangeId: int
}

// Define the arbitrage opportunity type
type ArbitrageOpportunity = {
    CryptoCurrencyPair: string
    ExchangeToBuyFrom: string
    ExchangeToSellTo: string
}

// Convert exchange ID to name
let convertExIdToName id =
    match id with
    | 2 -> "Bitfinex"
    | 3 -> "Bitstamp"
    | 4 -> "Kraken"
    | _ -> "Unknown"

// Initialize the market data cache
let MarketDataCache: Quote list = []

// Update the market data cache with the new quote
let updateMarketDataCache (cache: Quote list) (quote: Quote): Quote list =
    let existingQuoteOption = cache |> List.tryFind (fun q -> q.CryptoPair = quote.CryptoPair && q.ExchangeId = quote.ExchangeId)
    match existingQuoteOption with
    | Some(existingQuote) when existingQuote.Timestamp >= quote.Timestamp ->
        cache
    | _ ->
        let cacheWithoutOldQuote = cache |> List.filter (fun q -> not (q.CryptoPair = quote.CryptoPair && q.ExchangeId = quote.ExchangeId))
        quote :: cacheWithoutOldQuote

// Identify arbitrage opportunities
let identifyArbitrageOpportunity (cache: Quote list) (quote: Quote) (minimalPriceSpread: decimal): unit =
    cache
    |> List.filter (fun q -> q.CryptoPair = quote.CryptoPair && q.ExchangeId <> quote.ExchangeId)
    |> List.iter (fun otherQuote ->
        let spreadBuy = otherQuote.BidPrice - quote.AskPrice
        let spreadSell = quote.BidPrice - otherQuote.AskPrice
        if spreadBuy >= minimalPriceSpread then
            printfn "Arbitrage opportunity found: Buy %s from %s and sell on %s." quote.CryptoPair (convertExIdToName quote.ExchangeId) (convertExIdToName otherQuote.ExchangeId)
        elif spreadSell >= minimalPriceSpread then
            printfn "Arbitrage opportunity found: Buy %s from %s and sell on %s." quote.CryptoPair (convertExIdToName otherQuote.ExchangeId) (convertExIdToName quote.ExchangeId))

// Mock minimal price spread
let minimalPriceSpread = 100M

// Mock quotes
let mockQuotes: Quote list = [
    { CryptoPair = "BTC-USD"; BidPrice = 33000.00M; AskPrice = 33100.00M; Timestamp = 1610462411115L; ExchangeId = 3 }; // First mock quote
    { CryptoPair = "BTC-USD"; BidPrice = 33050.00M; AskPrice = 33150.00M; Timestamp = 1610462412115L; ExchangeId = 2 }; // No arbitrage opportunity
    { CryptoPair = "BTC-USD"; BidPrice = 33200.00M; AskPrice = 33300.00M; Timestamp = 1610462413115L; ExchangeId = 4 }; // Arbitrage opportunity
    { CryptoPair = "BTC-USD"; BidPrice = 33200.00M; AskPrice = 33300.00M; Timestamp = 1610462414115L; ExchangeId = 3 }; // New quote for BTC-USD
    { CryptoPair = "ETH-USD"; BidPrice = 1000.00M; AskPrice = 1050.00M; Timestamp = 1610462415115L; ExchangeId = 2 }; // Mock qupte for ETH-USD
    { CryptoPair = "ETH-USD"; BidPrice = 2000.00M; AskPrice = 2050.00M; Timestamp = 1610462416115L; ExchangeId = 2 }; // New quote for ETH-USD
    { CryptoPair = "ETH-USD"; BidPrice = 2150.00M; AskPrice = 2050.00M; Timestamp = 1610462417115L; ExchangeId = 3 }; // Arbitrage opportunity
    { CryptoPair = "XRP-USD"; BidPrice = 110M; AskPrice = 100M; Timestamp = 1610462422115L; ExchangeId = 2 }; // New quote for XRP-USD
    { CryptoPair = "XRP-USD"; BidPrice = 100M; AskPrice = 100M; Timestamp = 1610462423115L; ExchangeId = 3 }; // New quote for XRP-USD
    { CryptoPair = "XRP-USD"; BidPrice = 100M; AskPrice = 10M; Timestamp = 1610462422115L; ExchangeId = 3 }; // Not entered into cache due to timestamp
]

// Function that directs the processing of quotes
let processQuotes (cache: Quote list) (quotes: Quote list) (minimalPriceSpread: decimal): Quote list =
    let initialCache = MarketDataCache

    quotes |> List.fold (fun accCache quote ->
        let newCache = updateMarketDataCache accCache quote
        match List.tryFind (fun q -> q.CryptoPair = quote.CryptoPair && q.ExchangeId = quote.ExchangeId) newCache with
        | Some(existingQuote) when existingQuote.Timestamp = quote.Timestamp ->
            identifyArbitrageOpportunity newCache quote minimalPriceSpread
            newCache
        | _ ->
            newCache
    ) initialCache

// Process all mock quotes with the initial empty cache
let finalCache = processQuotes MarketDataCache mockQuotes minimalPriceSpread