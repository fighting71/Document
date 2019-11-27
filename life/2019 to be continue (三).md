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