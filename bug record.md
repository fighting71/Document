# bug记录 #

## dapper一对多查询bug ##

### environment ###

.net core 3.0

dapper.dll 2.0.30

db:sqlserver

### 问题描述 ###

	connection.QueryAsync<QueryRes, ExtraService, QueryRes>(sql, (res, ext) =>
    {...省略},splitOn: "OrderID,ExtraServiceHistoryID");

调试时ext只能取得主键值 其他列没有取到值

### reason ###

因为第二个数据的列在第二个表主键之前

### fix ###

将列值放在主键之后就ok了。

### sample ###

	SELECT a.Id,a.Name,**b.Tel,b.Id**

to ==>

	SELECT a.Id,a.Name,**b.Id,b.Tel**