using BenchmarkDotNet.Attributes;

namespace Common.TestModule
{
    public class Test3
    {
        [Benchmark]
        public void BaseTest()
        {
            B a = new B();

            for (int i = 0; i < int.MaxValue; i++)
            {
                a.Do();
            }

        }

        [Benchmark]
        public void TestInterface()
        {
            A a = new B();

            for (int i = 0; i < int.MaxValue; i++)
            {
                a.Do();
            }

        }

        [Benchmark]
        public void TestAbstract()
        {
            A1 a = new B1();

            for (int i = 0; i < int.MaxValue; i++)
            {
                a.Do();
            }

        }

        [Benchmark]
        public void TestInherit()
        {
            A2 a = new B2();

            for (int i = 0; i < int.MaxValue; i++)
            {
                a.Do();
            }

        }

        interface A
        {
            void Do();
        }

        class B : A
        {
            public void Do()
            {
                // do nothing
            }
        }

        abstract class A1
        {
            public abstract void Do();
        }

        class B1 : A1
        {
            public override void Do()
            {
                // do nothing
            }
        }

        abstract class A2
        {
            public virtual void Do() { }
        }

        class B2 : A2
        {
            public override void Do()
            {
                // do nothing
            }
        }

    }
}
