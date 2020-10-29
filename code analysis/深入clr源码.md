
源码地址[github](https://github.com/SSCLI/sscli20_20060311)

根据以前找锁源码(也没理解透，就大概的找了下).

继续从 **clr/vm/ecall.cpp** 出发:

	FCFuncStart(gMonitorFuncs)
	    FCFuncElement("Enter", JIT_MonEnter)
	FCFuncEnd()

	#define FCFuncFlags(intrinsicID, dynamicID) \
	(BYTE*)( (((BYTE)intrinsicID) << 16) + (((BYTE)dynamicID) << 24) )
	
	#define FCFuncElement(name, impl) FCFuncFlags(CORINFO_INTRINSIC_Illegal, ECall::InvalidDynamicFCallId), \
	    GetEEFuncEntryPoint(impl), NULL, NULL, (LPVOID)name,
	
	#define FCFuncStart(name) static LPVOID name[] = {
	#define FCFuncEnd() FCFuncFlag_EndOfArray + FCFuncFlags(CORINFO_INTRINSIC_Illegal, ECall::InvalidDynamicFCallId) };

*由于是cpp,去查了下c++语法，然后还是不清楚，然后才知道define是C语言中的宏定义命令*

[C++宏定义说明](https://blog.csdn.net/shuzfan/article/details/52860664)
[宏定义中的#,##,@#,\符号的作用](https://blog.csdn.net/u012234115/article/details/42169815)

文章标出了非常关键的一个点：**理解宏定义的关键在于 “替换”。**，再结合文章中的示例，就能够认识到这个定义是啥了

结合文章，让我们来进行替换：


[extern "C"的作用](https://www.cnblogs.com/xiangtingshen/p/10980055.html)

