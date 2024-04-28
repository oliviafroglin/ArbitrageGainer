module OrderExecutionInfra

open System
open System.Net
open System.Net.Mail
open System.Threading
open MySql.Data.MySqlClient
open Newtonsoft.Json
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open FSharp.Data
open Newtonsoft.Json.Linq

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors

open ArbitrageService
open ArbitrageModels
// open ManagePnLThresholdInfra
// open PnLCalculationCore
// open PnLCalculationService
// run this file separately for this milestone! 

// type Exchange = Kraken | Bitstamp | Bitfinex
// type CryptoCurrencyPair = string

// type UserDefinedParameters = {
//     ProfitThreshold: decimal option
// }

// type ArbitrageOpportunity = {
//     CryptoCurrencyPair: string
//     ExchangeToBuyFrom: Exchange
//     ExchangeToSellTo: Exchange
//     BuyPrice: decimal
//     BuyQuantity: decimal
//     SellPrice: decimal
//     SellQuantity: decimal
// }
// type TransactionType = Buy | Sell

// type OrderDetails = {
//     Pair: string
//     Size: decimal
//     Price: decimal
//     OrderType: TransactionType  // Buy or Sell
// }
// type OrderResponse = {
//     Id: string
//     Market: string
//     DateTime: string
//     Type: string
//     Price: decimal
//     Amount: decimal
//     ClientOrderId: string
//     Status: string
//     Remaining: decimal
// }

// type ApiResponse<'T> = {
//     Error: string list
//     Result: 'T
// }

// type Result<'TSuccess, 'TFailure> = 
//     | Success of 'TSuccess 
//     | Failure of 'TFailure

// type CompletedTransaction = {
//     TransactionType: TransactionType
//     Price: decimal
//     Amount: decimal
//     TransactionDate: DateTime
// }

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;SslMode=Required;"

type BitstampResponse = JsonProvider<"""{"id":"1234","market":"FET/USD","datetime":"2023-12-31 14:43:15.796000","type":"0","price":"22.45","amount":"58.06000000","client_order_id":"123456789"}""">
//     | GetProfit of AsyncReplyChannel<decimal>
//     | SetProfit of decimal

let profitAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop totalProfit =
        async {
            let! message = inbox.Receive()
            match message with
            | GetProfit replyChannel ->
                // Send the current total profit to the requester
                replyChannel.Reply(totalProfit)
                return! loop totalProfit
            | SetProfit newProfit ->
                // Update the total profit with the new value
                printfn "Total profit updated to: %A" newProfit
                return! loop newProfit
            | AddProfit profitToAdd ->
                // Add the profit to the total profit
                printfn "Adding profit: %A" profitToAdd
                return! loop (totalProfit + profitToAdd)
        }
    // Initially, total profit is set to zero
    loop 0m
)

let emailAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop email =
        async {
            let! message = inbox.Receive()
            match message with
            | GetEmail replyChannel ->
                replyChannel.Reply(email)
                return! loop email
            | SetEmail newEmail ->
                printfn "Email updated to: %s" newEmail
                return! loop newEmail
        }
    loop ""
    // for testing, can do: loop "xiaojun3@andrew.cmu", will be integrated to use next milestone
)

let autoStopAgent = MailboxProcessor.Start(fun inbox ->
    let rec loop isAutoStop =
        async {
            let! message = inbox.Receive()
            match message with
            | GetAutoStop replyChannel ->
                replyChannel.Reply(isAutoStop)
                return! loop isAutoStop
            | SetAutoStop newAutoStop ->
                return! loop newAutoStop
        }
    loop true
)

// profitAgent.Post(SetProfit 1000m)
// let profit = profitAgent.PostAndReply(GetProfit)
let sendEmail (toAddress: string) (subject: string) (body: string) : Result<unit, string> =
    let fromAddress = "oliviaxjlin@gmail.com"
    let fromPassword = "glulmzsxusizlpja"
    let smtpHost = "smtp.gmail.com"
    let smtpPort = 587
    let message = new MailMessage(fromAddress, toAddress, subject, body)
    message.IsBodyHtml <- false
    try
        using (new SmtpClient(smtpHost, smtpPort)) <| fun smtpClient ->
            smtpClient.EnableSsl <- true
            smtpClient.Credentials <- new NetworkCredential(fromAddress, fromPassword)
            smtpClient.Send(message)
            printfn "Email sent successfully to %s." toAddress
            Success ()
    with
    | ex -> 
        printfn "Failed to send email. Error: %s" ex.Message
        Failure (sprintf "Failed to send email. Error: %s" ex.Message)

let notifyUser (email: string) (subject: string) (body: string) =
    printfn "Attempting to send email to %s with subject: %s" email subject
    match sendEmail email subject body with
    | Success () ->
        printfn "Email successfully sent to %s." email
    | Failure errMsg ->
        printfn "Failed to send email. Error: %s" errMsg
    ()

