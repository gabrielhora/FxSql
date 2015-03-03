module FxSql

open System
open System.Data
open System.Text.RegularExpressions
open System.IO
open Dapper


/// <summary>
/// IDbConnection wrapper to keep the state of the connection unchanged
/// </summary>
type DisposableConnection(con : IDbConnection) =
    let dispose = con.State = ConnectionState.Closed
    do if dispose then con.Open()
    
    member this.Connection = con

    interface IDisposable with
        member this.Dispose() =
            if dispose && this.Connection <> null && this.Connection.State = ConnectionState.Open then
                this.Connection.Close()
                this.Connection.Dispose()


/// <summary>
/// The query runner
/// </summary>
type Query = 
    { Name: string; Text: string; mutable Connection: DisposableConnection option }

    /// <summary>
    /// Run the query and return a IEnumerable<object>
    /// </summary>
    member this.Query (?param0 : obj, ?trans0 : IDbTransaction, ?buffered0 : bool, ?cmdTimeout0 : Nullable<int>, ?cmdType0 : Nullable<CommandType>) =
        let param = defaultArg param0 null
        let trans = defaultArg trans0 null
        let buffered = defaultArg buffered0 true
        let cmdTimeout = defaultArg cmdTimeout0 (Nullable<int>())
        let cmdType = defaultArg cmdType0 (Nullable<CommandType>())
        match this.Connection with
        | None -> raise (new Exception("No IDbConnection specified"))
        | Some(dc) ->
            SqlMapper.Query(dc.Connection, this.Text, param, trans, buffered, cmdTimeout, cmdType)

    (* These methods facilitate the use from C# *)

    member this.Query (param : obj) = this.Query(param, null, true, Nullable<int>(), Nullable<CommandType>())
    member this.Query (param : obj, trans : IDbTransaction) = this.Query(param, trans, true, Nullable<int>(), Nullable<CommandType>())
    member this.Query (param : obj, trans : IDbTransaction, buffered : bool) = this.Query(param, trans, buffered, Nullable<int>(), Nullable<CommandType>())
    member this.Query (param : obj, trans : IDbTransaction, buffered : bool, commandTimeout : int) = this.Query(param, trans, buffered, Nullable(commandTimeout), Nullable<CommandType>())
    member this.Query (param : obj, trans : IDbTransaction, buffered : bool, commandTimeout : int, commandType : CommandType) = this.Query(param, trans, buffered, Nullable(commandTimeout), Nullable(commandType))

    /// <summary>
    /// Run the query and return a typed IEnumerable
    /// </summary>
    member this.Query<'a> (?param0 : obj, ?trans0 : IDbTransaction, ?buffered0 : bool, ?cmdTimeout0 : Nullable<int>, ?cmdType0 : Nullable<CommandType>) =
        let param = defaultArg param0 null
        let trans = defaultArg trans0 null
        let buffered = defaultArg buffered0 true
        let cmdTimeout = defaultArg cmdTimeout0 (Nullable<int>())
        let cmdType = defaultArg cmdType0 (Nullable<CommandType>())
        match this.Connection with
        | None -> raise (new Exception("No IDbConnection specified"))
        | Some(dc) ->
            SqlMapper.Query<'a>(dc.Connection, this.Text, param, trans, buffered, cmdTimeout, cmdType)

    (* These methods facilitate the use from C# *)

    member this.Query<'a> (param : obj) = this.Query<'a>(param, null, true, Nullable<int>(), Nullable<CommandType>())
    member this.Query<'a> (param : obj, trans : IDbTransaction) = this.Query<'a>(param, trans, true, Nullable<int>(), Nullable<CommandType>())
    member this.Query<'a> (param : obj, trans : IDbTransaction, buffered : bool) = this.Query<'a>(param, trans, buffered, Nullable<int>(), Nullable<CommandType>())
    member this.Query<'a> (param : obj, trans : IDbTransaction, buffered : bool, commandTimeout : int) = this.Query<'a>(param, trans, buffered, Nullable(commandTimeout), Nullable<CommandType>())
    member this.Query<'a> (param : obj, trans : IDbTransaction, buffered : bool, commandTimeout : int, commandType : CommandType) = this.Query<'a>(param, trans, buffered, Nullable(commandTimeout), Nullable(commandType))

    /// <summary>
    /// Run a scalar query and return the number of changed rows
    /// </summary>
    member this.Execute (?param0 : obj, ?trans0 : IDbTransaction, ?cmdTimeout0 : Nullable<int>, ?cmdType0 : Nullable<CommandType>) =
        let param = defaultArg param0 null
        let trans = defaultArg trans0 null
        let cmdTimeout = defaultArg cmdTimeout0 (Nullable<int>())
        let cmdType = defaultArg cmdType0 (Nullable<CommandType>())
        match this.Connection with
        | None -> raise (new Exception("No IDbConnection specified"))
        | Some(dc) ->
            SqlMapper.Execute(dc.Connection, this.Text, param, trans, cmdTimeout, cmdType)

    (* These methods facilitate the use from C# *)

    member this.Execute (param : obj) = this.Execute(param, null, Nullable<int>(), Nullable<CommandType>())
    member this.Execute (param : obj, trans : IDbTransaction) = this.Execute(param, trans, Nullable<int>(), Nullable<CommandType>())
    member this.Execute (param : obj, trans : IDbTransaction, commandTimeout : int) = this.Execute(param, trans, Nullable(commandTimeout), Nullable<CommandType>())
    member this.Execute (param : obj, trans : IDbTransaction, commandTimeout : int, commandType : CommandType) = this.Execute(param, trans, Nullable(commandTimeout), Nullable(commandType))


(* Parse a query into a Query type *)
let private parseQuery (query : string) =
    match query.Split([| "\n" |], StringSplitOptions.None) |> Array.toList with
    | head :: tail ->
        if String.IsNullOrEmpty head then None
        else
            Some({ Name = head.Trim();
                   Text = String.Join(Environment.NewLine, tail).Trim();
                   Connection = None })
    | _ -> None


(* Parse a bunch of queries from text to a Query list *)
let parse (content : string) =
    Regex.Split(content.Trim(), "--\s+name\:") 
    |> Array.map parseQuery
    |> Array.choose id
    |> Array.toList


/// <summary>
/// Keep track of queries and give them to you if you ask nicely
/// </summary>
type Database(connection: IDbConnection) =
    member val Queries : Map<string, Query> = Map.empty with get, set

    member this.FromMap (map : Map<string, Query>) =
        this.Queries <- Map.fold (
            fun acc key value ->
                value.Connection <- Some(new DisposableConnection(connection))
                Map.add key value acc
            ) this.Queries map
        this

    member this.FromString str =
        [| for q in parse str -> (q.Name, q) |] |> Map.ofSeq |> this.FromMap |> ignore
        this

    member this.FromFiles files =
        files |> List.map File.ReadAllText |> List.map this.FromString |> ignore
        this

    member this.Item
        with get(name : string) =
            if not(this.Queries.ContainsKey name) then
                raise (Exception(sprintf "Query %s not found" name))
            this.Queries.[name]
        and set name value =
            this.Queries <- this.Queries.Add(name, value)


(* Get a Query by name *)
let (?) (db: Database)(name: string) = db.[name]