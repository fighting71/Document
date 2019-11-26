
11/26/2019 2:28:52 PM 

前言:刚看完《More_Effective_C#中文版》，能够引发思考，但深度并不高，基本上就是说一个点，介绍一些相关的示例，简单说下反例的缺点什么的... 当做了解了。

 但空闲时间有点多，就去做plan了。在搜内存泄露的参考时发现了[https://www.cnblogs.com/novaCN/p/10328380.html](https://www.cnblogs.com/novaCN/p/10328380.html "参考博文")

 内容不多，但都是我羡慕的内容... 便以博文lz为目标，参考这篇博文开始了这篇study&summary


----------

## List和Set的区别 ##

以经常使用的泛型类进行说明:List<THashSet<T>

从使用上来说，仅List支持下标操作，仅Set不支持添加重复值

### Set是否有序？ ###

List不用说了，本身就是有序集合。那set是否有序呢？

1. 查看<GetEnumerator>定义：

	public HashSet<T>.Enumerator GetEnumerator()
	{
		return new HashSet<T>.Enumerator(this);
	}

返回了一个内部类实例，查看此类的定义：

	public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator

即Enumerator是一个实现了IEnumerator遍历的类,查看**List.GetEnumerator**使用的构造和此内部类的MoveNext

	internal Enumerator(HashSet<Tset)
	{
		this.set = set;
		this.index = 0;
		this.version = set.m_version;
		this.current = default(T);
	}

构造仅是正常赋值，此处的version是用来校验遍历时数据是否修改(list和set一般都有的检查，略过...)

	public bool MoveNext()
	{
		if (this.version != this.set.m_version)// 版本效验
		{
			throw new InvalidOperationException(SR.GetString("InvalidOperation_EnumFailedVersion"));
		}
		//  startIndex->lastIndex 有点类似list
		while (this.index < this.set.m_lastIndex)
		{
			// set 此处有hashCode验证 
			if (this.set.m_slots[this.index].hashCode >= 0)
			{
				// 但最终的值还是通过下标访问的
				this.current = this.set.m_slots[this.index].value;
				this.index++;
				return true;
			}
			this.index++;// 下标加一类似list
		}
		this.index = this.set.m_lastIndex + 1;
		this.current = default(T);
		return false;
	}

仅从**index++**来看，set是有序的。**m_lastIndex**充当最大下标，再来看下**set.Add**来验证一下猜测

	public bool Add(T item)
	{
		return this.AddIfNotPresent(item);
	}

	private bool AddIfNotPresent(T value)
	{
		if (this.m_buckets == null)
		{
			this.Initialize(0);
		}
		int num = this.InternalGetHashCode(value);
		int num2 = num % this.m_buckets.Length;
		int num3 = 0;
		for (int i = this.m_buckets[num % this.m_buckets.Length] - 1; i >= 0; i = this.m_slots[i].next)
		{
			if (this.m_slots[i].hashCode == num && this.m_comparer.Equals(this.m_slots[i].value, value))
			{
				return false;
			}
			num3++;
		}
		int num4;
		if (this.m_freeList >= 0)
		{
			num4 = this.m_freeList;
			this.m_freeList = this.m_slots[num4].next;
		}
		else
		{
			if (this.m_lastIndex == this.m_slots.Length)
			{
				this.IncreaseCapacity();
				num2 = num % this.m_buckets.Length;
			}
			num4 = this.m_lastIndex;
			this.m_lastIndex++;
		}
		this.m_slots[num4].hashCode = num;
		this.m_slots[num4].value = value;
		this.m_slots[num4].next = this.m_buckets[num2] - 1;
		this.m_buckets[num2] = num4 + 1;
		this.m_count++;
		this.m_version++;
		if (num3 100 && HashHelpers.IsWellKnownEqualityComparer(this.m_comparer))
		{
			this.m_comparer = (IEqualityComparer<T>)HashHelpers.GetRandomizedEqualityComparer(this.m_comparer);
			this.SetCapacity(this.m_buckets.Length, true);
		}
		return true;
	}

不考虑验证失效的情况下，**this.m_slots[num4].value = value;**是最终赋值的地方，所以 顺序取决于**num4**的值，此处**num4**有两个赋值可能，

1.**num4 = this.m_lastIndex;**，不言而喻，lastIndex始终递增，肯定保持了顺序

2.**num4 = this.m_freeList;**，查看构造来分析**m_freeList**的值变化

	public HashSet() : this(EqualityComparer<T>.Default)
	{
	}

最常用的构造

	public HashSet(IEqualityComparer<Tcomparer)
	{
		if (comparer == null)
		{
			comparer = EqualityComparer<T>.Default;
		}
		this.m_comparer = comparer;
		this.m_lastIndex = 0;
		this.m_count = 0;
		this.m_freeList = -1;
		this.m_version = 0;
	}

赋值为-1即永远是第一种方式赋值，故默认构造的set是有序的。

### 线程安全问题 ###

由上面**set.GetEnumerator**可知，此类使用了一个version作为了改变的保障，查看字段定义

	private int version;

set和其内部类都是普通的字段定义，与lock无关，故set和list并不是线程安全的。若需要lock保障，可以参考**System.Collections.Generic.List<T>.SynchronizedList**

通过以上分析，**List**和**Set**其实都是使用数组作为其底层数据源，只是构建的特性不一样。

## HashSet 是如何保证不重复的 ##

相关关键代码:

	private bool AddIfNotPresent(T value)
	{
		if (this.m_buckets == null)
		{
			this.Initialize(0);
		}
		int num = this.InternalGetHashCode(value);
		int num2 = num % this.m_buckets.Length;
		int num3 = 0;
		for (int i = this.m_buckets[num % this.m_buckets.Length] - 1; i >= 0; i = this.m_slots[i].next)
		{
			// 验证hashCode 和 值是否相同
			if (this.m_slots[i].hashCode == num && this.m_comparer.Equals(this.m_slots[i].value, value))
			{
				return false;
			}
			num3++;
		}
		...省略
		else
		{
			if (this.m_lastIndex == this.m_slots.Length)
			{
				this.IncreaseCapacity();
				num2 = num % this.m_buckets.Length;
			}
			num4 = this.m_lastIndex;
			this.m_lastIndex++;
		}
		this.m_slots[num4].hashCode = num;
		this.m_slots[num4].value = value;
		this.m_slots[num4].next = this.m_buckets[num2] - 1;
		this.m_buckets[num2] = num4 + 1;
		...省略
	}

里面的if容易理解，主要是**int i = this.m_buckets[num % this.m_buckets.Length] - 1**如何理解？

看后续代码的**this.m_buckets[num2] = num4 + 1;**，若存在那么**this.m_buckets[num % this.m_buckets.Length] - 1**则为其下标

那么遍历->0的意义是什么？不应该只比较一次就够了？因为**int num2 = num % this.m_buckets.Length;**中使用了**this.m_buckets.Length**，而HashSet存在动态扩容的情况，故需要向前进行遍历

那么为什么一定是向next遍历？ 结合扩容时的**num2 = num % this.m_buckets.Length;**和**this.m_slots[num4].next = this.m_buckets[num2] - 1;**，此处需要更深度的源码分析...

**总得来说就是根据hashCode和equals值的比较结果来判断来实现不重复**

### Dictionary 是线程安全的吗，为什么不是线程安全的（最好画图说明多线程环境下不安全）? ###

首先查看*Dictionary*的*Add*源码

	public void Add(TKey key, TValue value)
	{
		this.Insert(key, value, true);
	}

	private void Insert(TKey key, TValue value, bool add)
	{
		...部分省略
		int num = this.comparer.GetHashCode(key) & 2147483647;
		int num2 = num % this.buckets.Length;
		int num3 = 0;
		for (int i = this.buckets[num2]; i >= 0; i = this.entries[i].next)
		{
			if (this.entries[i].hashCode == num && this.comparer.Equals(this.entries[i].key, key))
			{
				if (add)// 对于添加标识抛出异常，故添加和替换共用同一方法。
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
				}
				this.entries[i].value = value;
				this.version++;
				return;
			}
			num3++;
		}
		int num4;
		if (this.freeCount 0)
		{
			num4 = this.freeList;
			this.freeList = this.entries[num4].next;
			this.freeCount--;
		}
		else
		{
			if (this.count == this.entries.Length)
			{
				this.Resize();
				num2 = num % this.buckets.Length;
			}
			num4 = this.count;
			this.count++;
		}
		this.entries[num4].hashCode = num;
		this.entries[num4].next = this.buckets[num2];
		this.entries[num4].key = key;
		this.entries[num4].value = value;
		this.buckets[num2] = num4;
		this.version++;
		if (num3 100 && HashHelpers.IsWellKnownEqualityComparer(this.comparer))
		{
			this.comparer = (IEqualityComparer<TKey>)HashHelpers.GetRandomizedEqualityComparer(this.comparer);
			this.Resize(this.entries.Length, true);
		}
	}