let updateProfitAndCheckThreshold  =
    //TODO: Fetch threshold from another module
    let ProfitThreshold : decimal option = Some 10000m
    // Retrieve the current total profit synchronously
    let totalProfit = profitAgent.PostAndReply(GetProfit)
    printfn "Checking threshold: Current Total Profit = %A, Threshold = %A" totalProfit ProfitThreshold

    match ProfitThreshold with
    | Some threshold when totalProfit >= threshold ->
        let isAutoStop = autoStopAgent.PostAndReply(GetAutoStop)
        match isAutoStop with
        | true ->
            tradingAgent.Post (Stop)
        | false ->
            ()
        // Notify the user if the profit threshold has been exceeded
        printfn "Threshold met or exceeded. Triggering email notification."
        let emailUser = emailAgent.PostAndReply(GetEmail)
        notifyUser emailUser "Your Arbitrage Gainer" ("Profit threshold reached: " + string totalProfit)
        () 
    | _ ->
        // Log the current total profit for information
        printfn "Threshold not met. Current Total Profit: %A" totalProfit
        ()

let insertCompletedTransaction (transaction: CompletedTransaction) =
    try
        using (new MySqlConnection(connectionString)) <| fun connection ->
            connection.Open()
            let sql = "INSERT INTO Transactions (TransactionType, Price, Amount, TransactionDate) VALUES (@TransactionType, @Price, @Amount, @TransactionDate)"
            using (new MySqlCommand(sql, connection)) <| fun command ->
                command.Parameters.AddWithValue("@TransactionType", match transaction.TransactionType with | Buy -> "Buy" | Sell -> "Sell")
                command.Parameters.AddWithValue("@Price", transaction.Price)
                command.Parameters.AddWithValue("@Amount", transaction.Amount)
                command.Parameters.AddWithValue("@TransactionDate", transaction.TransactionDate)
                let result = command.ExecuteNonQuery()
                match result with
                | count when count > 0 ->
                    printfn "Insertion successful. %d row(s) affected." count
                | _ ->
                    printfn "No rows were inserted."
    with
    | ex -> printfn "An error occurred: %s" ex.Message


// Helper function to process a transaction and return the updated profit.
let processTransactionResponse direction (apiResponse: ApiResponse<OrderResponse>) remainingAmount : unit =
    // Calculate the impact on profit based on the transaction direction
    let priceImpact = apiResponse.Result.Price * apiResponse.Result.Amount
    let profitUpdate = match direction with
                       | Buy -> -priceImpact // Negative impact for buys
                       | Sell -> priceImpact // Positive impact for sells

    // Insert the main transaction
    let completedTransaction = {
        TransactionType = direction
        Price = apiResponse.Result.Price
        Amount = apiResponse.Result.Amount
        TransactionDate = DateTime.UtcNow
    }
    insertCompletedTransaction completedTransaction

    // Handle partially filled status
    // Check the status of the API response and handle accordingly
    match apiResponse.Result.Status with
    | "Partially filled" ->
        //TODO: Emit another transaction for the remaining amount
        // Calculate the remaining amount after accounting for the amount already processed
        let updatedRemainingAmount = remainingAmount - apiResponse.Result.Amount
        let remainingTransaction = {
            TransactionType = direction
            Price = apiResponse.Result.Price 
            Amount = updatedRemainingAmount
            TransactionDate = DateTime.UtcNow
        }
        // Insert the transaction for the remaining amount into the database
        insertCompletedTransaction remainingTransaction |> ignore
    | _ -> ()  // Do nothing if the transaction status is not 'Partially filled'
    
    profitAgent.Post(AddProfit profitUpdate)
    
