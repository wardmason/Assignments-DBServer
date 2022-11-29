using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using Microsoft.SqlServer.Server;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using static System.Net.Sockets.Socket;
using System.Threading.Tasks;
using System.Threading;

namespace DatabasesExam
{

    class Program
    {
        static void Main(string[] args)
        {
            //connect to the database
            //referenced from MySQL documentation

            /*
            string connStr = "server=localhost;user=root;database=Assignments;port=3306;password=mypassword";
            MySqlConnection conn = new MySqlConnection(connStr);

            Program p = new Program();

            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            */

            p.StartServer();

            conn.Close();
            return;
        }       

        void InitializeTables(MySqlConnection conn)
        {
            //create the student submission table
            //contains student name, asn ID, is marked Y/N, and submission path
            string sql1 = "CREATE TABLE if not exists studasn(studname varchar(260), asnID int, isMarked bool, subPath varchar(260))"; //student submission
            MySqlCommand cmd1 = new MySqlCommand(sql1, conn);
            cmd1.ExecuteNonQuery();

            //create the graded rubrics table
            //contains rubric values, student name, and asn ID
            string sql2 = "CREATE TABLE if not exists gradedrubrics(rubric varchar(260), studname varchar(260), asnID int)"; //graded rubrics
            MySqlCommand cmd2 = new MySqlCommand(sql2, conn);
            cmd2.ExecuteNonQuery();

            //create the professor assignment table
            //contains the assignment name and the file path
            string sql3 = "CREATE TABLE if not exists profasn(asnname varchar(260), asnpath varchar(260), asnID int, duedate varchar(260), isGroup bool)"; //assignment master list
            MySqlCommand cmd3 = new MySqlCommand(sql3, conn);
            cmd3.ExecuteNonQuery();
        }

        void NewAssignment(MySqlConnection conn, string asnname, string path, int asnID, string duedate, bool isGroup)
            //receive an assignment name and filepath
        {
            //insert the filepath into the db
            string sql = "INSERT INTO Assignments.profasn(asnname, asnpath, asnID, duedate, isGroup) VALUES (@n, @p, @a, @d, @g)";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@n", asnname);
            cmd.Parameters.AddWithValue("@p", path);
            cmd.Parameters.AddWithValue("@a", asnID);
            cmd.Parameters.AddWithValue("@d", duedate);
            cmd.Parameters.AddWithValue("@g", isGroup);
            cmd.ExecuteNonQuery();  
        }

        string GetAssignmentPath(MySqlConnection conn, int asnID)
        {
            string path;
            string sql1 = "SELECT asnpath FROM Assignments.profasn WHERE asnID = @aID";


            MySqlCommand cmd = new MySqlCommand(sql1, conn);
            cmd.Parameters.AddWithValue("@aID", asnID);
            MySqlDataReader rdr = cmd.ExecuteReader();

            rdr.Read();

            var v = rdr[0];
            path = (string)v;

            rdr.Close(); //close the reader

            return path;
        }

        void StoreStudAssignment(MySqlConnection conn, string studentname, int asnID, bool isMarked, string subPath)
        //contains student name, asn ID, is marked Y/N, and submission path
        {
            string sql = "INSERT INTO Assignments.studasn(studname, asnID, isMarked, subPath) VALUES (@n, @a, @m, @p)";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@n", studentname);
            cmd.Parameters.AddWithValue("@a", asnID);
            cmd.Parameters.AddWithValue("@m", isMarked);
            cmd.Parameters.AddWithValue("@p", subPath);
            cmd.ExecuteNonQuery();
        }

        void StoreGradedRubric(MySqlConnection conn, string rubricpath, string studentname, int asnID)
            //receive a rubric path, student name, and assignment ID
        {
            string sql = "INSERT INTO Assignments.gradedrubrics(rubric, studname, asnID) VALUES (@r, @n, @a)";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@r", rubricpath);
            cmd.Parameters.AddWithValue("@n", studentname);
            cmd.Parameters.AddWithValue("@a", asnID);
            cmd.ExecuteNonQuery();
        }
        
        async void StartServer()
        {
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 27015);

            try
            {

                // Create a Socket that will use Tcp protocol
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10);

                Console.WriteLine("Waiting for a connection...");
                //Socket handler = listener.Accept();
                Console.WriteLine("Connected...");

                var handler = await listener.AcceptAsync();
                while (true)
                {

                    var buffer = new byte[1024];
                    int bytesRec = handler.Receive(buffer);
                    var response = Encoding.ASCII.GetString(buffer, 0, bytesRec);
                    if (response.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }

                    var eom = "<|EOM|>";
                    if (response.IndexOf(eom) > -1)
                    {
                        Console.WriteLine(
                            $"Socket server received message: \"{response.Replace(eom, "")}\"");

                        var ackMessage = "<|ACK|>";
                        var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
                        handler.Send(echoBytes, 0);
                        Console.WriteLine( $"Socket server sent acknowledgment: \"{ackMessage}\"");

                        break;
                    }
                    // Sample output:
                    //    Socket server received message: "Hi friends 👋!"
                    //    Socket server sent acknowledgment: "<|ACK|>"
                    // Incoming data from the client.

                    byte[] msg = Encoding.ASCII.GetBytes(response);
                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        
        }
    }
}