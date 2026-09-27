using System.Runtime.CompilerServices;

// userData 容器的纯变换与设置的 Migrate 是 internal：测试换程序集可见性，而不是把它们公开成包 API。
[assembly: InternalsVisibleTo("Lin.Editor.Tests")]
