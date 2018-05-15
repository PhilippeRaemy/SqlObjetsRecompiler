
namespace SqlObjetsRecompiler
{
    using System;
    using System.Collections.Specialized;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Smo;
    using MoreLinq;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var serverName = ".";
            var databaseName = "master";
            args.GetArgumentValue("-S", v => serverName = v)
                .GetArgumentValue("-d", v => databaseName = v);
            var server =
                new Server(new ServerConnection(
                    new SqlConnection($"server={serverName};database={databaseName};trusted_connection=true;")));
            var scriptingOptions = new ScriptingOptions
            {
                AnsiFile = true,
                ClusteredIndexes = true,
                ContinueScriptingOnError = false,
                DdlBodyOnly = false,
                DdlHeaderOnly = false,
                DriAll = true,
                EnforceScriptingOptions = true,
                FullTextIndexes = true,
                IncludeDatabaseContext = false,
                IncludeIfNotExists = false,
                Indexes = true,
                Permissions = false,
                SchemaQualify = true,
                SchemaQualifyForeignKeysReferences = true,
                ScriptData = false,
                ScriptSchema = true,
                Triggers = true,
                ScriptBatchTerminator = true 
            };

            var db = server.Databases[databaseName];
            Console.WriteLine(db.Name);
            Console.WriteLine(new string('#', db.Name.Length));
            foreach (Table table in db.Tables)
                foreach (Trigger t in table.Triggers)                  ScriptObject(db, t.Script(scriptingOptions), $"Trigger {table.Schema}.{table.Name}.{t.Name}", @"CREATE\s+TRIGGER"  , "ALTER TRIGGER"  );
            foreach (StoredProcedure s in db.StoredProcedures)         ScriptObject(db, s.Script(scriptingOptions), $"Stored proc {s.Schema}.{s.Name}",              @"CREATE\s+PROCEDURE", "ALTER PROCEDURE");
            foreach (UserDefinedFunction f in db.UserDefinedFunctions) ScriptObject(db, f.Script(scriptingOptions), $"Function {f.Schema}.{f.Name}",                 @"CREATE\s+FUNCTION" , "ALTER FUNCTION" );
        }

        static void ScriptObject(Database db, StringCollection script, string name, string createStatement, string alterStatement)
        {
            var sql = new StringCollection();
            sql.AddRange(script
                .Cast<string>()
                .Select(s => Regex.Replace(s, createStatement, alterStatement,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                .Prepend("BEGIN TRANSACTION")
                .Concat("ROLLBACK")
                .ToArray()
            );
            try
            {
                // var cmd = new SqlCommand(sql, server.ConnectionContext.SqlConnectionObject);
                // cmd.Transaction = tran;
                // cmd.ExecuteNonQuery();
                db.ExecuteNonQuery(sql);
                Console.WriteLine($"Recompiled {name}.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(new string('=', name.Length));
                Console.Error.WriteLine(name);
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(new string('=', name.Length));
                Console.Error.WriteLine(sql);
                Console.Error.WriteLine(new string('=', name.Length));
            }
        }
    }
}
