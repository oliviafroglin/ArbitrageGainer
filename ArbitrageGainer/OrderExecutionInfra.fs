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
open ManagePnLThresholdCore
open ManagePnLThresholdService
open ManagePnLThresholdInfra

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors

open ArbitrageService
open ArbitrageModels
open TimeStamps
open Logging.Logger
// open ManagePnLThresholdInfra
// open PnLCalculationCore
// open PnLCalculationService

let connectionString = "Server=mysql_18656_team_01;Database=team_database_schema;Uid=root;Password=Functional!;SslMode=Required;"
type BitstampResponse = JsonProvider<"""{"id":"1234","market":"FET/USD","datetime":"2023-12-31 14:43:15.796000","type":"0","price":"22.45","amount":"58.06000000","client_order_id":"123456789"}""">

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

let updateProfitAndCheckThreshold() =
        //TODO: Fetch threshold from another module
    // let ProfitThreshold = 1000m
    let ProfitThreshold = thresholdAgent.GetThreshold()
    // Retrieve the current total profit synchronously
    let totalProfit = profitAgent.PostAndReply(GetProfit)
    printfn "Checking threshold: Current Total Profit = %A, Threshold = %A" totalProfit ProfitThreshold

    match ProfitThreshold.Value with
    | threshold when threshold <> 0M && totalProfit >= threshold ->
        let isAutoStop = autoStopAgent.PostAndReply(GetAutoStop)
        match isAutoStop with
        | true ->
            tradingAgent.Post (Stop)
        | false ->
            ()
        // Notify the user if the profit threshold has been exceeded
        printfn "Threshold met or exceeded. Triggering email notification."
        let emailUser = emailAgent.PostAndReply(GetEmail)
        notifyUser emailUser "Your Arbitrage Gainer" ("Profit threshold reached, set your new threshold at: localhost:8080/pnl/threshold, your threshold is reached and your current profit is: " + string totalProfit)
        () 
    | _ ->
        // Log the current total profit for information
        printfn "Threshold not met. Current Total Profit: %A" totalProfit
        ()

let insertCompletedTransaction (transaction: CompletedTransaction) =
    try
        using (new MySqlConnection(connectionString)) <| fun connection ->
            connection.Open()
            let sql = "INSERT INTO transactions (TransactionType, Price, Amount, TransactionDate) VALUES (@TransactionType, @Price, @Amount, @TransactionDate)"
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

