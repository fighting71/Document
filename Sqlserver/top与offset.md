#### 一、TOP 筛选 ####

　　如果有 ORDER BY 子句，TOP 筛选将根据排序的结果返回指定的行数。如果没有 ORDER BY 子句，TOP 筛选将按照行的物理顺序返回指定的行数。

1. 返回指定数目的行

　　TOP 用于指示从查询结果集返回指定数目的行。

　　例如，返回前2行记录

	SELECT TOP (2) ColumnA, ColumnB
	FROM Table1
　　

2. 返回指定百分比的行

　　可以使用百分比，如果遇到百分比的计算结果不是整数，将向上舍入（即“进一法”，而不是“四舍五入”或“截尾取整”）。例如，返回前10%的行：

	SELECT TOP (10) PERCENT ColumnA, ColumnB
	FROM Table

3.WITH TIES 参数

　　在与ORDER BY 子句组合使用时，有时候会出现并列排名的情况，例如，返回前10名优秀成绩的学生，可能遇到多名学生并列第10名。此时需要指定 WITH TIES，以确保并列第10名的学生都被包含到结果集中，此时的结果集可能多于10行。示例：

	SELECT TOP (10) WITH TIES ColumnA, ColumnB
	FROM Table1
	ORDER BY ColumnA DESC
　　

#### 二、OFFSET 筛选 ####

　　OFFSET 子句必须与 ORDER BY 子句组合使用，而且不可以与 TOP 同时使用。与 TOP 相比，OFFSET 即没有 PERCENT 参数，也没有 WITH TIES 参数。

1. 跳过指定的行数

　　OFFSET 子句指定在从查询表达式中开始返回行之前，将跳过的行数。OFFSET 子句的参数可以是大于或等于零的整数或表达式。ROW 和 ROWS 可以互换使用。例如：

	SELECT ColumnA, ColumnB
	 
	FROM Table1
	 
	ORDER BY ColumnA
	 
	OFFSET 10 ROWS
　　

2. 跳过指定的行数，再返回指定的行数

　　FETCH 子句不可以单独使用，必须跟在 OFFSET 子句之后。

　　FETCH 子句指定在处理 OFFSET 子句后，将返回的行数。FETCH 子句的参数可以是大于或等于 1 的整数或表达式。例如：

	SELECT ColumnA, ColumnB
	 
	FROM Table1
	 
	ORDER BY ColumnA
	 
	OFFSET 10 ROWS
	 
	FETCH NEXT 5 ROWS ONLY
　　

3. 参数互换

（1）ROW 和 ROWS 可以互换使用

　　“1 ROWS”的表述虽然 SQL Server 的语法，但是不符合英文语法，因此，ROW 和ROWS 可以互换，例如“1 ROW”。

 

（2）FIRST 和 NEXT 可以互换使用

　　遇到“OFFSET 0 ROWS”时（即不跳过任何行），“FETCH NEXT 5 ROWS ONLY”的表述看起来不太自然，因此，可以换为“FETCH FIRST 5 ROWS ONLY”。

 

4. 行数的表达式

　　行数可以使用返回整数值的任何算术、常量或参数表达式，但不可以使用标量子查询。