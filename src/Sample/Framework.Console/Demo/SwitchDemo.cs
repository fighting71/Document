using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.ConsoleDemo.Demo
{
    public class SwitchDemo
    {

        Random rand = new Random();

        public void Run(int num)
        {

        }

        bool Check(int num)
        {
            return rand.Next(num) >= num/2;
        }

    }
}
