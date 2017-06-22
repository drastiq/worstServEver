﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sieciServ
{
    class server
    {
        private TcpListener _listener;
       
        public readonly string Name;
        public readonly int Port;
        public readonly int playerReq;
        public readonly int Tick;
        public readonly int roundCount;
        public bool Running { get; private set; }
        //public String startMove = "NSWE";
        // Clients objects
        private List<TcpClient> _clients = new List<TcpClient>();
        private List<TcpClient> _waitingLobby = new List<TcpClient>();
        private List<Player> _playerList = new List<Player>();
        private Random rand = new Random();
        private int[,] board;
        private int roundLeft;
        public server(string name, int port, int _playerReq,int _round,  int _tick)
        {
            // Set some of the basic data
            Name = name;
            Port = port;
            Running = false;
            playerReq = _playerReq;
            roundCount = _round;
            Tick = _tick;

            // Create the listener
            _listener = new TcpListener(IPAddress.Any, Port);
        }
        
        
        public void Shutdown()
        {
            if (Running)
            {
                Running = false;
                Logger.WriteLine("Shutting down the Game(s) Server...");
            }
        }
        public void run()
        {
            int gameState = 0;
            Logger.WriteLine("Starting the "+Name+" Game(s) Server on port"+ Port +" .");
            _listener.Start();
            Running = true;
            bool gameStart = false;
            List<Task> newConnectionTasks = new List<Task>();
            Logger.WriteLine("Waiting for incommming connections...");
            int loggedplayers = 0;
            bool boardInit = false;
            roundLeft = roundCount;
            while (Running)
            {

                if (_listener.Pending())
                    newConnectionTasks.Add(_handleNewConnection());
                if (gameState == 0)
                {
                    foreach (var client in _clients)
                    {
                        String p = ReceivePacket(client).GetAwaiter().GetResult();


                        //var item = _playerList.FirstOrDefault(x => x.client == client && x.login == "");

                        bool logged = false;
                        var playerLoginCheck = _playerList.Where(tempPlayerCheck => tempPlayerCheck.client.Client.RemoteEndPoint == client.Client.RemoteEndPoint &&
                                    tempPlayerCheck.login != null);


                        if ((p != null && p.Contains("LOGIN")) && !playerLoginCheck.Any() && !logged)
                        {
                            _playerList.Add(new Player(client, p.Split(' ')[1], _clients.IndexOf(client) + 1,
                                            0, true, 0, 0,0));
                            var playerCl = _playerList.First(x => x.client == client);
                            Logger.WriteLine("added: " + playerCl.login);
                            /*
                            Logger.WriteLine(playerCl.id);
                            Logger.WriteLine(playerCl.login);
                            Logger.WriteLine(playerCl.posX);
                            Logger.WriteLine(playerCl.posY);
                            Logger.WriteLine(playerCl.Rot);*/
                            sendMsg(client, "OK").GetAwaiter();
                            logged = true;
                            if (_playerList.Count == playerReq) gameState = 1;
                        }
                        if ((p != null) && playerLoginCheck.Any() && !logged)
                        {
                            sendMsg(client, "ERROR").GetAwaiter();
                        }
                    }
                }
                if (gameState == 1 )
                {
                    if (roundLeft == 0)
                    {
                        var rank = _playerList.OrderBy(r=>r.points).ToList();
                        String endrank = "ENDGAME ";
                        foreach (var r in rank)
                        {
                            endrank += r.login.ToString()+" ";
                        }
                        foreach(var r in rank)
                        {
                            sendMsg(r.client,endrank).GetAwaiter();
                        }
                        Thread.Sleep(100000000);
                    }
                    Logger.WriteLine("Step1");
                    //Thread.Sleep(5000);
                    //rand.Next(0, 100)
                    String playerpos = "";
                    foreach (var pl in _playerList)
                    {
                        _playerList.First(x => x.client == pl.client).posX = rand.Next(0, 100);
                        _playerList.First(x => x.client == pl.client).posY = rand.Next(0, 100);
                        _playerList.First(x => x.client == pl.client).isAlive = true;
                    }
                    foreach (var pl in _playerList)
                    {
                        if (roundCount == roundLeft) sendMsg(pl.client, "START " + pl.id).GetAwaiter();
                        playerpos += (pl.posX.ToString() + " " + pl.posY.ToString() + " ");
                    }
                    foreach (var pl in _playerList)
                    {
                        sendMsg(pl.client, playerpos).GetAwaiter();
                    }
                    gameState = 2;
                }
                if (gameState == 2 && roundLeft!=0)
                {
                    // Logger.WriteLine("Step2");
                   // Logger.WriteLine("Step2");
                    //Thread.Sleep(5000);

                    foreach (var player in _playerList)
                    {
                        if (loggedplayers < _playerList.Count)
                        {
                            String p = ReceivePacket(player.client).GetAwaiter().GetResult();
                           // if (p != null) Logger.WriteLine(p + "Len" + p.Length);
                            var isPos = _playerList.First(x => x.client == player.client).Rot;
                            bool check = false;
                           // if (p != null) Logger.WriteLine(p.Length);
                            if (p != null && p.Contains("BEGIN N") && p.Length == 8 && isPos == 0 && !check)
                            {
                                // Logger.WriteLine(p + "Len" + p.Length);
                                _playerList.First(x => x.client == player.client).Rot = 1;
                                sendMsg(player.client, "OK").GetAwaiter();
                                check = true;
                                loggedplayers++;
                            }
                            if (p != null && p.Contains("BEGIN S") && p.Length == 8 && isPos == 0 && !check)
                            {

                                _playerList.First(x => x.client == player.client).Rot = 3;
                                sendMsg(player.client, "OK").GetAwaiter();
                                check = true;
                                loggedplayers++;
                            }
                            if (p != null && p.Contains("BEGIN W") && p.Length == 8 && isPos == 0 && !check)
                            {

                                _playerList.First(x => x.client == player.client).Rot = 4;
                                sendMsg(player.client, "OK").GetAwaiter();
                                check = true;
                                loggedplayers++;
                            }
                            if (p != null && p.Contains("BEGIN E") && p.Length == 8 && isPos == 0 && !check)
                            {

                                _playerList.First(x => x.client == player.client).Rot = 2;
                                sendMsg(player.client, "OK").GetAwaiter();
                                check = true;
                                loggedplayers++;
                            }
                            if (p != null && !p.Contains("BEGIN E") && !p.Contains("BEGIN W") && !p.Contains("BEGIN S") && !p.Contains("BEGIN N") && p.Length != 8)
                            {
                                sendMsg(player.client, "ERROR").GetAwaiter();
                            }
                        }
                        if (loggedplayers == _playerList.Count)
                        {
                            loggedplayers = 0;
                            gameState = 3;
                        }

                    }

                }
                if (gameState == 3)
                {
                    
                    //Logger.WriteLine("Step3");
                    if (!boardInit)
                    {
                        board = initBoard();
                        boardInit = true;
                    }
                    // Thread.Sleep(1000);
                    if (boardInit)
                    {
                        if (!gameStart)
                        {
                            foreach (var player in _playerList)
                            {
                                sendMsg(player.client, "GAME").GetAwaiter();
                            }
                            gameStart = true;
                        }
                        if (gameStart)
                        {

                            String boardFormated = "";
                            for (int i = 0; i < board.GetLength(0); i++)
                            {
                                for (int k = 0; k < board.GetLength(1); k++)
                                {
                                    boardFormated += (board[i, k]) + " ";
                                }

                                boardFormated += "\n";
                            }
                            foreach (var player in _playerList)
                            {
                                if (_playerList.First(x => x.client == player.client).isAlive)
                                {
                                    sendMsg(player.client, boardFormated).GetAwaiter();
                                    Logger.WriteLine("Board send to " + player.client.Client.RemoteEndPoint);

                                }
                            }
                            //TODO simulate move
                            foreach (var player in _playerList)
                            {

                                int leftPlayers = _playerList.Where(x => x.isAlive == true).Count();
                                // Logger.WriteLine("TICKmove: " + player.Rot);
                                if (_playerList.First(x => x.client == player.client).isAlive)
                                {
                                    String p = ReceivePacket(player.client).GetAwaiter().GetResult();
                                    if (p != null && (p.Contains("MOVE S") || p.Contains("MOVE R") || p.Contains("MOVE L")) && p.Length == 7)
                                    {

                                        _playerList.First(x => x.client == player.client).Rot = Move(player.Rot, p);
                                        sendMsg(player.client, "OK").GetAwaiter();


                                    }
                                    if (p != null && !(p.Contains("MOVE S") || p.Contains("MOVE R") || p.Contains("MOVE L")))
                                    {
                                        sendMsg(player.client, "ERROR").GetAwaiter();
                                    }
                                    if (player.Rot == 4)
                                    {
                                        if (player.posY - 1 > 0 && board[player.posX, player.posY - 1] == 0)
                                        {
                                            board[player.posX, player.posY - 1] = player.id;
                                            // Logger.WriteLine("inN");
                                            _playerList.First(x => x.client == player.client).posY = player.posY - 1;

                                        }
                                        else
                                        {
                                            _playerList.First(x => x.client == player.client).isAlive = false;
                                        }

                                    }
                                    if (player.Rot == 2)
                                    {
                                        if (player.posY + 1 < 100 && board[player.posX, player.posY + 1] == 0)
                                        {
                                            board[player.posX, player.posY + 1] = player.id;
                                            _playerList.First(x => x.client == player.client).posY = player.posY + 1;
                                        }
                                        else
                                        {
                                            _playerList.First(x => x.client == player.client).isAlive = false;
                                        }

                                    }
                                    if (player.Rot == 3)
                                    {
                                        if (player.posX + 1 < 100 && board[player.posX + 1, player.posY] == 0)
                                        {
                                            board[player.posX + 1, player.posY] = player.id;
                                            _playerList.First(x => x.client == player.client).posX = player.posX + 1;

                                        }
                                        else
                                        {
                                            _playerList.First(x => x.client == player.client).isAlive = false;
                                        }
                                    }
                                    if (player.Rot == 1)
                                    {
                                        if (player.posX - 1 > 0 && board[player.posX - 1, player.posY] == 0)
                                        {
                                            board[player.posX - 1, player.posY] = player.id;
                                            _playerList.First(x => x.client == player.client).posX = player.posX - 1;
                                        }
                                        else
                                        {
                                            _playerList.First(x => x.client == player.client).isAlive = false;
                                        }
                                    }
                                }
                                if (!player.isAlive)
                                {
                                    sendMsg(player.client, "LOST " + (leftPlayers )).GetAwaiter();
                                    
                                    _playerList.First(x => x.client == player.client).Rot = 0;
                                    _playerList.First(x => x.client == player.client).points += _playerList.Count - leftPlayers;
                                }
                                if (leftPlayers == 1)
                                {
                                    var t = _playerList.First(x => x.isAlive == true).login;
                                    sendMsg(_playerList.First(x => x.isAlive == true).client, "WIN").GetAwaiter();

                                    boardInit = false;
                                    gameStart = false;
                                    leftPlayers = 0;
                                    _playerList.First(x => x.client == player.client).Rot = 0;
                                    _playerList.First(x => x.client == player.client).points += _playerList.Count-leftPlayers;
                                    roundLeft--;
                                    gameState = 1;
                                    board = null;
                                    Logger.WriteLine("Round: "+roundLeft + " Winner: "+t );
                                    Thread.Sleep(1000);
                                }
                                //   Logger.WriteLine("TICKmove: " + player.id);

                            }

                            boardFormated = "";

                            Thread.Sleep(Tick);

                        }

                    }
                    
                }
            }



        }

        public int Move(int rota, String move)
        {
            int rot = rota;
            int maxRot = 4;
            int minRot = 1;
            if (move.Contains("R"))
            {
                rot++;
            }
            if (move.Contains("L"))
            {
                rot--;
            }
            if (move.Contains("S"))
            {
                return rot;
            }
            if (rot < minRot)
            {
                return rot = 4;
            }
            if (rot > maxRot)
            {
                return rot = 0;
            }
            return rot;
        }
        public int[,] initBoard()
        {
            int[,] board = new int[100, 100];
            for (int i = 0; i < 100; i++)
            {
                for (int y = 0; y < 100; y++)
                {
                    board[i, y] = 0;
                }
            }
            foreach (var player in _playerList)
            {
                board[player.posX, player.posY] = player.id;
            }
            return board;
        }
        private async Task _handleNewConnection()
        {
            // Get the new client using a Future
            TcpClient newClient = await _listener.AcceptTcpClientAsync();
            Logger.WriteLine("New connection from "+ newClient.Client.RemoteEndPoint);

            // Store them and put them in the waiting lobby
            _clients.Add(newClient);
            _waitingLobby.Add(newClient);

            // Send a welcome message
            string msg = "CONNECT";
            await sendMsg(newClient, msg);

            ////wait for login
            // var t = await getMsg(newClient);

            //  Logger.WriteLine("New msg from {0}.",t);


        }
        public async Task sendMsg(TcpClient client, string s)
        {
            string msg = string.Format($"{s}\n");
            NetworkStream ns = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(msg);
            await ns.WriteAsync(bytesToSend, 0, bytesToSend.Length);
            Logger.WriteLine("Send " +msg +"to "+client.Client.RemoteEndPoint);
            //  Logger.log("Sended: " + s + " to: " + client.Client.RemoteEndPoint);
        }


        public async Task<String> ReceivePacket(TcpClient client)
        {
            String packet = null;
            try
            {
                // First check there is data available
                if (client.Available == 0)
                    return null;

                NetworkStream msgStream = client.GetStream();

                int msgSize = client.Available;
                byte[] lengthBuffer = new byte[msgSize];
                await msgStream.ReadAsync(lengthBuffer, 0, msgSize);//block

                string msg = Encoding.UTF8.GetString(lengthBuffer);
                packet = msg;

               
            }
            catch (Exception e)
            {
               
                Logger.WriteLine("There was an issue sending a packet to."+ client.Client.RemoteEndPoint);
                Logger.WriteLine("Reason: "+ e.StackTrace);
            }

            return packet;
        }






    }
}
