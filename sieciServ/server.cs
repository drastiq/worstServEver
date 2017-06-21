using System;
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
        public bool Running { get; private set; }
        // Clients objects
        private List<TcpClient> _clients = new List<TcpClient>();
        private List<TcpClient> _waitingLobby = new List<TcpClient>();
        private List<Player> _playerList = new List<Player>();
        private Random rand = new Random();
        public server(string name, int port, int _playerReq)
        {
            // Set some of the basic data
            Name = name;
            Port = port;
            Running = false;
            playerReq = _playerReq;
            // Create the listener
            _listener = new TcpListener(IPAddress.Any, Port);
        }
        public void Shutdown()
        {
            if (Running)
            {
                Running = false;
                Console.WriteLine("Shutting down the Game(s) Server...");
            }
        }
        public void run()
        {    int gameState = 0;
            Console.WriteLine("Starting the \"{0}\" Game(s) Server on port {1}.", Name, Port);
            _listener.Start();
            Running = true;
            List<Task> newConnectionTasks = new List<Task>();
            Console.WriteLine("Waiting for incommming connections...");
            int loggedplayers = 0;

            while (Running)
            {
                if (_listener.Pending())
                    newConnectionTasks.Add(_handleNewConnection());
                if (gameState ==0 )
                {
                    foreach (var client in _clients)
                    {
                        String p = ReceivePacket(client).GetAwaiter().GetResult();


                        //var item = _playerList.FirstOrDefault(x => x.client == client && x.login == "");
                        //linQ power 
                        bool logged = false;
                        var playerLoginCheck = _playerList.Where(tempPlayerCheck => tempPlayerCheck.client.Client.RemoteEndPoint == client.Client.RemoteEndPoint &&
                                    tempPlayerCheck.login != null);


                        if ((p != null && p.Contains("LOGIN")) && !playerLoginCheck.Any() && !logged)
                        {
                            _playerList.Add(new Player(client, p.Split(' ')[1], _clients.IndexOf(client) + 1,
                                            "X", true,rand.Next(0,100),rand.Next(0,100)));
                            var playerCl = _playerList.First(x => x.client == client);
                            Console.WriteLine("added: " + playerCl.login);
                            /*
                            Console.WriteLine(playerCl.id);
                            Console.WriteLine(playerCl.login);
                            Console.WriteLine(playerCl.posX);
                            Console.WriteLine(playerCl.posY);
                            Console.WriteLine(playerCl.Rot);*/
                            sendMsg(client, "OK").GetAwaiter();
                            logged = true;
                            if (_playerList.Count == playerReq) gameState = 1;
                        }
                        if((p != null) && playerLoginCheck.Any() && !logged )
                        {
                            sendMsg(client, "ERROR").GetAwaiter();
                        }
                    }
                }
                if(gameState==1)
                {
                    Console.WriteLine("Step1");
                    String playerpos = "";
                    foreach (var pl in _playerList)
                    {
                        sendMsg(pl.client, "START " + pl.id).GetAwaiter();
                        playerpos +=(pl.posX.ToString() + " "+pl.posY.ToString()+" ");
                    }
                    foreach (var pl in _playerList)
                    {
                        sendMsg(pl.client, playerpos).GetAwaiter();
                    }
                    gameState = 2;
                }
                if (gameState == 2)
                {
                   // Console.WriteLine("Step2");
                    

                   
                        foreach (var player in _playerList)
                        {
                        if (loggedplayers<_playerList.Count)
                        {
                            String p = ReceivePacket(player.client).GetAwaiter().GetResult();
                            var isPos = _playerList.First(x => x.client == player.client).Rot;
                            if (p != null && p.Contains("N") && p.Length == 2 && isPos.Contains("X"))
                            {
                                // Console.WriteLine(p + "Len" + p.Length);
                                _playerList.First(x => x.client == player.client).Rot = p;
                                sendMsg(player.client, "OK").GetAwaiter();
                                loggedplayers++;
                            }
                            if (p != null && p.Contains("S") && p.Length == 2 && isPos.Contains("X"))
                            {

                                _playerList.First(x => x.client == player.client).Rot = p;
                                sendMsg(player.client, "OK").GetAwaiter();
                                loggedplayers++;
                            }
                            if (p != null && p.Contains("W") && p.Length == 2 && isPos.Contains("X"))
                            {

                                _playerList.First(x => x.client == player.client).Rot = p;
                                sendMsg(player.client, "OK").GetAwaiter();
                                loggedplayers++;
                            }
                            if (p != null && p.Contains("E") && p.Length == 2 && isPos.Contains("X"))
                            {

                                _playerList.First(x => x.client == player.client).Rot = p;
                                sendMsg(player.client, "OK").GetAwaiter();
                                loggedplayers++;
                            } 
                        }
                        if (loggedplayers== _playerList.Count)
                        {
                            gameState = 3;
                        }
                       
                        }
                    
                }
                if (gameState == 3)
                {
                    Console.WriteLine("step3");
                    Thread.Sleep(1000);
                }
            }



        }
        //public void (int Board[][]){

        private async Task _handleNewConnection()
        {
            // Get the new client using a Future
            TcpClient newClient = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("New connection from {0}.", newClient.Client.RemoteEndPoint);

            // Store them and put them in the waiting lobby
            _clients.Add(newClient);
            _waitingLobby.Add(newClient);

            // Send a welcome message
            string msg = "CONNECT";
            await sendMsg(newClient,msg);

            ////wait for login
            // var t = await getMsg(newClient);

            //  Console.WriteLine("New msg from {0}.",t);


        }
        public async Task sendMsg(TcpClient client, string s)
        {
            string msg = string.Format($"{s}\n");
            NetworkStream ns = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(msg);
            await ns.WriteAsync(bytesToSend, 0, bytesToSend.Length);
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
          

                // Now read that many bytes from what's left in the stream, it must be the Packet
            
                // Convert it into a packet datatype
                string msg = Encoding.UTF8.GetString(lengthBuffer);
                packet = msg;

               // Console.WriteLine("[RECEIVED]\n{0}", packet);
            }
            catch (Exception e)
            {
                // There was an issue in receiving
                Console.WriteLine("There was an issue sending a packet to {0}.", client.Client.RemoteEndPoint);
                Console.WriteLine("Reason: {0}", e.Message);
            }

            return packet;
        }






    }
}
