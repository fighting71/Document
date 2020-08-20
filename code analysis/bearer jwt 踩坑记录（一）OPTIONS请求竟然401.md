### 前言 ###

  最近一直使用jwt来做授权认证，配合前端请求也很方便... 

  突然有一天，项目需要加一个即时通讯的功能，加就加吧，反正用.net core 集成的signalR也容易上手。

  由于聊天需要进行授权(根据身份分配连接)，所以给**Hub**加上了**Authorize**特性

  由于signalR是先获取连接token，再连接ws/wss的，所以要先请求 `hub/negotiate?negotiateVersion=1` ,这一请求，问题就来了。   [signalR请求处理映射核心类：`Microsoft.AspNetCore.Builder.ConnectionEndpointRouteBuilderExtensions`]

  ps: 众所周知，在跨域时请求api时，浏览器会默认先发送options

  options请求时竟然出现401 导致api无法访问。

#### 原因排查 ####

首先，先确认了其他授权api并未出现此种情况，于是便从日志下手，

正常返回时:

	AuthenticationScheme: Bearer was not authenticated.

bug 401返回时：

	AuthenticationScheme: Bearer was not authenticated.

	AuthenticationScheme: Bearer was challenged.


多了一个`was challenged` ?,先找到处理类：`Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler`,通过断点分析是进入了`HandleChallengeAsync`方法

	protected override async Task HandleChallengeAsync(AuthenticationProperties properties)

此类是处理类，那只能找调用方来查找问题了

#### 是谁调用了HandleChallengeAsync？ ####



#### 解决方案 ####

old code : 

    app.UseRouting();

    //启用JWT鉴权
    app.UseAuthentication();

    //启用JWT授权
    app.UseAuthorization();

    app.UseCors(CorPolicy);

    app.UseEndpoints(endpoints =>
    {
		...
    });

fix code :

    app.UseRouting();

    app.UseCors(CorPolicy);

    //启用JWT鉴权
    app.UseAuthentication();

    //启用JWT授权
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
		...
    });


把授权、鉴权放在`app.UseCors(CorPolicy);`就Ok了 ... 

关键code:

	<Microsoft.AspNetCore.Cors.Infrastructure.CorsMiddleware>

	private Task EvaluateAndApplyPolicy(HttpContext context, CorsPolicy corsPolicy)
	{
		if (corsPolicy == null)
		{
			Logger.NoCorsPolicyFound();
			return _next(context);
		}
		CorsResult corsResult = CorsService.EvaluatePolicy(context, corsPolicy);

		// 简单描述->当请求为Options,且请求头中带有Access-Control-Request-Method则为true
		if (corsResult.IsPreflightRequest) <--- 此处为true直接返回
		{
			CorsService.ApplyResult(corsResult, context.Response);
			context.Response.StatusCode = 204;
			return Task.CompletedTask;
		}
		context.Response.OnStarting(OnResponseStartingDelegate, Tuple.Create(this, context, corsResult));
		return _next(context);
	}

因为`app.UseCors(CorPolicy);`中的中间件有此检测，然后由于注册顺序决定管道处理顺序，从而在授权之前返回了204 *破案了，又一个小细节.*

#### 参考文章 ####

[IdentityServer4之Jwt身份验证方案分析](https://my.oschina.net/u/4261771/blog/3399899/print)