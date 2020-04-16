using System;
using System.Net.Http;
using System.Net.Sockets;
using BenchmarkDotNet.Running;
using Common.TestModule;
using ConsoleApp.Menu;

namespace ConsoleApp
{

    public class Fly<T>
    {

        static Fly()
        {// 泛型缓存
            Console.WriteLine($"Fly create by {typeof(T).Name}");
        }

    }

    class Program
    {
        
        class A { }

        class B { }

        static void Main(string[] args)
        {

            new Fly<Program.A>();
            new Fly<Program.A>();
            new Fly<Program.B>();
            new Fly<int>();
            new Fly<string>();

            //Socket socket = new Socket(SocketType.Stream,ProtocolType.Tcp);

            //BenchmarkRunner.Run<Test3>();

            Console.WriteLine("Success");
            
            Console.ReadKey(true);

        }

        public void LookIL()
        {
            object a = 2;

            //  IL_0008:  ldloc.0
          //  IL_0009: unbox.any ConsoleApp.Menu.LevelTypes
          //IL_000e:  stloc.1
          // 引用类型转值类型 : 拆箱 直接赋值
            LevelTypes level = (LevelTypes)a;

            //castclass  ConsoleApp.Program
            Program g = (Program)a;
        }

        public static void LookIL2()
        {

            string str = string.Empty;

            if(str+"" == "")
            {
                Console.WriteLine("is empty");
            }

        }

    }
}
