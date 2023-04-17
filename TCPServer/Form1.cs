using SuperSimpleTcp;
using System.Text;
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
        bool isHost = false;
        List<string> connectedClients = new List<string>();
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
            if (!clientUsernames.ContainsKey(e.IpPort))
            {
                // The client has not yet chosen a username, so store the received message as their username.
                clientUsernames[e.IpPort] = message;
                this.Invoke((MethodInvoker)delegate
                {
                    lstMessages.Items.Add($"{e.IpPort} chose the username '{message}'");
                });
                //notify the client that their username  has been set
                server.Send(e.IpPort, $"Your username is {message}");
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
                if(lstClientIp.Items.Count == 0)
                {
                    //the first client to connect becomes the host
                    lstMessages.Items.Add($"{e.IpPort} is the host");
                    server.Send(e.IpPort, "You are the host");
                }else
                {
                lstMessages.Items.Add($"{e.IpPort} connected");
                lstClientIp.Items.Add(e.IpPort);
                }

                server.Send(e.IpPort, "Please enter your username");
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