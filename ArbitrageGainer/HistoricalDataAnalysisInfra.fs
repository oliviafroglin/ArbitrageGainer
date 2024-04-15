module HistoricalDataAnalysisInfra

open System.IO
open Newtonsoft.Json
open MySql.Data.MySqlClient
open HistoricalDataAnalysisCore
open HistoricalDataAnalysisService

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
    with
    | ex ->
        printfn "Error initializing database: %s" ex.Message
    connection.Close()

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
    with
    | ex ->
        printfn "Error saving opportunities to database: %s" ex.Message
    connection.Close()

// Reads market data from a file.
let readMarketDataFromFile (filePath: string): MarketData list =
    let json = File.ReadAllText(filePath)
    JsonConvert.DeserializeObject<MarketData list>(json)

// Print method for debugging
let printArbitrageOpportunities (opportunities: ArbitrageOpportunity list) =
    printfn "Arbitrage Opportunities:"
    opportunities |> List.iter (fun opportunity ->
        printfn "Pair: %s, Number of Opportunities: %d" opportunity.Pair opportunity.NumberOfOpportunities)

// Writes arbitrage opportunities to a file.
let writeOpportunitiesToFile(filePath: string, opportunities: ArbitrageOpportunity list) =
    let json = JsonConvert.SerializeObject(opportunities)
    File.WriteAllText(filePath, json)

// Reads market data from a file, identifies arbitrage opportunities, saves them to a database, and writes them to a file.
let getHistoricalSpread () = 
    let filePath = "../historicalData.txt"
    let marketData = readMarketDataFromFile filePath
    let opportunities = identifyArbitrageOpportunities marketData
    printArbitrageOpportunities opportunities
    initializeDatabase ()
    saveOpportunitiesToDatabase opportunities
    writeOpportunitiesToFile("./historicalspread.txt", opportunities)
    opportunities

// printfn "from infrastructure: getHistoricalSpread: %A" (getHistoricalSpread())