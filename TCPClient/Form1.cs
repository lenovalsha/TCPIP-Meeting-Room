using SuperSimpleTcp;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace TCPClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SimpleTcpClient client;
        bool isHost = false;
        private void Form1_Load(object sender, EventArgs e)
        {
            client = new SimpleTcpClient(txtServer.Text);
            client.Events.Connected += Events_Connected;
            client.Events.Disconnected += Events_Disconnected;
            client.Events.DataReceived += Events_DataReceived;
            btnSend.Enabled = false;
            btnKick.Enabled = false;
            btnShareScreen.Enabled = false;
        }

        private void Events_DataReceived(object? sender, DataReceivedEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Data).TrimEnd('\0');

            //if (message.StartsWith("Clients:")) //get a list of clients
            //{
            //    string[] clients = message.Substring(8).Split(',');
            //    this.Invoke((MethodInvoker)delegate
            //    {
            //        //lstClients.Items.Clear();
            //        //lstClients.Items.AddRange(clients);
            //        //MessageBox.Show("Test");
            //    });
            //}         
             if (message.StartsWith("SCREENSHOT:"))
            {
                byte[] image = Convert.FromBase64String( message.Substring(11));
                OnScreenshotReceived(image);
            }
            else
            {
                string serverIpAddress = "127.0.0.1:9100";
                string[] serverAddressParts = serverIpAddress.Split(':');
                string serverIp = serverAddressParts[0];
                int serverPort = int.Parse(serverAddressParts[1]);
                string ipAddress = e.IpPort;

                if (message.Contains("You are the host") && ipAddress == serverIpAddress)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        btnKick.Enabled = true;
                        btnShareScreen.Enabled = true;
                    });
                }
                else if (message.StartsWith("Clients:")) //get a list of clients
                {
                    string[] clients = message.Substring(8).Split(',');
                    this.Invoke((MethodInvoker)delegate
                    {
                        lstClients.Items.Clear();
                        lstClients.Items.AddRange(clients);
                    });
                }
                else
                {

                this.Invoke((MethodInvoker)delegate
                {
                    lstMessages.Items.Add($"{message}{Environment.NewLine}");
                });
                }
            }
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
                lstMessages.Items.Add($"Please enter your username");
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client.IsConnected)
            {
                if (!string.IsNullOrEmpty(txtMessage.Text))
                {
                    client.Send(txtMessage.Text);
                    lstMessages.Items.Add($"Me: {txtMessage.Text} {Environment.NewLine}");
                    txtMessage.Text = String.Empty;
                }
            }
        }

        private void btnKick_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedItems.Count > 0)
            {
                foreach (var selectedItem in lstClients.SelectedItems)
                {
                    string ipPort = selectedItem.ToString();
                    if (client.IsConnected)
                    {
                        client.Send($"kick {ipPort}");
                    }
                }
            }
        }

        private void btnShareScreen_Click(object sender, EventArgs e)
        {
            // Capture the client form as a bitmap
            Bitmap bitmap = new Bitmap(this.Width, this.Height);
            this.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

            // Convert the bitmap to a byte array
            byte[] imageBytes;
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                imageBytes = stream.ToArray();
            }

            // Convert the byte array to a Base64 string
            string base64String = Convert.ToBase64String(imageBytes);

            // Send the Base64 string to the server
            string buffer = ("SCREENSHOT:" + base64String);
            client.Send(buffer);
        }
        private void OnScreenshotReceived(byte[] image)
        {        
            using (MemoryStream ms = new MemoryStream(image))
            {
                // Load the screenshot image from the MemoryStream
                Image screenshotImage = Image.FromStream(ms);

                // Create a new form to display the screenshot image
                using (Form screenshotForm = new Form())
                {
                    screenshotForm.Text = "Screenshot";
                    screenshotForm.ClientSize = screenshotImage.Size;

                    // Create a new PictureBox to display the screenshot image
                    PictureBox screenshotPictureBox = new PictureBox();
                    screenshotPictureBox.Dock = DockStyle.Fill;
                    screenshotPictureBox.Image = screenshotImage;

                    // Add the PictureBox to the form
                    screenshotForm.Controls.Add(screenshotPictureBox);

                    // Show the form
                    screenshotForm.ShowDialog();
                }
            }
        }



    }
}