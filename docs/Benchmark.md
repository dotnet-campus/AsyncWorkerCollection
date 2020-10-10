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

### AsyncQueue

|                Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|------:|----------:|
| AsyncQueueEnqueueTest | 41.31 us | 0.784 us | 0.733 us |  0.87 |    0.02 | 8.1177 |      - |     - |  33.22 KB |
| ChannelWriteAsyncTest | 47.50 us | 0.508 us | 0.450 us |  1.00 |    0.00 | 4.1504 | 0.0610 |     - |  17.01 KB |


### DoubleBufferTask

|                                      Method | threadCount |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------------------------------- |------------ |-----------:|---------:|---------:|------:|--------:|--------:|-------:|------:|----------:|
|                DoubleBufferTaskReadAndWrite |           ? | 2,895.6 us | 39.62 us | 37.06 us | 22.65 |    0.46 | 15.6250 |      - |     - |  70.68 KB |
|    DoubleBufferTaskWithCapacityReadAndWrite |           ? | 2,914.2 us | 50.76 us | 47.48 us | 22.80 |    0.42 | 19.5313 |      - |     - |  86.29 KB |
|             AsyncQueueEnqueueAndDequeueTest |           ? |   275.4 us |  5.35 us |  5.73 us |  2.15 |    0.05 | 26.3672 | 0.4883 |     - | 104.48 KB |
|                     ChannelReadAndWriteTest |           ? |   127.8 us |  2.06 us |  1.92 us |  1.00 |    0.00 |  4.1504 |      - |     - |  17.15 KB |
|                                             |             |            |          |          |       |         |         |        |       |           |
| **DoubleBufferTaskWithMultiThreadReadAndWrite** |           **2** | **2,068.5 us** | **40.93 us** | **58.70 us** |     **?** |       **?** | **19.5313** |      **-** |     **-** |   **87.7 KB** |
|                                             |             |            |          |          |       |         |         |        |       |           |
| **DoubleBufferTaskWithMultiThreadReadAndWrite** |           **5** | **1,193.8 us** | **23.31 us** | **39.59 us** |     **?** |       **?** | **21.4844** |      **-** |     **-** |  **88.33 KB** |
|                                             |             |            |          |          |       |         |         |        |       |           |
| **DoubleBufferTaskWithMultiThreadReadAndWrite** |          **10** | **1,120.2 us** | **22.31 us** | **28.21 us** |     **?** |       **?** | **21.4844** |      **-** |     **-** |  **89.38 KB** |