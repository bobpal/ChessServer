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
        private int playerID = 0;
        private int gameID = 0;

        private class player
        {
            internal TcpClient tcp { get; private set; }
            internal NetworkStream stream { get; private set; }
            internal player opponent { get; set; }
            internal int pID { get; private set; }
            internal int gID { get; set; }
            internal bool firstPlayer { get; set; }

            public player(TcpClient c, NetworkStream n, int i, string s)
            {
                this.tcp = c;
                this.stream = n;
                this.pID = i;
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
            byte[] start = new byte[1];

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
            byte[] buffer = new byte[255];
            byte[] end = new byte[1] { 3 };
            byte[] lost = new byte[1] { 4 };

            while(true)
            {
                //wait for data to come in
                try
                {
                    bytesRead = threadStream.Read(buffer, 0, 255);
                    //if client clicks cancel
                    if (bytesRead == 0)
                    {
                        if (threadPlayer.opponent != null)
                        {
                            if (threadPlayer.opponent.tcp.Connected == true)
                            {
                                //tell opponent game ended
                                threadPlayer.opponent.stream.Write(end, 0, 1);
                            }
                        }
                        break;
                    }
                    //new game
                    else if (buffer[0] == 1)
                    {
                        //tell opponent game ended
                        threadPlayer.opponent.stream.Write(end, 0, 1);
                        break;
                    }
                    //game over
                    else if (buffer[0] == 2)
                    {
                        //tell opponent game ended
                        threadPlayer.opponent.stream.Write(lost, 0, 1);
                        break;
                    }
                    //move
                    else if (buffer[0] == 7)
                    {
                        //send move to opponent
                        threadPlayer.opponent.stream.Write(buffer, 0, 6);
                    }
                    //chat
                    else
                    {
                        threadPlayer.opponent.stream.Write(buffer, 0, bytesRead);
                    }
                }
                //Client closed
                catch(System.IO.IOException)
                {
                    //tell opponent game ended
                    if (threadPlayer.opponent != null)
                    {
                        threadPlayer.opponent.stream.Write(end, 0, 1);
                    }
                    break;
                }
            }
            threadStream.Close();
            threadClient.Close();
            if(waiting != null)
            {
                if (waiting.pID == threadPlayer.pID)
                {
                    waiting = null;
                }
            }
            //find game from gameID and see if can remove from list
            for(int i = 0; i < games.Count; i++)
            {
                if(games[i].id == threadPlayer.gID)
                {
                    if (threadPlayer.opponent != null)
                    {
                        if (games[i].player1.tcp.Connected == false && games[i].player2.tcp.Connected == false)
                        {
                            games.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
    }
}
