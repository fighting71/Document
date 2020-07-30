### 辅助工具 ###

 日志追踪包 : [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore)

 源码查看工具 : [ILSpy](https://github.com/icsharpcode/ILSpy)

### 项目环境 ###:

 ASP.NetCore 3.1

### 主题内容 ###

测试登录方式 : password

错误内容:

 connect/token 登陆出错

但百度/google网上的示例没有找到正确的调用方式，无奈只能自己动手，丰衣足食...

首先，先按照之前版本进行传参

- POST请求
- url: connect/token
- 参数传递通过 form-data

调用结果: 
	
	<调用结果>
	HTTP : 400

	{
	    "error": "invalid_request"
	}

查找调用日志

	Invoking IdentityServer endpoint: IdentityServer4.Endpoints.TokenEndpoint for /connect/token

可以看到地址匹配是成功的，那就是校验不通过了，接着看一下这个类的源码，找到错误触发地

	public async Task<IEndpointResult> ProcessAsync(HttpContext context)
	{
		if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasApplicationFormContentType())
		{
			return Error("invalid_request");
		}
	}

此处有两个验证:

1. POST请求 √
2. HasApplicationFormContentType ？

查看方法定义:

	internal static bool HasApplicationFormContentType(this HttpRequest request)
	{
		if (request.ContentType == null)
		{
			return false;
		}
		if (MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue parsedValue))
		{
			return parsedValue.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

... 好样的，最新版改用了**application/x-www-form-urlencoded**传参

然后改一下传参方式接着调用:

	<调用结果>
	HTTP : 400

	{
	    "error": "invalid_scope"
	}

换了个错误，至少说明传参改动还是有效的...

查找操作日志：

	IdentityServer4.Validation.TokenRequestValidator
	No scopes found in request

key code:

	string text = parameters.Get("scope");
	
	if (text.IsMissing())
	{
		text = clientAllowedScopes.Distinct().ToSpaceSeparatedString();
	}
	List<string> requestedScopes = text.ParseScopesString();
	if (requestedScopes == null)
	{
		LogError("No scopes found in request");
		return false;
	}

scope取值:

 如果没有传值，就去client中取
 
	public static List<string> ParseScopesString(this string scopes)
	{
		if (scopes.IsMissing())
		{
			return null;
		}
		scopes = scopes.Trim();
		List<string> list = scopes.Split(new char[1]
		{
			' '
		}, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
		if (list.Any())
		{
			list.Sort();
			return list;
		}
		return null;
	}

 如果有传值 就用传值的获取，多个scope用' '分隔

在配置中，是有AllowedScopes配置，但取不出来？ 先不管，用传值方式先试试...

	<调用结果>
	HTTP : 400

	{
	    "error": "invalid_scope"
	}

*???* 再查下日志:

	IdentityServer4.Validation.DefaultResourceValidator

	Scope api1 not found in store.

还好错误变了，不然没得玩了...

先确认配置信息:

	return new List<ApiResource>
    {
        new ApiResource("api1", "My API")
    };

是有这个api1的，好吧，只能再去查看源码了

### DefaultResourceValidator Scope验证分析 ###

	IdentityResource identity = resourcesFromStore.FindIdentityResourcesByScope(requestedScope.ParsedName);
	if (identity != null)
	{
		if (await IsClientAllowedIdentityResourceAsync(client, identity))
		{
			result.ParsedScopes.Add(requestedScope);
			result.Resources.IdentityResources.Add(identity);
		}
		else
		{
			result.InvalidScopes.Add(requestedScope.RawValue);
		}
		return;
	}

首先先通过name去拿**IdentityResource**，拿到了直接通过

	ApiScope apiScope = resourcesFromStore.FindApiScope(requestedScope.ParsedName);

否则再去拿一次ApiScope,验证结果即为最终结果


把**IdentityResource**的**Name**值配置到**Client**中的**AllowedScopes**去，然后再在传参里使用**IdentityResource**的**Name**值

	<调用结果>
	{
	    "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjUyM0EzMjE0Q0M4MzAzQjJBRDA2Mzk5N0E2RDI1NDEyIiwidHlwIjoiYXQrand0In0.eyJuYmYiOjE1OTQxMTAwNjcsImV4cCI6MTU5NDExMzY2NywiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAwIiwiY2xpZW50X2lkIjoicm8uY2xpZW50Iiwic3ViIjoiMiIsImF1dGhfdGltZSI6MTU5NDExMDA2NywiaWRwIjoibG9jYWwiLCJqdGkiOiJCMEY1NEI2NDI1QzUwRDU2REVEMjBGQUY0QkMwNTE5MiIsImlhdCI6MTU5NDExMDA2Nywic2NvcGUiOlsib3BlbmlkIl0sImFtciI6WyJwd2QiXX0.PqYMwqHfZ3CHtE8q_eCi5H1FVCPKe01uSPiSTNjV0q1m61s98OQezo9M3FCc4bGTw4c6VruylSQAbtT6sid2nYXV05Eq_fD_4KKPTya6NLuTdwgdUohzNN10f3SC0ea1nDhv_94Ewkov_9OWrCSLxAX9yVFKDDs6dB3V53_49n4-3Hd9BkCOevWk-_FzpkMOOhYMi-5LNeZRAXH3G5_GZ7INtypCUx2f0_v84UzQxx2LjcovzAy0ZR3GgFvAh5rgRwd5oBVeiLZOt2ZjvV0b5NAtPSbiEcufFK5box6qm_q2M6GrrMBUm0aTTTd3Vu6Zx-pjjITHQN934EICRKWYFg",
	    "expires_in": 3600,
	    "token_type": "Bearer",
	    "scope": "openid"
	}

over... 总算通了


----------

### 总结 ###

1. 传参需要使用 **application/x-www-form-urlencoded**
2. 配置**Client**的**AllowedScopes**值时要使用**IdentityResource**的**Name**值，或者配置ApiScope
3. 传参传**scope**时，通过制指定**scope**进行验证,未传时通过配置的信息(**AllowedScopes**)进行验证

### 新版的ApiResource作用 ###

	待补充...

### 配置参考 ###

	public static class Config
    {
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "alice",
                    Password = "password"
                },
                new TestUser
                {
                    SubjectId = "2",
                    Username = "bob",
                    Password = "password"
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new IdentityResource[]
            {
                new IdentityResources.OpenId()
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = "client",

                    // no interactive user, use the clientid/secret for authentication
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    // scopes that client has access to
                    AllowedScopes = { "api1" }
                },
                // resource owner password grant client
                new Client
                {
                    ClientId = "ro.client",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = { "openid" }
                }
            };
        }
    }

	public void ConfigureServices(IServiceCollection services)
    {

        var builder = services.AddIdentityServer()
            .AddInMemoryIdentityResources(Config.GetIdentityResources())
            .AddInMemoryClients(Config.GetClients())
            .AddTestUsers(Config.GetUsers());

        builder.AddDeveloperSigningCredential();

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseIdentityServer();

    }
