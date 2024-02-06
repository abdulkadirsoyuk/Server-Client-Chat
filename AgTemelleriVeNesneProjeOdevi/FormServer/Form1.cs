using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace FormServer
{
    public partial class Form1 : Form
    {
        private void Form1_Load(object sender, EventArgs e)
        {

            try
            {
                string dosyayolu = "../../posts.txt";
                if (File.Exists(dosyayolu))
                {
                    File.WriteAllText(dosyayolu, string.Empty);
                    File.Delete(dosyayolu);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        private Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private List<Socket> clientSockets = new List<Socket>();
        private List<string> clientusernames = new List<string>();
        private int postCount = Form1.CountPost();
        private bool terminating = false;
        private bool listening = false;


        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        public static string Decrypt(string encryptedText, string keyword)
        {
            encryptedText = encryptedText.ToLower();
            keyword = keyword.ToLower();
            string decryptedText = "";
            int keywordIndex = 0;

            foreach (char c in encryptedText)
            {
                if (char.IsLetter(c))
                {
                    int shift = keyword[keywordIndex] - 'a';
                    decryptedText += ShiftChar(c, -shift);
                    keywordIndex = (keywordIndex + 1) % keyword.Length;
                }
                else
                {
                    decryptedText += c;
                }
            }

            return decryptedText;
        }


        private static char ShiftChar(char c, int shift)
        {
            const int alphabetSize = 26;
            char shiftedChar = (char)(c + shift);
            if (shiftedChar > 'z')
            {
                shiftedChar = (char)(shiftedChar - alphabetSize);
            }
            else if (shiftedChar < 'a')
            {
                shiftedChar = (char)(shiftedChar + alphabetSize);
            }
            return shiftedChar;
        }
        private void buttonListen_Click(object sender, EventArgs e)
        {

            int portNo;
            if (int.TryParse(textBoxPort.Text, out portNo))
            {
                try
                {
                    serverSocket.Bind((EndPoint)new IPEndPoint(IPAddress.Any, portNo));
                    serverSocket.Listen(3);
                    listening = true;
                    buttonListen.Enabled = false;
                    new Thread(new ThreadStart(Accept)).Start();
                    richTextBox1.AppendText("Started listening on port: " + (object)portNo + "\n");
                    buttonListen.BackColor = Color.Green;
                }
                catch
                {
                    richTextBox1.AppendText("port is being used\n");
                }

            }
            else
                richTextBox1.AppendText("Please check port number \n");
        }

        private void Accept()
        {
            while (listening)
            {
                try
                {
                    Socket newClient = this.serverSocket.Accept();
                    new Thread((ThreadStart)(() => this.usernameCheck(newClient))).Start();
                }
                catch
                {
                    if (terminating)
                        listening = false;
                    else
                        richTextBox1.AppendText("The socket stopped working.\n");
                }
            }
        }


        private void usernameCheck(Socket thisClient)
        {
            string s = "NOT_FOUND";
            try
            {
                byte[] numArray = new byte[5000000];
                thisClient.Receive(numArray);
                string username = Encoding.Default.GetString(numArray);
                username = username.Substring(0, username.IndexOf("\0"));
                if (this.clientusernames.Contains(username))
                {
                    richTextBox1.AppendText(username + " has tried to connect from another client!\n");
                    s = "Already_Connected";
                }
                else
                {
                    string connectionString = ""; // Connection String
                    string usernameToCheck = username;

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            string sql = "SELECT COUNT(*) FROM username WHERE username = @Username";
                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@Username", usernameToCheck);

                                int userCount = (int)command.ExecuteScalar();

                                if (userCount > 0)
                                {
                                    this.clientSockets.Add(thisClient);
                                    this.clientusernames.Add(username);
                                    s = "SUCCESS";
                                    richTextBox1.AppendText(username + " has connected.\n");

                                    object lockObject = new object();
                                    new Thread((ThreadStart)(() => Receive(thisClient, username))).Start();
                                   
                                }

                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("failed to connect to database:\n " + ex);
                        }
                    }
                }
                if (s == "NOT_FOUND")
                    richTextBox1.AppendText(username + " tried to connect to the server but cannot!\n");
                try
                {
                    thisClient.Send(Encoding.Default.GetBytes(s));
                }
                catch
                {
                    richTextBox1.AppendText("There was a problem when sending the username response to the client.\n");
                }
            }
            catch
            {
                richTextBox1.AppendText("Problem receiving username.\n");
            }
        }
        private void Receive(Socket thisClient, string username)
        {
            bool flag = true;
            while (flag && !this.terminating)
            {
                try
                {
                    byte[] numArray = new byte[5000000];
                    thisClient.Receive(numArray);
                    string str = Encoding.Default.GetString(numArray).Trim(new char[1]);
                    if (str.Substring(0, 10) == "DISCONNECT")
                    {
                        thisClient.Close();
                        this.clientSockets.Remove(thisClient);
                        this.clientusernames.Remove(username);
                        flag = false;
                        richTextBox1.AppendText(username + " has disconnected\n");
                    }
                    else if (str.Substring(0, 10) == "SHOW_POSTS")
                        allposts(thisClient, username);
                    else if (str.Substring(0, 10) == "SEND_POSTS")
                    {
                        string post = str.Substring(10);
                        ++this.postCount;
                        this.postToLog(username, (object)this.postCount, post);
                    }


                }
                catch
                {
                    if (!this.terminating)
                        richTextBox1.AppendText(username + " has disconnected.\n");
                    thisClient.Close();
                    this.clientSockets.Remove(thisClient);
                    this.clientusernames.Remove(username);
                    flag = false;
                }
            }
        }
        public string Encrypt(string plainText, string keyword)
        {
            plainText = plainText.ToLower();
            keyword = keyword.ToLower();
            string encryptedText = "";
            int keywordIndex = 0;

            foreach (char c in plainText)
            {
                if (char.IsLetter(c))
                {
                    int shift = keyword[keywordIndex] - 'a';
                    encryptedText += ShiftChar(c, shift);
                    keywordIndex = (keywordIndex + 1) % keyword.Length;
                }
                else
                {
                    encryptedText += c;
                }
            }

            return encryptedText;
        }
        private void postToLog(string username, object postID, string post)
        {
            post = Decrypt(post, "agtemelleri");
            string str = DateTime.Now.ToString("s");
        
            using (StreamWriter streamWriter = new StreamWriter("../../posts.txt", true))
               
                streamWriter.WriteLine(str + " / " + username + " / " + postID.ToString() + " / " + post + " / ");
            richTextBox1.AppendText(username + " has sent a post:\n" + post + "\n");

            byte[] bytes1 = Encoding.Default.GetBytes("sent a post " + username + ": " + post);
            foreach (Socket clientSocket in clientSockets)
            {
                clientSocket.Send(bytes1);
            }

        }

        private void allposts(Socket thisClient, string username)
        {
            string input = System.IO.File.ReadAllText("../../posts.txt");
            string pattern = "\\d\\d\\d\\d[-]\\d\\d[-]\\d\\d[T]\\d\\d[:]\\d\\d[:]\\d\\d";
            string[] strArray = new Regex(pattern).Split(input);
            MatchCollection matchCollection = Regex.Matches(input, pattern);
            for (int index = 1; index < strArray.Length; index++)
            {
                int num1 = strArray[index].IndexOf("/", 2);
                int num2 = strArray[index].IndexOf("/", num1 + 1);
                string str1 = strArray[index].Substring(2, num1 - 2);
                string str2 = strArray[index].Substring(num1 + 1, num2 - num1 - 1);
                string str3 = strArray[index].Substring(num2 + 1, strArray[index].Length - 4 - num2);


                byte[] bytes1 = Encoding.Default.GetBytes("SHOW_POSTSUsername: " + str1);

                thisClient.Send(bytes1);
                byte[] numArray1 = new byte[5000000];
                thisClient.Receive(numArray1);
                Encoding.Default.GetString(numArray1);
                byte[] bytes2 = Encoding.Default.GetBytes("SHOW_POSTSPostID: " + str2);

                thisClient.Send(bytes2);
                byte[] numArray2 = new byte[5000000];
                thisClient.Receive(numArray1);
                Encoding.Default.GetString(numArray1);
                byte[] bytes3 = Encoding.Default.GetBytes("SHOW_POSTSPost: " + str3);

                thisClient.Send(bytes3);
                byte[] numArray3 = new byte[5000];
                thisClient.Receive(numArray1);
                Encoding.Default.GetString(numArray1);
                byte[] bytes4 = Encoding.Default.GetBytes("SHOW_POSTSTime: " + (object)matchCollection[index - 1] + "\n");

                thisClient.Send(bytes4);
                byte[] numArray4 = new byte[5000000];
                thisClient.Receive(numArray1);
                Encoding.Default.GetString(numArray1);



            }
            richTextBox1.AppendText("Showed all posts for " + username + ".\n");
        }

        private static int CountPost()
        {
            if (!System.IO.File.Exists("../../posts.txt"))
                System.IO.File.Create("../../posts.txt").Dispose();
            string input = System.IO.File.ReadAllText("../../posts.txt");
            if (input == "")
                return 0;
            string[] strArray = new Regex("\\d\\d\\d\\d[-]\\d\\d[-]\\d\\d[T]\\d\\d[:]\\d\\d[:]\\d\\d").Split(input);
            int num1 = strArray[strArray.Length - 1].IndexOf("/", 2);
            int num2 = strArray[strArray.Length - 1].IndexOf("/", num1 + 1);
            return int.Parse(strArray[strArray.Length - 1].Substring(num1 + 1, num2 - num1 - 1));
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string addUsername = textBoxUsername.Text;

            string connectionString = ""; // Connection String

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "INSERT INTO username (username) VALUES (@Username)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", addUsername);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("New user added successfully.");
                        }
                        else
                        {
                            MessageBox.Show("An error occurred while adding the user.");
                        }
                    }
                }
               
                catch (SqlException ex)
                {

                    if (ex.Number == 2627 || ex.Number == 2601)
                    {
                        MessageBox.Show("This username already exists.");
                    }
                    MessageBox.Show("failed to connect to database:\n " + ex);
                }
               

            }



        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {

            string removeUsername = textBoxUsername.Text;

            string connectionString = ""; // Connection String

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "DELETE FROM username WHERE username = @Username";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Parametre ekleniyor
                        command.Parameters.AddWithValue("@Username", removeUsername);

                        // Sorgu çalıştırılıyor
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("The user has been successfully removed.");
                            CloseConnectionForUsername(removeUsername);
                        }
                        else
                        {
                            MessageBox.Show("This user does not already exist.");
                        }
                    }
                }
                catch(Exception ex)
                {
                   
                    MessageBox.Show("failed to connect to database: \n" + ex);
                
            }
            }

        }


        private void CloseConnectionForUsername(string username)
        {
            for (int i = 0; i < clientusernames.Count; i++)
            {
                if (clientusernames[i] == username)
                {
                    Socket clientSocket = clientSockets[i];
                    try
                    {
                        byte[] disconnectMsg = Encoding.Default.GetBytes("DISCONNECT");
                        clientSocket.Send(disconnectMsg);

                        clientSocket.Close();

                        clientSockets.RemoveAt(i);
                        clientusernames.RemoveAt(i);

                        break;
                    }
                    catch (Exception ex)
                    {
                        richTextBox1.AppendText("Error: " + ex.Message + "\n");
                    }
                }
            }
        }
    }
}