可以看出*Dictionary*的源码类似于*HashSet* 且没有lock相关操作，故无法保障线程安全。

通过查看源码可知*Dictionary*的修改是永远不会有异常的，但使用*dictionary[i]++*时会异常，因为**++操作是先获取后赋值。 异常在get时产生**🤣🤣🤣

### 线程安全性(官方介绍) ###

> 只要不修改集合，就可以同时支持多个读取器。Dictionary<TKey,TValue尽管如此，枚举集合本身并不是一个线程安全的过程。 在具有写入访问的枚举竞争的罕见情况下，必须在整个枚举过程中锁定集合。 若要允许多个线程访问集合以进行读写操作，则必须实现自己的同步。
> 
> 有关线程安全的替代，请参阅 ConcurrentDictionary<TKey,TValue类或 ImmutableDictionary<TKey,TValue类。
> 
> 此类型的Shared公共静态（在 Visual Basic）成员是线程安全的。

![Dictionary 线程安全](https://i.ibb.co/RyqL321/Dictionary.png)

## Dictionary的扩容过程 ##

自动扩容的条件，查看*Dictionary.Insert*

	private void Insert(TKey key, TValue value, bool add)
	{
		int num3 = 0;
		for (int i = this.buckets[num2]; i >= 0; i = this.entries[i].next)
		{
			if (this.entries[i].hashCode == num && this.comparer.Equals(this.entries[i].key, key))
			{
				return;
			}
			num3++;
		}
		else
		{
			if (this.count == this.entries.Length)
			{
				this.Resize();
			}
		}
		if (num3 > 100 && HashHelpers.IsWellKnownEqualityComparer(this.comparer))
		{
			this.comparer = (IEqualityComparer<TKey>)HashHelpers.GetRandomizedEqualityComparer(this.comparer);
			this.Resize(this.entries.Length, true);
		}
	}

上面省略了大部分代码，但由此可知仅当数量已满和(查找次数过多且IsWellKnownEqualityComparer)时进行扩容，简单带过。

接着查看*Resize的定义*

	private void Resize()
	{
		this.Resize(HashHelpers.ExpandPrime(this.count), false);
	}

	public static int ExpandPrime(int oldSize)
	{
		int num = 2 * oldSize;
		if (num > 2146435069 && 2146435069 > oldSize)
		{
			return 2146435069;
		}
		return HashHelpers.GetPrime(num);
	}

newSize就不深究了，以常见的*2参考...

	private void Resize(int newSize, bool forceNewHashCodes)
	{
		int[] array = new int[newSize];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = -1;
		}
		// 构建新数据源
		Dictionary<TKey, TValue>.Entry[] array2 = new Dictionary<TKey, TValue>.Entry[newSize];
		// 先将原数据复制到新数据源
		Array.Copy(this.entries, 0, array2, 0, this.count);
		if (forceNewHashCodes)
		{
			for (int j = 0; j < this.count; j++)
			{
				if (array2[j].hashCode != -1)
				{
					array2[j].hashCode = (this.comparer.GetHashCode(array2[j].key) & 2147483647);
				}
			}
		}
		for (int k = 0; k < this.count; k++)
		{
			if (array2[k].hashCode >= 0)
			{
				int num = array2[k].hashCode % newSize;
				array2[k].next = array[num];
				array[num] = k;
			}
		}
		this.buckets = array;
		this.entries = array2;
	}

