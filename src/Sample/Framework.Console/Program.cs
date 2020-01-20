using Common.Tools;
using Framework.ConsoleDemo.Menu;
using Framework.ConsoleDemo.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Framework.ConsoleDemo
{

    [Customer]
    class Program
    {


        [return:Customer]
        static void Main(string[] args)
        {



            //Console.WriteLine("first");

            //SqlTools tools = new SqlTools();

            //BenchmarkRunner.Run<Test3>();

            Console.WriteLine("Success");

            Console.ReadKey(true);

        }

        #region C# 7.0+

        // 安全指针
        public static ref int Find(int[] arr)
        {
            return ref arr[3];
        }

        static void Test7()
        {
            var arr = new int[4];

            Find(arr) = 2;// 操作影响，即默认返回指针

            var num = Find(arr);

            num = 10;// 操作不影响，即上一行仅接收值 不接收指针

            ref var num2 = ref Find(arr);

            num2 = 10;// 操作影响原数组.

            var num1 = num2 + 10;

            Console.WriteLine(num1);

        }

        #endregion


        // 委托编译时会生成对应的类
        public delegate void ShowSomething<T>(T t);

        // 事件是委托的封装体 是一个特殊(封装了委托相关的add和remove)的结构
        //public event ShowSomething<int> evt;

        static void Test6()
        {

            #region C# 语法优化
            const int a = 123;
            int b = 0;

            for (int i = 0; i < 10; i++)
            {
                b = 456 * a;
            }
            #endregion

            #region 回调函数/委托
            // lambda表达式实际上就是匿名函数的语法糖，在编译后，会自动创建相应的方法
            // 委托实例在仅指定一个方法时，实际上就是指向回调方法的引用指针
            ShowSomething<int> something = (u) => {
                Console.WriteLine("show something");
            };

            something.Invoke(1);

            // lambda ==> 匿名函数
            Action action = () => { Console.WriteLine("---1"); };
            Action action2 = () => { Console.WriteLine("---2"); };
            #endregion

            #region 可变类型
            dynamic d = 123;
            int num = 1;

            Console.WriteLine(d + num);//124
            #endregion

        }

        #region C#8.0

        static void Test5(string name)
        {

            if(name is null)
            {

            }

            #region swtich语法糖

            // switch case情况较少会改用if判断.

            int flag = 1;
            bool flag2 = true;

            var flagRes = (flag, flag2) switch
            {
                (1, true) => 1,
                (2, _) => 4,
                (_, _) => 2
            };

            #endregion

        }

        #endregion

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
            T Show(T2 t2);
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
