# 常见问题及解答

下面是一些常见问题及解答，如果出现了下方没有列出的问题，请到[yanygm issues](https://github.com/yanygm/Launcher_V2/issues) 或 [TheMagicFlute issues](https://github.com/TheMagicFlute/Launcher_V2/issues) 查看，或者优先到[yanygm issues](https://github.com/yanygm/Launcher_V2/issues)提交一个新的issue。

## 该单机启动器有什么功能？

> 请至[功能](./feature.md)查看。

## 如何安装指定版本的中国跑跑卡丁车

> 访问 [BrownSugar的跑跑卡丁车存档](https://github.com/brownsugar/popkart-client-archive/releases) 以选取指定版本的中国跑跑卡丁车进行安装。（中国大陆地区 Github Release 下载文件速度较慢，视情况开启加速器）

## 终端提示我 `读取“Profile\xxx.xml”文件失败，建议删除文件后重试！`，该怎么办？

> 该文件可能已经损坏，或者更新版本之后读取该文件的方式发生了变化，只需要删除该文件，并重启启动器即可。

### 但是我想保留该文件中的存档，怎么办？

> 如果该文件能通过文本编辑器打开，那请先备份该文件，并提 [issue](https://github.com/TheMagicFlute/Launcher_V2/issues) 询问开发者如何更改到新的格式。
> 如果该文件无法打开，那非常遗憾，可能该文件已经损坏，无法恢复。

## 我想要改名字，该怎么做？

> 进入游戏界面，小屋 -> 我的物品 -> 使用 -> 更名卡 进行更改。

## 为什么我没有装备车膜，但是提示 `[xx车膜]已装备，请选择其他部件进行装备`？我该如何装备车膜？

> 详见[issue yanygm#3](https://github.com/yanygm/Launcher_V2/issues/3)，我们正在努力找到原因，并尝试修复该问题。
> 目前的解决方法是，记住该车膜的编号（从左往右，从上往下，从1开始）并到启动器目录的 `profile/PartsData.xml` ，找到对应的车辆，并将 `Coating` 属性改为该车模的编号，重启启动器并运行。

## 进入了游戏，过了一会马上闪退怎么办？

> 尝试更新/修复游戏。
> 如果无法解决，请提 [issue](https://github.com/yangym/Launcher_V2/issues)，并附上相关的日志。
