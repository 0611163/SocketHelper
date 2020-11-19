using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 字节数组工具类
    /// </summary>
    public class ByteUtil
    {
        #region 数组复制
        /// <summary>
        /// 数组复制
        /// </summary>
        public static void CopyTo(byte[] bArrSource, List<byte> listTarget, int sourceIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (sourceIndex + i < bArrSource.Length)
                {
                    listTarget.Add(bArrSource[sourceIndex + i]);
                }
            }
        }
        #endregion

        #region 数组复制
        /// <summary>
        /// 数组复制
        /// </summary>
        public static void CopyTo(List<byte> listSource, byte[] bArrTarget, int sourceIndex, int targetIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (targetIndex + i < bArrTarget.Length && sourceIndex + i < listSource.Count)
                {
                    bArrTarget[targetIndex + i] = listSource[sourceIndex + i];
                }
            }
        }
        #endregion

        #region 数组追加
        /// <summary>
        /// 数组追加
        /// </summary>
        public static void Append(ref List<byte> list, byte[] bArr)
        {
            for (int i = 0; i < bArr.Length; i++)
            {
                list.Add(bArr[i]);
            }
        }
        #endregion

    }
}
