# IL 相关知识储备 #

[博文-开始文章](https://www.cnblogs.com/zery/p/3366175.html#!comments)

通过评论找到了[msdn-官方说明](https://docs.microsoft.com/zh-tw/previous-versions/dd229210(v=msdn.10)?redirectedfrom=MSDN)

相关code:

	using System;
	
	public class Test {
	    public static void Main(String[] args) {
	        int i=1;
	        int j=2;
	        int k=3;
	        int answer = i+j+k;
	        Console.WriteLine("i+j+k="+answer);
	    }
	}

对应IL:

	ldc.i4.1
	stloc.0
	ldc.i4.2
	stloc.1
	ldc.i4.3
	stloc.2
	ldloc.0
	ldloc.1
	add
	ldloc.2
	add
	stloc.3
	ldstr      "i+j+k="
	ldloc.3
	box        [mscorlib]System.Int32
	call       string [mscorlib]System.String::Concat(object, object)
	call       void [mscorlib]System.Console::WriteLine(string)
	ret

此程式執行時，關鍵的記憶體有三種，分別是：

- **Managed Heap：**這是動態配置（Dynamic Allocation）的記憶體，由 Garbage Collector（GC）在執行時自動管理，整個 Process 共用一個 Managed Heap。


- **Call Stack：**這是由 .NET CLR 在執行時自動管理的記憶體，每個 Thread 都有自己專屬的 Call Stack。每呼叫一次 method，就會使得 Call Stack 上多了一個 Record Frame；呼叫完畢之後，此 Record Frame 會被丟棄。一般來說，Record Frame 內紀錄著 method 參數（Parameter）、返回位址（Return Address）、以及區域變數（Local Variable）。Java VM 和 .NET CLR 都是使用 0, 1, 2… 編號的方式來識別區域變數。


- **Evaluation Stack：**這是由 .NET CLR 在執行時自動管理的記憶體，每個 Thread 都有自己專屬的 Evaluation Stack。前面所謂的堆疊式虛擬機器，指的就是這個堆疊。

後面有一連串的示意圖，用來解說在執行時此三種記憶體的變化。首先，在進入 Main() 之後，尚未執行任何指令之前，記憶體的狀況如圖 1 所示：

![1](https://docs.microsoft.com/zh-tw/previous-versions/images/dd229210.il_f1%28zh-tw%2cmsdn.10%29.jpg)

> 首先执行ldc.i4.1 指令，在Evaluation Stack 置入一个4byte的常数，其值为1.

执行完后:

![2](https://docs.microsoft.com/zh-tw/previous-versions/images/dd229210.il_f2%28zh-tw%2cmsdn.10%29.jpg)

> 接着执行 stloc.0,从Evalution Stack取出一个值(栈类型 先进后出),放到(call stack)第0号(call stack 具有下标 故结构应该是一个列表)v0中

执行完后；

![3](https://docs.microsoft.com/zh-tw/previous-versions/images/dd229210.il_f3%28zh-tw%2cmsdn.10%29.jpg)

省略...

> 接着执行 ldloc.0以及ldloc.1,分部是将(call stack中的)v0和v1的值放到Evaluation Stack,这是准备相加的准备动作。

> 接着从Evaluation Stack取出两个值，相加后将结果放入Evaluation Stack中。

![示意图](https://docs.microsoft.com/zh-tw/previous-versions/images/dd229210.il_f10%28zh-tw%2cmsdn.10%29.jpg)