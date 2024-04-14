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
// open MySql.Data.MySqlClient
// open Microsoft.Data.SqlClient
// open FSharp.Data.SqlClient
open FSharp.Data.Npgsql

// let connectionString = "Host=34.42.239.81;Database=orders;Username=sqlserver;Password=-*lUp54$JMRku5Ay;Trusted_Connection=True;"
// type SqlDb = NpgsqlConnection<connectionString>
// let db = SqlDb.GetDataContext()

let fetchTransactions startDate endDate =
    // fake data in this format
    // type CompletedTransaction = {TransactionType: TransactionType PurchasePrice: decimal SalePrice: decimal Amount: decimal TransactionDate: DateTime}
    [
        { TransactionType = TransactionType.Buy; BuyPrice = 100.0M; SellPrice = 0.0M; Amount = 100.0M; TransactionDate = DateTime.Parse("2021-01-01") }
        { TransactionType = TransactionType.Sell; BuyPrice = 0.0M; SellPrice = 200.0M; Amount = 200.0M; TransactionDate = DateTime.Parse("2021-01-02") }
    ]

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



    