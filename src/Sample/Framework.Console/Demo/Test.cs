﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.ConsoleDemo.Demo
{
    /// <summary>
    /// @auth : monster
    /// @since : 2019/12/31 10:47:08
    /// @source : 
    /// @des : 
    /// </summary>
    public class Test
    {

        Test() { }

        public delegate void a();
        public event a b;

        public static void x()
        {
            Console.WriteLine("test");
        }


    }
}