简而言之就是:通过算法获取新的*newSize*，然后创建新数据源，将之前的数据复制过来并更新*next*指向和*buckets*,根据参数是否强制更新*hashCode*

## final/readonly finally finalize/Dispose ##

*readonly*用于修身类字段/属性，表示除构造方法可改变值外，其他地方不可变动.

以属性为例，静态属性只有类型构造可赋值，实例属性只有构造可赋值，且都可赋初始值

*finally*用于异常捕获

*Dispose*用于释放对象【简单说明】

## 强引用 、软引用、 弱引用、虚引用 ##

强引用：

只要引用存在，垃圾回收器永远不会回收
Object obj = new Object();
//可直接通过obj取得对应的对象 如obj.equels(new Object());
而这样 obj对象对后面new Object的一个强引用，只有当obj这个引用被释放之后，对象才会被释放掉，这也是我们经常所用到的编码形式。

 

软引用：

非必须引用，内存溢出之前进行回收，可以通过以下代码实现
Object obj = new Object();
SoftReference<Object> sf = new SoftReference<Object>(obj);
obj = null;
sf.get();//有时候会返回null
这时候sf是对obj的一个软引用，通过sf.get()方法可以取到这个对象，当然，当这个对象被标记为需要回收的对象时，则返回null；
软引用主要用户实现类似缓存的功能，在内存足够的情况下直接通过软引用取值，无需从繁忙的真实来源查询数据，提升速度；当内存不足时，自动删除这部分缓存数据，从真正的来源查询这些数据。

 

