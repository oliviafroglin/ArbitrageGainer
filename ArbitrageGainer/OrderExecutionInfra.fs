open System
open System.Net
open System.Net.Mail
open System.Threading
open MySql.Data.MySqlClient

// open ManagePnLThresholdInfra
// open PnLCalculationCore
// open PnLCalculationService
// run this file separately for this milestone! 

type Exchange = Kraken | Bitstamp | Bitfinex
type CryptoCurrencyPair = string

type UserDefinedParameters = {
    MinimalPriceSpreadValue: decimal
    MinimalTransactionProfit: decimal
    MaximalTransactionValue: decimal
    MaximalTradingValue: decimal
    EmailForNotification: string
    ProfitThreshold: decimal option
}

type ArbitrageOpportunity = {
    CryptoCurrencyPair: string
    ExchangeToBuyFrom: Exchange
    ExchangeToSellTo: Exchange
    BuyPrice: decimal
    BuyQuantity: decimal
    SellPrice: decimal
    SellQuantity: decimal
}

type OrderResponse = {
    Id: string
    Market: string
    DateTime: string
    Type: string
    Price: decimal
    Amount: decimal
    ClientOrderId: string
    Status: string
    Remaining: decimal
}

type ApiResponse<'T> = {
    Error: string list
    Result: 'T
}

type Result<'TSuccess, 'TFailure> = 
    | Success of 'TSuccess 
    | Failure of 'TFailure

type TransactionType = Buy | Sell

type CompletedTransaction = {
    TransactionType: TransactionType
    Price: decimal
    Amount: decimal
    TransactionDate: DateTime
}

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;SslMode=Required;"

type ProfitMessage =
    | GetProfit of AsyncReplyChannel<decimal>
    | SetProfit of decimal

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
        }
    // Initially, total profit is set to zero
    loop 0m
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

let updateProfitAndCheckThreshold userParams =
    // Retrieve the current total profit synchronously
    let totalProfit = profitAgent.PostAndReply(GetProfit)
    printfn "Checking threshold: Current Total Profit = %A, Threshold = %A" totalProfit userParams.ProfitThreshold

    match userParams.ProfitThreshold with
    | Some threshold when totalProfit >= threshold ->
        // Notify the user if the profit threshold has been exceeded
        printfn "Threshold met or exceeded. Triggering email notification."
        notifyUser userParams.EmailForNotification "Your Arbitrage Gainer" ("Profit threshold reached: " + string totalProfit)
        () 
    | _ ->
        // Log the current total profit for information
        printfn "Threshold not met. Current Total Profit: %A" totalProfit

        () 


let mockOrderResponse (opportunity: ArbitrageOpportunity) (isBuy: bool) (simulatePartial: bool) : Result<ApiResponse<OrderResponse>, string> =
    match simulatePartial with
    | true ->
        let filledAmount = opportunity.BuyQuantity * 0.5m
        let remaining = opportunity.BuyQuantity - filledAmount
        let status = "Partially filled"
        Success {
            Error = []
            Result = {
                Id = System.Guid.NewGuid().ToString()
                Market = opportunity.CryptoCurrencyPair
                DateTime = System.DateTime.UtcNow.ToString("o")
                Type = match isBuy with
                       | true -> "buy"
                       | false -> "sell"
                Price = match isBuy with
                        | true -> opportunity.BuyPrice
                        | false -> opportunity.SellPrice
                Amount = filledAmount
                ClientOrderId = System.Guid.NewGuid().ToString()
                Status = status
                Remaining = remaining
            }
        }
    | false ->
        let filledAmount = opportunity.BuyQuantity
        let remaining = 0m
        let status = "Filled"
        Success {
            Error = []
            Result = {
                Id = System.Guid.NewGuid().ToString()
                Market = opportunity.CryptoCurrencyPair
                DateTime = System.DateTime.UtcNow.ToString("o")
                Type = match isBuy with
                       | true -> "buy"
                       | false -> "sell"
                Price = match isBuy with
                        | true -> opportunity.BuyPrice
                        | false -> opportunity.SellPrice
                Amount = filledAmount
                ClientOrderId = System.Guid.NewGuid().ToString()
                Status = status
                Remaining = remaining
            }
        }

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
                    true
                | _ ->
                    printfn "No rows were inserted."
                    false
    with
    | ex -> printfn "An error occurred: %s" ex.Message
            false


// Helper function to process a transaction and return the updated profit.
let processTransactionResponse direction (apiResponse: ApiResponse<OrderResponse>) remainingAmount userParams =
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
    insertCompletedTransaction completedTransaction |> ignore

    // Handle partially filled status
    // Check the status of the API response and handle accordingly
    match apiResponse.Result.Status with
    | "Partially filled" ->
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
    profitUpdate

// Function to execute a transaction and handle the response.
let executeTransaction direction opportunity userParams remainingAmount initialProfit =
    mockOrderResponse {opportunity with BuyQuantity = remainingAmount; SellQuantity = remainingAmount} (direction = Buy) false
    |> function
    | Success apiResponse ->
        let profitUpdate = processTransactionResponse direction apiResponse remainingAmount userParams
        // Update the global profit and return new total
        profitAgent.Post(SetProfit (initialProfit + profitUpdate))
        initialProfit + profitUpdate
    | Failure error ->
        notifyUser userParams.EmailForNotification "Transaction Failure" error
        initialProfit

// Main function to handle both buy and sell transactions.
let simulateOrderExecution (opportunity: ArbitrageOpportunity) (userParams: UserDefinedParameters) =
    let initialProfit = 0m
    // Execute buy transaction
    let profitAfterBuy = executeTransaction Buy opportunity userParams opportunity.BuyQuantity initialProfit
    // Execute sell transaction
    let finalProfit = executeTransaction Sell opportunity userParams opportunity.SellQuantity profitAfterBuy

    // After processing both transactions, check if the total profit meets the threshold.
    updateProfitAndCheckThreshold userParams

[<EntryPoint>]
let main argv =

    printfn "Starting order execution..."
    // notifyUser "xiaojun3@andrew.cmu.edu" "Test Email" "This is a test email."

    let userParams = {
        MinimalPriceSpreadValue = 10m
        MinimalTransactionProfit = 20m
        MaximalTransactionValue = 1000000m
        MaximalTradingValue = 10000000m
        EmailForNotification = "xiaojun3@andrew.cmu.edu"
        ProfitThreshold = Some 1000m
    }

    let opportunities = [
        { CryptoCurrencyPair = "BTC-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 33000m; BuyQuantity = 1m; SellPrice = 34000m; SellQuantity = 1m }
        { CryptoCurrencyPair = "ETH-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 2500m; BuyQuantity = 5m; SellPrice = 2600m; SellQuantity = 5m }
        { CryptoCurrencyPair = "LTC-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 140m; BuyQuantity = 10m; SellPrice = 150m; SellQuantity = 10m }
        { CryptoCurrencyPair = "LTC-USD"; ExchangeToBuyFrom = Kraken; ExchangeToSellTo = Kraken; BuyPrice = 33000m; BuyQuantity = 1m; SellPrice = 35000m; SellQuantity = 1m }
    ]   

    for opportunity in opportunities do
        simulateOrderExecution opportunity userParams

    0
