module HistoricalDataAnalysisInfra

open System.IO
open Newtonsoft.Json
open MySql.Data.MySqlClient
open HistoricalDataAnalysisCore
open HistoricalDataAnalysisService
open System

// Define Errors.
type DatabaseError =
    | ConnectionFailed of Exception
    | QueryFailed of Exception

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;SslMode=Required;"

// Initializes the database by creating a table to store historical spread data.
let initializeDatabase () =
    let connection = new MySqlConnection(connectionString)
    let commandText = """
        DROP TABLE IF EXISTS historical_spread;
        CREATE TABLE historical_spread (
            Pair VARCHAR(255),
            NumberOfOpportunities INT
        );
    """
    try
        connection.Open()
        let command = new MySqlCommand(commandText, connection)
        command.ExecuteNonQuery() |> ignore
        connection.Close()
        Ok ()
    with
    | ex ->
        connection.Close()
        Error (ConnectionFailed ex)

// Saves a list of arbitrage opportunities to the database.
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
        connection.Close()
        Ok ()
    with
    | ex ->
        connection.Close()
        Error (QueryFailed ex)

// Reads market data from a file.
let readMarketDataFromFile (filePath: string): MarketData list =
    let json = File.ReadAllText(filePath)
    JsonConvert.DeserializeObject<MarketData list>(json)

// Print method for debugging
let printArbitrageOpportunities (opportunities: ArbitrageOpportunity list) =
    printfn "Arbitrage Opportunities:"
    opportunities |> List.iter (fun opportunity ->
        printfn "Pair: %s, Number of Opportunities: %d" opportunity.Pair opportunity.NumberOfOpportunities)

// Reads market data from a file, identifies arbitrage opportunities, saves them to a database, and writes them to a file.
let getHistoricalSpread () = 
    let filePath = "../historicalData.txt"
    let marketData = readMarketDataFromFile filePath
    let opportunities = identifyArbitrageOpportunities marketData
    printArbitrageOpportunities opportunities
    match initializeDatabase () with
    | Ok _ -> 
        match saveOpportunitiesToDatabase opportunities with
        | Ok _ -> Ok opportunities  // Return the opportunities when everything is successful
        | Error e -> Error (sprintf "Failed to save data: %A" e)
    | Error e -> Error (sprintf "Failed to initialize database: %A" e)
