using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string cs = @"server=xxx.compute.amazonaws.com;userid=xxx;password=xxx;database=sys";

            var con = new MySqlConnection(cs);
            con.Open();

            Console.WriteLine($"MySQL version : {con.ServerVersion}");

            string SQLStatement = "select * from `sys`.`material`";


            MySqlDataAdapter da = new MySqlDataAdapter(new MySqlCommand(SQLStatement, con));
            DataTable sqlresult = new DataTable();
            da.Fill(sqlresult);
            con.Close();
            da.Dispose();

            foreach (DataColumn sqlcol in sqlresult.Columns)
            {
                Console.WriteLine(sqlcol.DataType);
            }

            Console.WriteLine(sqlresult.Rows.Count);
            Console.Read();
        }
    }
}
