
**当我们使用HttpClient时，HttpClient为我们做了什么？**

简单使用示例：

	HttpResponseMessage res = await client.GetAsync("https://www.baidu.com/");

	// 验证res是否请求成功、读取返回内容...

那再让我们查看源码，看看具体做了什么

a.找到方法定义：

	public Task<HttpResponseMessage> GetAsync(string requestUri)
	{
		return GetAsync(CreateUri(requestUri));
	}

从方法名可以看出requestUri是为了构建Uri，所以httpClient实际是通过Uri来确定请求目标的.

再查看下*CreateUri*的源码：

	private Uri CreateUri(string uri)
	{
		if (string.IsNullOrEmpty(uri))
		{
			return null;
		}
		return new Uri(uri, UriKind.RelativeOrAbsolute);
	}

从*RelativeOrAbsolute*可知：*requestUri*允许传入相对或绝对路径

再回到*GetAsync*，查看其实际执行的*GetAsync*

	public Task<HttpResponseMessage> GetAsync(Uri requestUri)
	{
		return GetAsync(requestUri, HttpCompletionOption.ResponseContentRead);
	}

*HttpCompletionOption* 根据![api定义](https://docs.microsoft.com/zh-cn/dotnet/api/system.net.http.httpcompletionoption?view=netcore-3.1)，其职责是指示 HttpClient 操作是在响应可利用时立即视为已完成，还是在读取包含上下文的整个答案信息之后才视为已完成。

其中 *ResponseContentRead*代表的是：操作应在读取包含内容的整个响应之后完成。

由此可见，*httpClient*请求，默认是读取整个响应内容后才算完成.

再回到上一个*GetAsync*，查看其实际执行的*GetAsync*

	public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption)
	{
		return GetAsync(requestUri, completionOption, CancellationToken.None);
	}

简单说明，由于异步具有取消功能，而默认不需要取消，则给定*CancellationToken.None*作为默认值

next==>

	public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		return SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
	}

将*uri* + *Get请求标记* 构造成 *HttpRequestMessage*，调用通用的*SendAsync*方法

由此可见不管是什么类型的请求，其通通使用*SendAsync*发起执行

查看SendAsync源码：

	public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		CheckDisposed();// 查看对象是否已释放
		CheckRequestMessage(request);// 检查请求是否已发起
		SetOperationStarted();// 修改start标记

		// 进行准备工作 ==> 检验uri是否正确/若为相对路径则根据baseAddress构建为绝对路径
		// 注：可通过BaseAddress属性设置根路径
		PrepareRequestMessage(request); 
		// 是否设置超时
		bool flag = _timeout != s_infiniteTimeout;
		bool disposeCts;
		CancellationTokenSource cancellationTokenSource;

		// 取消任务、超时验证
		if (flag || cancellationToken.CanBeCanceled)
		{
			disposeCts = true;
			cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _pendingRequestsCts.Token);
			if (flag)
			{
				cancellationTokenSource.CancelAfter(_timeout);
			}
		}
		else
		{
			disposeCts = false;
			cancellationTokenSource = _pendingRequestsCts;
		}
		Task<HttpResponseMessage> sendTask;
		try
		{
			sendTask = base.SendAsync(request, cancellationTokenSource.Token);
		}
		catch
		{
			HandleFinishSendAsyncCleanup(cancellationTokenSource, disposeCts);
			throw;
		}
		if (completionOption != 0)
		{
			return FinishSendAsyncUnbuffered(sendTask, request, cancellationTokenSource, disposeCts);
		}
		return FinishSendAsyncBuffered(sendTask, request, cancellationTokenSource, disposeCts);
	}

又调用了父类的发送请求*sendTask = base.SendAsync(request, cancellationTokenSource.Token);*

	public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		CheckDisposed();
		if (NetEventSource.IsEnabled)
		{
			NetEventSource.Enter(this, request, "SendAsync");
		}
		Task<HttpResponseMessage> task = _handler.SendAsync(request, cancellationToken);
		if (NetEventSource.IsEnabled)
		{
			NetEventSource.Exit(this, task, "SendAsync");
		}
		return task;
	}

*_handler.SendAsync(request, cancellationToken);* 又通过字段发起调用...

	public HttpClient()
		: this(new HttpClientHandler())
	{
	}

