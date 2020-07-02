review: .net core api 使用 CAP.DotNet

environment :
 .net core web api 3.1
 DotNetCore.CAP 3.0.4.0
 DotNetCore.CAP.RabbitMQ 3.0.4.0

1. 包引用:
	1. DotNetCore.CAP (*required)
	2. MQ包 : DotNetCore.CAP.RabbitMQ/... (*required)
	3. Storage: DotNetCore.CAP.MySql/SqlServer/InMemoryStorage/... (*required)
	4. 仪表盘:DotNetCore.CAP.Dashboard (optional)
2. Cap注入、

	In ConfigureServices(IServiceCollection services):

		services.AddCap(delegate(CapOptions option)
		{
			option.UseDashboard();// 使用仪表盘
			option.UseRabbitMQ(delegate(RabbitMQOptions config) // 配置MQ
			{
				config.HostName = "132.232.105.129";
				config.Port = 5672;
				config.Password = "ca7dvvqVsWF5GgB";
				config.UserName = "ZeusSys";
				config.VirtualHost = "uaMQ";
			});
			option.UseSqlServer(Configuration.GetConnectionString("UArea")); // 配置storage
		});

review: CAP.DotNet 执行原理: 《source Code》

1.服务的启动：

  采用**BackgroudService**进行启动:

	services.AddHostedService<Bootstrapper>();

2.服务的执行:

  **Bootstrapper**使用注入的**IProcessingServer**进行执行

	public interface IProcessingServer : IDisposable
	{
		void Pulse();
	
		void Start();
	}

  <default> : 

	services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, CapProcessingServer>());
	services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, ConsumerRegister>());

3.消息的订阅

  a.在**ConsumerRegister**中通过特性标记获取订阅所需参数

  b.通过Task异步进行执行
	Task.Factory.StartNew(delegate, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

  c.具体订阅通过**factory**所创建的**IConsumerClient**执行

	IConsumerClient 默认实现 DotNetCore.CAP.RabbitMQ.RabbitMQConsumerClient

	消息监听：

	private IModel _channel;

	_channel.BasicConsume(_queueName, autoAck: false, eventingBasicConsumer);

4.消息的执行

  **RabbitMQConsumerClient**在MQ中消息订阅事件:

	eventingBasicConsumer.Received += OnConsumerReceived;

	// 最终执行:
	public event EventHandler<TransportMessage> OnMessageReceived;

  其**OnMessageReceived**定义为public即为外界注册 ==> DotNetCore.CAP.Internal.ConsumerRegister.**RegisterMessageProcessor**