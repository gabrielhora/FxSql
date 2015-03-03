using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            var con = new SqlConnection("Server=localhost;Database=Kiosk;User Id=sa;Password=134711;");

            var sql = @"
                -- name: AllClients
                SELECT * FROM Clients;

                -- name: OneClient
                SELECT * FROM Clients
                WHERE Id = @Id;";

            var list = FxSql.parse("");
            var db = new FxSql.Database(con).FromString(sql);
            var result = db["OneClient"].Query(new { Id = 4 });
        }
    }
}
