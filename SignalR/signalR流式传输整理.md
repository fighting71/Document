### 前言 ###

#### 一、简介 ####

[官方介绍](https://docs.microsoft.com/zh-cn/aspnet/core/signalr/streaming?view=aspnetcore-3.1)

注：

  在**.net core 2.1**时:ASP.NET Core SignalR 支持服务器方法的流返回值。[未检测是否是仅支持]

  而在**.net core 3.0+**时:ASP.NET Core SignalR 支持从客户端到服务器以及从服务器到客户端的流式传输

故为方便测试，需要升级到3.0+

#### 二、signalR.js的下载 ####

[官方示例](https://docs.microsoft.com/zh-cn/aspnet/core/tutorials/signalr?tabs=visual-studio&view=aspnetcore-3.1#add-the-no-locsignalr-client-library)

#### 三、服务器到客户端流式处理 ####

官方示例:
	public class AsyncEnumerableHub : Hub
	{
	    public async IAsyncEnumerable<int> Counter(
	        int count,
	        int delay,
	        [EnumeratorCancellation]
	        CancellationToken cancellationToken)
	    {
	        for (var i = 0; i < count; i++)
	        {
	            // Check the cancellation token regularly so that the server will stop
	            // producing items if the client disconnects.
	            cancellationToken.ThrowIfCancellationRequested();
	
	            yield return i;
	
	            // Use the cancellationToken in other APIs that accept cancellation
	            // tokens so the cancellation can flow down to them.
	            await Task.Delay(delay, cancellationToken);
	        }
	    }
	}

core 2.1示例:

	public ChannelReader<int> DelayCounter(int delay, CancellationToken cancellationToken)
    {

        Channel<int> channel = Channel.CreateUnbounded<int>();

        _ = WriteItems(channel, 20, delay);

        return channel.Reader;
    }

    private async Task WriteItems(Channel<int> channel, int count, int delay)
    {
        for (var i = 0; i < count; i++)
        {
            await channel.Writer.WriteAsync(i);
            await Task.Delay(delay);
        }

        channel.Writer.TryComplete();
    }

比较说明:

- 3.0+直接通过异步封装，无需关注其他对象(源码待查看)，而2.1则是借助了 **Channel<T>**来进行传递，相对而言复杂度更高
- 当连接断开时，**Channel<T>**会继续执行无法停止。

通过比较可知3.0+与2.1还是差别非常大的.

js socket 具体产生的消息:

↑ {"arguments":[500],"invocationId":"1","streamIds":[],"target":"DelayCounter","type":4}
↓ {"type":2,"invocationId":"1","item":0}
↓ {"type":2,"invocationId":"1","item":1}
...
↓ {"type":3,"invocationId":"1"}

#### 四、客户端到服务端的流式传输 ####

官方示例:

	public async Task UploadStream(ChannelReader<string> stream)
    {
        while (await stream.WaitToReadAsync())
        {
            while (stream.TryRead(out var item))
            {
                // do something with the stream item
                Console.WriteLine(item);
            }
        }
    }

js socket 具体产生的消息:

↑ {"arguments":[],"streamIds":["0"],"target":"UploadStream","type":1}
↑ {"invocationId":"0","item":"1","type":2}
↑ {"invocationId":"0","item":"2","type":2}
...
↑ {"invocationId":"0","type":3}

#### 五、补充 ####

**MessageType**

1. 调用消息
2. Stream Item , 流式传输的每个item
3. 完成消息-调用完成/流传输完成
4. 流调用消息
5. 取消调用消息

		取消调用时只需要传递 invocationId(当时获取流的id) 和 type(5) 即可取消，==> 关闭页面会自动取消

6. ping 心跳
7. 连接关闭消息


↑：客户端 To 服务端
↓: 服务端 To 客户端