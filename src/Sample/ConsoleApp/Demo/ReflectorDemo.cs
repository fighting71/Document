using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ConsoleApp.Demo
{
    /// <summary>
    /// @auth : monster
    /// @since : 2019/11/27 10:25:30
    /// @source : 
    /// @des : 反射示例
    /// </summary>
    public class ReflectorDemo
    {
        public static void Run()
        {
            //var type = typeof(int);
            //int a = 3;
            //var type = a.GetType();

            //Data data = new Data();
            //var type = data.GetType();
            //            var type = typeof(Data);

            //var assembly = Assembly.GetCallingAssembly();
            //var type = assembly.GetType(nameof(Data));

            //var type = typeof(Data);

            //var constructorInfo = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);

            //var obj = constructorInfo.Invoke(new object[0]);

            //var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            //var properties2 = type.GetProperties(BindingFlags.Public | BindingFlags.Static);

            //var fields = type.GetFields();

            //var method = type.GetMethod("DoSomething");

            //var parameterInfos = method.GetParameters();

            //var t = parameterInfos[0].ParameterType;

            //var method2 = type.GetMethod("DoSomething", new Type[] { parameterInfos[0].ParameterType });

            //var generic = method.MakeGenericMethod(typeof(int));

            //var method3 = type.GetMethod("DoOther", new Type[] { parameterInfos[0].ParameterType });

            //var methodInfo = type.GetMethod("DoSomething", new Type[] { typeof(int).MakeByRefType()});

            //var methodInfo2 = type.GetMethod("DoSomething", new Type[] { typeof(int).MakePointerType()});

            //var methodInfo3 = type.GetMethod("DoSomething", new Type[] { typeof(int).MakeArrayType()});

            Console.WriteLine("over");

            goto Over;

            Over:
                return;

        }

        static MethodInfo GetGenericMethod(Type type, string name, params Type[] types)
        {
            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.Instance))
            {
                if (mi.Name != name) continue;
                if (!mi.IsGenericMethod) continue;
                if (mi.GetGenericArguments().Length != types.Length) continue;

                return mi.MakeGenericMethod(types);
            }

            throw new MissingMethodException();
        }

    }

    public class Data
    {
        public void DoSomething()
        {
        }

        public void DoSomething<T>()
        {
        }

        public void DoSomething(out int num)
        {
            num = 3;
        }

//        public void DoSomething(int* num)
//        {
//            
//        }

        public void DoSomething(int[] num)
        {
        }

        public void DoOther<T>(T info)
        {
        }

        //public void DoSomething(ref int info)
        //{

        //}

        public void DoSomething<T>(T info)
        {
        }

        public static int Flag { get; set; }

        public string Name { get; set; }

        public int _version;

        private Data()
        {
            Console.WriteLine("构建了一个Data");
        }
    }
}