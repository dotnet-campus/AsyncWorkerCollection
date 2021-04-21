using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !NETCOREAPP
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace dotnetCampus.Threading
{
    // 尽管设置 csproj 不引用这个文件，但是在源代码引用的时候，依然会添加这个文件，因此需要判断当前如果是 net45 就忽略这个代码
#if NETFRAMEWORK || NETSTANDARD2_0
    // 这个接口在 .NET Framework 4.5 没有
    interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
#endif
}
