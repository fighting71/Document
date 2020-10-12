
.net core 配置示例:

	Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
		// 详细版.
		webBuilder.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            loggerConfiguration
             .MinimumLevel.Information()
            .Enrich.FromLogContext()
           .ReadFrom.Configuration(hostingContext.Configuration)
           .Enrich.FromLogContext()
           .WriteTo.File($"{AppContext.BaseDirectory}logs\\log.txt", rollingInterval: RollingInterval.Day, shared: true)
           .WriteTo.Console();
        });
    })
    .UseSerilog((context, configuration) =>
    {
		// 开发版
        configuration
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Async(a => a.File($"{AppContext.BaseDirectory}logs\\log.log", rollingInterval: RollingInterval.Day, shared: true))
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate);
    });

参数说明:

- rollingInterval: 指定日志文件文件名的格式(一般按天分)[默认不分]
- shared: 允许其他线程同时修改日志文件，[默认不允许],不允许时，其他程序不可编辑/删除日志文件...
- MinimumLevel.Override: 根据规则调整日志级别

异步写入包: **Serilog.Sinks.Async**

	日志会延时写入，且未做特殊处理时可能存在关闭进程导致的日志丢失...