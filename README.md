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

## Contributing

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](https://github.com/dotnet-campus/AsyncWorkerCollection/pulls)

If you would like to contribute, feel free to create a [Pull Request](https://github.com/dotnet-campus/AsyncWorkerCollection/pulls), or give us [Bug Report](https://github.com/dotnet-campus/AsyncWorkerCollection/issues/new).