
EasyNetQ 消费处理实现类：EasyNetQ.EventBus

EasyNetQ使用自定义容器 进行依赖注入  -  EasyNetQ.DI.DefaultServiceContainer

EasyNetQ创建连接方式：
轮询
触发地址：EasyNetQ.RabbitAdvancedBus.ctor() 
    key code:connection.Initialize();
connection 默认实现： EasyNetQ.PersistentConnection
具体地址：EasyNetQ.PersistentConnection.TryToConnect

EasyNetQ消费处理：EasyNetQ.Consumer.ConsumerDispatcher - 轮询

EasyNetQ 订阅方式： 通过订阅检索消息
订阅地址：EasyNetQ.Consumer.InternalConsumer
MQ订阅触发：EasyNetQ.Consumer.BasicConsumer - 监听

ClientCommandDispatcherSingleton


消息监听:

	RabbitAdvancedBus

	public IDisposable Consume(IQueue queue, Action<IHandlerRegistration> addHandlers, Action<IConsumerConfiguration> configure)

消息处理订阅: (down to up)

添加处理:**EasyNetQ.Consumer.HandlerCollection.Add**

	public IHandlerRegistration Add<T>(Func<IMessage<T>, MessageReceivedInfo, Task> handler) where T : class

**RabbitAdvancedBus.Consume**

	public IDisposable Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class

**RabbitBus**

	public virtual ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration> configure) where T : class

 key code : 

	IDisposable consumerCancellation = advancedBus.Consume(queue, (IMessage<T> message, MessageReceivedInfo messageReceivedInfo) => ...