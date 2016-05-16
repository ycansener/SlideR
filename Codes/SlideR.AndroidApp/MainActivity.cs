using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Net;
using System.Net.Sockets;

namespace SlideR
{
    [Activity(Label = "SlideR", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private Slider.Client.Core.CMDClient client;
        public static IPAddress GetLocalIPAddressFromHostName(string hostName)
        {
            var host = Dns.GetHostEntry(hostName);
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it

            Button buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);

            buttonConnect.Click += buttonConnect_Click;

            Button buttonPrevious = FindViewById<Button>(Resource.Id.buttonPrevious);
            buttonPrevious.Enabled = false;
            buttonPrevious.Click += buttonPrevious_Click;


            Button buttonNext = FindViewById<Button>(Resource.Id.buttonNext);
            buttonNext.Enabled = false;
            buttonNext.Click += buttonNext_Click;
        }
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            EditText editTextIpAddress = FindViewById<EditText>(Resource.Id.editTextServerIp);
            if (!string.IsNullOrEmpty(editTextIpAddress.Text))
            {
                string ipAddress = editTextIpAddress.Text;
                string[] ipParts = ipAddress.Split('.');

                if(ipParts.Length == 4)
                {
                    this.client = new Slider.Client.Core.CMDClient(IPAddress.Parse(ipAddress), 8000, "None");
                    client.ConnectToServer();
                    button.Enabled = false;

                    Button buttonNext = FindViewById<Button>(Resource.Id.buttonNext);
                    buttonNext.Enabled = true;
                    Button buttonPrevious = FindViewById<Button>(Resource.Id.buttonPrevious);
                    buttonPrevious.Enabled = true;
                }
                else
                {
                    Toast.MakeText(this.ApplicationContext, "Invalid host ip!", ToastLength.Short);
                }
            }
            else
            {
                Toast.MakeText(this.ApplicationContext, "Invalid host ip!", ToastLength.Short);
            }
        }
        private void buttonPrevious_Click(object sender, EventArgs e)
        {
            if (this.client.Connected)
            {
                this.client.SendCommand(new Slider.Client.Core.Command(Slider.Client.Core.CommandType.Message, System.Net.IPAddress.Broadcast, "prev"));
            }
        }
        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (this.client.Connected)
            {
                this.client.SendCommand(new Slider.Client.Core.Command(Slider.Client.Core.CommandType.Message, System.Net.IPAddress.Broadcast, "next"));
            }
        }
    }
}

