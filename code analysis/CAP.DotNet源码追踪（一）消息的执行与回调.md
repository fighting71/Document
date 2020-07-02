
**CAP.DotNet源码追踪（一）消息是如何执行的，执行后又是如何执行回调的**

----------

### 场景 ###

.NET Core 3.1
nuget包：DotNetCore.CAP.RabbitMQ 3.0.2

### 目录 ###

[CAP.DotNet源码追踪（一）消息是如何执行的，执行后又是如何执行回调的](https://www.cnblogs.com/monster17/p/12852266.html)

### 前言 ###

以对话为引，梳理我们的问题:

*小明-聪明绝顶的程序猿，小红-绝美程序媛*

小红:小明，在CAP中，我们通过**CapSubscribe**特性来订阅方法，而我们写的消息处理中，返回值一般都是void/Task，那么这个返回值类型有什么用呀或者说这个返回值有什么用呢？

小明一看表现的机会来了，马上回复了一句：百因必有果，既然有返回类型这个东西，那么CAP设计者肯定会利用这一点来进行应用，不会无故忽视这个功能的

小红：小明，那它到底有什么作用呢？

小明：既然想知道它的作用，那么就应该去学习它的消息执行原理，分析其执行过程，自然就明白它的返回值的作用了。

小红：哇，问题一下从返回值的作用上升到了CAP执行原理了，小明，快给我讲讲呀😉

小明心想是时候表演真正的技术了~✨ @#^*$&(^#@*&(^$#@(

### 正文 ###

从应用层面来看，我们使用CAP主要就是使用它的消息发送和订阅，既然要学习它的执行原理，那么就直接从消息发送出发来学习

**MQ发送**

以常用情况为例，我们通过注册的**ICapPublisher**下的**PublishAsync**来发送消息

首先先看一下方法定义：

    Task PublishAsync<T>(string name, [CanBeNull] T contentObj, string callbackName = null, CancellationToken cancellationToken = default);

name - 主题名称或交换路由器密钥。

contentObj - 消息内容

callbackName - callback subscriber name 回调的订阅者名称

cancellationToken - 略

那么它是怎么执行呢?

  既然是通过**ICapPublisher**调用，那么找到**ICapPublisher**的实现者不就知道了，更为直接的方法是查找注入的地方，查看**ICapPublisher**的对应实现类

通过**Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions**方法**AddCAP**可知其实现为**DotNetCore.CAP.Internal.CapPublisher** (ps:CAP支持自行实现ICapPublisher)

查看实现定义：

	public Task PublishAsync<T>(string name, T value, string callbackName = null,
            CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Publish(name, value, callbackName), cancellationToken);
    }

	public void Publish<T>(string name, T value, string callbackName = null)
    {
        var header = new Dictionary<string, string>
        {
            {Headers.CallbackName, callbackName}
        };

        Publish(name, value, header);
    }

	public void Publish<T>(string name, T value, IDictionary<string, string> headers)// 删减版
    {
		// 唯一id生成
        var messageId = SnowflakeId.Default().NextId().ToString();

		headers.Add(Headers.MessageId, messageId);

		// cap通过此内容来验证消息类型是否匹配
        headers.Add(Headers.Type, typeof(T).FullName);
		headers.Add(Headers.MessageName, name);
		headers.Add(Headers.Type, typeof(T).FullName);
        headers.Add(Headers.SentTime, DateTimeOffset.Now.ToString());
        if (!headers.ContainsKey(Headers.CorrelationId))
        {
            headers.Add(Headers.CorrelationId, messageId);
            headers.Add(Headers.CorrelationSequence, 0.ToString());
        }

        var message = new Message(headers, value);
		// ↑↑↑↑↑↑↑↑↑封装消息体↑↑↑↑↑↑↑↑↑

		long? tracingTimestamp = null;
        try
        {
            tracingTimestamp = TracingBefore(message);

            if (Transaction.Value?.DbTransaction == null)
            {
				// 此处返回用存储后的数据格式表示的消息体
                var mediumMessage = _storage.StoreMessage(name, message);

                TracingAfter(tracingTimestamp, message);

                _dispatcher.EnqueueToPublish(mediumMessage);
            }
            else
            {
                var transaction = (CapTransactionBase)Transaction.Value;

                var mediumMessage = _storage.StoreMessage(name, message, transaction.DbTransaction);

                TracingAfter(tracingTimestamp, message);

                transaction.AddToSent(mediumMessage);

                if (transaction.AutoCommit)
                {
                    transaction.Commit();
                }
            }
			// ↑↑↑↑↑↑↑↑↑存储消息并尝试发送↑↑↑↑↑↑↑↑↑
        }
        catch (Exception e)
        {
            TracingError(tracingTimestamp, message, e);

            throw;
        }

	}

