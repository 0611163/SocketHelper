using Castle.DynamicProxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 动态代理工厂
    /// </summary>
    public class ProxyFactory
    {
        /// <summary>
        /// 拦截器缓存
        /// </summary>
        private static ConcurrentDictionary<Type, IInterceptor> _interceptors = new ConcurrentDictionary<Type, IInterceptor>();

        /// <summary>
        /// 代理对象缓存
        /// </summary>
        private static ConcurrentDictionary<Type, object> _objs = new ConcurrentDictionary<Type, object>();

        private static ProxyGenerator _proxyGenerator = new ProxyGenerator();

        /// <summary>
        /// 动态创建代理
        /// </summary>
        /// <typeparam name="T">接口</typeparam>
        public static T CreateProxy<T>()
        {
            Type interfaceType = typeof(T);

            IInterceptor interceptor = _interceptors.GetOrAdd(interfaceType, type =>
            {
                return new ProxyInterceptor(interfaceType.Name.Substring(1));
            });

            return (T)_objs.GetOrAdd(interfaceType, type => _proxyGenerator.CreateInterfaceProxyWithoutTarget(typeof(T), interceptor)); //根据接口类型动态创建代理对象，接口没有实现类
        }
    }
}
