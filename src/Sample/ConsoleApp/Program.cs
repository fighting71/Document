using System;
using System.Net.Http;
using BenchmarkDotNet.Running;
using Common.TestModule;
using ConsoleApp.Menu;

namespace ConsoleApp
{
    class Program
    {
        
        static void Main(string[] args)
        {

            BenchmarkRunner.Run<Test3>();

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
