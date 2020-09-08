
### 环境说明 ###

- .net core web api

#### 前言 ####

 在.net core的配置文件中，你可能会想定义一个:

	"Dic": {
		"1": "xxx",
		"2": "xxx"
	}

 那么该用什么样的类来接收这个配置呢？

#### 主体 ####

由于key都是数字，且数字较小，你可能会想用`Dictionary<byte, string>`来接收，测试发现接收完以后集合为空...

会不会是`byte`太小，改用`Dictionary<int, string>`,依然如此...

为了证明配置正常，便使用`Dictionary<string, string>`接收，结果接收正常...

为了一探究竟，便打开了[.net core 源码](https://source.dot.net/)

通过类型分析，找到对应的扩展类：`Microsoft.Extensions.Configuration.ConfigurationBinder`

再通过一步步分析找到关键性代码：

	private static void BindDictionary(object dictionary, Type dictionaryType, IConfiguration config, BinderOptions options)
    {
        TypeInfo typeInfo = dictionaryType.GetTypeInfo();

        // IDictionary<K,V> is guaranteed to have exactly two parameters
        Type keyType = typeInfo.GenericTypeArguments[0];
        bool keyTypeIsEnum = keyType.GetTypeInfo().IsEnum;

        if (keyType != typeof(string) && !keyTypeIsEnum)
        {
            // We only support string and enum keys ****
            return;
        }
	}

`We only support string and enum keys` ... 仅支持枚举和string作为key

将key改为枚举，测试成功，over.