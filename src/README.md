# Launcher V2 Source Tree

国服跑跑卡丁车启动器源码。

该启动器为最新官服客户端做支持。

## 项目结构

- `App/`: 启动器主程序代码。
    - `Constant/`: 此处定义常量。
    - `Event/`: 事件处理，Bingo，比赛，商店购买。
    - `Forms/`: 用户交互窗口。
    - `ExcData`: 卡丁车数据调整。
    - `Logger/`: 日志工具。
    - `Profile/`: 配置文件处理。
    - `Server/`: **核心部分**，本地服务器，与客户端进行交互。
    - `Utility/`: 常用工具函数。
- `Library/`: LAON为启动器提供的的工具库。
