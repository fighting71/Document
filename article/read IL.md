
### runtime managed 与 cil managed  ###

- runtime managed 意味着完全不可推导

### sequential vs auto ###

	.class nested public sequential ansi sealed beforefieldinit b
		extends [mscorlib]System.ValueType

	.class nested public auto ansi beforefieldinit a
		extends [mscorlib]System.Object

- sequential 意味着可推导

### 回调函数/委托/事件 ###

- event是一个特殊的类型 (在IL中 类似于 class method...)

- event是对于delegate一个特殊的封装(效率低)

- 当委托仅绑定一个指定的函数时，它就是一个函数回调

- 回调函数--*封装*-->委托--*封装*-->事件

委托/事件的多播:

 **坑**

> 使用匿名函数时无法指定解绑，从而导致无法控制顺序

> 若中间出行异常，会导致后面的调用无法进行，且难以解决。

### hidebysig ###

允许隐藏，即允许 new/override 覆盖原有方法

涉及：

- 函数的隐藏

 **virtual/abstract的作用**

> 实际上 函数的调用即为指针函数的调用 当使用 virtual/abstract 时，方法仅指向一个指针

> 使用override时，实际上是覆盖了父类的函数指针指向,而使用new时 方法会指向多个指针(即父类有一个函数指向，子类有一个函数指向 ==> 存在多个函数指针)