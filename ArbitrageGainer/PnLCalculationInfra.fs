module PnLCalculationInfra

open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Writers
open Newtonsoft.Json
open System.Globalization
open PnLCalculationCore
open PnLCalculationService
open MySql.Data.MySqlClient

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Password=Functional!;SslMode=Required;"
let fetchTransactions startDate endDate =
    try
        use connection = new MySqlConnection(connectionString)
        connection.Open()

        let commandText = sprintf """
            SELECT TransactionType, Price, Amount, TransactionDate
            FROM Transactions
            WHERE TransactionDate >= @startDate AND TransactionDate <= @endDate;
            """
        
        use cmd = new MySqlCommand(commandText, connection)
        cmd.Parameters.AddWithValue("@startDate", startDate)
        cmd.Parameters.AddWithValue("@endDate", endDate)
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
            | false -> List.rev acc  // Reverse to maintain the order of transactions as they appeared in the database

        readTransactions []
    with
    | ex ->
        printfn "Error connecting and fetching transactions from database: %s" ex.Message
        raise ex

let parseDate (dateStr: string) =
    match DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
    | true, date -> Some date
    | _ -> None

let pnlHandler (ctx: HttpContext) : Async<HttpContext option> =
    async {
        let now = DateTime.UtcNow
        let startDateStr = ctx.request.queryParam "start"
        let endDateStr = ctx.request.queryParam "end"
        
        let parseQueryParam (param: Choice<string, string>) =
            match param with
            | Choice1Of2 value -> parseDate value
            | Choice2Of2 _ -> None
            
        let adjustEndDate (date: DateTime) =
            match date.Date with
            | d when d = now.Date -> now
            | d -> d.AddDays(1.0).AddTicks(-1L)

        let validateDates startDate endDate =
            match startDate, endDate with
            | s, e when e > now -> Some "End date cannot be in the future."
            | s, e when s > e -> Some "Start date must be before end date."
            | _ -> None
            
        match parseQueryParam startDateStr, parseQueryParam endDateStr with
        | Some startDate, Some endDate ->
            let adjustedEndDate = adjustEndDate endDate
            match validateDates startDate adjustedEndDate with
            | None ->
                let transactions = fetchTransactions startDate adjustedEndDate
                match transactions with
                | [] -> return! BAD_REQUEST "No transactions between start date and end date." ctx
                | _ ->
                    let dateRange = { StartDate = startDate; EndDate = adjustedEndDate }
                    let resultData = calculateHistoricalPnL transactions dateRange
                    let json = JsonConvert.SerializeObject(resultData)
                    return! OK json ctx
            | Some errorMsg -> return! BAD_REQUEST errorMsg ctx
        | _, _ -> return! BAD_REQUEST "Please provide both start and end dates." ctx
    }



    