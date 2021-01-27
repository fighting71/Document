#### 相关文档 ####

[官方文档](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html)

#### 食用方法 ####

1.创建自定义Filter类型

	public class CustomerFilter : JobFilterAttribute,
	    IClientFilter, IServerFilter, IElectStateFilter, IApplyStateFilter, IServerExceptionFilter
	{
		// 实现对应的方法完成拦截
	}

**不必实现所有接口，按需继承即可**

**JobFilterAttribute**： 若只想在某些job上进行拦截 可以用特性的方法添加拦截 *未实践*

**IServerExceptionFilter**: 异常拦截 *已实践*

2.添加Filter

  a. 使用扩展方法添加

	Hangfire.GlobalConfigurationExtensions.UseFilter

	public static IGlobalConfiguration<TFilter> UseFilter<TFilter>([NotNull] this IGlobalConfiguration configuration, [NotNull] TFilter filter)
	{
		if (configuration == null)
		{
			throw new ArgumentNullException("configuration");
		}
		if (filter == null)
		{
			throw new ArgumentNullException("filter");
		}
		return configuration.Use(filter, delegate(TFilter x)
		{
			GlobalJobFilters.Filters.Add(x);
		});
	}

  b.直接添加

	GlobalJobFilters.Filters.Add(x);

	public static JobFilterCollection Filters
	{
		get;
	}

	public class JobFilterCollection : IJobFilterProvider, IEnumerable<JobFilter>, IEnumerable
	{
		public void Add(object filter)
		{
			AddInternal(filter, null);
		}
	}

*

实话实说，这添加方式我直呼好家伙，直接object...,若不是有个示例类我还不知道怎么添加

至于**IServerExceptionFilter**也是慢慢翻出来的...

*

[Over]