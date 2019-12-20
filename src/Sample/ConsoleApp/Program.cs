using System;
using System.Threading;
using ConsoleApp.Demo;
using ConsoleApp.Menu;

namespace ConsoleApp
{
    class Program
    {
        
        static void Main(string[] args)
        {

            //            ReflectorDemo.Run();

            double a = 3;

            double b = a / a++;

            Console.WriteLine(b);

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

    }
}
