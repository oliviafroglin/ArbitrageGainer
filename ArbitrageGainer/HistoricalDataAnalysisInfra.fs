module HistoricalDataAnalysisInfra

open System.IO
open Newtonsoft.Json
open HistoricalDataAnalysisCore
open HistoricalDataAnalysisService

let readMarketDataFromFile (filePath: string): MarketData list =
    let json = File.ReadAllText(filePath)
    JsonConvert.DeserializeObject<MarketData list>(json)

let printArbitrageOpportunities (opportunities: (string * int) list) =
    printfn "Arbitrage Opportunities:"
    opportunities |> List.iter (fun (pair, numOpportunities) ->
        printfn "Pair: %s, Number of Opportunities: %d" pair numOpportunities)


let filePath = "../historicalData.txt"
let marketData = readMarketDataFromFile filePath
let opportunities = identifyArbitrageOpportunities marketData
printArbitrageOpportunities opportunities