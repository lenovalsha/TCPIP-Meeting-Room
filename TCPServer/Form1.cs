using SuperSimpleTcp;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TCPServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }
        SimpleTcpServer server;
        Dictionary<string, string> clientUsernames = new Dictionary<string, string>();
        List<string> clients = new List<string>();
        List<string> usernameLists = new List<string>();
        string hostIpAddress;


        private void Form1_Load(object sender, EventArgs e)
        {
            btnSend.Enabled = false;
            server = new SimpleTcpServer(txtServer.Text);
            server.Events.ClientConnected += Events_ClientConnected;
            server.Events.ClientDisconnected += Events_ClientDisconnected;
            server.Events.DataReceived += Events_DataReceived;
        }

        private void Events_DataReceived(object? sender, DataReceivedEventArgs e)
        {

            string message = $"{Encoding.UTF8.GetString(e.Data)}{Environment.NewLine}";
            if (message.StartsWith("kick"))
            {
                string kickUsername = message.Substring(5).TrimEnd('\r', '\n'); //this is the client that is getting kicked
                foreach(var user in clientUsernames)
                {

                    if(user.Value.Contains(kickUsername))
                    {
                        string[] userinfo = user.ToString().Split(',');
                        string ip = userinfo[0].TrimStart('[');
                        server.DisconnectClient(ip);
                        clients.Remove(ip);
                        clientUsernames.Remove(user.Value);
                        usernameLists.Remove(kickUsername +"\r\n");
                        this.Invoke((MethodInvoker)delegate
                        {
                            lstClientIp.Items.Remove(ip);
                        });
                   
                    }

                  
                }
                string clientList = string.Join(",", usernameLists.ToArray());
                foreach (var clientIp in lstClientIp.Items)
                {
                    //if(clientIp.ToString() == hostIpAddress)//if we want only the host to see the list 
                    server.Send(clientIp.ToString(), $"Clients:{clientList}");
                }
               
            }
            else if (message.StartsWith("SCREENSHOT:"))
            {
                string[] messageParts = message.Split(':');
                string imageData = messageParts[1];

                // Remove any non-Base64 characters from the string
                imageData = Regex.Replace(imageData, @"[^a-zA-Z0-9+/]", "");
                string datas = imageData.Remove(imageData.Length - 2);

                try
                {
                    // Convert the Base64 string to byte array
                    byte[] data = Convert.FromBase64String(datas);

                    // Process the decoded data...
                    foreach (var clientIp in lstClientIp.Items)
                    {
                        if (clientIp.ToString() != e.IpPort)
                            server.Send(clientIp.ToString(), "SCREENSHOT:" + Convert.ToBase64String(data));
                    }
                }
                catch (FormatException ex)
                {
                    // Handle the decoding error gracefully...
                    MessageBox.Show("Error decoding base64 string: " + ex.Message);
                }
            }
            else if (!clientUsernames.ContainsKey(e.IpPort) && !string.IsNullOrEmpty(message)) //first message
            {
                // The client has not yet chosen a username, so store the received message as their username.
                clientUsernames[e.IpPort] = message;
                string test = clientUsernames.ToString();
                usernameLists.Add(message);
                this.Invoke((MethodInvoker)delegate
                {
                    lstMessages.Items.Add($"{e.IpPort} chose the username '{message}'");
                });
                //notify the client that their username  has been set
                server.Send(e.IpPort, $"Your username is {message}");
                string clientList = string.Join(",", usernameLists.ToArray());
                foreach(var clientIp in lstClientIp.Items)
                {
                    //if(clientIp.ToString() == hostIpAddress)//if we want only the host to see the list 
                    server.Send(clientIp.ToString(), $"Clients:{clientList}");
                }
            }
            else
            {
                // The client has already chosen a username, so prepend it to the message.
                string username = clientUsernames[e.IpPort];
                message = $"{username}: {message}{Environment.NewLine}";
                foreach (var clientIp in lstClientIp.Items)
                {
                    if (clientIp.ToString() != e.IpPort)
                    {
                        server.Send(clientIp.ToString(), message);
                    }
                }
                this.Invoke((MethodInvoker)delegate
                {
                    lstMessages.Items.Add(message);
                });
            }
        }

        private void Events_ClientDisconnected(object? sender, ConnectionEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lstMessages.Items.Add($"{e.IpPort} disconnected.{Environment.NewLine}");
                lstClientIp.Items.Remove(e.IpPort);
            });
        }

        private void Events_ClientConnected(object? sender, ConnectionEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lstMessages.Items.Add($"{e.IpPort} connected");
                clients.Add(e.IpPort); // add the connected client to the list
                server.Send(e.IpPort, "Please enter your username" + Environment.NewLine);
                lstClientIp.Items.Add(e.IpPort);

                if (clients.Count == 1) // check if this is the first client to connect (i.e. the host)
                {
                    hostIpAddress = e.IpPort;
                    server.Send(e.IpPort, "You are the host." + Environment.NewLine); // notify the client that they are the host
                }
                else // if this is not the host, send the list of clients to the host
                {
                    string clientList = string.Join(",", clients.ToArray());
                    server.Send(clients[0], $"Clients:{clientList}"); // send the list of clients to the host
                }
            });

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            server.Start();
            lstMessages.Items.Add($"Starting ... {Environment.NewLine}");
            btnStart.Enabled = false;
            btnSend.Enabled = true;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (server.IsListening)
            {
                if (!string.IsNullOrEmpty(txtMessage.Text) && lstClientIp.SelectedItems != null)//check message & select client ip from listbox
                {
                    server.Send(lstClientIp.SelectedItem.ToString(), txtMessage.Text);
                    lstMessages.Items.Add($"Server: {txtMessage.Text} {Environment.NewLine}");
                    txtMessage.Text = String.Empty;
                }
            }
        }
        private void btnKick_Click(object sender, EventArgs e)
        {
            if (lstClientIp.SelectedItems.Count > 0)
            {
                foreach (var selectedItem in lstClientIp.SelectedItems)
                {
                    string ipPort = selectedItem.ToString();
                    if (server.IsConnected(ipPort))
                    {
                        server.DisconnectClient(ipPort);
                        lstMessages.Items.Add($"{ipPort} has been kicked by the server.{Environment.NewLine}");
                    }
                }
            }
        }
    }
}