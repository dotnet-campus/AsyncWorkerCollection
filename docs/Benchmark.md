# Benchmark

## Environment

``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.19041
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.402
  [Host] : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT  [AttachedDebugger]

Job=InProcess  Toolchain=InProcessEmitToolchain  

```

## Write and Read

|                          Method |     Mean |   Error |  StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------------------- |---------:|--------:|--------:|------:|--------:|--------:|-------:|------:|----------:|
| AsyncQueueEnqueueAndDequeueTest | 285.4 us | 5.54 us | 6.59 us |  2.22 |    0.06 | 26.3672 | 0.4883 |     - | 104.51 KB |
|         ChannelReadAndWriteTest | 128.4 us | 2.55 us | 2.39 us |  1.00 |    0.00 |  4.1504 |      - |     - |  17.15 KB |

## Write Only

|                Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|------:|----------:|
| AsyncQueueEnqueueTest | 41.31 us | 0.784 us | 0.733 us |  0.87 |    0.02 | 8.1177 |      - |     - |  33.22 KB |
| ChannelWriteAsyncTest | 47.50 us | 0.508 us | 0.450 us |  1.00 |    0.00 | 4.1504 | 0.0610 |     - |  17.01 KB |