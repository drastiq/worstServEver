using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int loggedplayers = 0;
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
            //start server with basic conf
            int gameState = 0;
            Logger.WriteLine("Starting the "+Name+" Game(s) Server on port"+ Port +" .");
            _listener.Start(10);
            
            bool gameStart = false;
            List<Task> newConnectionTasks = new List<Task>();
            Logger.WriteLine("Waiting for incommming connections...");
            //int loggedplayers = 0;
            bool boardInit = false;
            roundLeft = roundCount;
            Running = true;
            while (Running)
            {
                //main loop
                if (_listener.Pending())
                    newConnectionTasks.Add(_handleNewConnection());
                gameState = Stage0(gameState);
                gameState = Stage1(gameState);
                Stage2(ref gameState);
                Stage3(ref gameState, ref gameStart, ref boardInit);
               //Task.WaitAll();
                Thread.Sleep(Tick);
            }



        }

        private int Stage0(int gameState)
        {//get login
            if (gameState == 0)
            {
                foreach (var client in _clients)
                {
                    String p = ReceivePacket(client).GetAwaiter().GetResult();

                    

                    bool logged = false;
                    var playerLoginCheck = _playerList.Where(tempPlayerCheck => tempPlayerCheck.client.Client.RemoteEndPoint == client.Client.RemoteEndPoint &&
                                tempPlayerCheck.login != null);


                    if ((p != null && p.Contains("LOGIN")) && !playerLoginCheck.Any() && !logged)
                    {//add new player to list
                        _playerList.Add(new Player(client, p.Split(' ')[1], _clients.IndexOf(client) + 1,
                                        0, true,true, 0, 0, 0));
                        var playerCl = _playerList.FirstOrDefault(x => x.client == client);
                        Logger.WriteLine("added: " + playerCl.login);
                       
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

            return gameState;
        }

        private int Stage1(int gameState)
        {
            try
            {//send player pos
                if (gameState == 1)
                {
                    if (roundLeft <=0)
                    {
                        var rank = _playerList.OrderBy(r => r.points).ToList();
                        String endrank = "ENDGAME ";
                        foreach (var r in rank)
                        {
                            endrank += r.login.ToString() + " ";
                        }
                        foreach (var r in rank)
                        {
                            sendMsg(r.client, endrank).GetAwaiter();
                        }
                        foreach (var sL in rank)
                        {
                            Console.WriteLine("DEBUG PRINT:  "+endrank +" " +sL.points + " | "+sL.client.Client.RemoteEndPoint);
                        }//after game end pause~
                        Thread.Sleep(100000000);
                    }
                    Logger.WriteLine("Step1");
                 
                    String playerpos = "PLAYERS ";
                    foreach (var pl in _playerList)
                    {
                        pl.posX = rand.Next(0, 100);
                        pl.posY = rand.Next(0, 100);
                        pl.isAlive = true;
                        pl.isStillinGame = true;
                        pl.Rot = 0;
                    }
                    foreach (var pl in _playerList)
                    {
                        if (roundCount == roundLeft) { sendMsg(pl.client, "START " + pl.id).GetAwaiter(); }
                        playerpos += (pl.posX.ToString() + " " + pl.posY.ToString() + " ");
                    }
                    foreach (var pl in _playerList)
                    {
                        sendMsg(pl.client, playerpos).GetAwaiter();
                    }
                    gameState = 2;
                }
            }
            catch (Exception e)
            {

                Logger.WriteLine("EXCEPTION" + gameState + " " + e.Message);
                throw;
            }
           // Thread.Sleep(Tick);
            return gameState;
            
        }

        private void Stage2(ref int gameState)
        {
            //start move
            
            if (gameState == 2 && roundLeft > 0)
                {
                

                foreach (var player in _playerList)
                {
                    if (loggedplayers < _playerList.Count)
                    {

                        var p = ReceivePacket(player.client).GetAwaiter().GetResult();

                        var isPos = player.Rot;
                        bool check = false;
                        Console.WriteLine("in logged player " + loggedplayers);

                        if (p != null && p.Contains("BEGIN N") && isPos == 0)
                        {

                            player.Rot = 1;
                            sendMsg(player.client, "OK").GetAwaiter();
                            check = true;
                            loggedplayers++;
                        }
                        if (p != null && p.Contains("BEGIN S") && isPos == 0)
                        {

                            player.Rot = 3;
                            sendMsg(player.client, "OK").GetAwaiter();
                            check = true;
                            loggedplayers++;
                        }
                        if (p != null && p.Contains("BEGIN W") && isPos == 0)
                        {

                            player.Rot = 4;
                            sendMsg(player.client, "OK").GetAwaiter();
                            check = true;
                            loggedplayers++;
                        }
                        if (p != null && p.Contains("BEGIN E") && isPos == 0)
                        {

                            player.Rot = 2;
                            sendMsg(player.client, "OK").GetAwaiter();
                            check = true;
                            loggedplayers++;
                        }
                        if (p != null && (!p.Contains("BEGIN E") && !p.Contains("BEGIN W") && !p.Contains("BEGIN S") && !p.Contains("BEGIN N")))
                        {
                            sendMsg(player.client, "ERROR").GetAwaiter();
                        }
                        if (loggedplayers >= _playerList.Count)
                        {
                            Console.WriteLine("in logged player if " + loggedplayers);
                           // loggedplayers = 0;
                            gameState = 3;
                            break;
                        }




                    }
                }
                //gameState = 3;
                
            }
          //  Thread.Sleep(Tick);
            //gameState = 3;
        }

        private void Stage3(ref int gameState, ref bool gameStart, ref bool boardInit)
        {
                if (gameState == 3)
                {
                loggedplayers = 0;
                Console.WriteLine("in3");
                   //update board
                    if (!boardInit)
                    {
                        board = initBoard();
                        boardInit = true;
                    }
                
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
                        string boardFormated = UpdateBoard();//update board
                       // SendActivePlayers(); //zakomentowane poniewaz klienty innych nie przyjmowaly 

                        var leftPlayers = _playerList.Where(x => x.isAlive == true).Count();
                        
                        foreach (var player in _playerList)
                        {




                            if (player.isAlive && player.isStillinGame)
                            {
                                String p = ReceivePacket(player.client).GetAwaiter().GetResult();
                                if (p != null && (p.Contains("MOVE S") || p.Contains("MOVE R") || p.Contains("MOVE L")) && p.Length == 7)
                                {

                                    player.Rot = Move(player.Rot, p);
                                    sendMsg(player.client, "OK").GetAwaiter();


                                }
                                if (p != null && !(p.Contains("MOVE S") || p.Contains("MOVE R") || p.Contains("MOVE L") || p.Length != 7))
                                {
                                    sendMsg(player.client, "ERROR").GetAwaiter();
                                }
                                if (player.Rot == 4)//west
                                {
                                    if ((player.posY - 1) > 0 && board[player.posX, (player.posY - 1)] == 0)
                                    {
                                        board[player.posX, (player.posY - 1)] = player.id;

                                        player.posY = player.posY - 1;

                                    }
                                    else
                                    {
                                        player.isAlive = false;
                                    }

                                }
                                if (player.Rot == 2)//east
                                {
                                    if ((player.posY + 1) < 100 && board[player.posX, (player.posY + 1)] == 0)
                                    {
                                        board[player.posX, player.posY + 1] = player.id;
                                        player.posY = player.posY + 1;
                                    }
                                    else
                                    {
                                        player.isAlive = false;
                                    }

                                }
                                if (player.Rot == 3)//south
                                {
                                    if ((player.posX + 1) < 100 && board[(player.posX + 1), player.posY] == 0)
                                    {
                                        board[(player.posX + 1), player.posY] = player.id;
                                        player.posX = player.posX + 1;

                                    }
                                    else
                                    {
                                        player.isAlive = false;
                                    }
                                }
                                if (player.Rot == 1)//North
                                {
                                    if ((player.posX - 1) > 0 && board[(player.posX - 1), player.posY] == 0)
                                    {
                                        board[(player.posX - 1), player.posY] = player.id;
                                        player.posX = player.posX - 1;
                                    }
                                    else
                                    {
                                        player.isAlive = false;
                                    }
                                }
                                if (player.Rot == 0) { player.isAlive = false; }//North
                            }
                            if (!player.isAlive && player.isStillinGame && leftPlayers > 1)
                            {
                                sendMsg(player.client, "LOST " + (leftPlayers)).GetAwaiter();

                                player.Rot = 0;
                                player.points += _playerList.Count - leftPlayers;
                                player.isStillinGame = false;
                            }
                            if (leftPlayers == 1)
                            {

                                sendMsg(player.client, "WIN").GetAwaiter();



                                player.Rot = 0;
                                player.points += _playerList.Count;


                                Logger.WriteLine("Round: " + (roundLeft) + " Winner: " + player.login);
                                player.isAlive = false;
                                player.isStillinGame = false;

                                leftPlayers = 0;

                            }
                            if (leftPlayers == 0)
                            {
                                Console.WriteLine("ROUND END " + roundLeft);
                                boardInit = false;
                                gameStart = false;
                                roundLeft--;
                                board = initBoard();

                                gameState = 1;
                                break;
                                // Stage1(gameState);
                            }

                        }

                        boardFormated = "";



                    }

                }
               // Thread.Sleep(Tick);
            }

        }

        private void SendActivePlayers()
        {
            string players = "PLAYERS ";
            foreach (var item in _playerList)
            {
                if (item.isAlive == true)
                {
                    players += item.login + " ";
                }
            }
            foreach (var item in _playerList)
            {
                sendMsg(item.client, players).GetAwaiter();

            }
        }

        private string UpdateBoard()
        {
            String boardFormated = "BOARD ";
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
                if (player.isStillinGame)
                {
                    sendMsg(player.client, boardFormated).GetAwaiter();
                    Logger.WriteLine("Board send to " + player.client.Client.RemoteEndPoint);

                }
            }

            return boardFormated;
        }

        private int Move(int rota, String move)
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
                return maxRot;
            }
            if (rot > maxRot)
            {
                return minRot;
            }
            return rot;
        }
        private int[,] initBoard()
        {
            int[,] board = new int[100, 100];
            for (int i = 0; i < 100; i++)
            {
                for (int y = 0; y < 100; y++)
                {
                    board[i, y] = 0;
                }
            }
       
            return board;
        }
        private async Task _handleNewConnection()
        {
            // Get the new client using async
            TcpClient newClient = await _listener.AcceptTcpClientAsync();
            Logger.WriteLine("New connection from "+ newClient.Client.RemoteEndPoint);

            // Store them
            
            //_waitingLobby.Add(newClient);

            // Send a welcome message
            if (_clients.Count > playerReq)
            {
                await sendMsg(newClient, "ERROR");
            }
            else { 
            _clients.Add(newClient);
            string msg = "CONNECT";
            await sendMsg(newClient, msg);
            }
            ////wait for login
            // var t = await getMsg(newClient);

            //  Logger.WriteLine("New msg from {0}.",t);


        }
        public async Task sendMsg(TcpClient client, string s)
        {
            string msg = string.Format($"{s}\n");
            msg.Trim();//remove space end+begin
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
                // FirstOrDefault check there is data available
                if (client.Available == 0)
                    return null;

                NetworkStream msgStream = client.GetStream();

                int msgSize = client.Available;
                byte[] lengthBuffer = new byte[msgSize];
                await msgStream.ReadAsync(lengthBuffer, 0, msgSize);//block

                string msg = Encoding.UTF8.GetString(lengthBuffer);
                packet = msg;
                Logger.WriteLine("received: "+ packet+ " From: " + client.Client.RemoteEndPoint);
               
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
