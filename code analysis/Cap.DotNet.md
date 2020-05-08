
**CAP源码追踪（一）消息是如何执行的，执行后又是如何执行回调的**

----------

### 场景 ###

.NET Core 3.1
nuget包：DotNetCore.CAP.RabbitMQ 3.0.2

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


----------

至于返回值的作用，从callbackName可进行一个猜想：

何为回调，即是执行完后再调用这个方法，但这里只定义了一个callbackName,相对于name也是一个名称，

我们假设callbackName和name也是消息id,这时你就会发现这个回调的消息，只有消息id没有消息内容，那么消息内容为什么没有呢？  

我们再大胆的猜测一下，订阅消息执行完后，拿它的返回值作为回调消息的消息内容，不就有消息内容了吗

over~

----------


...待更