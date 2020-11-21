using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 记录耗时
    /// 封装Stopwatch
    /// </summary>
    public class LogTimeUtil
    {
        private Stopwatch _stopwatch;

        /// <summary>
        /// 记录耗时
        /// </summary>
        public LogTimeUtil()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        /// <summary>
        /// 记录耗时，并停止计时
        /// </summary>
        public void LogTime(string msg, bool restart = false)
        {
            LogUtil.Log(msg + "，耗时：" + _stopwatch.Elapsed.TotalSeconds.ToString("0.000") + " 秒");
            _stopwatch.Stop();
            if (restart) _stopwatch.Restart();
        }

        /// <summary>
        /// 记录耗时，并停止计时
        /// </summary>
        public void DebugTime(string msg, bool restart = false)
        {
            LogUtil.Debug(msg + "，耗时：" + _stopwatch.Elapsed.TotalSeconds.ToString("0.000") + " 秒");
            _stopwatch.Stop();
            if (restart) _stopwatch.Restart();
        }
    }
}
