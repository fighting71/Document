
### 一、初始化： ###

#### 1.获取授权token ####

 在获取连接前，需要先商议协议：

	POST 服务端地址/Hub映射路由/negotiate?negotiateVersion=1&其他参数

	示例返回：

	{
		"negotiateVersion": 1, // 协议版本
		"connectionId": "BXo0tlW4HJtk6VXXVeyPYw",// 连接标识
		"connectionToken": "btTHijVlgocg4iWVLYvzuA",// 授权token
		// 可用的传输协议
		"availableTransports": [{
			"transport": "WebSockets",
			"transferFormats": ["Text", "Binary"]
		}, {
			"transport": "ServerSentEvents",
			"transferFormats": ["Text"]
		}, {
			"transport": "LongPolling",
			"transferFormats": ["Text", "Binary"]
		}]
	}

#### 2.建立连接 ####

*此处以WebSockets为例：*

	ClientWebSocket clientWebSocket = new ClientWebSocket();
	
	await clientWebSocket.ConnectAsync(new Uri($"[wss/ws]://服务端地址/Hub映射路由?id=授权token&其他参数"), CancellationToken.None);

#### 3.告知消息协议 ####

*连接后并不会直接进行通讯，还需要与服务端约定消息的传输格式、版本*

*signalr在发送消息时，都需要在结尾带上结束符，以表示每条消息的终点*

	结束符 = ""
	var msg = "{\"protocol\":\"json\",\"version\":1}" + 结束符;
	return client.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Binary, false, CancellationToken.None);

### 二、消息发送与接收 ###

ps: 在初始化时，其实已经进行了一次消息发送即**告知消息协议**

#### 消息发送 ####

siglar的消息具有特定的格式：

	 public class SendMsgInfo
    {
        /// <summary>
        /// 参数
        /// </summary>
        public object[] Arguments { get; set; }
        /// <summary>
        /// 消息的唯一id
        /// </summary>
        public string InvocationId { get; set; }
        /// <summary>
        /// 调用目标
        /// </summary>
        public string Target { get; set; }
        /// <summary>
        /// 消息类型
        /// </summary>
        public MsgType Type { get; set; }
        /// <summary>
        /// 异步流id
        /// </summary>
        public string[] StreamIds { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; }
        /// <summary>
        /// 传递结果
        /// </summary>
        public object Result { get; set; }
    }

**Arguments**

  即服务端，自定义的参数列表

**InvocationId**

  消息的唯一id,因为通讯双方使用的是一问一答的方式进行通讯，故需要有一个唯一值来进行关联

**调用目标**
  
  服务端自定义的**HubMethodName**

**消息类型**

	public enum MsgType : byte
    {
        /// <summary>
        /// 消息推送
        /// </summary>
        Notice = 1,
        /// <summary>
        /// 流推送
        /// </summary>
        StreamNotice = 2,
        /// <summary>
        /// 执行返回
        /// </summary>
        CallBack = 3,
        /// <summary>
        /// 流调用消息
        /// </summary>
        StreamCall = 4,
        /// <summary>
        /// 取消调用
        /// </summary>
        CancelStream = 5,
        /// <summary>
        /// 心跳消息
        /// </summary>
        Heartbeat = 6,
        /// <summary>
        /// 连接关闭
        /// </summary>
        Closed = 7,
    }

**异步流id**

  尚未使用

**Error**

  调用失败时的错误提示

**Result**

  回复消息的结果

#### 消息接收 ####

*接收与发送传递的结构是一致的*

	while (client.State == WebSocketState.Open)
    {
        var result = new byte[1024];

        await client.ReceiveAsync(new ArraySegment<byte>(result), new CancellationToken());// 接收数据

		// 进行截断 -> 令bytes不解析多余的空白字节
        var lastbyte = result.ByteCut(0x00);

        var str = Encoding.UTF8.GetString(lastbyte, 0, lastbyte.Length);
		
		// 根据结束符进行分割（一次可能推送多条消息）
        string[] noticeArr = str.Split(CommonConst.EndCode);
	
		// 相应的处理

	}

	// bytes截断
	public static byte[] ByteCut(this byte[] bytes, byte endByte)
    {

        for (int i = bytes.Length - 1; i > 0; i--)
        {
            if (bytes[i] != endByte)
            {
                var cutBytes = new byte[i + 1];

                Array.Copy(bytes, cutBytes, i + 1);
                return cutBytes;
            }
        }

        return Array.Empty<byte>();
    }

#### 其他 ####

**流传递**

	【客户端】发送流调用消息->【服务端】回复消息，并发送流推送（传递的InvocationId即 发送流调用的InvocationId）

 ### 参考文章 ###
[ClientWebSocket博文](https://www.cnblogs.com/yanglang/p/9695883.html)

[官方.Net 客户端](https://docs.microsoft.com/zh-cn/aspnet/core/signalr/dotnet-client?view=aspnetcore-5.0&tabs=visual-studio)


6/17/2021 11:52:51 AM 