通过分析这里发送包括：

1.**唯一id的生成** -> 可自行查看SnowflakeId实现

2.**数据库存储** -> 可自行查看MySql或Sqlserver的实现

3.**TracingAfter(tracingTimestamp, message);** -> 可查看**System.Diagnostics.DiagnosticListener** 的应用

4.**_dispatcher.EnqueueToPublish(mediumMessage);** -> 可查看**System.Threading.Channels.Channel** 的应用

5.**CapTransactionBase** -> cap的事务处理 (略)

好了到此发送就结束了，简单来说就是 生成消息 + 消息存储，那么订阅是在哪里被触发呢？

这里引出一个新类**Microsoft.Extensions.Hosting.BackgroundService**，desc:用于实现长时间运行的 IHostedService 的基类。

让我们回到**AddCAP**中，在最后哪里使用了**services.AddHostedService<Bootstrapper>();**

查看**Bootstrapper**的定义:

	internal class Bootstrapper : BackgroundService, IBootstrapper

到了这里**CAP**的一个大致的轮廓就有了，

发送 -> 组装消息+消息存储+事务(配合着Channel,DiagnosticListener)

订阅/接收消息 -> 初始启动一个**BackgroundService**去扫描消息进行执行

既然找到了接收工作的地方，那我们就来看看它怎么实现的吧：**Bootstrapper.ExecuteAsync**

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BootstrapAsync(stoppingToken);
    }

	public async Task BootstrapAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("### CAP background task is starting.");

        try
        {
            await Storage.InitializeAsync(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Initializing the storage structure failed!");
        }

        stoppingToken.Register(() =>
        {
            _logger.LogDebug("### CAP background task is stopping.");

            foreach (var item in Processors)
            {
                try
                {
                    item.Dispose();
                }
                catch (OperationCanceledException ex)
                {
                    _logger.ExpectedOperationCanceledException(ex);
                }
            }
        });

        await BootstrapCoreAsync();

        _logger.LogInformation("### CAP started!");
    }

① **await Storage.InitializeAsync(stoppingToken);** 见名思意，应该是存储相关的初始化，例如：CAP在使用db存储时初始化会自动创建对应的表

查看**Processors**的定义：

	private IEnumerable<IProcessingServer> Processors { get; }

② 当**stoppingToken**取消时释放所有的IProcessingServer

③ await BootstrapCoreAsync(); 

	protected virtual Task BootstrapCoreAsync()
    {
        foreach (var item in Processors)
        {
            try
            {
                item.Start();
            }
            catch (Exception ex)
            {
                _logger.ProcessorsStartedError(ex);
            }
        }

        return Task.CompletedTask;
    }

即是启动所有的**IProcessingServer**

那么**IProcessingServer**具体有哪些实现呢？

既然有使用的地方就肯定有注册的地方，再次回到**AddCAP**注入处，通过源码可以看到：

	services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, CapProcessingServer>());
	services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, ConsumerRegister>());

注入了**CapProcessingServer**和**ConsumerRegister**

#### CapProcessingServer ####

略

#### ConsumerRegister ####

