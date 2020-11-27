# dotnetCampus.AsyncWorkerCollection

[中文](README.zh-cn.md)

| Build | NuGet |
| -- | -- |
|![](https://github.com/dotnet-campus/AsyncWorkerCollection/workflows/.NET%20Core/badge.svg)|[![](https://img.shields.io/nuget/v/dotnetCampus.AsyncWorkerCollection.svg)](https://www.nuget.org/packages/dotnetCampus.AsyncWorkerCollection)|

A collection of tools that support asynchronous methods and support high-performance multithreading.

## Install NuGet package

Two different libraries are provided for installation.

### Install the traditionary NuGet Dll library

.NET CLI:

```
dotnet add package dotnetCampus.AsyncWorkerCollection
```

PackageReference:

```xml
<PackageReference Include="dotnetCampus.AsyncWorkerCollection" Version="1.2.1" />
```

### Install the [SourceYard](https://github.com/dotnet-campus/SourceYard) NuGet source code

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

## Usage

### AsyncQueue

An asynchronous queue that supports multiple threads

Create a queue:

```csharp
  var asyncQueue = new AsyncQueue<FooTask>();
```

Add task to queue:

```csharp
  asyncQueue.Enqueue(new FooTask());
```

Waiting for the task to dequeue:

```csharp
  var fooTask = await asyncQueue.DequeueAsync();
```

The advantage of AsyncQueue over Channel is that it supports .NET Framework 45 and simple

### DoubleBufferTask

DoubleBufferTask supports multi-threaded fast input data and single-threaded batch processing of data, and supports waiting for buffer execution to complete

```csharp
var doubleBufferTask = new DoubleBufferTask<Foo>(list =>
{
    // Method to perform batch List<Foo> tasks
    // The incoming delegate will be called when there is data in the DoubleBufferTask, and it means that there is at least one element in the list
});

// Multiple other threads call this code to add the task data
doubleBufferTask.AddTask(new Foo());

// After the business code is completed, call the Finish method to indicate that no more tasks are added
// This Finish method is not thread-safe
doubleBufferTask.Finish();

// Other threads can call WaitAllTaskFinish to wait for the completion of all task data in DoubleBufferTask
// It will return after be Finish method be called and the all the task data be handled
await doubleBufferTask.WaitAllTaskFinish();
```

### AsyncAutoResetEvent

Asynchronous version of AutoResetEvent lock

AsyncAutoResetEvent is functionally the same as AutoResetEvent, except that WaitOne is replaced with WaitOneAsync to support asynchronous waiting

### AsyncManualResetEvent

Asynchronous version of ManualResetEvent lock

AsyncManualResetEvent is functionally the same as ManualResetEvent, except that WaitOne is replaced with WaitOneAsync to support asynchronous waiting

## Benchmark

See [Benchmark.md](docs/Benchmark.md)

## Contributing

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](https://github.com/dotnet-campus/AsyncWorkerCollection/pulls)

If you would like to contribute, feel free to create a [Pull Request](https://github.com/dotnet-campus/AsyncWorkerCollection/pulls), or give us [Bug Report](https://github.com/dotnet-campus/AsyncWorkerCollection/issues/new).