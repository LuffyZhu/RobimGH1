using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ConnectToDB
{
    public class DBConnect
    {
        private static MySqlConnection connection;
        private static string server;
        private static string database;
        private static string uid;
        private static string password;

        //Constructor
        public DBConnect()
        {
            Initialize();
        }

        //Initialize values
        private static void Initialize()
        {
            server = "106.14.212.171";
            database = "Robim";
            uid = "root";
            password = "Roboticplus001";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";" + "Connect Timeout = 10000" + ";";
            connection = new MySqlConnection(connectionString);
        }

        //open connection to database
        private static bool OpenConnection()
        {
            Initialize();
            try
            {
                connection.Open();
                //MessageBox.Show("Already connect to server.");
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        MessageBox.Show("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        MessageBox.Show("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }

        //Close connection
        private static bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        //Insert statement
        public static void Insert(string schemavalue, string tablevalue, string columnvalue,string value)
        {
            string query = string.Format("INSERT INTO {0}.{1} ({2}) VALUES({3})", schemavalue, tablevalue, columnvalue, value);

            //open connection
            if (OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                CloseConnection();
            }
        }

        //Update statement
        public static void Update(string schemavalue, string tablevalue, string[] columnvalue, string[] value, string wherecolumn, string wherevalue)
        {
            if(columnvalue.Length != value.Length)
            {
                return;
            }
            string query = $"UPDATE {schemavalue}.{tablevalue} SET ";
            for (int i = 0; i < columnvalue.Length; i++)
            {
                if(i != 0)
                {
                    query += ",";
                }
                query += $"{columnvalue[i]}='{value[i]}'";
            }
            query += $" WHERE {wherecolumn}='{wherevalue}'";

            //Open connection
            if (OpenConnection() == true)
            {
                //create mysql command
                MySqlCommand cmd = new MySqlCommand();
                //Assign the query using CommandText
                cmd.CommandText = query;
                //Assign the connection using Connection
                cmd.Connection = connection;

                //Execute query
                cmd.ExecuteNonQuery();

                //close connection
                CloseConnection();
            }
        }

        //Delete statement
        public static void Delete()
        {
            string query = "DELETE FROM tableinfo WHERE name='John Smith'";

            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                CloseConnection();
            }
        }

        //Select statement
        public static List<string> Select(string schemavalue, string tablevalue, string columnvalue)
        {
            //string query = "SELECT * FROM tableinfo";
            string query = string.Format("SELECT {0} FROM {1}.{2}", columnvalue, schemavalue, tablevalue);
            //Create a list to store the result
            List<string> list = new List<string>();
            //Open connection
            if (OpenConnection() == true)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, connection);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();
                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        list.Add(dataReader[i].ToString());
                    }
                }
                //close Data Reader
                dataReader.Close();
                //close Connection
                CloseConnection();
                //return list to be displayed
                return list;
            }
            else
            {
                return list;
            }
        }
        public static List<string> SelectWhere(string columnvalue,string schemavalue, string tablevalue,string wherecolumn,string wherevalue,string comparisonsymbol = "=")
        {
            //string query = "SELECT * FROM tableinfo";
            string query = string.Format("SELECT {0} FROM {1}.{2} WHERE {3} {4} '{5}'", columnvalue, schemavalue, tablevalue, wherecolumn, comparisonsymbol, wherevalue);
            //Create a list to store the result
            List<string> list = new List<string>();
            //Open connection
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        list.Add(dataReader[i].ToString());
                    }
                }
                dataReader.Close();
                CloseConnection();
                return list;
            }
            else
            {
                return list;
            }
        }
        public static List<string> SelectMulitWhere(string columnvalue, string schemavalue, string tablevalue, string[] wherecolumn, string[] wherevalue, string[] comparisonsymbol = null)
        {
            //string query = "SELECT * FROM tableinfo";
            string query = string.Format("SELECT {0} FROM {1}.{2} WHERE", columnvalue, schemavalue, tablevalue);
            for(int i = 0; i < wherecolumn.Length; i++)
            {
                if(i != 0)
                {
                    query += " AND";
                }
                if (comparisonsymbol == null)
                {
                    query += $" {wherecolumn[i]} = '{wherevalue[i]}'";
                }
                else
                {
                    query += $" {wherecolumn[i]} {comparisonsymbol[i]} '{wherevalue[i]}'";
                }
            }
            //Create a list to store the result
            List<string> list = new List<string>();
            //Open connection
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        list.Add(dataReader[i].ToString());
                    }
                }
                dataReader.Close();
                CloseConnection();
                return list;
            }
            else
            {
                return list;
            }
        }

        public static string GetTime()
        {
            //string query = "SELECT * FROM tableinfo";
            string query = "SELECT now()";
            //Create a list to store the result
            string Time = "";
            //Open connection
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    Time = dataReader[0].ToString();
                }
                dataReader.Close();
                CloseConnection();
                return Time;
            }
            else
            {
                return Time;
            }
        }
        //Count statement
        public static int Count()
        {
            string query = "SELECT Count(*) FROM tableinfo";
            int Count = -1;

            //Open Connection
            if (OpenConnection() == true)
            {
                //Create Mysql Command
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //ExecuteScalar will return one value
                Count = int.Parse(cmd.ExecuteScalar() + "");

                //close Connection
                CloseConnection();

                return Count;
            }
            else
            {
                return Count;
            }
        }
    }
}
