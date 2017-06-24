using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sieciServ
{
    class Program
    {
        static void Main(string[] args)
        {                             //name port players round boardSize tick
             server serv = new server("Test", 9000  ,8    ,6,    100,    100);
            serv.run();

           
            //Thread.Sleep(1000000);
        }
    }
}
