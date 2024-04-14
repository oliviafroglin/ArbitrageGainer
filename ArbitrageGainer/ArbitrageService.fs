module ArbitrageService

open System
open FSharp.Data
open MarketDataRetrieval
open ArbitrageOpportunityIdentifier
open ArbitrageModels

// Define a type provider for the market data JSON schema
type MarketData = JsonProvider<"""{"ev":"Q","sym":"MSFT","bx":4,"bp":114.125,"bs":100,"ax":7,"ap":114.128,"as":160,"c":0,"i":[604],"t":1536036818784,"q":50385480,"z":3}""">

// Service function to get a list of cryptocurrencies to subscribe to based on the number of cryptocurrencies to subscribe to, historical pairs, and cross-traded pairs
let getCryptoSubscriptions (numOfCryptoToSub: int) (historicalCryptoPairs: (string * int) list) (crossTradedCryptoPairs: string list) =
    let selectedPairs = selectCryptoPairsToSub numOfCryptoToSub historicalCryptoPairs crossTradedCryptoPairs
    selectedPairs
    // Convert the selected pairs to a list of strings in the format "Q.<CryptoPair>" and concatenate them with a comma
    |> List.map fst
    |> List.map (fun pair -> "Q." + pair)
    |> String.concat ","

// Service function to split a JSON string into a list of JSON objects
let splitJsonObjects (jsonString: string): string list =
        // Trim the JSON string to remove the outermost curly braces and square brackets
        let jsonStringTrimmed = jsonString.TrimStart('{').TrimEnd('}').TrimStart('[').TrimEnd(']')
        // Split the JSON string by the "},{" delimiter
        jsonStringTrimmed.Split("],[")
        // Add the outermost curly braces to each JSON object
        |> Array.map (fun str -> sprintf "{%s}" str)
        |> List.ofArray

// Service function to parse a JSON string into a Quote object
let tryParseQuote (message: string) : Result<Quote, string> =
    try
        let data = MarketData.Parse(message)
        // Create a Quote object from the parsed JSON data
        Success {
            CryptoPair = data.Sym
            BidPrice = data.Bp
            AskPrice = data.Ap
            BidQuantity = decimal data.Bs
            AskQuantity = decimal data.``As``
            ExchangeId = data.Bx
        }
    with
    // Handle any exceptions that occur during parsing
    | :? System.Exception as ex ->
        Failure (sprintf "Failed to parse quote due to error: %s" ex.Message)

// Service function to update the market data cache with the latest quote
let updateAndProcessQuote (cache: (string * int * Quote) list) (quote: Quote) (config: TradingConfig) (accumulatedTradingValue: decimal) =
    match isRelevantExchange quote with
    | true ->
        // Update the market data cache with the latest quote
        let updatedCache = updateMarketDataCache cache quote
        // Identify any arbitrage opportunities based on the updated cache, quote, and trading configuration
        let newAccTradingValue, opportunity = identifyArbitrageOpportunity updatedCache quote config.MinimalPriceSpread config.MinimalProfit config.MaximalTotalTransactionValue config.MaximalTradingValue accumulatedTradingValue
        printfn "Updated market data cache: %A" updatedCache
        printfn "Accumulated trading value: %M" newAccTradingValue
        printfn "Opportunity: %A" opportunity
        (updatedCache, newAccTradingValue, opportunity)
    | false ->
        printfn "Quote from exchange ID %d ignored" quote.ExchangeId
        (cache, accumulatedTradingValue, None)


// Service function to process a list of JSON strings containing market data quotes    
let processQuotes (cache: (string * int * Quote) list) (jsonString: string) (config: TradingConfig) (accumulatedTradingValue: decimal)=
    // Split the JSON string into a list of JSON objects
    let jsonStrings = splitJsonObjects jsonString
    // Process each JSON object in the list using railway-oriented programming
    jsonStrings |> List.fold (fun (currentCache, currentAccVal) jsonString ->
        match tryParseQuote jsonString with
        | Success quote ->
            // Update the market data cache with the latest quote and identify any arbitrage opportunities
            let updatedCache, newAccVal, opportunity = updateAndProcessQuote currentCache quote config currentAccVal
            match opportunity with
            // TODO: Call Order Execution Service to execute the arbitrage opportunity
            | Some arb ->
                printfn "Opportunity to execute: %A" arb
            | None -> ()
            (updatedCache, newAccVal)
        | Failure errorMsg ->
            (currentCache, currentAccVal)
    ) (cache, accumulatedTradingValue)
