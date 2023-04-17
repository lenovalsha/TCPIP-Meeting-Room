using SuperSimpleTcp;
using System.Text;

namespace TCPClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SimpleTcpClient client;

        private void Form1_Load(object sender, EventArgs e)
        {
            client = new SimpleTcpClient(txtServer.Text);
            client.Events.Connected += Events_Connected;
            client.Events.Disconnected += Events_Disconnected;
            client.Events.DataReceived += Events_DataReceived;
            btnSend.Enabled = false;
            
        }

        private void Events_DataReceived(object? sender, DataReceivedEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Data).TrimEnd('\0');
          
            this.Invoke((MethodInvoker)delegate
            {
                lstMessages.Items.Add(message);
            });
        }

        private void Events_Disconnected(object? sender, ConnectionEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lstMessages.Items.Add($"Server disconnected . {Environment.NewLine}");
            });
        }

        private void Events_Connected(object? sender, ConnectionEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lstMessages.Items.Add($"Server connected . {Environment.NewLine}");
            });
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                client.Connect();
                btnSend.Enabled = true;
                btnConnect.Enabled = false;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if(client.IsConnected)
            {
                if(!string.IsNullOrEmpty(txtMessage.Text))
                {
                    client.Send(txtMessage.Text);
                    lstMessages.Items.Add($"Me: {txtMessage.Text} {Environment.NewLine}");
                    txtMessage.Text = String.Empty;
                }
            }
        }
    }
}