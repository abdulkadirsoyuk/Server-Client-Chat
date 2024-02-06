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
using System.Threading;
using System.IO;
using System.Net;

namespace FormClient
{
    public partial class Form1 : Form
    {
        private bool terminated = false;
        private bool connected = false;
        private bool disconnectPressed = false;
        private string clientUsername = "";
        private Socket clientSocket;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            FormClosing += new FormClosingEventHandler(this.Form1_Closing);
            InitializeComponent();
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {

            connected = false;
            terminated = true;
            Environment.Exit(0);
        }

        private void Receive()
        {
            while (connected)
            {
                try
                {
                    byte[] numArray = new byte[5000000];
                    this.clientSocket.Receive(numArray);
                    this.clientSocket.Send(Encoding.Default.GetBytes("receivedinfo"));
                    string str1 = Encoding.Default.GetString(numArray);
                    string str2 = str1.Substring(0, str1.IndexOf("\0"));

                    if (str2.Substring(0, 10) == "SHOW_POSTS")
                    {

                        richTextBox1.AppendText(str2.Substring(10) + "\n");
                    }
                    else if (str2.Substring(0, 11) == "sent a post")
                    {

                        richTextBox1.AppendText(str2 + "\n");
                    }
                    

                }
                catch
                {
                    if (!terminated && !disconnectPressed)
                    {
                        richTextBox1.AppendText("The server has disconnected.\n");
                        buttonConnect.Enabled = true;
                        textBoxSend.Enabled = false;
                        buttonSend.Enabled = false;
                        buttonDisconnect.Enabled = false;
                        richTextBox1.Enabled = false;
                        buttonAllposts.Enabled = false;
                       
                        buttonEncrypt.Enabled = false;
                    }
                    clientSocket.Close();
                    connected = false;
                }
            }

        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
         
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string ip = textBoxIp.Text;
            int portNo;
            if (int.TryParse(textBoxPort.Text, out portNo))
            {
                string userName = textBoxUsername.Text;
                if (userName == "")
                    richTextBox1.AppendText("Please enter you username.\n");
                else if (ip == "")
                {
                    richTextBox1.AppendText("Please enter an IP address.\n");
                }
                else
                {
                    try
                    {
                        clientSocket.Connect(ip, portNo);
                        try
                        {
                            clientSocket.Send(Encoding.Default.GetBytes(userName));
                            try
                            {
                                byte[] numArray = new byte[5000000];
                                clientSocket.Receive(numArray);
                                switch (Encoding.Default.GetString(numArray).Trim(new char[1]))
                                {
                                    case "NOT_FOUND":
                                        richTextBox1.AppendText("Please enter a valid username.\n");
                                        break;
                                    case "Already_Connected":
                                        richTextBox1.AppendText("This user is already connected.\n");
                                        break;
                                    case "SUCCESS":
                                        buttonConnect.Enabled = false;
                                        buttonSend.Enabled = true;
                                        buttonDisconnect.Enabled = true;
                                        buttonAllposts.Enabled = true;
                                        richTextBox1.Enabled = true;
                                        buttonEncrypt.Enabled = true;
                                        textBoxSend.Enabled = true;
                                    
                                        disconnectPressed = false;
                                        connected = true;
                                        clientUsername = userName;
                                        richTextBox1.AppendText("Hello " + userName + "! You are connected to the server.\n");

                                        new Thread(new ThreadStart(this.Receive)).Start();
                                        break;
                                }
                            }
                            catch
                            {
                                richTextBox1.AppendText("There was a problem receiving response.\n");
                            }
                        }
                        catch
                        {
                            richTextBox1.AppendText("Problem occured while username is sent.\n");
                        }
                    }
                    catch
                    {
                        richTextBox1.AppendText("Could not connect to the server.\n");
                    }
                }
            }
            else
                richTextBox1.AppendText("Check the port\n");

        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {

            byte[] bytes = Encoding.Default.GetBytes("DISCONNECT");
            try
            {
                clientSocket.Send(bytes);
                disconnectPressed = true;
                buttonConnect.Enabled = true;
                textBoxSend.Enabled = false;
                buttonSend.Enabled = false;
            
                buttonDisconnect.Enabled = false;
                buttonEncrypt.Enabled = false;
                richTextBox1.Enabled = false;
                buttonAllposts.Enabled = false;
                clientSocket.Close();
                connected = false;
                richTextBox1.AppendText("Successfuly disconnected.\n");
            }
            catch
            {
                richTextBox1.AppendText("\n Error \n");
            }

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

        public string Decrypt(string encryptedText, string keyword)
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

        private void buttonSend_Click(object sender, EventArgs e)
        {

            string s = "SEND_POSTS" + textBoxSend.Text;
            textBoxSend.Text = "";
            if (!(s != "") || s.Length > 5000000)
                return;
            byte[] bytes = Encoding.Default.GetBytes(s);
            try
            {
                clientSocket.Send(bytes);
                richTextBox1.AppendText("You have successfully sent a post!\n");
                richTextBox1.AppendText(this.clientUsername + ": " + textBoxSend.Text.Substring(10) + " \n");
            }
            catch
            {
            }
        }

        private void buttonAllposts_Click(object sender, EventArgs e)
        {

            byte[] bytes = Encoding.Default.GetBytes("SHOW_POSTS");
            try
            {
                richTextBox1.AppendText("\nShowing all posts from clients: \n");
                clientSocket.Send(bytes);
            }
            catch
            {
                richTextBox1.AppendText("There was a problem in the request of reaching posts page to server.\n");
            }

        }

        private void buttonEncrypt_Click(object sender, EventArgs e)
        {

            textBoxSend.Text = Encrypt(textBoxSend.Text, "agtemelleri");
        }

    }
}
