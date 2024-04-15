module ArbitrageInfra

open System
open System.Net
open System.Net.WebSockets
open System.Text.Json
open System.Threading
open System.Text
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open FSharp.Data
open ArbitrageService
open ArbitrageModels
open System.Globalization
open MySql.Data.MySqlClient

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;SslMode=Required;"

let fetchCrossAndHistPairs =
    use connection = new MySqlConnection(connectionString)
    connection.Open()

    // Command Text for cross_traded_pairs table
    let crossCommandText = "SELECT * FROM `cross_traded_pairs`;"
    use crossCmd = new MySqlCommand(crossCommandText, connection)
    use crossReader = crossCmd.ExecuteReader()

    // A function to read and parse the cross-traded pairs from the database
    let rec readCrossPairs acc =
        match crossReader.Read() with
        | true ->
            let baseCurrency = crossReader.GetString(crossReader.GetOrdinal("BaseCurrency"))
            let quoteCurrency = crossReader.GetString(crossReader.GetOrdinal("QuoteCurrency"))
            let pair = sprintf "%s-%s" baseCurrency quoteCurrency
            readCrossPairs (pair :: acc)
        | false ->
            List.rev acc

    let crossPairs = readCrossPairs []

    // Close the first reader
    crossReader.Close()

    // Command Text for historical_spread table
    let historicalCommandText = "SELECT * FROM `historical_spread`;"
    use histCmd = new MySqlCommand(historicalCommandText, connection)
    use histReader = histCmd.ExecuteReader()

    // A function to read and parse the historical spread data from the database
    let rec readHistoricalPairs acc =
        match histReader.Read() with
        | true ->
            let pair = histReader.GetString(histReader.GetOrdinal("Pair"))
            let numberOfOpportunities = histReader.GetInt32(histReader.GetOrdinal("NumberOfOpportunities"))
            readHistoricalPairs ((pair, numberOfOpportunities) :: acc)
        | false ->
            List.rev acc

    let historicalPairs = readHistoricalPairs []

    // Final result as a tuple of both lists
    (crossPairs, historicalPairs)


// A mailbox processor for the trading agent
let tradingAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop tradingActive =
        async {
            let! message = inbox.Receive()
            match message with
            | Start ->
                printfn "Trading started."
                // Set trading to active
                return! loop true
            | Stop ->
                printfn "Trading stopped."
                // Set trading to inactive
                return! loop false
            | CheckStatus replyChannel ->
                // Respond with the current trading state
                replyChannel.Reply(tradingActive)
                return! loop tradingActive
        }
    //Initially, trading is not active.
    loop false
)

// A mailbox processor for the configuration agent
let configAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop (numSubscriptions, minSpread, minProfit, maxTransactionValue, maxTradingValue) =
        async {
            let! message = inbox.Receive()
            match message with
            // Update the configuration with the new values
            | UpdateConfig (newNumSubscriptions, newMinSpread, newMinProfit, newMaxTransactionValue, newMaxTradingValue) ->
                return! loop (newNumSubscriptions, newMinSpread, newMinProfit, newMaxTransactionValue, newMaxTradingValue)
            // Get the current configuration values
            | GetConfig reply ->
                reply.Reply (numSubscriptions, minSpread, minProfit, maxTransactionValue, maxTradingValue)
                return! loop (numSubscriptions, minSpread, minProfit, maxTransactionValue, maxTradingValue)
        }
    // Start the loop with the initial configuration values
    loop (0, 0M, 0M, 0M, 0M))

//Define a function to connect to the WebSocket
let connectToWebSocket (uri: Uri) =
        async {
        let wsClient = new ClientWebSocket()
        //Convert a .NET task into an async workflow
        //Run an asynchronous computation in a non-blocking way
        do! Async.AwaitTask (wsClient.ConnectAsync(uri, CancellationToken.None))
        //Returning Websockets instance from async workflow
        return wsClient
        }
     
// Define a function to receive data from the WebSocket
let receiveData (wsClient: ClientWebSocket) : Async<unit> =
    let buffer = Array.zeroCreate 10024

    let initialCache = []
    let initialAccumulatedTradingValue = 0M

    let rec receiveLoop (cache: (string * int * Quote) list) (accumulatedTradingValue: decimal) = async {
        let segment = new ArraySegment<byte>(buffer)

        let! result = wsClient.ReceiveAsync(segment, CancellationToken.None) |> Async.AwaitTask

        match result.MessageType with
        | WebSocketMessageType.Text ->
            let message = Encoding.UTF8.GetString(buffer, 0, result.Count)
            printfn "Received message: %s" message

            let! isTradingActive = tradingAgent.PostAndAsyncReply CheckStatus

            match isTradingActive with
            // If trading is active, proceed with processing
            | true ->
                let! (_, spread, profit, maxTrans, maxTrade) = configAgent.PostAndAsyncReply GetConfig
                let config = {
                    MinimalPriceSpread = spread
                    MinimalProfit = profit
                    MaximalTotalTransactionValue = maxTrans
                    MaximalTradingValue = maxTrade
                }
                
                let (updatedCache, newAccumulatedValue) = 
                    processQuotes cache message config accumulatedTradingValue

                return! receiveLoop updatedCache newAccumulatedValue
            // If trading is not active, skip processing
            | false ->
                printfn "Trading is inactive. Skipping processing."
                return! receiveLoop cache accumulatedTradingValue
        | _ ->
            return! receiveLoop cache accumulatedTradingValue
    }

    // Start the loop with the initial state
    receiveLoop initialCache initialAccumulatedTradingValue
    
