``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i3-4170 CPU 3.70GHz (Haswell), 1 CPU, 4 logical and 2 physical cores
  [Host]     : .NET Framework 4.7.2 (4.7.3468.0), X86 LegacyJIT
  DefaultJob : .NET Framework 4.7.2 (4.7.3468.0), X86 LegacyJIT


```
|        Method |       Mean |    Error |    StdDev |
|-------------- |-----------:|---------:|----------:|
|      BaseTest |   621.5 ms | 12.10 ms |  20.54 ms |
| TestInterface | 3,660.4 ms | 24.38 ms |  20.36 ms |
|  TestAbstract | 3,026.5 ms | 13.09 ms |  11.60 ms |
|   TestInherit | 3,153.1 ms | 61.70 ms | 111.25 ms |
