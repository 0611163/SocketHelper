using Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer
{
    public class MyTest2 : IMyTest2
    {
        public string RunMyTest3(string arg1, int arg2, out string arg3)
        {
            arg3 = "";
            return "";
        }
    }
}
