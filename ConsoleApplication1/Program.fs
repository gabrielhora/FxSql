// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.


open FxSql
open System.Data.SqlClient
open System


type Client() =
    member val Email = "" with get, set
    member val FirstName = "" with get, set

type ClientParameter = {Id : int}

[<EntryPoint>]
let main argv = 

    let sql = "
    -- name: AllClients
    SELECT * FROM Clients;

    -- name: OneClient
    SELECT * FROM Clients
    WHERE Id = @Id;"

    let con = new SqlConnection("Server=localhost;Database=Kiosk;User Id=sa;Password=134711;")
    con.Open()

    let db = (new Database(con)).FromString sql
    let result = db?OneClient.Query { Id = 4 }

    //db.["OneClient"].query ["Id", 1] |> Map.ofList

    0
