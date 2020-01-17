``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i3-4170 CPU 3.70GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


```
|        Method |     Mean |    Error |   StdDev |
|-------------- |---------:|---------:|---------:|
|      BaseTest | 608.4 ms |  8.61 ms |  7.64 ms |
| TestInterface | 609.1 ms |  6.66 ms |  5.90 ms |
|  TestAbstract | 622.3 ms | 14.52 ms | 12.87 ms |
|   TestInherit | 619.2 ms | 14.60 ms | 16.23 ms |
