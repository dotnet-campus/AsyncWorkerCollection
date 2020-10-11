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


### AsyncQueue with multi-thread

|                                         Method |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------------------------------- |-----------:|---------:|---------:|------:|--------:|--------:|-------:|------:|----------:|
|                   DoubleBufferTaskReadAndWrite | 2,995.9 us | 55.31 us | 54.32 us | 22.89 |    0.82 | 15.6250 |      - |     - |  70.68 KB |
|       DoubleBufferTaskWithCapacityReadAndWrite | 3,005.9 us | 59.24 us | 74.92 us | 22.96 |    0.72 | 19.5313 |      - |     - |  86.29 KB |
|                AsyncQueueEnqueueAndDequeueTest |   141.6 us |  2.76 us |  3.68 us |  1.08 |    0.03 | 25.1465 | 2.4414 |     - | 103.53 KB |
| AsyncQueueEnqueueAndDequeueTestWithMultiThread |   284.4 us |  5.63 us |  7.52 us |  2.17 |    0.07 | 26.3672 | 2.4414 |     - | 104.55 KB |
|                        ChannelReadAndWriteTest |   130.9 us |  2.55 us |  3.41 us |  1.00 |    0.00 |  4.1504 |      - |     - |  17.15 KB |
|         ChannelReadAndWriteTestWithMultiThread |   273.9 us |  4.98 us |  4.66 us |  2.10 |    0.07 |  3.9063 |      - |     - |  17.83 KB |

### DoubleBufferTask with multi-thread

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

### DoubleBufferTask with Batch task

|                                          Method |         Mean |     Error |    StdDev | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------------ |-------------:|----------:|----------:|------:|------:|------:|------:|----------:|
| DoubleBufferTaskReadAndWriteTestWithMultiThread |     31.50 ms |  0.597 ms |  0.587 ms | 0.002 |     - |     - |     - |  90.67 KB |
|          ChannelReadAndWriteTestWithMultiThread | 15,791.17 ms | 43.934 ms | 41.095 ms | 1.000 |     - |     - |     - | 645.11 KB |



## DoubleBuffer Add task

|                                                         Method |        Mean |       Error |      StdDev |  Ratio | RatioSD |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|--------------------------------------------------------------- |------------:|------------:|------------:|-------:|--------:|--------:|--------:|--------:|----------:|
|                                         AddTaskToConcurrentBag |    539.1 us |    10.09 us |     9.91 us |   1.00 |    0.00 | 54.6875 | 30.2734 | 27.3438 | 256.45 KB |
|                          AddTaskToConcurrentBagWithMultiThread |    857.0 us |    16.84 us |    18.72 us |   1.59 |    0.05 | 35.1563 | 17.5781 |  3.9063 | 163.54 KB |
|                            AddTaskToDoubleBufferWithLock |    259.0 us |     2.98 us |     2.64 us |   0.48 |    0.01 | 37.5977 |  9.2773 |       - | 156.45 KB |
|                 AddTaskToDoubleBufferWithLockMultiThread |    599.9 us |     7.60 us |     6.74 us |   1.11 |    0.02 | 38.0859 |  8.7891 |       - | 159.63 KB |
|            AddTaskToDoubleBufferWithReaderWriterLockSlim |    485.8 us |     9.63 us |     9.89 us |   0.90 |    0.02 | 37.5977 |  9.2773 |       - | 156.55 KB |
| AddTaskToDoubleBufferWithReaderWriterLockSlimMultiThread | 62,228.6 us | 2,209.10 us | 6,513.57 us | 118.39 |   13.81 |       - |       - |       - | 160.15 KB |
