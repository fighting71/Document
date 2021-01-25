# bug记录 #

## dapper一对多查询bug ##

### environment ###

.net core 3.0

dapper.dll 2.0.30

db:sqlserver

### 问题描述 ###

	connection.QueryAsync<QueryRes, ExtraService, QueryRes>(sql, (res, ext) =>
    {...省略},splitOn: "OrderID,ExtraServiceHistoryID");

调试时ext只能取得主键值 其他列没有取到值

### reason ###

因为第二个数据的列在第二个表主键之前

### fix ###

将列值放在主键之后就ok了。

### sample ###

	SELECT a.Id,a.Name,**b.Tel,b.Id**

to ==>

	SELECT a.Id,a.Name,**b.Id,b.Tel**

### RequestSizeLimit 特性设置无效 ###

[相关代码](https://source.dot.net/#Microsoft.AspNetCore.Server.Kestrel.Core/Internal/Http/HttpProtocol.FeatureCollection.cs,32)

核心代码：

	bool IHttpMaxRequestBodySizeFeature.IsReadOnly => HasStartedConsumingRequestBody || IsUpgraded;

**HasStartedConsumingRequestBody** 翻译： **已经开始消费请求正文了?**

看到这瞬间就明白了...,自己写了个中间件实现**IMiddleware**然后通过**UseMiddleware**进行了拦截

好巧不巧，这里面读取了body [自己挖坑] 从而导致了 **RequestSizeLimit**特性无效 😢😢😢

	var request = context.Request;
    request.EnableBuffering();
    StreamReader streamReader = new StreamReader(request.Body);
    var body = await streamReader.ReadToEndAsync();
    request.Body.Seek(0, SeekOrigin.Begin);
    try
    {
        await next(context);
    }
    catch (Exception e)
    {
		....
        throw e;
    }

**解决方案：**

1.在拦截器进行读取之前修改：

	IHttpMaxRequestBodySizeFeature httpMaxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    httpMaxRequestBodySizeFeature.MaxRequestBodySize = 【...】;

2.在启动中配置

	public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>().UseKestrel(options =>
                {
                    //控制中间件允许的上传文件大小为：不限制
                    options.Limits.MaxRequestBodySize = null;
                });
            });

3.不在拦截器中读取**Body**