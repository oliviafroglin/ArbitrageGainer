module HistoricalDataAnalysisInfra

open System.IO
open Newtonsoft.Json
open MySql.Data.MySqlClient
open HistoricalDataAnalysisCore
open HistoricalDataAnalysisService

let connectionString = "Server=34.42.239.81;Database=orders;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;default command timeout=60"

let initializeDatabase () =
    // let connection = new MySqlConnection(connectionString)
    let commandText = """
        DROP TABLE IF EXISTS historical_spread;
        CREATE TABLE historical_spread (
            Pair VARCHAR(255),
            NumberOfOpportunities INT
        );
    """
    try
        printfn "Connecting to MySQL..."
        let connection = new MySqlConnection(connectionString)
        printfn "sassss: %A" connection.State
        connection.Open()
    with
        | ex -> printfn "Error: %A" ex
        | _ -> ()
    // printfn "Closing connection to MySQL..."
    // connection.Close()

let saveOpportunitiesToDatabase (opportunities: ArbitrageOpportunity list) =
    let connection = new MySqlConnection(connectionString)
    let insertCommand = "INSERT INTO historical_spread (Pair, NumberOfOpportunities) VALUES (@pair, @numOpportunities)"
    try
        connection.Open()
        opportunities |> List.iter (fun { Pair = pair; NumberOfOpportunities = numOpportunities } ->
            let command = new MySqlCommand(insertCommand, connection)
            command.Parameters.AddWithValue("@pair", pair)
            command.Parameters.AddWithValue("@numOpportunities", numOpportunities)
            command.ExecuteNonQuery() |> ignore)
    finally
        connection.Close()

let readMarketDataFromFile (filePath: string): MarketData list =
    let json = File.ReadAllText(filePath)
    JsonConvert.DeserializeObject<MarketData list>(json)

let printArbitrageOpportunities (opportunities: ArbitrageOpportunity list) =
    printfn "Arbitrage Opportunities:"
    opportunities |> List.iter (fun opportunity ->
        printfn "Pair: %s, Number of Opportunities: %d" opportunity.Pair opportunity.NumberOfOpportunities)

let writeOpportunitiesToFile(filePath: string, opportunities: ArbitrageOpportunity list) =
    let json = JsonConvert.SerializeObject(opportunities)
    File.WriteAllText(filePath, json)

let getHistoricalSpread () = 
    let filePath = "../historicalData.txt"
    let marketData = readMarketDataFromFile filePath
    let opportunities = identifyArbitrageOpportunities marketData
    printArbitrageOpportunities opportunities
    // initializeDatabase ()
    // saveOpportunitiesToDatabase opportunities
    writeOpportunitiesToFile("./historicalspread.txt", opportunities)
    opportunities

// printfn "getHistoricalSpread: %A" (getHistoricalSpread())