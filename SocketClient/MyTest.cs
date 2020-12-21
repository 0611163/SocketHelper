using Contract;
using Model;
using Newtonsoft.Json;
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

        public List<TestModel> GetList(List<TestModel> oldList)
        {
            LogUtil.Debug("处理数据：" + JsonConvert.SerializeObject(oldList));

            TestModel model = new TestModel();
            model.Name = "新记录";
            model.Count = 9;
            model.Total = (decimal)12.3;
            oldList.Add(model);
            return oldList;
        }
    }
}
