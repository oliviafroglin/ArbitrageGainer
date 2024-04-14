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

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;SslMode=Required;"

let fetchTransactionsForDay (date: DateTime) : list<CompletedTransaction>=
    let startOfDay = date.Date
    let endOfDay = date.Date.AddDays(1.0).AddTicks(-1L)

    use connection = new MySqlConnection(connectionString)
    connection.Open()

    let commandText = """
        SELECT TransactionType, Price, Amount, TransactionDate
        FROM transactions
        WHERE TransactionDate BETWEEN @startOfDay AND @endOfDay;
        """
    
    use cmd = new MySqlCommand(commandText, connection)
    
    cmd.Parameters.AddWithValue("@startOfDay", startOfDay)
    cmd.Parameters.AddWithValue("@endOfDay", endOfDay)

    use reader = cmd.ExecuteReader()
    
    let transactions = 
        [ while reader.Read() do
            let transactionType = 
                match reader.GetString("TransactionType") with
                | "Buy" -> Buy
                | "Sell" -> Sell
                | _ -> raise (InvalidOperationException("Invalid transaction type"))
            
            let price = reader.GetDecimal("Price")
            let amount = reader.GetDecimal("Amount")
            let transactionDate = reader.GetDateTime("TransactionDate")
            
            yield { TransactionType = transactionType; Price = price; Amount = amount; TransactionDate = transactionDate }
        ]

    transactions

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
                let transactions = fetchTransactionsForDay startDate  // Correctly uses DateTime
                let initialInvestment = calculateInitialInvestment transactions
                let endDate = DateTime.Now
                let dateRange = { StartDate = startDate; EndDate = endDate }
                let totalReturn = calculateHistoricalPnL transactions dateRange
                let durationYears = (endDate - startDate).TotalDays / 365.25

                match totalReturn with
                | Ok profitOrLoss ->
                    let totalReturnAmount = 
                        match profitOrLoss with
                        | Profit p -> p
                        | Loss l -> -l
                    let investmentDetails = {
                        StartDate = startDate; 
                        InitialInvestment = initialInvestment; 
                        TotalReturn = totalReturnAmount; 
                        DurationYears = durationYears 
                    }
                    let annualizedReturn = calculateAnnualizedReturn investmentDetails
                    return! OK (sprintf "{\"Annualized Return\": %f}" annualizedReturn) ctx
                | Error msg -> return! BAD_REQUEST (sprintf "Error calculating PnL: %s" msg) ctx
            | None -> return! BAD_REQUEST "Invalid startDate format. Please use YYYY-MM-DD." ctx
        | Choice2Of2 errMsg -> return! BAD_REQUEST (sprintf "Error retrieving 'startDate' parameter: %s" errMsg) ctx
    }



