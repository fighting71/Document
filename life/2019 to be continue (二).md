## 反射 ##

首先，反射分为**动态反射**和**静态反射**，动态反射是通过反射创建出本不存在的内容，而静态反射是获取通过已有的信息

### 静态反射 ###

一般情况下，我们将反射用于获取类的信息，而想要获取类的信息，我们需要先获取这个类的*Type*(类型)，简而言之就是反射就是通过类型来获取类的信息

### 获取类型的方法有哪些？ ###

假设我们现在有一个*Data*类

	public class Data
    {
        
    }

1.通过实例获取

参考int的GetType实例

	int a = 3;
    var type = a.GetType();

对应IL:

	IL_0001:  ldc.i4.3
	IL_0002:  stloc.0
	IL_0003:  ldloc.0
	IL_0004:  box        [System.Runtime]System.Int32
	IL_0009:  call       instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
	IL_000e:  stloc.1

可以这里有**box**(装箱操作)，实例调用的还是Object的GetType

这种也比较容易理解，既然你都知道了这个物品，那么你肯定可以通过这物品来获取他的类型。由于所有类都应该能通过类实例获取类对象，所以**GetType**被定义在**Object**中，所以在使用时要考虑**空引用**和**结构的装箱**调用


2.通过**typeof**获取

	var type = typeof(Data);

为方便比较方式1，来查看一下*typeof(int)*的IL:

	IL_0001:  ldtoken    [System.Runtime]System.Int32
	IL_0006:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
	IL_000b:  stloc.0

可以看出这种方式不需要创建实例也不会进行装箱什么的，比较常用

3.通过程序集获取

	var assembly = Assembly.GetCallingAssembly();//此处有多种获取途径。
    var type = assembly.GetType(nameof(Data));

这种一般用在获取程序集下的多个Type，比较少用

等...

### 获取成员信息 ###

### 构造获取 ###

常用api定义：

	public ConstructorInfo GetConstructor(
	      BindingFlags bindingAttr,
	      Binder binder,
	      [Nullable(1)] Type[] types,
	      ParameterModifier[] modifiers);
参数说明：
![https://docs.microsoft.com/zh-cn/dotnet/api/system.type.getconstructor?view=netcore-3.0#System_Type_GetConstructor_System_Reflection_BindingFlags_System_Reflection_Binder_System_Type___System_Reflection_ParameterModifier___](https://i.ibb.co/k99X6Dv/office-type-get-Constructor.png)

	public ConstructorInfo GetConstructor(Type[] types)
	{
		return this.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, types, null);
	}

**注：BindingFlags具有[Flags]特性**

这种是默认查找实例公开构造。可想而只非公开构造则是**BindingFlags.NonPublic | BindingFlags.Instance**，由此可见*private*的约束对于反射毫无作用，所以在使用反射时应该注意这些约束尽量访问公开的信息，不破坏类的私密性。即直接调用**GetConstructor(Type[] types)**

### 字段/属性获取 ###

查看相关api定义:

	public abstract PropertyInfo[] GetProperties(BindingFlags bindingAttr);

	public PropertyInfo[] GetProperties()
	{
		return this.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

	public abstract FieldInfo[] GetFields(BindingFlags bindingAttr);

	public FieldInfo[] GetFields()
	{
		return this.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	}

属性和字段的定义都比较类型，不过值得注意的是此处的**BindingFlags.Instance 和BindingFlags.Static** 是同一位面的约束 即 **BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static**表示的是实例属性或静态属性

### 方法的获取 ###

常用api:

	public System.Reflection.MethodInfo GetMethod (string name, System.Reflection.BindingFlags bindingAttr);

	public MethodInfo GetMethod(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return this.GetMethodImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, null, null);
	}

总的来说使用都比较方便，只需要记住一些常用的api就足够了。

### 泛型信息的获取 ###

泛型的获取比较麻烦，以泛型方法为例，在存在重载时不能通过GetMethod直接获取