弱引用：

第二次垃圾回收时回收，可以通过如下代码实现
Object obj = new Object();
WeakReference<Object> wf = new WeakReference<Object>(obj);
obj = null;
wf.get();//有时候会返回null
wf.isEnQueued();//返回是否被垃圾回收器标记为即将回收的垃圾
弱引用是在第二次垃圾回收时回收，短时间内通过弱引用取对应的数据，可以取到，当执行过第二次垃圾回收时，将返回null。
弱引用主要用于监控对象是否已经被垃圾回收器标记为即将回收的垃圾，可以通过弱引用的isEnQueued方法返回对象是否被垃圾回收器标记。

 
虚引用：

垃圾回收时回收，无法通过引用取到对象值，可以通过如下代码实现
Object obj = new Object();
PhantomReference<Object> pf = new PhantomReference<Object>(obj);
obj=null;
pf.get();//永远返回null
pf.isEnQueued();//返回是否从内存中已经删除
虚引用是每次垃圾回收的时候都会被回收，通过虚引用的get方法永远获取到的数据为null，因此也被成为幽灵引用。
虚引用主要用于检测对象是否已经从内存中删除。


----------

⑴强引用（StrongReference）
强引用是使用最普遍的引用。如果一个对象具有强引用，那垃圾回收器绝不会回收它。当内存空间不足，Java虚拟机宁愿抛出OutOfMemoryError错误，使程序异常终止，也不会靠随意回收具有强引用的对象来解决内存不足的问题。  ps：强引用其实也就是我们平时A a = new A()这个意思。

⑵软引用（SoftReference）
如果一个对象只具有软引用，则内存空间足够，垃圾回收器就不会回收它；如果内存空间不足了，就会回收这些对象的内存。只要垃圾回收器没有回收它，该对象就可以被程序使用。软引用可用来实现内存敏感的高速缓存（下文给出示例）。
软引用可以和一个引用队列（ReferenceQueue）联合使用，如果软引用所引用的对象被垃圾回收器回收，Java虚拟机就会把这个软引用加入到与之关联的引用队列中。

⑶弱引用（WeakReference）
弱引用与软引用的区别在于：只具有弱引用的对象拥有更短暂的生命周期。在垃圾回收器线程扫描它所管辖的内存区域的过程中，一旦发现了只具有弱引用的对象，不管当前内存空间足够与否，都会回收它的内存。不过，由于垃圾回收器是一个优先级很低的线程，因此不一定会很快发现那些只具有弱引用的对象。
弱引用可以和一个引用队列（ReferenceQueue）联合使用，如果弱引用所引用的对象被垃圾回收，Java虚拟机就会把这个弱引用加入到与之关联的引用队列中。

⑷虚引用（PhantomReference）
“虚引用”顾名思义，就是形同虚设，与其他几种引用都不同，虚引用并不会决定对象的生命周期。如果一个对象仅持有虚引用，那么它就和没有任何引用一样，在任何时候都可能被垃圾回收器回收。
虚引用主要用来跟踪对象被垃圾回收器回收的活动。虚引用与软引用和弱引用的一个区别在于：虚引用必须和引用队列 （ReferenceQueue）联合使用。当垃圾回收器准备回收一个对象时，如果发现它还有虚引用，就会在回收对象的内存之前，把这个虚引用加入到与之 关联的引用队列中。

[https://blog.csdn.net/u013041642/article/details/78700768](https://blog.csdn.net/u013041642/article/details/78700768 "文章转载")

C# 常见的有强/软(弱)引用