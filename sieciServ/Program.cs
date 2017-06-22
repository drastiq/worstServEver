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
        {
             server serv = new server("Test", 9000,2,2,500);
            serv.run();

           
            //Thread.Sleep(1000000);
        }
    }
}
