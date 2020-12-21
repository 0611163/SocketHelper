using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 契约
/// </summary>
namespace Contract
{
    public interface IMyTest
    {
        /// <summary>
        /// 计算arg1和arg2的和
        /// </summary>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        void Calc(int arg1, int arg2);

        List<TestModel> GetList(List<TestModel> oldList);
    }
}