let getOrderStatusBitstamp (orderId: string) =
    async {
        use client = new HttpClient()
        let uri = "https://18656-testing-server.azurewebsites.net/order/status/api/v2/order_status/"
        let requestBody = sprintf "id=%s" orderId
        let content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded")
        
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderStatus = JsonConvert.DeserializeObject<OrderResponse>(responseString)
                printfn "Order status retrieved: %A" orderStatus
                return Success orderStatus
            with ex ->
                printfn "Failed to retrieve order status: %s" ex.Message
                return Failure (sprintf "Failed to retrieve order status: %s" ex.Message)
        else
            printfn "Failed to retrieve order status: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }

let getOrderStatusKraken (orderId: string) =
    async {
        use client = new HttpClient()
        let uri = "https://18656-testing-server.azurewebsites.net/order/status/0/private/QueryOrders"
        let requestBody = sprintf "nonce=%i&txid=%s&trades=true" 1 orderId
        let content = new StringContent(requestBody, Encoding.UTF8, "application/json")

        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderStatus = JsonConvert.DeserializeObject<OrderResponse>(responseString)
                printfn "Order status retrieved for Kraken: %A" orderStatus
                return Success orderStatus
            with ex ->
                printfn "Failed to retrieve Kraken order status: %s" ex.Message
                return Failure (sprintf "Failed to retrieve Kraken order status: %s" ex.Message)
        else
            printfn "Failed to retrieve Kraken order status: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }

let getOrderStatusBitfinex (cryptoPair: string) (orderId: string) =
    async {
        use client = new HttpClient()
        let uri = sprintf "https://18656-testing-server.azurewebsites.net/order/status/auth/r/order/t%s:%s/trades" cryptoPair orderId
        let requestBody = sprintf ""
        let content = new StringContent(requestBody, Encoding.UTF8, "application/json")

        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderStatus = JsonConvert.DeserializeObject<OrderResponse>(responseString)
                printfn "Order status retrieved for Bitfinex: %A" orderStatus
                return Success orderStatus
            with ex ->
                printfn "Failed to retrieve Bitfinex order status: %s" ex.Message
                return Failure (sprintf "Failed to retrieve Bitfinex order status: %s" ex.Message)
        else
            printfn "Failed to retrieve Bitfinex order status: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }


let submitOrderInBitstamp (order: OrderDetails) =
    async {
        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"))
        
        let normalizedPair = order.Pair.Replace("-", "").ToLower()
        let uri = sprintf "https://18656-testing-server.azurewebsites.net/order/place/api/v2/%s/market/%s/" (if order.OrderType = Buy then "buy" else "sell") normalizedPair
        let body = sprintf "amount=%f&price=%f" order.Size order.Price
        let content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        
        printfn "Sending request to Bitstamp with data: %s" body
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderResponse = BitstampResponse.Parse(responseString)
                let order = {
                    StampId = string orderResponse.Id
                }
                return Success { Error = []; Result = BitstampResult order }
            with ex ->
                printfn "Failed to deserialize JSON response: %s" ex.Message
                return Failure (sprintf "Failed to deserialize JSON response: %s" ex.Message)
        else
            printfn "Failed to place order on Bitstamp: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }


let submitOrderInKraken (order: OrderDetails) =
    async {
        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        
        let uri = "https://18656-testing-server.azurewebsites.net/order/place/0/private/AddOrder"
        let pair = "XX" + order.Pair.Replace("-", "")
        let body = sprintf "nonce=%s&ordertype=market&type=%s&volume=%s&pair=%s&price=%s" "1" (if order.OrderType = Buy then "buy" else "sell") (string order.Size) pair (string order.Price)
        let content = new StringContent(body, Encoding.UTF8, "application/json")
        
        printfn "Sending request to Kraken with data: %s" body
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderResponse = JsonConvert.DeserializeObject<KrakenDecodeRes>(responseString)
                match orderResponse.result.txid with
                    | [| txid |] ->
                        let order = {
                            TxId = txid
                        }
                        return Success { Error = []; Result = KrakenResult order }
                    | _ -> 
                        return Failure "Failed to parse Kraken response"
            with ex ->
                printfn "Failed to deserialize JSON response: %s" ex.Message
                return Failure (sprintf "Failed to deserialize JSON response: %s" ex.Message)
        else
            printfn "Failed to place order on Kraken: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }


let submitOrderInBitfinex (order: OrderDetails) =
    async {
        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        
        let uri = "https://18656-testing-server.azurewebsites.net/order/place/v2/auth/w/order/submit"
        let pair = "t" + order.Pair.Replace("-", "")
        let requestBody = sprintf "type=%s&symbol=%s&amount=%s&price=%s" "MARKET" pair (string order.Size) (string order.Price)
        let content = new StringContent(requestBody, Encoding.UTF8, "application/json")

        printfn "Sending request to Bitfinex with data: %s" requestBody
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderResponse = JsonConvert.DeserializeObject<BitfinexDecodeRes[]>(responseString)
                match orderResponse with
                    | [| (_, _, _, _, [| (orderId, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _) |], _, _, _) |] ->
                        let order = {
                            FinexId = string orderId
                        }
                        return Success { Error = []; Result = BitfinexResult order }
                    | _ -> 
                        return Failure ("Failed to parse Bitfinex response")
            with ex ->
                printfn "Failed to deserialize JSON response: %s" ex.Message
                return Failure (sprintf "Failed to deserialize JSON response: %s" ex.Message)
        else
            printfn "Failed to place order on Bitfinex: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }

let convertToOrderDetails (opportunity: ArbitrageOpportunity) (isBuy: bool) (remainingAmount: decimal) : OrderDetails =
    {
        Pair = opportunity.CryptoCurrencyPair
        Size = remainingAmount
        Price = if isBuy then opportunity.BuyPrice else opportunity.SellPrice
        OrderType = if isBuy then Buy else Sell
    }
// Function to execute a transaction and handle the response.
let executeTransaction (direction: TransactionType) (opportunity: ArbitrageOpportunity) (remainingAmount: decimal) : Async<unit> =
    async {
        let exchange = if direction = Buy then opportunity.ExchangeToBuyFrom else opportunity.ExchangeToSellTo
        let orderDetails = convertToOrderDetails opportunity (direction = Buy) remainingAmount
        
        let! apiResponse =
            match exchange with
            | Kraken -> submitOrderInKraken orderDetails
            | Bitstamp -> submitOrderInBitstamp orderDetails
            | Bitfinex -> submitOrderInBitfinex orderDetails
            | Unknown -> async.Return (Failure "Unknown Exchange")
        
        printfn "Transaction for exchange %A completed. API response: %A" exchange apiResponse
        match apiResponse with
        | Success result ->
            printfn "Transaction for exchange %A successful. Result: %A" exchange result
            do! Async.Sleep(5000) // 5-second delay

            let! statusResult = 
                match result.Result with
                | KrakenResult kr -> 
                    getOrderStatusKraken kr.TxId
                | BitstampResult bs -> 
                    getOrderStatusBitstamp bs.StampId
                | BitfinexResult bf -> 
                    let pair = "t" + orderDetails.Pair.Replace("-", "")
                    getOrderStatusBitfinex pair bf.FinexId
                | _ -> async.Return (Failure "Unknown or unsupported exchange result type")

            match statusResult with
            | Success status ->
                printfn "Status after 5 seconds: %A" status
                //TODO: Process the status and update the profit
                // processTransactionResponse direction result remainingAmount |> ignore
            | Failure errorStatus ->
                printfn "Failed to retrieve order status after 5 seconds: %s" errorStatus
        | Failure error ->
            printfn "Transaction for exchange %A failed. Error: %s" exchange error
            let emailUser = emailAgent.PostAndReply(GetEmail)
            notifyUser emailUser "Transaction Failure" error
    }



// Main function to handle both buy and sell transactions.
let simulateOrderExecution (opportunity: ArbitrageOpportunity) =
    // Execute buy transaction
    executeTransaction Buy opportunity opportunity.BuyQuantity |> Async.RunSynchronously
    // Execute sell transaction
    executeTransaction Sell opportunity opportunity.SellQuantity |> Async.RunSynchronously

    // After processing both transactions, check if the total profit meets the threshold.
    updateProfitAndCheckThreshold


let updateEmail (ctx: HttpContext): Async<HttpContext option> =
    match ctx.request.query |> List.tryFind (fun (key, _) -> key = "email") with
    | Some (_, Some(email)) ->
        do emailAgent.Post (SetEmail email)  // This should return Async<unit>
        ctx |> OK ("Email updated")  // Wrap in Async
    | _ ->
        ctx |> BAD_REQUEST ("Email key not found or no value provided")  // Handle other cases

let updateAutoStop (ctx: HttpContext): Async<HttpContext option> =
    match ctx.request.query |> List.tryFind (fun (key, _) -> key = "stop") with
    | yes ->
        do autoStopAgent.Post (SetAutoStop true)  // This should return Async<unit>
        ctx |> OK ("AutoStop updated to yes")  // Wrap in Async
    | no ->
        do autoStopAgent.Post (SetAutoStop false)  // This should return Async<unit>
        ctx |> OK ("AutoStop updated to no")  // Wrap in Async
    | _ ->
        ctx |> BAD_REQUEST ("Invalid AutoStop value")  // Handle other cases

// below is commented out, can be commented in for testing reasons: 
// let app : WebPart =
//     choose [
//         POST >=> path "/email" >=> updateEmail
//     ]


// [<EntryPoint>]
// let main argv =

//     printfn "Starting order execution..."
//     // notifyUser "xiaojun3@andrew.cmu.edu" "Test Email" "This is a test email."

//     let userParams = {
//         ProfitThreshold = Some 1000m 
//     }

//     let opportunities = [
//         { CryptoCurrencyPair = "BTC-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 33000m; BuyQuantity = 1m; SellPrice = 34000m; SellQuantity = 1m }
//         { CryptoCurrencyPair = "ETH-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 2500m; BuyQuantity = 5m; SellPrice = 2600m; SellQuantity = 5m }
//         { CryptoCurrencyPair = "LTC-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 140m; BuyQuantity = 10m; SellPrice = 150m; SellQuantity = 10m }
//         { CryptoCurrencyPair = "LTC-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 33000m; BuyQuantity = 1m; SellPrice = 35000m; SellQuantity = 1m }
//     ]   

//     for opportunity in opportunities do
//         simulateOrderExecution opportunity userParams

//     startWebServer defaultConfig app // start the web server

//     0
