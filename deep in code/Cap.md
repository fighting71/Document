
> .net core api + sqlserver + rabbitMq

### 发送消息如何确保消息id唯一/cap如何生成唯一id ###

以异步发送为例，定位到ICapPublisher.PublishAsync,因为此处使用Sqlserver 查看实现类SqlServerPublisher.PublishAsync

	public class SqlServerPublisher : CapPublisherBase, ICallbackPublisher

在SqlServerPublisher中未找到PublishAsync，再查看其父类CapPublisherBase中的定义

	public abstract class CapPublisherBase : ICapPublisher

	public async Task PublishAsync<T>(
      string name,
      T contentObj,
      string callbackName = null,
      CancellationToken cancellationToken = default (CancellationToken))
    {
      await this.PublishAsyncInternal(new CapPublishedMessage()
      {
        Id = SnowflakeId.Default().NextId(),
        Name = name,
        Content = this.Serialize<T>(contentObj, callbackName),
        StatusName = "Scheduled"
      });
    }

上面出现了**Id = SnowflakeId.Default().NextId(),** 那么这就是确保其id唯一的保障了

查看Default定义：

	public static SnowflakeId Default()
    {
      lock (SnowflakeId.SLock)
      {
        if (SnowflakeId._snowflakeId != null)
          return SnowflakeId._snowflakeId;
        Random random = new Random();
        int result1;
        if (!int.TryParse(Environment.GetEnvironmentVariable("CAP_WORKERID", EnvironmentVariableTarget.Machine), out result1))
          result1 = random.Next(31);
        int result2;
        if (!int.TryParse(Environment.GetEnvironmentVariable("CAP_DATACENTERID", EnvironmentVariableTarget.Machine), out result2))
          result2 = random.Next(31);
        return SnowflakeId._snowflakeId = new SnowflakeId((long) result1, (long) result2, 0L);
      }
    }

查看最终使用的构造定义:

	public SnowflakeId(long workerId, long datacenterId, long sequence = 0)
    {
      this.WorkerId = workerId;
      this.DatacenterId = datacenterId;
      this.Sequence = sequence;
      if (workerId > 31L || workerId < 0L)
        throw new ArgumentException(string.Format("worker Id can't be greater than {0} or less than 0", (object) 31L));
      if (datacenterId > 31L || datacenterId < 0L)
        throw new ArgumentException(string.Format("datacenter Id can't be greater than {0} or less than 0", (object) 31L));
    }

NextId定义：

	public virtual long NextId()
    {
      lock (this._lock)
      {
        long num = this.TimeGen();
        if (num < this._lastTimestamp)
          throw new Exception(string.Format("InvalidSystemClock: Clock moved backwards, Refusing to generate id for {0} milliseconds", (object) (this._lastTimestamp - num)));
        if (this._lastTimestamp == num)
        {
          this.Sequence = this.Sequence + 1L & 4095L;
          if (this.Sequence == 0L)
            num = this.TilNextMillis(this._lastTimestamp);
        }
        else
          this.Sequence = 0L;
        this._lastTimestamp = num;
        return num - 1288834974657L << 22 | this.DatacenterId << 17 | this.WorkerId << 12 | this.Sequence;
      }
    }

	protected virtual long TimeGen()
    {
      return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

simple explain: 使用时间戳 和 2个随机数(查看Default创建时Sequence是赋0值)

详细自行研究...

### cap在哪里保存消息到db? ###

沿着上面 查看PublishAsyncInternal的定义：

	protected async Task PublishAsyncInternal(CapPublishedMessage message)
    {
      Guid operationId = new Guid();
      try
      {
        ValueTuple<Guid, TracingHeaders> valueTuple = this.TracingBefore(message.Name, message.Content);
        operationId = valueTuple.Item1;
        message.Content = valueTuple.Item2 != null ? Helper.AddTracingHeaderProperty(message.Content, valueTuple.Item2) : message.Content;
        if (this.Transaction.Value?.DbTransaction == null)
        {
          await this.ExecuteAsync(message, (ICapTransaction) null, new CancellationToken());
          CapPublisherBase.s_diagnosticListener.WritePublishMessageStoreAfter(operationId, message, nameof (PublishAsyncInternal));
          this._dispatcher.EnqueueToPublish(message);
        }
		// 省略有事务
      }
      catch (Exception ex)
      {
        CapPublisherBase.s_diagnosticListener.WritePublishMessageStoreError(operationId, message, ex, nameof (PublishAsyncInternal));
        throw;
      }
    }

可以看到ExecuteAsync，考虑到抽象关系，我们去查看SqlServerPublisher的.ExecuteAsync的定义

	protected override async Task ExecuteAsync(
      CapPublishedMessage message,
      ICapTransaction transaction = null,
      CancellationToken cancel = default (CancellationToken))
    {
      if (transaction == null)
      {
        using (SqlConnection connection = new SqlConnection(this._options.ConnectionString))
        {
          int num = await ((IDbConnection) connection).ExecuteAsync(this.PrepareSql(), (object) message, (IDbTransaction) null, new int?(), new CommandType?());
        }
      }
      // 省略有事务
    }

    private string PrepareSql()
    {
      return "INSERT INTO " + this._options.Schema + ".[Published] ([Id],[Version],[Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName])VALUES(@Id,'" + this._options.Version + "',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
    }

看到这标准的ado.net应该就明白了，就是通过子类实现 **ExecuteAsync** 来同步db的

然后，由于到PublishAsyncInternal才会调用子类的方法，所以唯一id的生成方式是不可变的(code design.)

> MSMQ的实现原理是：消息的发送者把自己想要发送的信息放入一个容器，然后把它保存到一个系统公用空间的消息队列中，本地或异地的消息接收程序再从该队列中取出发给它的消息进行处理。



----------

since:11/7/2019 11:01:58 AM 