找到**ConsumerRegister.Start**:

	public void Start()
	{
	    var groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();
	
	    foreach (var matchGroup in groupingMatches)
	    {
	        for (int i = 0; i < _options.ConsumerThreadCount; i++)
	        {
	            Task.Factory.StartNew(() =>
	            {
	                try
	                {
	                    using (var client = _consumerClientFactory.Create(matchGroup.Key))
	                    {
	                        _serverAddress = client.BrokerAddress;
	
	                        RegisterMessageProcessor(client);
	
	                        client.Subscribe(matchGroup.Value.Select(x => x.Attribute.Name));
	
	                        client.Listening(_pollingDelay, _cts.Token);
	                    }
	                }
	                catch (OperationCanceledException)
	                {
	                    //ignore
	                }
	                catch (BrokerConnectionException e)
	                {
	                    _isHealthy = false;
	                    _logger.LogError(e, e.Message);
	                }
	                catch (Exception e)
	                {
	                    _logger.LogError(e, e.Message);
	                }
	            }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
	        }
	    }
	    _compositeTask = Task.CompletedTask;
	}

*step by step*:

a:获取所有组别及其相关信息[即获取所有有**TopicAttribute**特性标记的信息(from Interface or Controller)]

	var groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();

具体实现（略：通过Type查找Attribute，再筛选相符合的信息）

b:遍历这些组别，通过组别名称，创建相应的消费者**client**

c:注册client的消息接收事件

	 RegisterMessageProcessor(client);

d:开启订阅

	client.Subscribe(matchGroup.Value.Select(x => x.Attribute.Name));

e:开启监听

	client.Listening(_pollingDelay, _cts.Token);

可见**ConsumerRegister.Start**的主要功能就是消费者的前置初始化，根据代码中的特性标记开启相应的订阅和监听，既然消息监听已经开启了，那消息是如何处理的呢？

让我们回过头来关注“注册client的消息接收事件”，“注册client的消息接收事件”即是定义消息的具体实现，查看源码：

	private void RegisterMessageProcessor(IConsumerClient client)
	{
	    client.OnMessageReceived += async (sender, transportMessage) =>
	    {
	        _logger.MessageReceived(transportMessage.GetId(), transportMessage.GetName());
	
	        long? tracingTimestamp = null;
	        try
	        {
	            tracingTimestamp = TracingBefore(transportMessage, _serverAddress);
	
	            var name = transportMessage.GetName();
	            var group = transportMessage.GetGroup();
	
	            Message message;
	
	            var canFindSubscriber = _selector.TryGetTopicExecutor(name, group, out var executor);
	            try
	            {
	                if (!canFindSubscriber)
	                {
	                    var error = $"Message can not be found subscriber. Name:{name}, Group:{group}. {Environment.NewLine} see: https://github.com/dotnetcore/CAP/issues/63";
	                    var ex = new SubscriberNotFoundException(error);
	
	                    TracingError(tracingTimestamp, transportMessage, client.BrokerAddress, ex);
	
	                    throw ex;
	                }
	
	                var type = executor.Parameters.FirstOrDefault(x => x.IsFromCap == false)?.ParameterType;
	                message = await _serializer.DeserializeAsync(transportMessage, type);
	            }
	            catch (Exception e)
	            {
	                transportMessage.Headers.Add(Headers.Exception, nameof(SerializationException) + "-->" + e.Message);
	                var dataUri = $"data:{transportMessage.Headers[Headers.Type]};base64," + Convert.ToBase64String(transportMessage.Body);
	                message = new Message(transportMessage.Headers, dataUri);
	            }
	
	            if (message.HasException())
	            {
	                var content = StringSerializer.Serialize(message);
	
	                _storage.StoreReceivedExceptionMessage(name, group, content);
	
	                client.Commit(sender);
	
	                try
	                {
	                    _options.FailedThresholdCallback?.Invoke(new FailedInfo
	                    {
	                        ServiceProvider = _serviceProvider,
	                        MessageType = MessageType.Subscribe,
	                        Message = message
	                    });
	
	                    _logger.ConsumerExecutedAfterThreshold(message.GetId(), _options.FailedRetryCount);
	                }
	                catch (Exception e)
	                {
	                    _logger.ExecutedThresholdCallbackFailed(e);
	                }
	
	                TracingAfter(tracingTimestamp, transportMessage, _serverAddress);
	            }
	            else
	            {
	                var mediumMessage = _storage.StoreReceivedMessage(name, group, message);
	                mediumMessage.Origin = message;
	
	                client.Commit(sender);
	
	                TracingAfter(tracingTimestamp, transportMessage, _serverAddress);
	
	                _dispatcher.EnqueueToExecute(mediumMessage, executor);
	            }
	        }
	        catch (Exception e)
	        {
	            _logger.LogError(e, "An exception occurred when process received message. Message:'{0}'.", transportMessage);
	
	            client.Reject(sender);
	
	            TracingError(tracingTimestamp, transportMessage, client.BrokerAddress, e);
	        }
	    };
	
	    client.OnLog += WriteLog;
	}

*side by side~*

a: 注册接收时的实现

	client.OnMessageReceived += async (sender, transportMessage) =>

查看**OnMessageReceived**定义：

	event EventHandler<TransportMessage> OnMessageReceived;

故此处就是添加一个委托实现，那么具体的实现便是我们的消息处理了

b: 获取消息订阅(执行)者

	var canFindSubscriber = _selector.TryGetTopicExecutor(name, group, out var executor);

查找我们的消息执行者(根据name,group) [内部实现略：反射配合特性]

c: 获取消息内容

	var type = executor.Parameters.FirstOrDefault(x => x.IsFromCap == false)?.ParameterType;
    message = await _serializer.DeserializeAsync(transportMessage, type);

 查看**_serializer**定义：

	private readonly ISerializer _serializer;

 具体获取：

	_serializer = serviceProvider.GetService<ISerializer>();

 *故我们可以通过实现ISerializer再进行注入，实现我们自己的消息解析*

 默认实现：略[可自行查看DotNetCore.CAP.Serialization.JsonUtf8Serializer]

d: 存储接收消息

	var mediumMessage = _storage.StoreReceivedMessage(name, group, message);

 在CAP的db存储中，有着**published**和**received**两个表，发送存储**published**，接收存储**received**

e: 处理消息

	_dispatcher.EnqueueToExecute(mediumMessage, executor);

 查看其定义：

	private readonly IDispatcher _dispatcher;

 也是和**_serializer**类似通过IOC容器获取

然后让我们回到**AddCAP**查看其注入类

	services.TryAddSingleton<IDispatcher, Dispatcher>();

查看**Dispatcher.EnqueueToExecute**实现：

	public void EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor)
    {
        _receivedChannel.Writer.TryWrite((message, descriptor));
    }

