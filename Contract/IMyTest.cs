using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    /// <summary>
    /// 动态代理测试接口
    /// </summary>
    public interface IMyTest
    {
        /// <summary>
        /// 测试方法
        /// </summary>
        /// <param name="arg1">测试参数1</param>
        /// <param name="arg2">测试参数2</param>
        string RunMyTest(string arg1, int arg2);

        /// <summary>
        /// 测试方法2
        /// </summary>
        /// <param name="arg1">测试参数1</param>
        /// <param name="arg2">测试参数2</param>
        /// <param name="arg3">测试参数3 out参数</param>
        string RunMyTest2(string arg1, int arg2, out string arg3);
    }
}
