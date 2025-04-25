# 安装

有两种方式进行安装。

## 1. 下载二进制文件

前往 [release](https://github.com/yanygm/Launcher_V2 "release") 中下载最新的二进制文件压缩包，解压即可。

此方法**容易报毒**！

## 2. 使用Visual Studio进行编译

1. 下载 [Visual Studio 2022](https://visualstudio.microsoft.com/zh-hans/vs/ "Visual Studio")
2. 安装 .NET 开发依赖项，以及 [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) 或以上版本的运行时
3. 克隆该仓库到本地 `git clone https://github.com/yanygm/Launcher_V2.git --depth=1`
4. 使用 Visual Studio 打开该项目
5. 生成项目，并运行

## 3. 命令行

1. 确保已经安装了 [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) 或以上版本的运行时
2. 运行以下命令

```bash
# 克隆仓库
git clone https://github.com/yanygm/Launcher_V2.git --depth=1
cd Launcher_V2
# 编译项目
dotnet build -c Release -o <你想要的目录>
# 或者 单文件的形式（稍慢几秒，但推荐，因为更新更方便）
dotnet publish -c Release -o <你想要的目录>
# 运行项目
<你想要的目录>/Launcher.exe
```