获取方式参考：

	static MethodInfo GetGenericMethod(Type type, string name, params Type[] types)
	{
	    foreach (MethodInfo mi in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
	    {
	        if (mi.Name != name) continue;
	        if (!mi.IsGenericMethod) continue;
	        if (mi.GetGenericArguments().Length != types.Length) continue;
	
	        return mi.MakeGenericMethod(types);
	    }
	
	    throw new MissingMethodException();
	}

这里只是简单用了Length来验证，实际可能存在更为复杂的重载...

### 包含特殊参数的方法的获取 ###

	// 获取参数包含ref/out参数的方法
	var methodInfo = type.GetMethod("DoSomething", new Type[] { typeof(int).MakeByRefType()});
	            
	// 获取包含类型指针的参数 的方法
	var methodInfo2 = type.GetMethod("DoSomething", new Type[] { typeof(int).MakePointerType()});
	
	// 数组。
	var methodInfo3 = type.GetMethod("DoSomething", new Type[] { typeof(int).MakeArrayType()});

### 动态反射 ###

动态一般直接使用的情况很少，现在基本上都会使用lambda/experssion让程序来帮我们来实现，还有就是一些动态代理。

属于高级编程，可参考使用EMIT创建动态类。

### Type与TypeInfo的区别 ###

查看类定义:

	public abstract class TypeInfo : Type, IReflectableType

...不言而喻 TypeInfo其实就是一个Type 只是提供的方法更多，更全面。

Type获取TypeInfo

	public static TypeInfo GetTypeInfo(this Type type)
	{
	    if (type == null)
	        throw new ArgumentNullException(nameof(type));
	
	    if (type is IReflectableType reflectableType)
	        return reflectableType.GetTypeInfo();
	
	    return new TypeDelegator(type);
	}

感兴趣的自行查看一下。[https://github.com/dotnet/corefx/blob/d3911035f2ba3eb5c44310342cc1d654e42aa316/src/Common/src/CoreLib/System/Reflection/TypeDelegator.cs](https://github.com/dotnet/corefx/blob/d3911035f2ba3eb5c44310342cc1d654e42aa316/src/Common/src/CoreLib/System/Reflection/TypeDelegator.cs "TypeDelegator源码地址")

## Array.Sort ##

看到这里，建议是直接去看排序算法，学习常用的算法并熟知每个算法的不同性。

![稳定的排序](https://i.ibb.co/gZb1dmw/sort-list.png)

![不稳定的排序](https://i.ibb.co/qjHt32p/sort-list2.png)

![不实用的排序](https://i.ibb.co/rd46FG4/sort-list3.png)

[https://zh.wikipedia.org/wiki/%E6%8E%92%E5%BA%8F%E7%AE%97%E6%B3%95](https://zh.wikipedia.org/wiki/%E6%8E%92%E5%BA%8F%E7%AE%97%E6%B3%95 "wiki参考")

回到主题，既然谈到了就来分析分析。首先，先查看Sort的源码:

	public static void Sort(Array array)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		Array.Sort(array, null, array.GetLowerBound(0), array.Length, null);
	}

	// System.Array
	[__DynamicallyInvokable, ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail), SecuritySafeCritical]
	public static void Sort(Array keys, Array items, int index, int length, IComparer comparer)
	{
		... 省略检测代码
		if (length > 1)
		{
			if (comparer == Comparer.Default || comparer == null)
			{
				bool flag = Array.TrySZSort(keys, items, index, index + length - 1);
				if (flag)
				{
					return;
				}
			}
			object[] array = keys as object[];
			object[] array2 = null;
			if (array != null)
			{
				array2 = (items as object[]);
			}
			if (array != null && (items == null || array2 != null))
			{
				Array.SorterObjectArray sorterObjectArray = new Array.SorterObjectArray(array, array2, comparer);
				sorterObjectArray.Sort(index, length);
				return;
			}
			Array.SorterGenericArray sorterGenericArray = new Array.SorterGenericArray(keys, items, comparer);
			sorterGenericArray.Sort(index, length);
		}
	}

由判断可知 默认情况下调用**Array.TrySZSort**

	private static extern bool TrySZSort(Array keys, Array items, int left, int right);

扩展方法，很好，那就来追clr源码吧

[https://github.com/SSCLI/sscli20_20060311](https://github.com/SSCLI/sscli20_20060311 "github地址")

首先找TrySZSort定义 

*clr/src/vm/ecall.cpp*

	FCFuncElement("TrySZSort", ArrayHelper::TrySZSort)

由此可知实际是使用*ArrayHelper的TrySZSort*，再来查找*ArrayHelper*， 

*clr/src/vm/comarrayhelpers.cpp*

	FCIMPL4(FC_BOOL_RET, ArrayHelper::TrySZSort, ArrayBase * keys, ArrayBase * items, UINT32 left, UINT32 right)
	    WRAPPER_CONTRACT;
	    STATIC_CONTRACT_SO_TOLERANT;
	    
	    VALIDATEOBJECTREF(keys);
		VALIDATEOBJECTREF(items);
	    _ASSERTE(keys != NULL);
	
	    if (keys->GetRank() != 1 || keys->GetLowerBoundsPtr()[0] != 0)
	        FC_RETURN_BOOL(FALSE);
	
		_ASSERTE(left <= right);
		_ASSERTE(right < keys->GetNumComponents() || keys->GetNumComponents() == 0);
	
	    TypeHandle keysTH = keys->GetArrayElementTypeHandle();
	    const CorElementType keysElType = keysTH.GetVerifierCorElementType();
	    if (!CorTypeInfo::IsPrimitiveType(keysElType))
	        FC_RETURN_BOOL(FALSE);
		if (items != NULL) {
			TypeHandle itemsTH = items->GetArrayElementTypeHandle();
			if (keysTH != itemsTH)
				FC_RETURN_BOOL(FALSE);   // Can't currently handle sorting different types of arrays.
		}
	
		if (left == right || right == 0xffffffff)
			FC_RETURN_BOOL(TRUE);
	
	    switch(keysElType) {
	    case ELEMENT_TYPE_I1:
			ArrayHelpers<I1>::QuickSort((I1*) keys->GetDataPtr(), (I1*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
			break;
	
	    case ELEMENT_TYPE_U1:
	    case ELEMENT_TYPE_BOOLEAN:
	        ArrayHelpers<U1>::QuickSort((U1*) keys->GetDataPtr(), (U1*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_I2:
	        ArrayHelpers<I2>::QuickSort((I2*) keys->GetDataPtr(), (I2*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_U2:
	    case ELEMENT_TYPE_CHAR:
			ArrayHelpers<U2>::QuickSort((U2*) keys->GetDataPtr(), (U2*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_I4:
			ArrayHelpers<I4>::QuickSort((I4*) keys->GetDataPtr(), (I4*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_U4:
	        ArrayHelpers<U4>::QuickSort((U4*) keys->GetDataPtr(), (U4*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_R4:
	        ArrayHelpers<R4>::QuickSort((R4*) keys->GetDataPtr(), (R4*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_I8:
	        ArrayHelpers<I8>::QuickSort((I8*) keys->GetDataPtr(), (I8*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_U8:
	        ArrayHelpers<U8>::QuickSort((U8*) keys->GetDataPtr(), (U8*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
		case ELEMENT_TYPE_R8:
	        ArrayHelpers<R8>::QuickSort((R8*) keys->GetDataPtr(), (R8*) (items == NULL ? NULL : items->GetDataPtr()), left, right);
	        break;
	
	    case ELEMENT_TYPE_I:
	    case ELEMENT_TYPE_U:
	        // In V1.0, IntPtr & UIntPtr are not fully supported types.  They do 
	        // not implement IComparable, so searching & sorting for them should
	        // fail.  In V1.1 or V2.0, this should change.                                   
	        FC_RETURN_BOOL(FALSE);
	
	    default:
	        _ASSERTE(!"Unrecognized primitive type in ArrayHelper::TrySZSort");
	        FC_RETURN_BOOL(FALSE);
	    }
	    FC_RETURN_BOOL(TRUE);
	FCIMPLEND

看到了大片的**QuickSort**... *快速排序了解一下~*，

若是走到**sorterObjectArray.Sort(index, length);**，里面其实也是各种排序算法混用，所以 **掌握常用的排序的算法**->**再来学习那些混合使用的code**

此主题比较简单的回答就是快速排序。

## LinkedHashMap的应用/以LinkedListNode作为数据源实现map ##

...略略略

## cloneable接口实现原理 ##

首先介绍下拷贝方式，clone分两种：**深拷贝**和**浅拷贝**

### 什么是“浅拷贝” ###

> 当针对一个对象浅拷贝的时候，对于对象的值类型成员，会复制其本身，对于对象的引用类型成员，**仅仅复制对象引用**，这个引用指向托管堆上的对象实例。

实现：一般通过**Object.MemberwiseClone**即可完成

### 什么是“深拷贝” ###

**对引用成员指向的对象也进行复制**，在托管堆上赋值原先对象实例所包含的数据，再在托管堆上创建新的对象实例。

实现：一般借助**序列化**来进行拷贝

即拷贝对象里面又有对象引用时，浅拷贝仅拷贝对象指针，深拷贝会重新创建引用对象，简而言之深拷贝后的对象的操作不影响原先的对象，而浅拷贝可能会影响

### 实现原理 ###

略略略....

