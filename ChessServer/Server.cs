using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ChessServer
{
    class Server
    {
        public int PORT = 54321;
        public string SERVER_IP = "127.0.0.1";
        private List<clientObject> connectedGames = new List<clientObject>();

        private class clientObject
        {
            private TcpClient objClient { get; private set; }
            private string status { get; private set; }

            public clientObject(TcpClient c, string s)
            {
                this.objClient = c;
                this.status = s;
            }
        }

        static void Main(string[] args)
        {
            Server matchMaker = new Server();
        }

        public Server()
        {
            IPAddress address = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(address, PORT);
            listener.Start();

            while(true) //Server main loop
            {
                //wait for clients to connect
                TcpClient conClient = listener.AcceptTcpClient();
                clientObject game = new clientObject(conClient, "waiting");
                connectedGames.Add(game);
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(conClient);

                //Check if two games are "waiting"
            }
        }

        private void HandleClientComm(object c)
        {
            TcpClient threadClient = (TcpClient)c;
            NetworkStream nwStream = threadClient.GetStream();
            byte[] buffer = new byte[threadClient.ReceiveBufferSize];

            int bytesRead = nwStream.Read(buffer, 0, threadClient.ReceiveBufferSize);

            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            nwStream.Write(buffer, 0, bytesRead);
            threadClient.Close();
            Console.ReadLine();
        }
    }
}
