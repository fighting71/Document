using Framework.ConsoleDemo.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Framework.ConsoleDemo
{

    [Customer]
    class Program
    {

        [return:Customer]
        static void Main(string[] args)
        {

            try
            {
                Console.WriteLine("try1");

                try
                {
                    Console.WriteLine("try2");
                }
                finally
                {
                    Console.WriteLine("finally2");
                }

            }
            finally
            {
                Console.WriteLine("finally1");
            }

            Action action = () => { Console.WriteLine("---1"); };
            Action action2 = () => { Console.WriteLine("---2"); };

            dynamic d = 123;
            int num = 1;

            Console.WriteLine(d + num);//124

            Console.ReadKey(true);

        }

        #region 逆变与协变

        static void Test4() {

            IEnumerable<Father> enumer = new List<Son>();// .net 本身支持的协变

            IDemo<Father, Son> obj = new Demo<Father, Son>();//正常使用
            IDemo<Father, Son> obj2 = new Demo<Son, Son>();//协变
            IDemo<Father, Son> obj3 = new Demo<Father, Father>();//逆变
            IDemo<Father, Son> obj4 = new Demo<Son, Father>();//协变+逆变

            // 假定定义了 麻雀和鸟的类且麻雀属于鸟

            // 协变： 给你一只麻雀相当于给你一只鸟 仅作用于返回值类型
            // 逆变:  我有了一只麻雀，我可以把它当做鸟来使用 仅作用于参数类型
        }

        class Father { }

        class Son : Father { }

        public interface IDemo<out T,in T2>
        {
            public T Show(T2 t2);
        }

        public class Demo<T, T2> : IDemo<T, T2>
        {
            public T Show(T2 t2)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region 泛型缓存

        static void Test3()
        {

            GenericCache<string> cache = new GenericCache<string>();

            GenericCache<Program> cache2 = new GenericCache<Program>();

        }

        // 泛型缓存
        // 每个类型T会创建一份不同的副本
        class GenericCache<T> {


            static GenericCache()
            {
                _date = DateTime.Now.ToString() + typeof(T).FullName;
            }

            private static string _date;

            public static string GetDate()
            {
                return _date;
            }

        }

        #endregion

        #region 内存泄露

        static void Test()
        {
            var arr = new int[1024 * 1024 * 3];

            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("again");
                Console.ReadLine();

                // 会导致GC不回收内存
                arr = new int[1024 * 1024 * 3];

                // 会触发回收
                Array.Clear(arr, 0, arr.Length);


            }
        }
        #endregion

        #region test override&new

        /**
         * 
         * override 是直接覆盖父类方法指针
         * 
         * new 是创建一个新的方法指针 与 父类方法指针无关
         * 
         */

        static void Test2()
        {
            /*
            1
            a:0,b:0
            2
            3
             */
            A a = new B();

            Console.WriteLine("------------");

            // print test 抗变
            C c = new D();

            // print a:0,b:0
            (c as D).Show();
        }

        class A
        {

            public A()
            {
                Console.WriteLine("1");
                Show();
                Console.WriteLine("2");
            }

            // .method public hidebysig newslot virtual 
            // instance void Show() cil managed
            public virtual void Show()
            {
                Console.WriteLine("test");
            }
        }

        class B : A
        {

            private int a, b;

            public B()
            {
                Console.WriteLine("3");
                b = -1;// 由于构造的顺序执行，在A构造中调用B的Show方法显示的是0
            }

            public override void Show()
            {
                Console.WriteLine($"a:{a},b:{b}");
            }
        }

        class C
        {

            public C()
            {
                Show();
            }

            // .method public hidebysig 
            // instance void Show() cil managed
            public void Show()
            {
                Console.WriteLine("test");
            }
        }

        class D : C
        {

            private int a, b;

            public new void Show()
            {
                Console.WriteLine($"a:{a},b:{b}");
            }
        }

        #endregion
    }
}
