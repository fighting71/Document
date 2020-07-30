**前言**

 作为[IdentityServer4](https://github.com/IdentityServer/IdentityServer4)的使用者，我觉得有必要了解一下其底层的实现，不能只知道使用而不知其所以然。

那怎么去找想要知道的内容呢? 

- 是去github把项目下下来慢慢看、慢慢调。 - 最简单，但需要对整体有一定的认知，不然开头比较慢 *开源大法好！*
- 通过ILSpy查看源码，有目的性的去搜索 - 最快速
- other ... 

**主题**

1. id4是如何校验的
2. id4校验后的Token是如何生成的


ok,首先来看一下入口的实现：

	public static IIdentityServerBuilder AddIdentityServer(this IServiceCollection services)
	{
		IIdentityServerBuilder identityServerBuilder = services.AddIdentityServerBuilder();
		identityServerBuilder.AddRequiredPlatformServices().AddCookieAuthentication().AddCoreServices()
			.AddDefaultEndpoints()
			.AddPluggableServices()
			.AddValidators()
			.AddResponseGenerators()
			.AddDefaultSecretParsers()
			.AddDefaultSecretValidators();
		identityServerBuilder.AddInMemoryPersistedGrants();
		return identityServerBuilder;
	}

这么多注入，先看哪个？ 不知道的话建议先看看每个单词的意思(反正我是这么干的.)

Pluggable - 可插的

IdentityServer4既然这么强大，验证和输出的结果适应范围广，那么它的验证和生成应该是可插的/可替换的(不是就看下一个...)

	public static IIdentityServerBuilder AddPluggableServices(this IIdentityServerBuilder builder)
	{

		*这么多注入，应该是了*
		builder.Services.TryAddTransient<IPersistedGrantService, DefaultPersistedGrantService>();
		builder.Services.TryAddTransient<IKeyMaterialService, DefaultKeyMaterialService>();
		builder.Services.TryAddTransient<ITokenService, DefaultTokenService>();
		builder.Services.TryAddTransient<ITokenCreationService, DefaultTokenCreationService>();
		builder.Services.TryAddTransient<IClaimsService, DefaultClaimsService>();
		builder.Services.TryAddTransient<IRefreshTokenService, DefaultRefreshTokenService>();
		builder.Services.TryAddTransient<IDeviceFlowCodeService, DefaultDeviceFlowCodeService>();
		builder.Services.TryAddTransient<IConsentService, DefaultConsentService>();
		builder.Services.TryAddTransient<ICorsPolicyService, DefaultCorsPolicyService>();
		builder.Services.TryAddTransient<IProfileService, DefaultProfileService>();
		builder.Services.TryAddTransient<IConsentMessageStore, ConsentMessageStore>();
		builder.Services.TryAddTransient<IMessageStore<LogoutMessage>, ProtectedDataMessageStore<LogoutMessage>>();
		builder.Services.TryAddTransient<IMessageStore<LogoutNotificationContext>, ProtectedDataMessageStore<LogoutNotificationContext>>();
		builder.Services.TryAddTransient<IMessageStore<ErrorMessage>, ProtectedDataMessageStore<ErrorMessage>>();
		builder.Services.TryAddTransient<IIdentityServerInteractionService, DefaultIdentityServerInteractionService>();
		builder.Services.TryAddTransient<IDeviceFlowInteractionService, DefaultDeviceFlowInteractionService>();
		builder.Services.TryAddTransient<IAuthorizationCodeStore, DefaultAuthorizationCodeStore>();
		builder.Services.TryAddTransient<IRefreshTokenStore, DefaultRefreshTokenStore>();
		builder.Services.TryAddTransient<IReferenceTokenStore, DefaultReferenceTokenStore>();
		builder.Services.TryAddTransient<IUserConsentStore, DefaultUserConsentStore>();
		builder.Services.TryAddTransient<IHandleGenerationService, DefaultHandleGenerationService>();
		builder.Services.TryAddTransient<IPersistentGrantSerializer, PersistentGrantSerializer>();
		builder.Services.TryAddTransient<IEventService, DefaultEventService>();
		builder.Services.TryAddTransient<IEventSink, DefaultEventSink>();
		builder.Services.TryAddTransient<IUserCodeService, DefaultUserCodeService>();
		builder.Services.TryAddTransient<IUserCodeGenerator, NumericUserCodeGenerator>();
		builder.Services.TryAddTransient<ILogoutNotificationService, LogoutNotificationService>();
		builder.Services.TryAddTransient<IBackChannelLogoutService, DefaultBackChannelLogoutService>();
		builder.Services.TryAddTransient<IResourceValidator, DefaultResourceValidator>();
		builder.Services.TryAddTransient<IScopeParser, DefaultScopeParser>();
		builder.AddJwtRequestUriHttpClient();
		builder.AddBackChannelLogoutHttpClient();
		builder.Services.AddTransient<IClientSecretValidator, ClientSecretValidator>();
		builder.Services.AddTransient<IApiSecretValidator, ApiSecretValidator>();
		builder.Services.TryAddTransient<IDeviceFlowThrottlingService, DistributedDeviceFlowThrottlingService>();
		builder.Services.AddDistributedMemoryCache();
		return builder;
	}

这里说明一下**TryAddTransient**，这个方法是如果容器已存在注入就不继续注入了

### 验证 ###

首先找验证实现**IdentityServer4.Validation.IResourceValidator**

	builder.Services.TryAddTransient<IResourceValidator, DefaultResourceValidator>();

**IdentityServer4.Validation.DefaultResourceValidator.ValidateRequestedResourcesAsync**

	public virtual async Task<ResourceValidationResult> ValidateRequestedResourcesAsync(ResourceValidationRequest request)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request");
		}
		ParsedScopesResult parsedScopesResult = _scopeParser.ParseScopeValues(request.Scopes);
		ResourceValidationResult result = new ResourceValidationResult();
		if (!parsedScopesResult.Succeeded)
		{
			foreach (ParsedScopeValidationError error in parsedScopesResult.Errors)
			{
				_logger.LogError("Invalid parsed scope {scope}, message: {error}", error.RawValue, error.Error);
				result.InvalidScopes.Add(error.RawValue);
			}
			return result;
		}
		string[] scopeNames = parsedScopesResult.ParsedScopes.Select((ParsedScopeValue x) => x.ParsedName).Distinct().ToArray();
		Resources resourcesFromStore = await _store.FindEnabledResourcesByScopeAsync(scopeNames);
		foreach (ParsedScopeValue parsedScope in parsedScopesResult.ParsedScopes)
		{
			await ValidateScopeAsync(request.Client, resourcesFromStore, parsedScope, result);
		}
		if (result.InvalidScopes.Count > 0)
		{
			result.Resources.IdentityResources.Clear();
			result.Resources.ApiResources.Clear();
			result.Resources.ApiScopes.Clear();
			result.ParsedScopes.Clear();
		}
		return result;
	}

### 生成 ###

**IdentityServer4.Services.DefaultTokenService**