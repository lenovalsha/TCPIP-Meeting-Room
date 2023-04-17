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

            this.Invoke((MethodInvoker)delegate
            {
                lstMessages.Items.Add($"{e.IpPort}: " + message);
            });
            foreach (var clientIp in lstClientIp.Items)
            {
                if (clientIp.ToString() != e.IpPort)
                {
                    server.Send(clientIp.ToString(), message);
                }
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
                lstClientIp.Items.Add(e.IpPort);
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
    }
}