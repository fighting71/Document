
### 方案一、指定由数据库默认值构建（类似于标识列） ###

在属性上增加特性即可：

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]

注：此值与自增id类似，需要提交更改后才能获取此值

	
其次，标注后，此属性只会取默认值（指定值无效）

### 方案二、配置时指定默认值 ###

	// 由于默认值非常量值，需要使用 HasDefaultValueSql 而非 HasDefaultValue ...
    .HasDefaultValueSql("getdate()")


此种方式，仅在未赋值的情况下取默认值