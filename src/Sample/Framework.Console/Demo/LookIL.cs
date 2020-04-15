using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.ConsoleDemo.Demo
{
    class LookIL
    {

        public void Run()
        {
            // maxstack 预估使用的栈空间
            //int i = 1, j = 2;

            ////var k = i + j;

            //Console.WriteLine("-------");

            //Run3(1, 2, 3, 4);
            Run4(new[] { 1, 2, 3, 4 });

        }

        public void Run2()
        {

            int[] arr = { 1};

        }

        public void Run3(int a,int b,int c,int d)
        {

        }

        public void Run4(int[] arr)
        {

        }

    }
}
