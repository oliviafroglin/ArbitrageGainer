// Program.fs
open System
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Newtonsoft.Json
open IdentifyCrossTradedPairsInfra

[<EntryPoint>]
let main argv =
    0 // Return an integer exit code