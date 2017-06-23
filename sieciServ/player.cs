using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace sieciServ
{
    class Player
    {
        
        public TcpClient client { get; set; }
        public String login { get; set; }
        public int id { get; set; }
        public int Rot { get; set; }
        public bool isAlive { get; set; }
        public int posX { get; set; }
        public int posY { get; set; }
        public int points { get; set; }
        public bool isStillinGame { get; set; }
        public Player(TcpClient _client, String _login, int _id, int _rot, bool _isAlive,bool _isInGame,int _posX,int _posY,int _points) {
            client = _client;
            login = _login;
            id = _id;
            Rot = _rot;
            isStillinGame = _isInGame;
            isAlive = _isAlive;
            posX = _posX;
            posY = _posY;
            points = _points;
        }
        
        //public int MyProperty { get; set; }
    }
}
