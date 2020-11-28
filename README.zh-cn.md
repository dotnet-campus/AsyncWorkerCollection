# dotnetCampus.AsyncWorkerCollection

一个支持异步方法和支持高性能多线程的工具集合


## 安装 NuGet 包

这个库提供了两个不同的包可以给大家安装。其中一个包是传统的 Dll 引用包。另一个包是使用 [SourceYard](https://github.com/dotnet-campus/SourceYard) 打出来的源代码包，源代码包安装之后将会引用源代码

### 安装传统 NuGet Dll 库

.NET CLI:

```
dotnet add package dotnetCampus.AsyncWorkerCollection
```

PackageReference:

```xml
<PackageReference Include="dotnetCampus.AsyncWorkerCollection" Version="1.2.1" />
```

### 安装源代码包

.NET CLI:

```
dotnet add package dotnetCampus.AsyncWorkerCollection.Source --version 1.2.1
```

PackageReference:

```xml
<PackageReference Include="dotnetCampus.AsyncWorkerCollection.Source" Version="1.2.1">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

安装源代码包将会让你的项目引用的是 dotnetCampus.AsyncWorkerCollection 的 C# 源代码，而不是 dll 程序集。使用此方法可以减少 dll 文件以及程序集的引入

## 使用方法

### AsyncQueue

高性能内存生产者消费者队列，支持多线程入队和多线程等待出队

最简使用方法

```csharp
// 下面的 FooTask 是任意自定义类
var asyncQueue = new AsyncQueue<FooTask>();

// 线程1
asyncQueue.Enqueue(new FooTask());

// 线程2
var fooTask = await asyncQueue.DequeueAsync();
```

详细请看 [dotnet 使用 AsyncQueue 创建高性能内存生产者消费者队列](https://blog.lindexi.com/post/dotnet-%E4%BD%BF%E7%94%A8-AsyncQueue-%E5%88%9B%E5%BB%BA%E9%AB%98%E6%80%A7%E8%83%BD%E5%86%85%E5%AD%98%E7%94%9F%E4%BA%A7%E8%80%85%E6%B6%88%E8%B4%B9%E8%80%85%E9%98%9F%E5%88%97.html )

### DoubleBufferTask

双缓存任务和双缓存类，支持多线程快速写入和单线程读取批量的数据，支持等待缓存执行完成

最简使用方法

```csharp
var doubleBufferTask = new DoubleBufferTask<Foo>(list =>
{
    // 执行批量的 List<Foo> 任务的方法
    // 这个传入的委托将会在缓存有数据时被调用，每次调用的时候传入的 list 列表至少存在一个元素
});

// 其他线程调用 AddTask 方法加入任务
doubleBufferTask.AddTask(new Foo());

// 在业务端完成之后，调用 Finish 方法表示不再有任务加入
// 此 Finish 方法非线程安全，必须业务端根据业务调用
doubleBufferTask.Finish();

// 其他线程可以调用 WaitAllTaskFinish 等待缓存所有任务执行完成
// 在调用 Finish 方法之后，缓存的所有任务被全部执行之后将会返回
await doubleBufferTask.WaitAllTaskFinish();
```

详细请看 [dotnet 双缓存数据结构设计 下载库的文件写入缓存框架](https://blog.lindexi.com/post/dotnet-%E5%8F%8C%E7%BC%93%E5%AD%98%E6%95%B0%E6%8D%AE%E7%BB%93%E6%9E%84%E8%AE%BE%E8%AE%A1-%E4%B8%8B%E8%BD%BD%E5%BA%93%E7%9A%84%E6%96%87%E4%BB%B6%E5%86%99%E5%85%A5%E7%BC%93%E5%AD%98%E6%A1%86%E6%9E%B6.html )

### AsyncAutoResetEvent

异步版本的 AutoResetEvent 锁

功能上和 AutoResetEvent 相同，只是将 WaitOne 替换为 WaitOneAsync 用于支持异步等待

详细请看 [C# dotnet 高性能多线程工具 AsyncAutoResetEvent 异步等待使用方法和原理](https://blog.lindexi.com/post/C-dotnet-%E9%AB%98%E6%80%A7%E8%83%BD%E5%A4%9A%E7%BA%BF%E7%A8%8B%E5%B7%A5%E5%85%B7-AsyncAutoResetEvent-%E5%BC%82%E6%AD%A5%E7%AD%89%E5%BE%85%E4%BD%BF%E7%94%A8%E6%96%B9%E6%B3%95%E5%92%8C%E5%8E%9F%E7%90%86.html )

### AsyncManualResetEvent

异步版本的 ManualResetEvent 锁

功能上和 ManualResetEvent 相同，只是将 WaitOne 替换为 WaitOneAsync 用于支持异步等待

### ExecuteOnceAwaiter

支持本机内多线程调用某一确定的任务的执行，任务仅执行一次，多次调用均返回相同结果

在任务执行完成之后，可以重置任务状态，让任务再次执行

如用来作为执行 同步 这个业务的工具。也就是在 同步 这个业务执行过程中，不允许再次执行 同步 这个业务。同时只要同步过了，那么再次调用只是返回同步结果。只有在同步之后状态发生变更之后，才能再次同步

详细请看 [C# dotnet 高性能多线程工具 ExecuteOnceAwaiter 只执行一次的任务](https://lindexi.gitee.io/post/C-dotnet-%E9%AB%98%E6%80%A7%E8%83%BD%E5%A4%9A%E7%BA%BF%E7%A8%8B%E5%B7%A5%E5%85%B7-ExecuteOnceAwaiter-%E5%8F%AA%E6%89%A7%E8%A1%8C%E4%B8%80%E6%AC%A1%E7%9A%84%E4%BB%BB%E5%8A%A1.html )