也是和发送一样，通过**Channel**进行传递，好了，我们就来看看这个**_receivedChannel**的定义：

	private readonly Channel<(MediumMessage, ConsumerExecutorDescriptor)> _receivedChannel;

私有字段-》外界无法访问，那么处理要不在子类中，要不就是直接在此类中，查看**Dispatcher**的构造：

	public Dispatcher(ILogger<Dispatcher> logger,
            IMessageSender sender,
            IOptions<CapOptions> options,
            ISubscribeDispatcher executor)
    {
        _logger = logger;
        _sender = sender;
        _executor = executor;

        _publishedChannel = Channel.CreateUnbounded<MediumMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        _receivedChannel = Channel.CreateUnbounded<(MediumMessage, ConsumerExecutorDescriptor)>();

        Task.Factory.StartNew(Sending, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        Task.WhenAll(Enumerable.Range(0, options.Value.ConsumerThreadCount)
            .Select(_ => Task.Factory.StartNew(Processing, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());
    }

a: 初始化
	_receivedChannel = Channel.CreateUnbounded<(MediumMessage, ConsumerExecutorDescriptor)>();


看看**CreateUnbounded**的说明：

	Creates an unbounded channel usable by any number of readers and writers concurrently.
	创建可由任意数量的读取器和写入器并发使用的无限制通道。

一个允许并发处理的通道?

b: 使用

	Task.WhenAll(Enumerable.Range(0, options.Value.ConsumerThreadCount)
    .Select(_ => Task.Factory.StartNew(Processing, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

创建了**options.Value.ConsumerThreadCount**个**Task**(并设置**TaskCreationOptions.LongRunning**-长期运行)

再查看其**Processing**实现：

	private async Task Processing()
    {
        try
        {
            while (await _receivedChannel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_receivedChannel.Reader.TryRead(out var message))
                {
                    await _executor.DispatchAsync(message.Item1, message.Item2, _cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }
	
查看**ChannelReader<T>.WaitToReadAsync**说明：返回将在有数据可供读取时完成的 ValueTask<TResult>。

**TryRead**便是取出消息

<伪代码>:

	循环(等待Channel中有数据写入)
	{
		循环(尝试从通道中读取项)
		{
			处理消息
		}
	}

消息处理：

	 await _executor.DispatchAsync(message.Item1, message.Item2, _cts.Token);

结合构造和**Dispatcher**的构建可以看出**_executor**是通过IOC容器获取的，再次查看**AddCAP**的注册：

	services.TryAddSingleton<ISubscribeDispatcher, SubscribeDispatcher>();

查找**SubscribeDispatcher.DispatchAsync**

	public async Task<OperateResult> DispatchAsync(MediumMessage message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
    {
        bool retry;
        OperateResult result;
        do
        {
            var executedResult = await ExecuteWithoutRetryAsync(message, descriptor, cancellationToken);
            result = executedResult.Item2;
            if (result == OperateResult.Success)
            {
                return result;
            }
            retry = executedResult.Item1;
        } while (retry);

        return result;
    }

调用**ExecuteWithoutRetryAsync**并设计了重试机制

	private async Task<(bool, OperateResult)> ExecuteWithoutRetryAsync(MediumMessage message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var sp = Stopwatch.StartNew();

            await InvokeConsumerMethodAsync(message, descriptor, cancellationToken);

            sp.Stop();

            await SetSuccessfulState(message);

            _logger.ConsumerExecuted(sp.Elapsed.TotalMilliseconds);

            return (false, OperateResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An exception occurred while executing the subscription method. Topic:{message.Origin.GetName()}, Id:{message.DbId}");

            return (await SetFailedState(message, ex), OperateResult.Failed(ex));
        }
    }

 1.核心执行**await InvokeConsumerMethodAsync(message, descriptor, cancellationToken);**

 2.异常处理**(await SetFailedState(message, ex), OperateResult.Failed(ex));** ： 略

查看**SubscribeDispatcher.InvokeConsumerMethodAsync**:

	private async Task InvokeConsumerMethodAsync(MediumMessage message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
    {
        var consumerContext = new ConsumerContext(descriptor, message.Origin);
        var tracingTimestamp = TracingBefore(message.Origin, descriptor.MethodInfo);
        try
        {
            var ret = await Invoker.InvokeAsync(consumerContext, cancellationToken);

            TracingAfter(tracingTimestamp, message.Origin, descriptor.MethodInfo);

            if (!string.IsNullOrEmpty(ret.CallbackName))
            {
                var header = new Dictionary<string, string>()
                {
                    [Headers.CorrelationId] = message.Origin.GetId(),
                    [Headers.CorrelationSequence] = (message.Origin.GetCorrelationSequence() + 1).ToString()
                };

                await _provider.GetService<ICapPublisher>().PublishAsync(ret.CallbackName, ret.Result, header, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            //ignore
        }
        catch (Exception ex)
        {
            var e = new SubscriberExecutionFailedException(ex.Message, ex);

            TracingError(tracingTimestamp, message.Origin, descriptor.MethodInfo, e);

            throw e;
        }
    }

 1.核心执行：

	var ret = await Invoker.InvokeAsync(consumerContext, cancellationToken);

 又是一个通过IOC容器跳转执行，具体执行：略-通过**consumerContext**参数值和反射拿到对应执行方法并通过IOC容器获取对应的对象，执行相应的方法

 2.回调处理

	await _provider.GetService<ICapPublisher>().PublishAsync(ret.CallbackName, ret.Result, header, cancellationToken);

再放一下方法定义

	Task PublishAsync<T>(string name, [CanBeNull] T contentObj, IDictionary<string, string> headers, CancellationToken cancellationToken = default);

 所以最终答案就是：**当发布时设置了CallbackName后，当消息执行完后，会发布一个以CallbackName为name,消息返回值为消息体的消息**

>>>>>小红:小明好感度+100~<<<<<

----------
参考博文：

[谈谈.NET Core中基于Generic Host来实现后台任务](https://www.cnblogs.com/catcher1994/p/9961228.html)
