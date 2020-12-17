using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 动态代理类工厂
    /// </summary>
    public class ProxyFactory
    {
        /// <summary>
        /// 创建动态代理类
        /// </summary>
        public static T CreateProxy<T>(bool isSave = false)
        {
            string strAssemblyName = typeof(T).Name.Substring(1) + "Proxy";
            Type interfaceType = typeof(T);

            //动态创建程序集
            AssemblyName assemblyName = new AssemblyName(strAssemblyName);
            AssemblyBuilderAccess assemblyBuilderAccess = isSave ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run;
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, assemblyBuilderAccess);

            //动态创建模块
            ModuleBuilder moduleBuilder = isSave ? assemblyBuilder.DefineDynamicModule(strAssemblyName, strAssemblyName + ".dll") : assemblyBuilder.DefineDynamicModule(strAssemblyName);

            //代理类名
            string proxyClassName = typeof(T).Name.Substring(1);

            //动态创建类代理
            TypeBuilder typeBuilder = moduleBuilder.DefineType(proxyClassName, TypeAttributes.Public);

            //添加继承接口
            typeBuilder.AddInterfaceImplementation(interfaceType);

            //动态创建方法
            MethodInfo[] methodInfoArr = interfaceType.GetMethods();
            foreach (MethodInfo methodInfo in methodInfoArr)
            {
                ParameterInfo[] paramInfoArr = methodInfo.GetParameters();
                Type[] paramTypeArr = new Type[paramInfoArr.Length];
                for (int i = 0; i < paramInfoArr.Length; i++)
                {
                    paramTypeArr[i] = paramInfoArr[i].ParameterType;
                }

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, paramTypeArr);
                ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ret);
            }

            //使用动态类创建类型
            Type proxyClassType = typeBuilder.CreateType();

            //保存动态创建的程序集
            if (isSave) assemblyBuilder.Save(strAssemblyName + ".dll");

            //创建类实例
            object instance = Activator.CreateInstance(proxyClassType);
            return (T)instance;
        }
    }
}
