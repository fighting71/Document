
#### 计算时间差 ####

	示例：

	SqlServerDbFunctionsExtensions.DateDiffDay(null, startDate, endDate)

	类定义： Microsoft.EntityFrameworkCore.SqlServerDbFunctionsExtensions from Microsoft.EntityFrameworkCore.SqlServer.dll

注：此方法可用于ef查询表达式，且能解析成对应的Sql函数执行

#### database.ExecuteSqlRaw参数化传递 ####

	示例：
	
		ExecuteSqlRaw("SELECT * FROM tab WHERE id = @id", new SqlParameter("id", "xxx"));	

#### 踩坑注意 ####

在Select查询中，若使用其他表查询的查询结果作为列

示例：

	_users.Select(u => new { Count = _log.Count() }).xxx

由于ef会将查询转换为子查询，且此处应使用同步`Count`而非`CountAsync`

若存在此种查询,后续应避免使用此列进行`Where`筛选或是`OrderBy`,因为这样会导致使用子查询作为筛选/排序条件而非想象中的嵌套`Select`

sql说明:

	SELECT *,(SELECT COUNT(id) FROM Log) AS COUNT FROM User
	WHERE (SELECT COUNT(id) FROM Log) > ? -- 作为筛选条件
	ORDER BY (SELECT COUNT(id) FROM Log) -- 作为排序列 

	示例生成 √ 

#### join 中指定多个条件 ####

示例：

		from order in db.T_ClientWorkOrder
     join
     work in db.T_ClientWorkOrder_Work on order.ID equals work.FK_OrderId
     join
     nodeconfig in db.T_ClientWorkOrder_NodeConfig  on new { key1 = work.NodeKey, key2 = order.ProjectId } equals new { key1 = nodeconfig.NodeKey, key2 = nodeconfig.ProjectId }
	where order.OrderCode == orderCode

使用匿名类