通过构造方法/反射可知其默认值为*System.Net.Http.HttpClientHandler*

再查看*HttpClientHandler*的*SendAsync*源码：

	protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (_winHttpHandler != null)
		{
			WindowsProxyUsePolicy windowsProxyUsePolicy = _winHttpHandler.WindowsProxyUsePolicy;
			if (_useProxy)
			{
				if (_winHttpHandler.Proxy == null)
				{
					if (windowsProxyUsePolicy != WindowsProxyUsePolicy.UseWinInetProxy)
					{
						_winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
					}
				}
				else if (windowsProxyUsePolicy != WindowsProxyUsePolicy.UseCustomProxy)
				{
					_winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
				}
			}
			else if (windowsProxyUsePolicy != 0)
			{
				_winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy;
			}
			if (!DiagnosticsHandler.IsEnabled())
			{
				return _winHttpHandler.SendAsync(request, cancellationToken);
			}
			return _diagnosticsHandler.SendAsync(request, cancellationToken);
		}
		if (!DiagnosticsHandler.IsEnabled())
		{
			return _socketsHttpHandler.SendAsync(request, cancellationToken);
		}
		return _diagnosticsHandler.SendAsync(request, cancellationToken);
	}

按照默认参数(反射确定)，最终执行为*_diagnosticsHandler.SendAsync(request, cancellationToken);*
*_diagnosticsHandler*为*System.Net.Http.DiagnosticsHandler*

再来看*DiagnosticsHandler*的*SendAsync*源码：

	protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		Activity activity = null;
		Guid loggingRequestId = Guid.Empty;
		if (s_diagnosticListener.IsEnabled("System.Net.Http.HttpRequestOut", request))
		{
			activity = new Activity("System.Net.Http.HttpRequestOut");
			if (s_diagnosticListener.IsEnabled("System.Net.Http.HttpRequestOut.Start"))
			{
				s_diagnosticListener.StartActivity(activity, new
				{
					Request = request
				});
			}
			else
			{
				activity.Start();
			}
		}
		if (s_diagnosticListener.IsEnabled("System.Net.Http.Request"))
		{
			long timestamp = Stopwatch.GetTimestamp();
			loggingRequestId = Guid.NewGuid();
			s_diagnosticListener.Write("System.Net.Http.Request", new
			{
				Request = request,
				LoggingRequestId = loggingRequestId,
				Timestamp = timestamp
			});
		}
		Activity current = Activity.Current;
		if (current != null && !request.Headers.Contains("Request-Id"))
		{
			request.Headers.Add("Request-Id", current.Id);
			using (IEnumerator<KeyValuePair<string, string>> enumerator = current.Baggage.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					List<string> list = new List<string>();
					do
					{
						KeyValuePair<string, string> current2 = enumerator.Current;
						list.Add(new NameValueHeaderValue(current2.Key, current2.Value).ToString());
					}
					while (enumerator.MoveNext());
					request.Headers.Add("Correlation-Context", list);
				}
			}
		}
		Task<HttpResponseMessage> responseTask = null;
		try
		{
			responseTask = base.SendAsync(request, cancellationToken);
			return await responseTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (TaskCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			if (s_diagnosticListener.IsEnabled("System.Net.Http.Exception"))
			{
				s_diagnosticListener.Write("System.Net.Http.Exception", new
				{
					Exception = exception,
					Request = request
				});
			}
			throw;
		}
		finally
		{
			if (activity != null)
			{
				DiagnosticListener diagnosticListener = s_diagnosticListener;
				Activity activity2 = activity;
				Task<HttpResponseMessage> task = responseTask;
				diagnosticListener.StopActivity(activity2, new
				{
					Response = ((task != null && task.Status == TaskStatus.RanToCompletion) ? responseTask.Result : null),
					Request = request,
					RequestTaskStatus = (responseTask?.Status ?? TaskStatus.Faulted)
				});
			}
			if (s_diagnosticListener.IsEnabled("System.Net.Http.Response"))
			{
				long timestamp2 = Stopwatch.GetTimestamp();
				DiagnosticListener diagnosticListener2 = s_diagnosticListener;
				Task<HttpResponseMessage> task2 = responseTask;
				diagnosticListener2.Write("System.Net.Http.Response", new
				{
					Response = ((task2 != null && task2.Status == TaskStatus.RanToCompletion) ? responseTask.Result : null),
					LoggingRequestId = loggingRequestId,
					TimeStamp = timestamp2,
					RequestTaskStatus = (responseTask?.Status ?? TaskStatus.Faulted)
				});
			}
		}
	}

