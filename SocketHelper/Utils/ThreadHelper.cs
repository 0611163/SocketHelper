using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 线程帮助类
    /// 封装Task.Factory.StartNew
    /// </summary>
    public class ThreadHelper
    {
        /// <summary>
        /// 执行 
        /// 例：ThreadHelper.Run(() => { }, (ex) => { });
        /// </summary>
        /// <param name="doWork">在线程中执行</param>
        /// <param name="errorAction">错误处理</param>
        public static Task Run(Action doWork, Action<Exception> errorAction = null)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                try
                {
                    if (doWork != null) doWork();
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "ThreadHelper.Run 错误");
                }
            });
            return task;
        }

        /// <summary>
        /// 执行 
        /// 例：ThreadHelper.Run((obj) => { }, arg, (ex) => { });
        /// </summary>
        /// <param name="doWork">在线程中执行</param>
        /// <param name="arg">参数</param>
        /// <param name="errorAction">错误处理</param>
        public static Task Run(Action<object> doWork, object arg = null, Action<Exception> errorAction = null)
        {
            Task task = Task.Factory.StartNew((obj) =>
            {
                try
                {
                    if (doWork != null) doWork(obj);
                }
                catch (Exception ex)
                {
                    if (errorAction != null) errorAction(ex);
                    LogUtil.Error(ex, "ThreadHelper.Run 错误");
                }
            }, arg);
            return task;
        }
    }
}
