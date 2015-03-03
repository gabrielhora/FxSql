// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r @"bin\Debug\Dapper.dll"

#load "FxSql.fs"
open FxSql
open System.Data.SqlClient


let sql = "
-- name: AllClients
SELECT * FROM Clients;

-- name: OneClient
SELECT * FROM Clients
WHERE Id = @Id;"

let con = new SqlConnection("Server=localhost;Database=Kiosk;User Id=sa;Password=134711;")
con.Open()

let db = (new Database(con)).fromString sql

db.["OneClient"].query ["Id", 1] |> Map.ofList