let getOrderStatusBitstamp (orderId: string) =
    async {
        use client = new HttpClient()
        let uri = "https://18656-testing-server.azurewebsites.net/order/status/api/v2/order_status/"
        let requestBody = sprintf "id=%s" orderId
        let content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded")
        
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderStatus = JsonConvert.DeserializeObject<BitstampStatusRes>(responseString)
                return Success (BitstampStatus orderStatus)
            with ex ->
                printfn "Failed to retrieve order status: %s" ex.Message
                return Failure (sprintf "Failed to retrieve order status: %s" ex.Message)
        | false ->
            printfn "Failed to retrieve order status: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }


let getOrderStatusKraken (orderId: string) =
    async {
        use client = new HttpClient()
        let uri = "https://18656-testing-server.azurewebsites.net/order/status/0/private/QueryOrders"
        let requestBody = sprintf "nonce=%i&txid=%s&trades=true" 1 orderId
        let content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded")

        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderStatus = JsonConvert.DeserializeObject<KrakenStatusRes>(responseString)
                return Success (KrakenStatus orderStatus)
            with ex ->
                printfn "Failed to retrieve Kraken order status: %s" ex.Message
                return Failure (sprintf "Failed to retrieve Kraken order status: %s" ex.Message)
        | false ->
            printfn "Failed to retrieve Kraken order status: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }

let getOrderStatusBitfinex (cryptoPair: string) (orderId: string) =
    async {
        use client = new HttpClient()
        let uri = sprintf "https://18656-testing-server.azurewebsites.net/order/status/auth/r/order/t%s:%s/trades" cryptoPair orderId
        let requestBody = sprintf ""
        let content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded")

        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            
            try
                let orderStatus = JsonConvert.DeserializeObject<JArray>(responseString)
                let orders = orderStatus.[0] :?> JArray
                let executedAmount = orders.[4].Value<decimal>()

                return Success (BitfinexStatus executedAmount)
            with ex ->
                printfn "Failed to parse Bitfinex response: %s" ex.Message
                return Failure (sprintf "Failed to parse Bitfinex response: %s" ex.Message)
        | false ->
            printfn "Failed to retrieve Bitfinex order status: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }


let submitOrderInBitstamp (order: OrderDetails) =
    async {
        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"))
        
        let normalizedPair = order.Pair.Replace("-", "").ToLower()
        let orderType = 
            match order.OrderType with
            | Buy -> "buy"
            | Sell -> "sell"

        let uri = sprintf "https://18656-testing-server.azurewebsites.net/order/place/api/v2/%s/market/%s/" orderType normalizedPair
        let body = sprintf "amount=%f&price=%f" order.Size order.Price
        let content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        
        printfn "Sending request to Bitstamp with data: %s" body
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderResponse = BitstampResponse.Parse(responseString)
                return Success { Error = []; Result = string orderResponse.Id}
            with ex ->
                printfn "Failed to deserialize JSON response: %s" ex.Message
                return Failure (sprintf "Failed to deserialize JSON response: %s" ex.Message)
        | false ->
            printfn "Failed to place order on Bitstamp: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }

let submitOrderInKraken (order: OrderDetails) =
    async {
        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        
        let uri = "https://18656-testing-server.azurewebsites.net/order/place/0/private/AddOrder"
        let pair = "XX" + order.Pair.Replace("-", "")
        let orderType = 
            match order.OrderType with
            | Buy -> "buy"
            | Sell -> "sell"

        let body = sprintf "nonce=%s&ordertype=market&type=%s&volume=%s&pair=%s&price=%s" "1" orderType (string order.Size) pair (string order.Price)
        let content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        
        printfn "Sending request to Kraken with data: %s" body
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let orderResponse = JsonConvert.DeserializeObject<KrakenDecodeRes>(responseString)
                match orderResponse.result.txid with
                    | [| txid |] ->
                        return Success { Error = []; Result = string txid }
                    | _ -> 
                        return Failure "Failed to parse Kraken response"
            with ex ->
                printfn "Failed to deserialize JSON response: %s" ex.Message
                return Failure (sprintf "Failed to deserialize JSON response: %s" ex.Message)
        | false ->
            printfn "Failed to place order on Kraken: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }


let tryExtractOrderId (json: JArray) =
    try
        let items = json.[4] :?> JArray
        let innerItems = items.[0] :?> JArray
        let orderId = innerItems.[0].Value<int64>()
        Some orderId
    with
    | :? System.Exception as ex ->
        printfn "Error during JSON parsing: %s" ex.Message
        None

let submitOrderInBitfinex (order: OrderDetails) =
    async {
        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        
        let uri = "https://18656-testing-server.azurewebsites.net/order/place/v2/auth/w/order/submit"
        let pair = "t" + order.Pair.Replace("-", "")
        let requestBody = sprintf "type=%s&symbol=%s&amount=%s&price=%s" "MARKET" pair (string order.Size) (string order.Price)
        let content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded")

        printfn "Sending request to Bitfinex with data: %s" requestBody
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            try
                let parsedJson = JsonConvert.DeserializeObject<JArray>(responseString)

                match tryExtractOrderId(parsedJson) with
                   | Some orderId ->
                        return Success { Error = []; Result = string orderId }
                   | None ->
                        return Failure "Failed to parse Bitfinex response"
            with ex ->
                printfn "Failed to deserialize JSON response: %s" ex.Message
                return Failure (sprintf "Failed to deserialize JSON response: %s" ex.Message)
        | false ->
            printfn "Failed to place order on Bitfinex: %s" response.ReasonPhrase
            return Failure response.ReasonPhrase
    }

let convertToOrderDetails (opportunity: ArbitrageOpportunity) (isBuy: bool) (remainingAmount: decimal) : OrderDetails =
    {
        Pair = opportunity.CryptoCurrencyPair
        Size = remainingAmount
        Price = match isBuy with
                | true -> opportunity.BuyPrice
                | false -> opportunity.SellPrice
        OrderType = match isBuy with
                    | true -> Buy
                    | false -> Sell
    }
let processTransactionResponse (statusResponse: UnifiedStatusRes) (orderDetails: OrderDetails) (Exchange: Exchange): unit =
    // Determine the remaining amount based on the status response
    let remainingAmount = match statusResponse with
                          | KrakenStatus krStatus ->
                              match krStatus.Result |> Map.toList |> List.tryHead with
                              | Some (_, krDetails) -> (decimal krDetails.Vol) - (decimal krDetails.Vol_exec)
                              | None -> 0m  // No details found, handle as appropriate
                          | BitstampStatus bsStatus ->
                              match String.IsNullOrWhiteSpace(bsStatus.AmountRemaining) with
                              | false -> bsStatus.AmountRemaining |> decimal
                              | true -> 0m  // Handle case where AmountRemaining is null or whitespace
                          | BitfinexStatus remainingAmt->
                              orderDetails.Size - remainingAmt
                          | _ -> 0m

    // Insert the main transaction into the database
    let completedTransaction = {
        TransactionType = orderDetails.OrderType
        Price = orderDetails.Price
        Amount = orderDetails.Size - remainingAmount  // Compute actual executed amount
        TransactionDate = DateTime.UtcNow
    }
    insertCompletedTransaction completedTransaction

    // If there is any remaining amount, place another order for that amount
    match remainingAmount > 0m with
    | true ->
        let remainingOrderDetails = { orderDetails with Size = remainingAmount }
        Async.RunSynchronously (
            match Exchange with
            | Kraken -> submitOrderInKraken remainingOrderDetails
            | Bitstamp -> submitOrderInBitstamp remainingOrderDetails
            | Bitfinex -> submitOrderInBitfinex remainingOrderDetails
            | _ -> async.Return (Failure "Unknown Exchange")  // Handle unknown exchange
        ) |> ignore

        // Log that an additional order was placed
        printfn "Additional order placed for remaining amount: %A on %A" remainingAmount Exchange

        // Insert the transaction for the remaining amount into the database
        let remainingTransaction = {
            TransactionType = orderDetails.OrderType
            Price = orderDetails.Price
            Amount = remainingAmount
            TransactionDate = DateTime.UtcNow
        }
        insertCompletedTransaction remainingTransaction
    | false -> ()  // No action needed if no remaining amount

    let priceImpact = orderDetails.Size * orderDetails.Price
    let profitUpdate = match orderDetails.OrderType with
                       | Buy -> -priceImpact // Negative impact for buys
                       | Sell -> priceImpact // Positive impact for sells
    
    profitAgent.Post(AddProfit profitUpdate)
    // Log the completion of processing
    printfn "Processed transaction for order on %A with remaining amount %A" Exchange remainingAmount

// Function to execute a transaction and handle the response.
let executeTransaction (direction: TransactionType) (opportunity: ArbitrageOpportunity) (remainingAmount: decimal) : Async<unit> =
    async {
        let exchange = 
            match direction with
            | Buy -> opportunity.ExchangeToBuyFrom
            | Sell -> opportunity.ExchangeToSellTo
        let orderDetails = convertToOrderDetails opportunity (direction = Buy) remainingAmount
        
        timeAgent.Post(UpdateEndTime DateTime.UtcNow)
        match timeAgent.PostAndReply(GetLogged) with
        | false ->
            let timeDiff = timeAgent.PostAndReply(GetTimeDiff)
            let logger = createLogger
            logger (sprintf "First order placement completed in %A seconds" (timeDiff))
        | true -> ()

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
                match exchange with
                | Kraken -> getOrderStatusKraken result.Result
                | Bitstamp -> getOrderStatusBitstamp result.Result
                | Bitfinex -> 
                    let pair = "t" + orderDetails.Pair.Replace("-", "")
                    getOrderStatusBitfinex pair result.Result
                | Unknown -> async.Return (Failure "Unknown or unsupported exchange result type")

            match statusResult with
            | Success status ->
                printfn "Status after 5 seconds: %A" status
                //TODO: Process the status and update the profit
                processTransactionResponse status orderDetails exchange
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
    updateProfitAndCheckThreshold()


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
