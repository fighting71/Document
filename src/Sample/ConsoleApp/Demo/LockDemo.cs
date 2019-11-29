using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConsoleApp.Demo
{
    /// <summary>
    /// @auth : monster
    /// @since : 2019/11/28 11:35:48
    /// @source : 
    /// @des : 
    /// </summary>
    public class LockDemo
    {
        private object _lock = new object();
        private object _lock2 = new object();

        public void Run()
        {
            Thread thread = new Thread(MethodA);
            Thread thread2 = new Thread(MethodB);
            thread.Start();
            thread2.Start();
        }

        private void MethodA()
        {
            lock (_lock)
            {
                Thread.Sleep(500);
                lock (_lock2)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var info = _lock2.ToString();
                        Console.WriteLine(i + "，线程-" + Thread.CurrentThread.ManagedThreadId);
                    }
                }
            }
        }

        private void MethodB()
        {
            lock (_lock2)
            {
                Thread.Sleep(500);
                lock (_lock)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var info = _lock.ToString();
                        Console.WriteLine(i + "，线程-" + Thread.CurrentThread.ManagedThreadId);
                    }
                }
            }
        }
    }
}