using Contract;
using SocketUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClient
{
    public class MyTest : IMyTest
    {
        public void Calc(int arg1, int arg2)
        {
            int result = arg1 + arg2;
            LogUtil.Debug(string.Format("{0} + {1} = {2}", arg1, arg2, result));
        }
    }
}
