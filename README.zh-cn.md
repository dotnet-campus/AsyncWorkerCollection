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

