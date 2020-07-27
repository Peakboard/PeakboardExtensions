using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Ingres.Client;

namespace IngresTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string SQLStatement = "select * from revenue";
            // SQLStatement = "delete from revenue";
            // SQLStatement = "insert into revenue(myid, material, revenue) values(1, 'Flower', 12.34)";
            // SQLStatement = "insert into revenue(myid, material, revenue) values(2, 'car', 1.34)";
            // SQLStatement = "insert into revenue(myid, material, revenue) values(3, '花', 26.989)";
            // SQLStatement = "insert into revenue(myid, material, revenue) values(3, '汽车',26.989)";

            IngresConnection con = new IngresConnection(string.Format("Host={0};Database={1};Uid={2};Pwd={3}", "XXX", "demodb", "Administrator", "HX"));
            IngresDataAdapter da = new IngresDataAdapter(new IngresCommand(SQLStatement, con));
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
