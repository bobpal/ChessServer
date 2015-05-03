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
        private Thread clientThread;
        private TcpClient client;
        private NetworkStream nwStream;
        private player waiting = null;
        private byte[] start = new byte[1];
        private byte[] end = new byte[1] {3};
        private int playerID = 0;
        private int gameID = 0;

        private class player
        {
            internal TcpClient tcp { get; private set; }
            internal NetworkStream stream { get; private set; }
            internal player opponent { get; set; }
            internal int pID { get; private set; }
            internal int gID { get; set; }
            internal string status { get; set; }
            internal bool firstPlayer { get; set; }

            public player(TcpClient c, NetworkStream n, int i, string s)
            {
                this.tcp = c;
                this.stream = n;
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
                client = listener.AcceptTcpClient();
                nwStream = client.GetStream();
                player newPlayer = new player(client, nwStream, playerID, "waiting");
                playerID++;
                clientThread = new Thread(new ParameterizedThreadStart(clientComm));
                clientThread.Start(newPlayer);
                addPlayer(newPlayer);
            }
        }

        private void addPlayer(player nPlayer)
        {
            if (waiting == null)
            {
                waiting = nPlayer;
            }
            else
            {
                waiting.firstPlayer = true;
                nPlayer.firstPlayer = false;
                waiting.gID = gameID;
                nPlayer.gID = gameID;
                waiting.status = "playing";
                nPlayer.status = "playing";
                waiting.opponent = nPlayer;
                nPlayer.opponent = waiting;
                games.Add(new game(waiting, nPlayer, gameID));
                gameID++;
                //Tell clients to start game
                start[0] = 1;
                waiting.stream.Write(start, 0, 1);
                start[0] = 2;
                nPlayer.stream.Write(start, 0, 1);
                waiting = null;
            }
        }

        private void clientComm(object p)
        {
            int bytesRead;
            player threadPlayer = (player)p;
            TcpClient threadClient = threadPlayer.tcp;
            NetworkStream threadStream = threadPlayer.stream;
            byte[] buffer = new byte[1024];

            while(true)
            {
                //wait for data to come in
                bytesRead = threadStream.Read(buffer, 0, 1024);

                if(bytesRead == 0)
                {
                    break;
                }
                //if status update
                else if(buffer.Length == 1)
                {
                    //game over
                    if(buffer[0] == 1)
                    {
                        threadPlayer.status = "gameEnded";
                    }
                    //new game
                    else if(buffer[0] == 2)
                    {
                        //tell opponent game ended
                        threadPlayer.opponent.stream.Write(end, 0, 1);
                        threadPlayer.opponent.status = "gameEnded";
                        //move player to waiting
                        threadPlayer.status = "waiting";
                        addPlayer(threadPlayer);
                    }
                }
                //move
                else
                {
                    //send move to opponent
                    threadPlayer.opponent.stream.Write(buffer, 0, 1024);
                }
            }
            threadStream.Close();
            threadClient.Close();
        }
    }
}
