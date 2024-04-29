module IdentifyCrossTradedPairsInfra

open IdentifyCrossTradedPairsCore
open IdentifyCrossTradedPairsService

open System
open System.Net.Http
open FSharp.Data
open System.Threading.Tasks
open System.IO
open MySql.Data.MySqlClient

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Writers
open Newtonsoft.Json
open System.Globalization

open Logging.Logger

// Define Errors.
type DatabaseError =
    | ConnectionFailed of Exception
    | QueryFailed of Exception

type FetchError = 
    | HttpError of string
    | ParsingError of string
    | DatabaseError of string

let httpClient = new HttpClient()

// Fetches currency pairs from Bitfinex.
let fetchCurrencyPairsFromBitfinex () =
    async {
        try
            let url = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:exchange"
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            // printfn "Response: %A" response
            match response.IsSuccessStatusCode with
            | false -> return Error (HttpError response.ReasonPhrase)
            | true ->
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                let data = JsonValue.Parse(content)
                return Ok (data.[0].AsArray() |> Array.map (fun x -> 
                    let pair = x.AsString()
                    match pair.Contains(":") with
                    | true -> pair.Split(':')
                    | false ->
                        let middleIndex = pair.Length / 2
                        [| pair.[0..middleIndex-1]; pair[middleIndex..] |]
                    ) |> Array.choose (fun arr -> 
                        match arr.Length = 2 with 
                        | true -> Some (arr.[0], arr.[1])
                        | false -> None))
        with
        | ex ->
            return Error (ParsingError ex.Message)
    }

// Fetches currency pairs from Bitstamp.
let fetchCurrencyPairsFromBitstamp () =
    async {
        try
            let url = "https://www.bitstamp.net/api/v2/ticker/"
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            // printfn "Response: %A" response
            match response.IsSuccessStatusCode with
            | false -> return Error (HttpError response.ReasonPhrase)
            | true ->
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                let data = JsonValue.Parse(content)
                return Ok (data.AsArray() |> Array.map (fun x -> x.["pair"].AsString().Replace("/", ":").Split(':')) |> Array.choose (fun arr -> 
                        match arr.Length = 2 with 
                        | true -> Some (arr.[0], arr.[1])
                        | false -> None))
        with
        | ex ->
            return Error (ParsingError ex.Message)
    }

// Fetches currency pairs from Kraken.
let fetchCurrencyPairsFromKraken () =
    async {
        try
            let url = "https://api.kraken.com/0/public/AssetPairs"
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            // printfn "Response: %A" response
            match response.IsSuccessStatusCode with
            | false -> return Error (HttpError response.ReasonPhrase)
            | true ->
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                let data = JsonValue.Parse(content)
                return Ok (data.["result"].Properties() |> Array.map (fun (_, v) -> (v.["base"].AsString(), v.["quote"].AsString())))
        with
        | ex ->
            return Error (ParsingError ex.Message)
    }

// Initializes the database by creating a table to store cross-traded pairs.
let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Password=Functional!;SslMode=Required;"
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
        connection.Close()
        Ok()
    with
    | ex ->
        connection.Close()
        Error (ConnectionFailed ex)

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
        connection.Close()
        Ok()
    with
    | ex ->
        connection.Close()
        Error (QueryFailed ex)

// Identifies cross-traded pairs by fetching currency pairs from Bitfinex, Bitstamp, and Kraken.
let identifyCrossTradedPairs () =
    let logger = createLogger

    async {

        logger "Starting Identify Cross-Traded Pairs"
        let startTime = DateTime.Now

        let! bitfinexResult = fetchCurrencyPairsFromBitfinex ()
        let! bitstampResult = fetchCurrencyPairsFromBitstamp ()
        let! krakenResult = fetchCurrencyPairsFromKraken ()

        match bitfinexResult, bitstampResult, krakenResult with
        | Ok bitfinexPairs, Ok bitstampPairs, Ok krakenPairs ->
            let bitfinexSet = Set.ofArray bitfinexPairs
            let bitstampSet = Set.ofArray bitstampPairs
            let krakenSet = Set.ofArray krakenPairs

            let bitfinexSet1 = processCurrencyPairs bitfinexSet
            let bitstampSet1 = processCurrencyPairs bitstampSet
            let krakenSet1 = processKrakenCurrencyPairs krakenSet
            
            let crossTradedPairs = IdentifyCrossTradedPairsService(bitfinexSet1, bitstampSet1, krakenSet1)

            let endTime = DateTime.Now
            logger (sprintf "Identify Cross-Traded Pairs completed in %A seconds" (endTime - startTime))

            match initializeDatabase (), savePairsToDatabase (Set.toList crossTradedPairs) with
            | Ok (), Ok () ->
                // printfn "Stored cross-traded pairs: %A" crossTradedPairs
                let crossTradedPairsList = Set.toList crossTradedPairs
                // crossTradedPairsList |> List.iter (fun (b, quote) -> printfn "Base: %s, Quote: %s" b quote)
                return Ok (crossTradedPairsList)
            | Error e, _ -> return Error (sprintf "Failed to initialize database: %A" e)
            | _, Error e -> return Error (sprintf "Failed to save data: %A" e)
        | Error e, _, _ -> return Error (sprintf "Failed to fetch currency pairs from Bitfinex: %A" e)
        | _, Error e, _ -> return Error (sprintf "Failed to fetch currency pairs from Bitstamp: %A" e)
        | _, _, Error e -> return Error (sprintf "Failed to fetch currency pairs from Kraken: %A" e)
    }

let identifyCrossTradedPairsHandler (ctx: HttpContext) : Async<HttpContext option> =
    async {
        let! result = identifyCrossTradedPairs ()
        match result with
        | Ok pairs -> return! Successful.OK (JsonConvert.SerializeObject(pairs)) ctx
        | Error e -> return! BAD_REQUEST e ctx
    }