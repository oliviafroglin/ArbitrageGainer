module ArbitrageService

open System
open FSharp.Data
open MarketDataRetrieval
open ArbitrageOpportunityIdentifier
open ArbitrageModels

// Define a type provider for the market data JSON schema
type MarketData = JsonProvider<"""{"ev":"XQ","pair":"DOT-USD","lp":0,"ls":0,"bp":6.7818,"bs":14.22,"ap":6.7819,"as":385.28012,"t":1714260364410,"x":23,"r":1714260364453}""">

// A mailbox processor for the accumulated trading value agent
let tradingValueAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop tradingValue =
        async {
            let! message = inbox.Receive()
            match message with
            | UpdateTradingValue newTradingValue ->
                return! loop newTradingValue
            | GetTradingValue replyChannel ->
                replyChannel.Reply tradingValue
                return! loop tradingValue
        }
    loop 0M
)

// Service function to get a list of cryptocurrencies to subscribe to based on the number of cryptocurrencies to subscribe to, historical pairs, and cross-traded pairs
let getCryptoSubscriptions (numOfCryptoToSub: int) (historicalCryptoPairs: (string * int) list) (crossTradedCryptoPairs: string list) =
    let selectedPairs = selectCryptoPairsToSub numOfCryptoToSub historicalCryptoPairs crossTradedCryptoPairs
    selectedPairs
    // Convert the selected pairs to a list of strings in the format "Q.<CryptoPair>" and concatenate them with a comma
    |> List.map fst
    |> List.map (fun pair -> "XQ." + pair)
    |> String.concat ","

// Service function to split a JSON string into a list of JSON objects
let splitJsonObjects (jsonString: string): string list =
        // Trim the JSON string to remove the outermost curly braces and square brackets
        let jsonStringTrimmed = jsonString.TrimStart('[').TrimEnd(']').TrimStart('{').TrimEnd('}')
        // Split the JSON string by the "},{" delimiter
        jsonStringTrimmed.Split("},{")
        // Add the outermost curly braces to each JSON object
        |> Array.map (fun str -> sprintf "{%s}" str)
        |> List.ofArray

// Service function to parse a JSON string into a Quote object
let tryParseQuote (message: string) : Result<Quote, string> =
    try
        let data = MarketData.Parse(message)
        // Create a Quote object from the parsed JSON data
        Success {
            CryptoPair = data.Pair
            BidPrice = data.Bp
            AskPrice = data.Ap
            BidQuantity = decimal data.Bs
            AskQuantity = decimal data.``As``
            ExchangeId = data.X
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
        // printfn "Updated market data cache: %A" updatedCache
        // printfn "Accumulated trading value: %M" newAccTradingValue
        // printfn "Opportunity: %A" opportunity
        (updatedCache, newAccTradingValue, opportunity)
    | false ->
        printfn "Quote from exchange ID %d ignored" quote.ExchangeId
        (cache, accumulatedTradingValue, None)


// Service function to process a list of JSON strings containing market data quotes    
let processQuotes (cache: (string * int * Quote) list) (jsonString: string) (config: TradingConfig) =
    // Split the JSON string into a list of JSON objects

    let jsonStrings = splitJsonObjects jsonString
    // Process each JSON object in the list using railway-oriented programming
    jsonStrings |> List.fold (fun (currentCache) jsonString ->
        match tryParseQuote jsonString with
        | Success quote ->
            printfn "Quote: %A" quote
            // retrieve the current accumulated trading value
            let currentAccVal = tradingValueAgent.PostAndReply GetTradingValue 
            // Update the market data cache with the latest quote and identify any arbitrage opportunities
            let updatedCache, newAccVal, opportunity = updateAndProcessQuote currentCache quote config currentAccVal
            // Update the accumulated trading value
            tradingValueAgent.Post (UpdateTradingValue newAccVal)

            match opportunity with
            | Some arb ->
                // TODO: Call Order Execution Service to execute the arbitrage opportunity
                printfn "Opportunity to execute: %A" arb
            | None -> ()
            updatedCache
        | Failure errorMsg ->
            // printfn "Failed to parse quote due to error: %s" errorMsg
            currentCache
    ) (cache)

// Async version for performance improvement testing
// Service function to process a list of JSON strings containing market data quotes
// let processQuotes (cache: (string * int * Quote) list) (jsonString: string) (config: TradingConfig) : Async<(string * int * Quote) list> =
//     async {
//         // Split the JSON string into a list of JSON objects
//         let jsonStrings = splitJsonObjects jsonString
//         // Process each JSON object in the list using railway-oriented programming
//         return! jsonStrings |> List.foldAsync (fun currentCache jsonString ->
//             async {
//                 match tryParseQuote jsonString with
//                 | Success quote ->
//                     // Retrieve the current accumulated trading value
//                     let! currentAccVal = tradingValueAgent.PostAndAsyncReply GetTradingValue
//                     // Update the market data cache with the latest quote and identify any arbitrage opportunities
//                     let updatedCache, newAccVal, opportunity = updateAndProcessQuote currentCache quote config currentAccVal
//                     // Update the accumulated trading value
//                     do! tradingValueAgent.Post (UpdateTradingValue newAccVal)

//                     match opportunity with
//                     | Some arb ->
//                         // TODO: Call Order Execution Service to execute the arbitrage opportunity
//                         printfn "Opportunity to execute: %A" arb
//                     | None -> ()
//                     return updatedCache
//                 | Failure errorMsg ->
//                     // printfn "Failed to parse quote due to error: %s" errorMsg
//                     return currentCache
//             }
//         ) cache
//     }
