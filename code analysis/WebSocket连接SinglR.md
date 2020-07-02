
environment : 
 .net core 3.1
 
后端配置:

	services.AddSignalR(hubOptions =>
    {
        //hubOptions.EnableDetailedErrors = false;
        //心跳检测
        //hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(55);
        //握手超时
        //hubOptions.HandshakeTimeout = TimeSpan.MaxValue;
    }).AddJsonProtocol(options =>
    {
        // 名称序列化
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

	 app.UseEndpoints(endpoints =>
    {
        endpoints.MapHub<ChatHub>("/chatHub");
    });

前端访问:

1.获取连接token:

  url : /chatHub/negotiate?negotiateVersion=1

  返回示例：

	{
	    "negotiateVersion": 1,
	    "connectionId": "DrwysYTAHCh6AFvSstoTmw",
	    "connectionToken": "1bCYObUE4UfmqQ2S5Af3PA",
	    "availableTransports": [
	        {
	            "transport": "WebSockets",
	            "transferFormats": [
	                "Text",
	                "Binary"
	            ]
	        },
	        {
	            "transport": "ServerSentEvents",
	            "transferFormats": [
	                "Text"
	            ]
	        },
	        {
	            "transport": "LongPolling",
	            "transferFormats": [
	                "Text",
	                "Binary"
	            ]
	        }
	    ]
	}

2.打开连接:[websocket方式]

  url:ws://localhost:5000/chatHub?id={connectionToken}

 2.1.告知协议，版本:

  ws.send('{"protocol":"json","version":1}');

  **''作为结束符,所有消息都需要带上(接收/发送)**

3.消息demo:

消息回执：
	
	正常返回：{"type":3,"invocationId":"0","result":{...}}

	异常返回：{"type":3,"invocationId":"1","error":"Failed to invoke \u0027SendMessage\u0027 due to an error on the server. HubException: Method does not exist."}

发送消息：{"arguments":[],"invocationId":"0","streamIds":[],"target":"{Hub中的方法名}","type":1}

消息推送：Received Message: {"type":1,"target":"ReceiverMessage","arguments":["9b9091a27ae54e59b0344c2e62270286","\u6D4B\u8BD5\u6D88\u606F","none"]}

参数说明:

> arguments:

> 传递参数，固定为数组，下标决定接收参数的顺序

> invocationId

> 执行id,对应着消息回执中的invocationId

> type 消息类型

> 1 - 消息推送(Client To Server / Server To Client)
> 3 - 消息回执

4.source code:

.net core signalR处理类： Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher<THub>

.net core signalR 对象解析： Microsoft.AspNetCore.SignalR.Protocol.HubMethodInvocationMessage

.net core signalR 简单消息处理核心：
 Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher<THub>.DispatchMessageAsync
 Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher<THub>.ProcessInvocation

.net core signalR web api 发送消息: 
1.注入IHubContext<ChatHub> context
2.发送示例：context.Clients.All.SendAsync("notice", "you have new order");