// Define a type for the message we want to send to the WebSocket
type Message = { action: string; params: string }    
// Define a function to send a message to the WebSocket
let sendJsonMessage (wsClient: ClientWebSocket) message =
        let messageJson = JsonSerializer.Serialize(message)
        let messageBytes = Encoding.UTF8.GetBytes(messageJson)
        wsClient.SendAsync((new ArraySegment<byte>(messageBytes)), WebSocketMessageType.Text, true, CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously

// Define a function to start the WebSocket client
// Sample subscripton parameters: "XT.BTC-USD"
// See https://polygon.io/docs/crypto/ws_getting-started
let startWebSocket(uri: Uri, apiKey: string, subscriptionParameters: string) =
            async {
            //Establish websockets connectivity
            //Run underlying async workflow and await the result
            let! wsClient = connectToWebSocket uri
            //Authenticate with Polygon
            sendJsonMessage wsClient { action = "auth"; params = apiKey }
            //Subscribe to market data
            sendJsonMessage wsClient { action = "subscribe" ; params = subscriptionParameters }
            //Process market data
            do! receiveData wsClient
            } |> Async.Start

let updateConfig (ctx : HttpContext) : Async<HttpContext option> =
    let queryList = ctx.request.query

    // A function to find and parse an integer value from the query list
    let findAndParseInt key =
        queryList
        |> List.tryPick (fun (k, v) ->
            match k, v with
            | k, Some value when k = key -> Some value
            | _ -> None)
        |> Option.bind (fun value ->
            match Int32.TryParse value with
            | true, result -> Some result
            | _ -> None)

    // A function to find and parse a decimal value from the query list
    let findAndParseDecimal key =
        queryList
        |> List.tryPick (fun (k, v) ->
            match k, v with
            | k, Some value when k = key -> Some value
            | _ -> None)
        |> Option.bind (fun value ->
            match Decimal.TryParse value with
            | true, result -> Some result
            | _ -> None)
            
    printfn "nSub: %A" (findAndParseInt "nSub")
    printfn "minSpread: %A" (findAndParseDecimal "minSpread")
    printfn "minProfit: %A" (findAndParseDecimal "minProfit")
    printfn "maxTransactionValue: %A" (findAndParseDecimal "maxTransactionValue")
    printfn "maxTradingValue: %A" (findAndParseDecimal "maxTradingValue")

    // Update the configuration agent with the new values
    match (findAndParseInt "nSub",
            findAndParseDecimal "minSpread",
            findAndParseDecimal "minProfit",
            findAndParseDecimal "maxTransactionValue",
            findAndParseDecimal "maxTradingValue") with
            | Some numSubscriptions, Some minSpread, Some minProfit, Some maxTransactionValue, Some maxTradingValue ->
                let configMessage = UpdateConfig (numSubscriptions, minSpread, minProfit, maxTransactionValue, maxTradingValue)
                do configAgent.Post configMessage
                ctx |> OK ("update success")
            | _ ->
                ctx |> BAD_REQUEST ("update failed")
    
let startTrading (ctx : HttpContext) : Async<HttpContext option> =
    let isTradingActive = tradingAgent.PostAndReply (CheckStatus)
    match isTradingActive with
    // Start trading if it is not already active
    | false ->
        let uri = Uri("wss://socket.polygon.io/crypto")
        let apiKey = "phN6Q_809zxfkeZesjta_phpgQCMB2Dw"

        let (crossTradedCryptoPairs, historicalCryptoPairs) = fetchCrossAndHistPairs
        printfn "Historical Crypto Pairs: %A" historicalCryptoPairs
        printfn "Cross-Traded Crypto Pairs: %A" crossTradedCryptoPairs

        let config = configAgent.PostAndReply(GetConfig)
        let (numSubscriptions, _, _, _, _) = config
        let subscriptionParameters = getCryptoSubscriptions numSubscriptions historicalCryptoPairs crossTradedCryptoPairs

        startWebSocket (uri, apiKey, subscriptionParameters)
        tradingAgent.Post (Start)  // Change trading state to active
        ctx |> OK "Trading started"
    | true ->
        ctx |> OK "Trading is already active"

let stopTrading (ctx : HttpContext) : Async<HttpContext option> =
    tradingAgent.Post (Stop)  // Change trading state to inactive
    ctx |> OK "Trading stopped"