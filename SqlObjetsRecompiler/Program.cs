
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
            using (var conn = new SqlConnection($"server={serverName};database={databaseName};trusted_connection=true;"))
            {
                var server = new Server(new ServerConnection(conn));
                var scriptingOptions = new ScriptingOptions
                {
                    AnsiFile                           = true,
                    ClusteredIndexes                   = true,
                    ContinueScriptingOnError           = false,
                    DdlBodyOnly                        = false,
                    DdlHeaderOnly                      = false,
                    DriAll                             = true,
                    EnforceScriptingOptions            = true,
                    FullTextIndexes                    = true,
                    IncludeDatabaseContext             = false,
                    IncludeIfNotExists                 = false,
                    Indexes                            = true,
                    Permissions                        = false,
                    SchemaQualify                      = true,
                    SchemaQualifyForeignKeysReferences = true,
                    ScriptData                         = false,
                    ScriptSchema                       = true,
                    Triggers                           = true,
                    ScriptBatchTerminator              = true
                };

                var db = server.Databases[databaseName];
                Console.WriteLine(db.Name);
                Console.WriteLine(new string('#', db.Name.Length));
                (from table in db.Tables.Cast<Table>()
                        where !table.IsSystemObject
                        from trigger in table.Triggers.Cast<Trigger>()
                        select (Table: table, Trigger: trigger)
                    ).ForEach(t => ScriptObject(db, t.Trigger.Script(scriptingOptions),
                        $"Trigger {t.Table.Schema}.{t.Table.Name}.{t.Trigger.Name}", @"CREATE\s+TRIGGER",
                        "ALTER TRIGGER"));

                (from s in db.StoredProcedures.Cast<StoredProcedure>()
                        where !s.IsSystemObject
                        select s
                    ).ForEach(s => ScriptObject(db, s.Script(scriptingOptions), $"Stored proc {s.Schema}.{s.Name}",
                        @"CREATE\s+PROCEDURE", "ALTER PROCEDURE"));

                (from f in db.UserDefinedFunctions.Cast<UserDefinedFunction>()
                        where !f.IsSystemObject
                        select f
                    ).ForEach(f => ScriptObject(db, f.Script(scriptingOptions), $"Function {f.Schema}.{f.Name}",
                        @"CREATE\s+FUNCTION", "ALTER FUNCTION"));

                (from v in db.Views.Cast<View>()
                        where !v.IsSystemObject
                        select v
                    ).ForEach(v => ScriptObject(db, v.Script(scriptingOptions), $"View {v.Schema}.{v.Name}",
                        @"CREATE\s+VIEW", "ALTER VIEW"));
            }
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
