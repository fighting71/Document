
### 解决 .net core 3.0 中json默认按帕斯卡命名返回 ###

[https://gitee.com/skyMay/codes/gq60eyjul72fb5rtk8c4o43](https://gitee.com/skyMay/codes/gq60eyjul72fb5rtk8c4o43 "fix")

code: Startup.ConfigureServices(IServiceCollection services) 中.

	services.AddMvc()
        .AddJsonOptions(options =>
        {
            // 取消帕斯卡命名格式返回.
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });

### 解决 .net core 3.0升级相关问题 (json格式返回，swagger显示格式问题等) ###

[https://www.cnblogs.com/laozhang-is-phi/p/11520048.html#autoid-5-3-0](https://www.cnblogs.com/laozhang-is-phi/p/11520048.html#autoid-5-3-0 "博文")

### C# 源码地址 ###

[clr相关](https://github.com/SSCLI/sscli20_20060311)

### 文章相关 ###

Thread.BeginCriticalRegion(); 相关：

[关键区域 critical region](https://blogs.msdn.microsoft.com/bclteam/2005/06/13/constrained-execution-regions-and-other-errata-brian-grunkemeyer/)