emm... 内容太长，不知道看什么？

先看返回内容即关注点：*base.SendAsync(request, cancellationToken);*

又调用父类的*SendAsync*,再来查看父类的源码：

	protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request", SR.net_http_handler_norequest);
		}
		SetOperationStarted();
		return _innerHandler.SendAsync(request, cancellationToken);
	}

又调用了*_innerHandler*的*SendAsync*，查看*_innerHandler*定义：

	private HttpMessageHandler _innerHandler;

好，又弄一个HttpMessageHandler定义，通过反射查看其默认值：*System.Net.Http.SocketsHttpHandler*

	😒我吐了...
	发起一个请求，先是通过*HttpClient*一系列方法传递到*SendAsync*
	
	你以为这样完了？
	
	上来先给你各种代理验证，然后默认给你调用*DiagnosticsHandler*的*SendAsync*
	
	你以为这样完了？
	
	再给你*DiagnosticsHandler*里面弄个*SocketsHttpHandler*，再通过父类通用方法给你调用*SocketsHttpHandler*的*SendAsync*
	
	而且在*HttpClient*的*SendAsync*中，如果不调用*DiagnosticsHandler*下一个就会调用*SocketsHttpHandler*，默认方法弄这么混乱真的好吗...
	
	怕了怕了，里面还包含一系列验证，一系列构造... 看个代码七大爷八大妈的真是太惨了...
	
	我是谁，我在哪里...

看都看了，继续看*SocketsHttpHandler*的*SendAsync*：

	protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		CheckDisposed();
		HttpMessageHandler httpMessageHandler = _handler ?? SetupHandlerChain();
		Exception ex = ValidateAndNormalizeRequest(request);
		if (ex != null)
		{
			return Task.FromException<HttpResponseMessage>(ex);
		}
		return httpMessageHandler.SendAsync(request, cancellationToken);
	}

好，又一个*HttpMessageHandler*😒

查看*SocketsHttpHandler*的*SetupHandlerChain*源码：

	private HttpMessageHandler SetupHandlerChain()
	{
		HttpConnectionSettings httpConnectionSettings = _settings.Clone();
		HttpConnectionPoolManager poolManager = new HttpConnectionPoolManager(httpConnectionSettings);
		HttpMessageHandler httpMessageHandler = (httpConnectionSettings._credentials != null) ? ((HttpMessageHandler)new HttpAuthenticatedConnectionHandler(poolManager)) : ((HttpMessageHandler)new HttpConnectionHandler(poolManager));
		if (httpConnectionSettings._allowAutoRedirect)
		{
			HttpMessageHandler redirectInnerHandler = (httpConnectionSettings._credentials == null || httpConnectionSettings._credentials is CredentialCache) ? httpMessageHandler : new HttpConnectionHandler(poolManager);
			httpMessageHandler = new RedirectHandler(httpConnectionSettings._maxAutomaticRedirections, httpMessageHandler, redirectInnerHandler);
		}
		if (httpConnectionSettings._automaticDecompression != 0)
		{
			httpMessageHandler = new DecompressionHandler(httpConnectionSettings._automaticDecompression, httpMessageHandler);
		}
		if (Interlocked.CompareExchange(ref _handler, httpMessageHandler, null) != null)
		{
			httpMessageHandler.Dispose();
		}
		return _handler;
	}

key: 

	HttpMessageHandler httpMessageHandler = (httpConnectionSettings._credentials != null) ? ((HttpMessageHandler)new HttpAuthenticatedConnectionHandler(poolManager)) : ((HttpMessageHandler)new HttpConnectionHandler(poolManager));

好样的，判断给两个*HttpMessageHandler*返回😒

通过源码分析默认无证书，即默认返回*HttpConnectionHandler*

查看*HttpConnectionHandler*的*SendAsync*源码：

	protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return _poolManager.SendAsync(request, doRequestAuth: false, cancellationToken);
	}

又调用*_poolManager*的*SendAsync*😒[真是不绕死人不罢休]

