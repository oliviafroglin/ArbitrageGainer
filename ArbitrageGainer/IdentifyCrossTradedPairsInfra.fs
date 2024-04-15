module IdentifyCrossTradedPairsInfra

open IdentifyCrossTradedPairsCore
open IdentifyCrossTradedPairsService

open System
open System.Net.Http
open FSharp.Data
open System.Threading.Tasks
open System.IO
open MySql.Data.MySqlClient

let httpClient = new HttpClient()

// Fetches currency pairs from Bitfinex.
let fetchCurrencyPairsFromBitfinex () =
    async {
        try
            let url = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:exchange"
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            printfn "Response: %A" response
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let data = JsonValue.Parse(content)
            return data.[0].AsArray() |> Array.map (fun x -> 
                let pair = x.AsString()
                match pair.Contains(":") with
                | true -> pair.Split(':')
                | false ->
                    let middleIndex = pair.Length / 2
                    [| pair.[0..middleIndex-1]; pair[middleIndex..] |]
                ) |> Array.choose (fun arr -> 
                    match arr.Length = 2 with 
                    | true -> Some (arr.[0], arr.[1])
                    | false -> None)
        with
        | ex ->
            printfn "Error: %A" ex
            return [||]
    }

// Fetches currency pairs from Bitstamp.
let fetchCurrencyPairsFromBitstamp () =
    async {
        try
            let url = "https://www.bitstamp.net/api/v2/ticker/"
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            printfn "Response: %A" response
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let data = JsonValue.Parse(content)
            return data.AsArray() |> Array.map (fun x -> x.["pair"].AsString().Replace("/", ":").Split(':')) |> Array.choose (fun arr -> 
                    match arr.Length = 2 with 
                    | true -> Some (arr.[0], arr.[1])
                    | false -> None)
        with
        | ex ->
            printfn "Error: %A" ex
            return [||]
    }

// Fetches currency pairs from Kraken.
let fetchCurrencyPairsFromKraken () =
    async {
        try
            let url = "https://api.kraken.com/0/public/AssetPairs"
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            printfn "Response: %A" response
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let data = JsonValue.Parse(content)
            return data.["result"].Properties() |> Array.map (fun (_, v) -> (v.["base"].AsString(), v.["quote"].AsString()))
        with
        | ex ->
            printfn "Error: %A" ex
            return [||]
    }

// Saves a set of currency pairs to a file.
let saveSetToFile filePath (currencyPairs : Set<CurrencyPair>) =
    let content = 
        currencyPairs
        |> Set.map (fun (b, quote) -> sprintf "%s, %s" b quote)
        |> Set.fold (fun acc pair -> acc + pair + "\n") ""
    File.WriteAllText(filePath, content)

// Initializes the database by creating a table to store cross-traded pairs.
let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;SslMode=Required;"

let initializeDatabase () =
    let connection = new MySqlConnection(connectionString)
    let commandText = """
        DROP TABLE IF EXISTS cross_traded_pairs;
        CREATE TABLE cross_traded_pairs (
            BaseCurrency VARCHAR(255),
            QuoteCurrency VARCHAR(255),
            PRIMARY KEY (BaseCurrency, QuoteCurrency)
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

// Saves a list of currency pairs to the database.
let savePairsToDatabase (pairs: (string * string) list) =
    let connection = new MySqlConnection(connectionString)
    let insertCommand = "INSERT INTO cross_traded_pairs (BaseCurrency, QuoteCurrency) VALUES (@base, @quote)"
    try
        connection.Open()
        pairs |> List.iter (fun (b, quote) ->
            let command = new MySqlCommand(insertCommand, connection)
            command.Parameters.AddWithValue("@base", b)
            command.Parameters.AddWithValue("@quote", quote)
            command.ExecuteNonQuery() |> ignore)
    with
    | ex ->
        printfn "Error saving cross pairs to database: %s" ex.Message
    connection.Close()

// Identifies cross-traded pairs by fetching currency pairs from Bitfinex, Bitstamp, and Kraken.
let identifyCrossTradedPairs () =
    async {
        let! bitfinexPairs = fetchCurrencyPairsFromBitfinex ()
        let! bitstampPairs = fetchCurrencyPairsFromBitstamp ()
        let! krakenPairs = fetchCurrencyPairsFromKraken ()

        let bitfinexSet = Set.ofArray bitfinexPairs
        let bitstampSet = Set.ofArray bitstampPairs
        let krakenSet = Set.ofArray krakenPairs
        
        let bitfinexSet1 = processCurrencyPairs bitfinexSet
        let bitstampSet1 = processCurrencyPairs bitstampSet
        let krakenSet1 = processKrakenCurrencyPairs krakenSet

        let crossTradedPairs = IdentifyCrossTradedPairsService(bitfinexSet1, bitstampSet1, krakenSet1)

        saveSetToFile "./crossTradedPairs.txt" crossTradedPairs

        initializeDatabase()
        savePairsToDatabase(Set.toList crossTradedPairs)

        printfn "Stored cross-traded pairs: %A" crossTradedPairs

        return crossTradedPairs
    }

// let printCrossTradedPairs () =
//     async {
//         let crossTradedPairs = identifyCrossTradedPairs () |> Async.RunSynchronously
//         crossTradedPairs |> Set.iter (fun (pair1, pair2) -> printfn "%s-%s" pair1 pair2)
//     }

// printCrossTradedPairs () |> Async.RunSynchronously
// identifyCrossTradedPairs () |> Async.RunSynchronously