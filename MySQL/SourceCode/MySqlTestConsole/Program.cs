using System;
using System.Data;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using Peakboard.ExtensionKit;
using PeakboardExtensionMySql;

namespace MySqlTestConsole
{
    class Program
    {
        //static string Host = "xxx.compute.amazonaws.com";
        //static string Database = "sys";
        //static string UserID = "xxx";
        //static string Password = "xxx";
        static string Host = "ec2-3-120-231-115.eu-central-1.compute.amazonaws.com";
        static string Database = "sys";
        static string UserID = "peakboard";
        static string Password = "P34kB0rd_db_user";

        static void Main(string[] args)
        {
            try
            {
                //Test1();
                Test2();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey();
            }
        }

        static void Test1()
        {
            string cs = $"server={Host};userid={UserID};password={Password};database={Database}";

            string SQLStatement = "select * from `sys`.`material`";

            using (var con = new MySqlConnection(cs))
            {
                con.Open();

                Console.WriteLine($"MySQL version : {con.ServerVersion}");

                var command = new MySqlCommand(SQLStatement, con);
                var reader = command.ExecuteReader();
                var schemaTable = reader.GetSchemaTable();

                foreach (DataRow sqlcol in schemaTable.Rows)
                {
                    Console.WriteLine($"{sqlcol["ColumnName"]}: {sqlcol["DataType"]}");
                }
            }

            using (var con = new MySqlConnection(cs))
            {
                con.Open();

                MySqlDataAdapter da = new MySqlDataAdapter(new MySqlCommand(SQLStatement, con));
                DataTable sqlresult = new DataTable();
                da.Fill(sqlresult);
                con.Close();
                da.Dispose();

                Console.WriteLine(sqlresult.Rows.Count);
            }
        }

        static void Test2()
        {
            var extension = new MySqlExtension(null);
            var customList = extension.GetCustomLists()?.Value[0];

            var data = new CustomListData();
            data.Properties.Add("Host", Host);
            data.Properties.Add("Database", Database);
            data.Properties.Add("Username", UserID);
            data.Properties.Add("Password", Password);
            data.Properties.Add("SQLStatement", "select * from `sys`.`material`");

            var columns = customList.GetColumns(data)?.Value;
            var items = customList.GetItems(data)?.Value;
        }
    }
}