回到上个源码中的

	HttpConnectionPoolManager poolManager = new HttpConnectionPoolManager(httpConnectionSettings);

	你都在这创建了HttpConnectionPoolManager了，干嘛不直接执行？？？

	而且HttpAuthenticatedConnectionHandler和HttpConnectionHandler两个执行的区别只有doRequestAuth值不一样，何必呢？？？

查看*HttpConnectionPoolManager*的*SendAsync*源码：

	public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (_proxy == null)
		{
			return SendAsyncCore(request, null, doRequestAuth, isProxyConnect: false, cancellationToken);
		}
		Uri uri = null;
		try
		{
			if (!_proxy.IsBypassed(request.RequestUri))
			{
				uri = _proxy.GetProxy(request.RequestUri);
			}
		}
		catch (Exception ex)
		{
			if (NetEventSource.IsEnabled)
			{
				NetEventSource.Error(this, $"Exception from IWebProxy.GetProxy({request.RequestUri}): {ex}", "SendAsync");
			}
		}
		if (uri != null && uri.Scheme != "http")
		{
			throw new NotSupportedException(SR.net_http_invalid_proxy_scheme);
		}
		return SendAsyncCore(request, uri, doRequestAuth, isProxyConnect: false, cancellationToken);
	}

按照无代理模式(常用的也是这种)，直接调用*SendAsyncCore*，查看源码：

	public Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, Uri proxyUri, bool doRequestAuth, bool isProxyConnect, CancellationToken cancellationToken)
	{
		HttpConnectionKey connectionKey = GetConnectionKey(request, proxyUri, isProxyConnect);
		HttpConnectionPool value;
		while (!_pools.TryGetValue(connectionKey, out value))
		{
			bool flag = connectionKey.Host != null && request.RequestUri.HostNameType == UriHostNameType.IPv6;
			value = new HttpConnectionPool(this, connectionKey.Kind, flag ? ("[" + connectionKey.Host + "]") : connectionKey.Host, connectionKey.Port, connectionKey.SslHostName, connectionKey.ProxyUri, _maxConnectionsPerServer);
			if (_cleaningTimer == null)
			{
				break;
			}
			if (_pools.TryAdd(connectionKey, value))
			{
				lock (SyncObj)
				{
					if (!_timerIsRunning)
					{
						SetCleaningTimer(_cleanPoolTimeout);
					}
				}
				break;
			}
		}
		return value.SendAsync(request, doRequestAuth, cancellationToken);
	}

又一个*HttpConnectionPool*获取，啥时候到头...,不过这里可以理解下，因为是一个连接池构建，还是有一定的意义，而之前的纯属就是繁杂的验证...

*HttpConnectionPool.SendAsync*:

	public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (doRequestAuth && Settings._credentials != null)
		{
			return AuthenticationHelper.SendWithRequestAuthAsync(request, Settings._credentials, Settings._preAuthenticate, this, cancellationToken);
		}
		return SendWithProxyAuthAsync(request, doRequestAuth, cancellationToken);
	}

默认无证书，直接到*HttpConnectionPool.SendWithProxyAuthAsync*

	public Task<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if ((_kind == HttpConnectionKind.Proxy || _kind == HttpConnectionKind.ProxyConnect) && _poolManager.ProxyCredentials != null)
		{
			return AuthenticationHelper.SendWithProxyAuthAsync(request, _proxyUri, _poolManager.ProxyCredentials, doRequestAuth, this, cancellationToken);
		}
		return SendWithRetryAsync(request, doRequestAuth, cancellationToken);
	}

默认无代理，再到*HttpConnectionPool.SendWithRetryAsync*

	public async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
	{
		HttpResponseMessage item;
		while (true)
		{
			(HttpConnection, HttpResponseMessage) valueTuple = await GetConnectionAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			HttpConnection connection = valueTuple.Item1;
			item = valueTuple.Item2;
			if (item != null)
			{
				break;
			}
			bool isNewConnection = connection.IsNewConnection;
			connection.Acquire();
			try
			{
				return await SendWithNtConnectionAuthAsync(connection, request, doRequestAuth, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (HttpRequestException ex) when (!isNewConnection && ex.InnerException is IOException && connection.CanRetry)
			{
			}
			finally
			{
				connection.Release();
			}
		}
		return item;
	}
