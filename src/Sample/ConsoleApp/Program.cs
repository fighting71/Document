using System;
using System.Threading;
using ConsoleApp.Demo;

namespace ConsoleApp
{
    class Program
    {
        
        static void Main(string[] args)
        {

            new LockDemo().Run();
            
//            ReflectorDemo.Run();

            Console.WriteLine("Success");
            
            Console.ReadKey(true);

        }
    }
}
