
### 方法调用性能测试 ###

 **前言**

	BaseTest - 实现类调用

	TestInterface - 接口调用

	TestAbstract - 抽象调用

	TestInherit - 继承调用

	方法内容 ： 空


 **FRAMEWORK中benchmark测试**:

|        Method |       Mean |    Error |    StdDev |
|-------------- |-----------:|---------:|----------:|
|      BaseTest |   621.5 ms | 12.10 ms |  20.54 ms |
| TestInterface | 3,660.4 ms | 24.38 ms |  20.36 ms |
|  TestAbstract | 3,026.5 ms | 13.09 ms |  11.60 ms |
|   TestInherit | 3,153.1 ms | 61.70 ms | 111.25 ms |

可以看出由接口/抽象/继承 调子类方法的效率比直接调子类方法慢5+倍

 **.NET CORE下benchmark测试**:

|        Method |     Mean |    Error |   StdDev |
|-------------- |---------:|---------:|---------:|
|      BaseTest | 608.4 ms |  8.61 ms |  7.64 ms |
| TestInterface | 609.1 ms |  6.66 ms |  5.90 ms |
|  TestAbstract | 622.3 ms | 14.52 ms | 12.87 ms |
|   TestInherit | 619.2 ms | 14.60 ms | 16.23 ms |

.net core 下无明显差别.

### FileStream ###

 **write test code**

		static void TestWriteFile()
        {
            //把线程池的最大值设置为1000
            //ThreadPool.SetMaxThreads(1000, 1000);
            ThreadPoolMessage("Start");

            //新立文件File.sour
            FileStream stream = new FileStream("File.sour", FileMode.OpenOrCreate,
                                       FileAccess.ReadWrite, FileShare.ReadWrite, 1024, true);
            byte[] bytes = new byte[16384];
            string message = "An operating-system ThreadId has no fixed relationship........";
            bytes = Encoding.Unicode.GetBytes(message);

            //启动异步写入
            stream.BeginWrite(bytes, 0, (int)bytes.Length, new AsyncCallback(Callback), stream);
            //stream.Flush(); 

            Console.ReadKey();
        }

        static void Callback(IAsyncResult result)
        {
            //显示线程池现状
            Thread.Sleep(200);
            ThreadPoolMessage("AsyncCallback");
            //结束异步写入
            FileStream stream = (FileStream)result.AsyncState;
            stream.EndWrite(result);
            stream.Flush();
            stream.Close();
        }

        //显示线程池现状
        static void ThreadPoolMessage(string data)
        {
            int a, b;
            ThreadPool.GetAvailableThreads(out a, out b);
            string message = string.Format("{0}\n  CurrentThreadId is {1}\n  " +
                  "WorkerThreads is:{2}  CompletionPortThreads is :{3}",
                  data, Thread.CurrentThread.ManagedThreadId, a.ToString(), b.ToString());
            Console.WriteLine(message);
        }

 **read test code**

            var path = @"F:\Workspace\源代码\1.源代码\浪花微服务\LH.MicroServicesSolution\WebApp\OrdersApi\bin\Debug\netcoreapp3.1\logs\nlog-all-2019-12-23.log";

            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 20, true))
                {
                    var bytes = new byte[1025];
                    stream.BeginRead(bytes, 0, bytes.Length, (u) =>
                    {

                        PrintThreadInfo("read  new FileStream");
                        Console.WriteLine(Encoding.UTF8.GetString(bytes));

                    }, null);
                }
            }

测试结果：core占用工作线程 framework占用IO线程 （具体待查...）

总之core下的filestream读写与framework是有区别的.

 

