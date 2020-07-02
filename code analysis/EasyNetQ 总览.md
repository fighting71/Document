
EasyNetQ 消费处理实现类：EasyNetQ.EventBus

EasyNetQ使用自定义容器 进行依赖注入  -  EasyNetQ.DI.DefaultServiceContainer

EasyNetQ创建连接方式：
轮询
触发地址：EasyNetQ.RabbitAdvancedBus.ctor() 
    key code:connection.Initialize();
connection 默认实现： EasyNetQ.PersistentConnection
具体地址：EasyNetQ.PersistentConnection.TryToConnect

EasyNetQ消费处理：EasyNetQ.Consumer.ConsumerDispatcher - 轮询

EasyNetQ 订阅方式： 通过订阅检索消息
订阅地址：EasyNetQ.Consumer.InternalConsumer
MQ订阅触发：EasyNetQ.Consumer.BasicConsumer - 监听