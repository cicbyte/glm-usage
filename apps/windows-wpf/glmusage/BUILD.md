# GLM Usage 构建与打包指南

## 前置要求

- .NET 8 SDK
- Inno Setup 6（[下载](https://jrsoftware.org/isdl.php)）

## 发布

运行 `publish.bat`，输出到 `publish\` 目录。

## 生成安装包

用 Inno Setup 打开 `installer.iss` 编译，或命令行：

```cmd
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

输出：`installer\GLMUsage-Setup-1.0.0.exe`

## 注意事项

- 当前为依赖框架构建，用户需安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- 每次发布前更新 `installer.iss` 中的 `MyAppVersion`
