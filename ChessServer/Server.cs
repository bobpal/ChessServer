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
        private List<player> waitingPlayers = new List<player>();
        private List<game> games = new List<game>();
        private TcpListener listener;
        private int playerID = 0;
        private int gameID = 0;

        private class player
        {
            internal TcpClient me { get; private set; }
            internal TcpClient opponent { get; set; }
            internal int pID { get; private set; }
            internal int gID { get; set; }
            internal string status { get; set; }
            internal bool firstPlayer { get; set; }

            public player(TcpClient c, int i, string s)
            {
                this.me = c;
                this.pID = i;
                this.status = s;
            }
        }

        private class game
        {
            internal player player1 { get; private set; }
            internal player player2 { get; private set; }
            internal int id { get; private set; }

            public game(player one, player two, int i)
            {
                this.player1 = one;
                this.player2 = two;
                this.id = i;
            }
        }

        static void Main(string[] args)
        {
            Server Alfred = new Server();
        }

        public Server()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            listener = new TcpListener(address, 54321);
            listener.Start();
            matchMaking();
        }

        private void matchMaking()
        {
            while (true) //Server main loop
            {
                //wait for clients to connect
                TcpClient client = listener.AcceptTcpClient();
                player newPlayer = new player(client, playerID, "waiting");
                waitingPlayers.Add(newPlayer);
                playerID++;
                Thread clientThread = new Thread(new ParameterizedThreadStart(clientComm));
                clientThread.Start(newPlayer);
                
                if(waitingPlayers.Count > 1)
                {
                    waitingPlayers[0].firstPlayer = true;
                    waitingPlayers[1].firstPlayer = false;
                    waitingPlayers[0].gID = gameID;
                    waitingPlayers[1].gID = gameID;
                    waitingPlayers[0].status = "playing";
                    waitingPlayers[1].status = "playing";
                    waitingPlayers[0].opponent = waitingPlayers[1].me;
                    waitingPlayers[1].opponent = waitingPlayers[0].me;
                    games.Add(new game(waitingPlayers[0], waitingPlayers[1], gameID));
                    gameID++;
                    //tell clients to start game
                    //either create a stream or interrupt thread
                    waitingPlayers.RemoveRange(0, 2);
                }
            }
        }

        private void clientComm(object p)
        {
            int bytesRead;
            string dataReceived;
            player threadPlayer = (player)p;
            TcpClient threadClient = threadPlayer.me;
            NetworkStream threadStream = threadClient.GetStream();
            byte[] buffer = new byte[threadClient.ReceiveBufferSize];

            while(true)
            {
                //wait for data to come in
                bytesRead = threadStream.Read(buffer, 0, threadClient.ReceiveBufferSize);

                if(bytesRead == 0)
                {
                    break;
                }
                dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            threadStream.Close();
            threadClient.Close();
            //games.Remove();
        }

        private void sendData(TcpClient to, NetworkStream stream /*, var data*/)
        {

        }
    }
}
