module AnnualizedReturnCalculationInfra

open PnLCalculationCore
open Suave
open Suave.Successful
open Suave.RequestErrors
open AnnualizedReturnCalculationCore
open System.Globalization
open System
open PnLCalculationService
open MySql.Data.MySqlClient

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Password=-*lUp54$JMRku5Ay;SslMode=Required;"


let fetchTransactionsForDay (date: DateTime) : list<CompletedTransaction> =
    try
        let startOfDay = date.Date
        let now = DateTime.Now
        let endOfDay = now.Date

        use connection = new MySqlConnection(connectionString)
        connection.Open()

        let commandText = """
            SELECT TransactionType, Price, Amount, TransactionDate
            FROM Transactions
            WHERE TransactionDate BETWEEN @startOfDay AND @endOfDay;
            """
        
        use cmd = new MySqlCommand(commandText, connection)
        cmd.Parameters.AddWithValue("@startOfDay", startOfDay)
        cmd.Parameters.AddWithValue("@endOfDay", endOfDay)
        
        printfn "Executing SQL: %s" commandText
        printfn "Parameters: startOfDay = %O, endOfDay = %O" startOfDay endOfDay

        use reader = cmd.ExecuteReader()

        let rec readTransactions acc =
            match reader.Read() with
            | true ->
                let transactionType = 
                    match reader.GetString("TransactionType") with
                    | "Buy" -> Buy
                    | "Sell" -> Sell
                    | _ -> raise (InvalidOperationException("Invalid transaction type"))
                
                let price = reader.GetDecimal("Price")
                let amount = reader.GetDecimal("Amount")
                let transactionDate = reader.GetDateTime("TransactionDate")
                let transaction = { TransactionType = transactionType; Price = price; Amount = amount; TransactionDate = transactionDate }
                readTransactions (transaction :: acc)
            | false -> List.rev acc  // Reverse the list to maintain the original order

        readTransactions []
    with
    | ex ->
        printfn "Error connecting and fetching transactions from database: %s" ex.Message
        raise ex 

let annualizedReturnHandler (ctx: HttpContext) : Async<HttpContext option> =
    async {
        match ctx.request.queryParam "startDate" with
        | Choice1Of2 startDateStr ->
            let parseDate (dateStr: string) =
                match DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | true, date -> Some date
                | _ -> None

            match parseDate startDateStr with
            | Some startDate ->
                let now = DateTime.UtcNow
                match startDate > now with
                | true -> return! BAD_REQUEST "Start date cannot be in the future." ctx
                | false ->
                    let endDate = match startDate.Date with
                                  | d when d = now.Date -> now
                                  | _ -> now
                    let transactions = fetchTransactionsForDay startDate
                    let initialInvestment = calculateInitialInvestment transactions
                    let dateRange = { StartDate = startDate; EndDate = endDate }
                    let totalReturn = calculateHistoricalPnL transactions dateRange
                    let durationYears = (endDate - startDate).TotalDays / 365.25

                    match totalReturn with
                    | Ok profitOrLoss ->
                        let totalReturnAmount = 
                            match profitOrLoss with
                            | Profit p -> p
                            | Loss l -> l
                        let investmentDetails = {
                            StartDate = startDate; 
                            InitialInvestment = initialInvestment; 
                            TotalReturn = totalReturnAmount; 
                            DurationYears = durationYears 
                        }
                        let annualizedReturn = calculateAnnualizedReturn investmentDetails
                        return! OK (sprintf "{\"Annualized Return\": %f}" annualizedReturn) ctx
                    | Error msg -> return! BAD_REQUEST (sprintf "Error calculating PnL: %s" msg) ctx
            | _ -> return! BAD_REQUEST "Invalid startDate format. Please use YYYY-MM-DD." ctx
        | _ -> return! BAD_REQUEST "Missing or invalid 'startDate' parameter." ctx
    }



