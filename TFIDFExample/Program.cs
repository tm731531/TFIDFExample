using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFIDFExample
{
    class Program
    {
        public static string conn = ConfigurationManager.ConnectionStrings["DB_Conn"].ConnectionString;

        static void Main(string[] args)
        {
            string[] documents;
            // Some example documents.
            //string[] documents =
            //{
            //    "The sun in the sky is bright.",
            //    "We can see the shining sun, the bright sun."
            //};
            SqlConnection connection = new SqlConnection(conn);
            SqlCommand command = new SqlCommand("SELECT  top 100 [case_number] "+
      " ,[description] from [dbo].[GCC_Support_Case] ", connection);
            SqlDataAdapter custAdapter = new SqlDataAdapter();
            DataSet customerEmail = new DataSet();
            command.CommandType = CommandType.Text;
            List<string> stList = new List<string>();
            byte[] byteData = new byte[0];
            try
            {
                if (connection.State != ConnectionState.Open) connection.Open();
                custAdapter.SelectCommand = command;
                custAdapter.Fill(customerEmail, "tblTransaction");
                foreach (DataRow pRow in customerEmail.Tables["tblTransaction"].Rows)
                {
                    stList.Add(pRow["description"].ToString());
                    
                }
            }
            catch (SqlException ex)
            { }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();

            }
            documents = stList.ToArray();
            customerEmail.Clear();
            customerEmail.Dispose();
            // Apply TF*IDF to the documents and get the resulting vectors.
            //List<List<double>> inputs = TFIDF.Transform(documents, 0);
             TFIDF.Transform(documents, 0);
            //inputs = TFIDF.Normalize(inputs);

            // Display the output.
            //for (int index = 0; index < inputs.Length; index++)
            //{
            //    Console.WriteLine(documents[index]);

            //    foreach (double value in inputs[index])
            //    {
            //        Console.Write(value + ", ");
            //    }

            //    Console.WriteLine("\n");
            //}
            //string targetTable = "TFIDF";
            //SqlDataAdapter adapter = new SqlDataAdapter("SELECT top(0) * FROM " + targetTable, conn);
            //DataTable datatable = new DataTable();
            //adapter.Fill(datatable);
            //SqlBulkCopy SBC = new SqlBulkCopy(connection);
            //SBC.BulkCopyTimeout = 0;
            //SBC.DestinationTableName = "dbo." + targetTable;

            //List<object> colData = new List<object>();
            //connection.Open();
            //foreach (var value in TFIDF._vocabularyIDF.OrderByDescending(x => x.Value).ToList())
            //{
            //    colData.Clear();
            //    colData.Add(null);
            //    colData.Add(null);
            //    colData.Add(value.Key);
            //    colData.Add(value.Value);
            //    datatable.Rows.Add(colData.ToArray());

            //     Console.WriteLine(value.Key + "  :  " + value.Value + "\n");
            //        Console.Write(value + ", ");
            //}
            //SBC.WriteToServer(datatable);
            //connection.Close();
            Console.WriteLine("Press any key ..");
            Console.ReadKey();
        }
    }
}
