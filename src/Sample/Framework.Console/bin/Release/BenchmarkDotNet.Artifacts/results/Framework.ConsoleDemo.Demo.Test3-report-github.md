``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i3-4170 CPU 3.70GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
  [Host]     : .NET Framework 4.7.2 (4.7.3468.0), X86 LegacyJIT  [AttachedDebugger]
  DefaultJob : .NET Framework 4.7.2 (4.7.3468.0), X86 LegacyJIT


```
|        Method |       Mean |     Error |    StdDev |
|-------------- |-----------:|----------:|----------:|
|      BaseTest |   608.5 ms |   6.17 ms |   5.77 ms |
| TestInterface | 4,011.2 ms | 111.81 ms | 324.38 ms |
|  TestAbstract | 3,356.8 ms |  67.06 ms | 192.42 ms |
|   TestInherit | 3,193.9 ms |  64.17 ms |  81.16 ms |
