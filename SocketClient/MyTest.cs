using Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClient
{
    public class MyTest : IMyTest
    {
        public string RunMyTest(string arg1, int arg2)
        {
            return "";
        }

        public string RunMyTest2(string arg1, int arg2, out string arg3)
        {
            arg3 = "";
            return "";
        }
    }
}
