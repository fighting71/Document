
[项目地址](https://github.com/EasyNetQ/EasyNetQ)

### 简单使用示例 ###

1.创建Bus

	IBus bus = RabbitHutch.CreateBus("连接地址");

2.创建订阅类

	AutoSubscriber subscriber = new AutoSubscriber(bus, "订阅前缀");

3.进行订阅

	// 扫描同步处理订阅，扫实现 IConsume<in T> 的类
	subscriber.Subscribe(assembly);

	// 扫描异步处理订阅，扫实现 IConsumeAsync<in T> 的类
	subscriber.SubscribeAsync(assembly);

### 源码分析 ###

以异步为例

	public virtual void SubscribeAsync(params Type[] consumerTypes)
	{
		MethodInfo subscribeMethodOfBus = GetSubscribeMethodOfBus("SubscribeAsync", typeof(Func<, >));// 从IBus中获取SubscribeAsync方法
		IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> subscriptionInfos = GetSubscriptionInfos(consumerTypes, typeof(IConsumeAsync<>));// 获取所有实现了 IConsumeAsync<> 的类型
		InvokeMethods(subscriptionInfos, "DispatchAsync", subscribeMethodOfBus, SubscriberDelegate);

		// 传入一个 T 返回一个  Func<T,Task>
		// T ==> Func<T,Task>
		static Type SubscriberDelegate(Type messageType)
		{
			return typeof(Func<, >).MakeGenericType(messageType, typeof(Task));
		}
	}

	protected virtual void InvokeMethods(IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> subscriptionInfos, string dispatchName, MethodInfo genericBusSubscribeMethod, Func<Type, Type> subscriberTypeFromMessageTypeDelegate)
	{
		foreach (KeyValuePair<Type, AutoSubscriberConsumerInfo[]> subscriptionInfo in subscriptionInfos)
		{
			AutoSubscriberConsumerInfo[] value = subscriptionInfo.Value;
			foreach (AutoSubscriberConsumerInfo autoSubscriberConsumerInfo in value)
			{
				Delegate @delegate = 
					AutoSubscriberMessageDispatcher.GetType().GetMethod(dispatchName, BindingFlags.Instance | BindingFlags.Public)// 获取AutoSubscriberMessageDispatcher类型的{dispatchName}方法
					.MakeGenericMethod(autoSubscriberConsumerInfo.MessageType, autoSubscriberConsumerInfo.ConcreteType)// 此方法为泛型方法，在此处指定泛型参数
					.CreateDelegate(subscriberTypeFromMessageTypeDelegate(autoSubscriberConsumerInfo.MessageType), AutoSubscriberMessageDispatcher);// 通过方法定义和实现类创建一个委托

                AutoSubscriberConsumerAttribute subscriptionAttribute = GetSubscriptionAttribute(autoSubscriberConsumerInfo);
                string text = (subscriptionAttribute != null) ? subscriptionAttribute.SubscriptionId : GenerateSubscriptionId(autoSubscriberConsumerInfo);
                MethodInfo methodInfo = genericBusSubscribeMethod.MakeGenericMethod(autoSubscriberConsumerInfo.MessageType);
                Action<ISubscriptionConfiguration> action = GenerateConfigurationAction(autoSubscriberConsumerInfo);
                methodInfo.Invoke(bus, new object[3]
                {
                    text,
                    @delegate,
                    action
                });
            }
		}
	}

	public IAutoSubscriberMessageDispatcher AutoSubscriberMessageDispatcher{get;set;}

	public interface IAutoSubscriberMessageDispatcher
	{
		void Dispatch<TMessage, TConsumer>(TMessage message) where TMessage : class where TConsumer : class, IConsume<TMessage>;

		Task DispatchAsync<TMessage, TConsumer>(TMessage message) where TMessage : class where TConsumer : class, IConsumeAsync<TMessage>;
	}

#### 消息处理 ####

 首先你得知道**@delegate**是什么，因为这个关乎着**最终的处理是如何进行的**。

 首先根据**CreateDelegate**定义，它会产生传入参数1的类型的一个委托，而根据**SubscriberDelegate**方法可知:

  @delegate 是一个Func<T,Task>的一个处理类，而这个 T 又是什么呢？

 根据**MessageType**可知，它是处理时的传入参数，即 IConsumeAsync<T> 中的T,那么它就是一个Func<{传入参数},Task>的委托

 接着看它的处理实现类:

	public AutoSubscriber(IBus bus, string subscriptionIdPrefix)
	{
		AutoSubscriberMessageDispatcher = new DefaultAutoSubscriberMessageDispatcher();
	}

 由于**AutoSubscriberMessageDispatcher**是**public**且带有**set**方法，你可自行指定实现类，但其默认实现就是**DefaultAutoSubscriberMessageDispatcher**

	public class DefaultAutoSubscriberMessageDispatcher : IAutoSubscriberMessageDispatcher
	{
		private class ActivatorBasedResolver : IServiceResolver
		{
			public TService Resolve<TService>() where TService : class
			{
				return Activator.CreateInstance<TService>();
			}

			public IServiceResolverScope CreateScope()
			{
				return new ServiceResolverScope(this);
			}
		}

		private readonly IServiceResolver resolver;

		public DefaultAutoSubscriberMessageDispatcher(IServiceResolver resolver)
		{
			this.resolver = resolver;
		}

		public DefaultAutoSubscriberMessageDispatcher()
			: this(new ActivatorBasedResolver())
		{
		}

		public void Dispatch<TMessage, TConsumer>(TMessage message) where TMessage : class where TConsumer : class, IConsume<TMessage>
		{
			using (IServiceResolverScope serviceResolverScope = resolver.CreateScope())
			{
				serviceResolverScope.Resolve<TConsumer>().Consume(message);
			}
		}

		public async Task DispatchAsync<TMessage, TAsyncConsumer>(TMessage message) where TMessage : class where TAsyncConsumer : class, IConsumeAsync<TMessage>
		{
            Console.WriteLine("DefaultAutoSubscriberMessageDispatcher.DispatchAsync");
			using (IServiceResolverScope scope = resolver.CreateScope())
			{
				await scope.Resolve<TAsyncConsumer>().ConsumeAsync(message).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

很简单的处理，通过无参构造获取实例然后调用执行方法

#### 消息订阅 ####

最终的执行分析完后，继续来看它的订阅与触发.根据之前的推断可知，其最终是通过**IBus.SubscribeAsync**进行处理，IBus 默认实现 : RabbitBus

	<RabbitBus>

	public virtual ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
	{
		SubscriptionConfiguration configuration = new SubscriptionConfiguration(connectionConfiguration.PrefetchCount);
		configure(configuration);
		string text = configuration.QueueName ?? conventions.QueueNamingConvention(typeof(T), subscriptionId);
		IQueue queue = advancedBus.QueueDeclare(text, passive: false, autoDelete: configuration.AutoDelete, durable: configuration.Durable, exclusive: false, perQueueMessageTtl: null, expires: configuration.Expires, maxPriority: configuration.MaxPriority, deadLetterExchange: null, deadLetterRoutingKey: null, maxLength: configuration.MaxLength, maxLengthBytes: configuration.MaxLengthBytes);
		IExchange exchange = exchangeDeclareStrategy.DeclareExchange(typeof(T), "topic");
		foreach (string item in configuration.Topics.DefaultIfEmpty("#"))
		{
			// 队列绑定 
			advancedBus.Bind(exchange, queue, item);
		}
		// 监听核心
		IDisposable consumerCancellation = advancedBus.Consume(queue, (IMessage<T> message, MessageReceivedInfo messageReceivedInfo) => onMessage(message.Body), delegate(IConsumerConfiguration x)
		{
			x.WithPriority(configuration.Priority).WithPrefetchCount(configuration.PrefetchCount);
			if (configuration.IsExclusive)
			{
				x.AsExclusive();
			}
		});
		return new SubscriptionResult(exchange, queue, consumerCancellation);
	}

	// 默认实现: 	EasyNetQ.RabbitAdvancedBus
	private readonly IAdvancedBus advancedBus;


----------

	<RabbitAdvancedBus>


	【队列绑定】
	public virtual IBinding Bind(IExchange exchange, IQueue queue, string routingKey)
	{
		return Bind(exchange, queue, routingKey, null);
	}

	public virtual IBinding Bind(IExchange exchange, IQueue queue, string routingKey, IDictionary<string, object> headers)
	{
		IDictionary<string, object> arguments = headers ?? new Dictionary<string, object>();
		// 使用 clientCommandDispatcher 去代理进行队列绑定
		clientCommandDispatcher.Invoke(delegate(IModel x)
		{
			x.QueueBind(queue.Name, exchange.Name, routingKey, arguments);
		});
		if (logger.IsDebugEnabled())
		{
			logger.DebugFormat("Bound queue {queue} to exchange {exchange} with routingKey={routingKey} and arguments={arguments}", queue.Name, exchange.Name, routingKey, arguments.Stringify());
		}
		return new Binding(queue, exchange, routingKey, arguments);
	}

	略...内容有点多，走了内部的IEventBus

	[消息监听]

	public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class
	{
		return Consume(queue, delegate(IHandlerRegistration x)
		{
			x.Add(onMessage);
		}, configure);
	}

	public IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure)
	{
		IHandlerCollection handlerCollection = handlerCollectionFactory.CreateHandlerCollection(queue);
		addHandlers(handlerCollection);
		return Consume(queue, delegate(byte[] body, MessageProperties properties, MessageReceivedInfo messageReceivedInfo)
		{
			IMessage message = messageSerializationStrategy.DeserializeMessage(properties, body);
			return handlerCollection.GetHandler(message.MessageType)(message, messageReceivedInfo);
		}, configure);
	}

	public virtual IDisposable Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure)
	{
		ConsumerConfiguration consumerConfiguration = new ConsumerConfiguration(connectionConfiguration.PrefetchCount);
		configure(consumerConfiguration);
		return consumerFactory.CreateConsumer(queue, delegate(byte[] body, MessageProperties properties, MessageReceivedInfo receivedInfo)
		{
			RawMessage rawMessage = produceConsumeInterceptor.OnConsume(new RawMessage(properties, body));
			return onMessage(rawMessage.Body, rawMessage.Properties, receivedInfo);
		}, connection, consumerConfiguration).StartConsuming();
	}

	略，最终通过

	string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer);

	进行消费监听，监听方法为之前注册的 Func<IMessage<T>, MessageReceivedInfo, Task> onMessage