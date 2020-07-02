
### 解决 .net core 3.0 中json默认按帕斯卡命名返回 ###

[https://gitee.com/skyMay/codes/gq60eyjul72fb5rtk8c4o43](https://gitee.com/skyMay/codes/gq60eyjul72fb5rtk8c4o43 "fix")

code: Startup.ConfigureServices(IServiceCollection services) 中.

	services.AddMvc()
        .AddJsonOptions(options =>
        {
            // 取消帕斯卡命名格式返回.
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });

### .net core help site ###

[解决 .net core 3.0升级相关问题 (json格式返回，swagger显示格式问题等)](https://www.cnblogs.com/laozhang-is-phi/p/11520048.html#autoid-5-3-0)

[.net core 3.1注入Options](https://www.lzys.cc/p/1446771.html)

### C# 源码地址 ###

[clr相关](https://github.com/SSCLI/sscli20_20060311)

### 文章相关 ###

Thread.BeginCriticalRegion(); 相关：

[关键区域 critical region](https://blogs.msdn.microsoft.com/bclteam/2005/06/13/constrained-execution-regions-and-other-errata-brian-grunkemeyer/)

### 基础扫盲 ###

[固态/机械硬盘存取](https://sspai.com/post/55277)

### RabbitMQ ###

[RabbitMQ.Client api说明](https://www.cnblogs.com/hsyzero/p/6297644.html)

### C# Function ###

[提升循环块小的执行效率](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-speed-up-small-loop-bodies)

