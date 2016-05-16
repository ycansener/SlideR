using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Windows.Forms;
using Slider.Server.Core;

namespace Slider.HostApp
{
    class Program
    {
        private System.Collections.Generic.List<ClientManager> clients;
        private BackgroundWorker bwListener;
        private Socket listenerSocket;
        private IPAddress serverIP;
        private int serverPort;

        /// <summary>
        /// Start the console server.
        /// </summary>
        /// <param name="args">These are optional arguments.Pass the local ip address of the server as the first argument and the local port as the second argument.</param>
        static void Main(string[] args)
        {
            Program progDomain = new Program();
            progDomain.clients = new List<ClientManager>();

            progDomain.serverPort = 8000;
            //progDomain.serverIP = IPAddress.Any;
            progDomain.serverIP = GetLocalIPAddress();

            progDomain.bwListener = new BackgroundWorker();
            progDomain.bwListener.WorkerSupportsCancellation = true;
            progDomain.bwListener.DoWork += new DoWorkEventHandler(progDomain.StartToListen);
            progDomain.bwListener.RunWorkerAsync();

            Console.WriteLine("*** Listening on port {0}{1}{2} started.Press ENTER to shutdown server. ***\n", progDomain.serverIP.ToString(), ":", progDomain.serverPort.ToString());

            Console.ReadLine();

            progDomain.DisconnectServer();
        }
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        private void StartToListen(object sender, DoWorkEventArgs e)
        {
            this.listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.listenerSocket.Bind(new IPEndPoint(this.serverIP, this.serverPort));
            this.listenerSocket.Listen(200);
            while (true)
                this.CreateNewClientManager(this.listenerSocket.Accept());
        }
        private void CreateNewClientManager(Socket socket)
        {
            ClientManager newClientManager = new ClientManager(socket);
            newClientManager.CommandReceived += new CommandReceivedEventHandler(CommandReceived);
            newClientManager.Disconnected += new DisconnectedEventHandler(ClientDisconnected);
            this.CheckForAbnormalDC(newClientManager);
            this.clients.Add(newClientManager);
            this.UpdateConsole("Connected.", newClientManager.IP, newClientManager.Port);
        }

        private void CheckForAbnormalDC(ClientManager mngr)
        {
            if (this.RemoveClientManager(mngr.IP))
                this.UpdateConsole("Disconnected.", mngr.IP, mngr.Port);
        }

        void ClientDisconnected(object sender, ClientEventArgs e)
        {
            if (this.RemoveClientManager(e.IP))
                this.UpdateConsole("Disconnected.", e.IP, e.Port);
        }

        private bool RemoveClientManager(IPAddress ip)
        {
            lock (this)
            {
                int index = this.IndexOfClient(ip);
                if (index != -1)
                {
                    string name = this.clients[index].ClientName;
                    this.clients.RemoveAt(index);

                    //Inform all clients that a client had been disconnected.
                    Command cmd = new Command(CommandType.ClientLogOffInform, IPAddress.Broadcast);
                    cmd.SenderName = name;
                    cmd.SenderIP = ip;
                    this.BroadCastCommand(cmd);
                    return true;
                }
                return false;
            }
        }

        private int IndexOfClient(IPAddress ip)
        {
            int index = -1;
            foreach (ClientManager cMngr in this.clients)
            {
                index++;
                if (cMngr.IP.Equals(ip))
                    return index;
            }
            return -1;
        }

        private void CommandReceived(object sender, CommandEventArgs e)
        {
            if (e.Command.MetaData.Equals("next"))
            {
                SendKeys.SendWait("{RIGHT}");
            }
            else if (e.Command.MetaData.Equals("prev"))
            {
                SendKeys.SendWait("{LEFT}");
            }

            this.SendCommandToTarget(e.Command);
        }

        private void BroadCastCommand(Command cmd)
        {
            foreach (ClientManager mngr in this.clients)
                if (!mngr.IP.Equals(cmd.SenderIP))
                    mngr.SendCommand(cmd);
        }

        private void SendCommandToTarget(Command cmd)
        {
            Console.WriteLine(cmd.SenderName + ": " + cmd.MetaData);
        }
        private void UpdateConsole(string status, IPAddress IP, int port)
        {
            Console.WriteLine("Client {0}{1}{2} has been {3} ( {4}|{5} )", IP.ToString(), ":", port.ToString(), status, DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
        }
        public void DisconnectServer()
        {
            if (this.clients != null)
            {
                foreach (ClientManager mngr in this.clients)
                    mngr.Disconnect();

                this.bwListener.CancelAsync();
                this.bwListener.Dispose();
                this.listenerSocket.Close();
                GC.Collect();
            }
        }
    }
}
