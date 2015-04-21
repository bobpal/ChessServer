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
        private List<game> games = new List<game>();
        private TcpListener listener;
        private int playerID = 0;
        private int gameID = 0;

        private class player
        {
            internal TcpClient tcp { get; private set; }
            internal player opponent { get; set; }
            internal int pID { get; private set; }
            internal int gID { get; set; }
            internal string status { get; set; }
            internal bool firstPlayer { get; set; }

            public player(TcpClient c, int i, string s)
            {
                this.tcp = c;
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
            Thread clientThread;
            player waiting = null;
            player newPlayer;
            TcpClient client;
            NetworkStream wStream;
            NetworkStream nStream;
            byte[] start = new byte[7];

            while (true) //Server main loop
            {
                //wait for clients to connect
                client = listener.AcceptTcpClient();
                newPlayer = new player(client, playerID, "waiting");
                playerID++;
                clientThread = new Thread(new ParameterizedThreadStart(clientComm));
                clientThread.Start(newPlayer);
                
                if(waiting == null)
                {
                    waiting = newPlayer;
                }
                else
                {
                    waiting.firstPlayer = true;
                    newPlayer.firstPlayer = false;
                    waiting.gID = gameID;
                    newPlayer.gID = gameID;
                    waiting.status = "playing";
                    newPlayer.status = "playing";
                    waiting.opponent = newPlayer;
                    newPlayer.opponent = waiting;
                    games.Add(new game(waiting, newPlayer, gameID));
                    gameID++;
                    //Tell clients to start game
                    wStream = waiting.tcp.GetStream();
                    nStream = newPlayer.tcp.GetStream();
                    sendData(waiting.tcp, wStream, start);
                    sendData(newPlayer.tcp, nStream, start);
                    waiting = null;
                }
            }
        }

        private void clientComm(object p)
        {
            int bytesRead;
            string dataReceived;
            player threadPlayer = (player)p;
            TcpClient threadClient = threadPlayer.tcp;
            NetworkStream threadStream = threadClient.GetStream();
            byte[] buffer = new byte[threadClient.ReceiveBufferSize];

            while(true)
            {
                //wait for data to come in
                bytesRead = threadStream.Read(buffer, 0, threadClient.ReceiveBufferSize);

                if(bytesRead == 7)  //if client send signal, move, etc...
                {
                    //do something
                }

                else if(bytesRead == 0)
                {
                    break;
                }

                dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            threadStream.Close();
            threadClient.Close();
        }

        private void sendData(TcpClient to, NetworkStream stream , byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }
    }
}
