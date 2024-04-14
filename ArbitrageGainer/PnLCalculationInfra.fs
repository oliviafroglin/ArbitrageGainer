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

let connectionString = "Server=cmu-fp.mysql.database.azure.com;Database=team_database_schema;Uid=sqlserver;Pwd=-*lUp54$JMRku5Ay;SslMode=Required;"

let fetchTransactions startDate endDate =
    use connection = new MySqlConnection(connectionString)
    connection.Open()

    let commandText = sprintf """
        SELECT TransactionType, Price, Amount, TransactionDate
        FROM transactions
        WHERE TransactionDate >= @startDate AND TransactionDate <= @endDate;
        """
    
    use cmd = new MySqlCommand(commandText, connection)
    
    cmd.Parameters.AddWithValue("@startDate", startDate)
    cmd.Parameters.AddWithValue("@endDate", endDate)

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

let parseDate (dateStr: string) =
    match DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
    | true, date -> Some date
    | _ -> None

let pnlHandler (ctx: HttpContext) : Async<HttpContext option> =
    async {
        let startDateStr = ctx.request.queryParam "start"
        let endDateStr = ctx.request.queryParam "end"
        
        let parseQueryParam (param: Choice<string, string>) =
            match param with
            | Choice1Of2 value -> parseDate value
            | Choice2Of2 _ -> None

        match parseQueryParam startDateStr, parseQueryParam endDateStr with
        | Some startDate, Some endDate ->
            let transactions = fetchTransactions startDate endDate
            match calculateHistoricalPnL transactions { StartDate = startDate; EndDate = endDate } with
            | Ok resultData ->
                let json = JsonConvert.SerializeObject(resultData)
                return! OK json ctx
            | Error errMsg ->
                return! BAD_REQUEST errMsg ctx
        | _, _ ->
            return! BAD_REQUEST "Invalid date format. Please use YYYY-MM-DD." ctx
    }



    