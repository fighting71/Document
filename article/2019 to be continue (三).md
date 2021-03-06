## 异常分类以及处理机制 ##

参考《More Effective C#》 

尽量不要在共用方法中进行try-catch,避免抛出异常被掩盖。提供检查验证方法，在验证通过的情况下 保证方法失败率最小。

## wait和sleep的区别 ##

> sleep和wait都是使线程暂时停止执行的方法，但它们有很大的不同。
> 
> 1. sleep是线程类Thread 的方法，它是使当前线程暂时睡眠，可以放在任何位置。
> 
> 而wait，它是使当前线程暂时放弃对象的使用权进行等待，必须放在同步方法或同步块里。
> 2.Sleep使用的时候，线程并不会放弃对象的使用权，即不会释放对象锁，所以在同步方法或同步块中使用sleep，一个线程访问时，其他的线程也是无法访问的。
> 
> 而wait是会释放对象锁的，就是当前线程放弃对象的使用权，让其他的线程可以访问。
> 
> 3.线程执行wait方法时，需要其他线程调用Monitor.Pulse()或者Monitor.PulseAll()进行唤醒或者说是通知等待的队列。
> 
> 而sleep只是暂时休眠一定时间，时间到了之后，自动恢复运行，不需另外的线程唤醒.

[https://blog.csdn.net/zhuoyue008/article/details/53382194](https://blog.csdn.net/zhuoyue008/article/details/53382194 "参考")

## 数组在内存中如何分配 ##

1、简单的值类型的数组，每个数组成员是一个引用（指针），引用到栈上的空间（因为值类型变量的内存分配在栈上）

2、引用类型，类类型的数组，每个数组成员仍是一个引用（指针），引用到堆上的空间（因为类的实例的内存分配在堆上）

### 初始化数组 ###

第一步：栈存储局部变量（在方法定义中或方法声明上的变量），所以int[] arr 存放在了栈中；
第二步：new出的变量放在堆中，所以new int【3】在堆中。
第三步：每一个new出来的东西都有地址值（系统随机分配），所以new int【3】的地址值为0x001;
把0x001赋给arr，在栈中的数组通过地址值找到堆中相应的地址。用数组名和编号的配合就可以找 到数组中指定编号的元素，这种方法就叫做索引。
第四步：int类型的数据默认值为0
第五步：给数组中的每个元素赋值，把原先的默认值干掉。
第六步：逐个输出相应的值

[https://blog.csdn.net/lcl19970203/article/details/54428358](https://blog.csdn.net/lcl19970203/article/details/54428358 "参考文章")