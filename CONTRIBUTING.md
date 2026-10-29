Contributing

欢迎为跑跑卡丁车工具项目贡献代码、文档、Bug 修复与优化建议！本项目基于开源仓库发展而来，秉持开源协作、共同优化的原则，以下是详细贡献规范，请仔细阅读。

Acknowledgments / 致谢

This project is based on and modified from the original work of the following open-source repositories:

- [Launcher.cn_3075](https://github.com/MyPuppy/Launcher.cn_3075)
Served as the original core codebase of this project. Most core logic and structure are derived and improved from this repository.

- [Kartrider-File-Reader](https://github.com/xpoi5010/Kartrider-File-Reader)
Provided important references for Kartrider file parsing and data reading logic.

- [kart_data_Transform](https://github.com/lkk9898969/kart_data_Transform)
Provided reference implementation for kart vehicle data parsing and transformation.

Sincere thanks to the authors of the above repositories for their excellent work and open-source spirit.

可贡献类型

- 修复基于 [Launcher.cn_3075](https://github.com/MyPuppy/Launcher.cn_3075) 原始代码衍生的 Bug（如启动、文件解析异常）

- 新增跑跑卡丁车文件格式支持（Rho、Rho5、XML等）及车辆数据相关功能

- 优化车辆数据解析、转换逻辑（参考 [kart_data_Transform](https://github.com/lkk9898969/kart_data_Transform) 实现，提升准确性和效率）

- 完善项目文档、使用说明、注释，优化代码可读性

- 提交车辆数据相关的功能建议、使用问题反馈

- 优化项目性能、UI交互、兼容性（适配Windows常见版本）

行为准则

- 友善交流，尊重其他贡献者，不发布攻击性、无关或广告类内容

- 不提交恶意代码、破解程序、侵权内容，遵守开源协议

- 尊重各参考仓库的版权，不篡改原始代码的核心标识与致谢信息

- 提交的代码、文档需符合项目整体风格，不引入无关依赖

提交 Issue 规范

🐛 Bug 报告

- 清晰描述 Bug 现象，提供完整复现步骤

- 注明系统版本（Windows 7/10/11）、项目版本/Commit 号

- 附上报错截图、日志信息，若涉及文件解析异常，可提供脱敏后的文件样本

- 说明该 Bug 是否影响核心功能（如车辆数据解析、原始代码运行）

💡 功能建议

- 明确功能的使用场景（如车辆数据批量转换、新增某种文件解析）

- 描述预期的输入、输出效果，可附上参考示例或截图

- 若涉及车辆数据相关功能，可参考 [kart_data_Transform](https://github.com/lkk9898969/kart_data_Transform) 的实现思路并提出优化建议

🔒 安全问题

请勿公开提 Issue，直接联系项目维护者处理，避免安全风险。

开发流程

1. Fork 本仓库到个人 GitHub 仓库

2. 拉取主分支最新代码，确保与上游仓库同步：
  - git checkout main
  - git pull upstream main

3. 新建分支，分支命名规范（统一小写，用斜杠分隔）：

  - Bug 修复：fix/具体问题（如 fix/vehicle-data-parse-error）

  - 功能开发：feature/具体功能（如 feature/batch-vehicle-data-transform）

  - 文档修改：docs/具体内容（如 docs/update-vehicle-data-docs）

4. 本地开发：基于 [Launcher.cn_3075](https://github.com/MyPuppy/Launcher.cn_3075) 原始代码修改时，保留核心逻辑；车辆数据相关开发可参考 [kart_data_Transform](https://github.com/lkk9898969/kart_data_Transform) 实现，确保代码可运行

5. 本地测试：完成开发后，验证功能正常，无新增 Bug，通过基础编译/运行检查

6. 提交 Commit：遵循规范填写提交信息（详见下方 Commit 规范）

7. Push 分支到个人仓库，发起 Pull Request（PR），关联相关 Issue（如有）

8. 等待维护者审核，根据反馈修改代码，直至审核通过后合并

Commit 信息规范（必须遵守）

Commit 信息格式：type(scope): description（英文，简洁明了，不超过50字符）

类型说明（type 可选值）：

feat: 新增功能（如 新增车辆数据批量转换功能）
fix: 修复 Bug（如 修复车辆数据解析异常）
docs: 更新文档（如 完善车辆数据使用说明）
style: 格式化代码（不改变代码逻辑）
refactor: 重构代码（如 重构车辆数据解析逻辑）
test: 补充测试用例（如 新增车辆数据解析测试）
chore: 依赖更新、构建调整等（不涉及核心功能）

示例：feat(vehicle-data): add batch transform function / fix(parse): fix rho file read error

代码规范

C# 代码（基于 [Launcher.cn_3075](https://github.com/MyPuppy/Launcher.cn_3075)）

- 遵循 .NET 命名规范，类名、方法名、变量名清晰易懂，避免乱命名

- 编译无警告、无报错，确保代码可正常运行

- 新增代码需添加必要注释，说明功能用途、参数含义

PR 提交要求

- PR 标题清晰，格式与 Commit 一致，关联相关 Issue（如 Fix #123）

- 单 PR 只做一件事，避免一次性提交大量无关修改，便于审核

- 提交前确保本地测试通过，代码无报错、无冗余，通过基本 lint 检查

- PR 描述中需说明：改动内容、改动原因、测试结果，若涉及车辆数据，需说明数据验证情况

- 不破坏已有功能，若需修改核心逻辑（如 Launcher.cn_3075 原始代码），需在 PR 中详细说明原因

测试要求

- 修改基于 [Launcher.cn_3075](https://github.com/MyPuppy/Launcher.cn_3075) 的核心代码后，需验证原始功能正常运行

- 修改车辆数据解析、转换逻辑后，需验证原有车辆数据可正常解析，新增功能符合预期

- 提供测试步骤或测试文件（可脱敏），便于维护者复现测试

- 确保代码适配 Windows 7/10/11 常见版本，无兼容性问题

联系方式

若有疑问、建议或合作需求，可通过以下方式联系：

- GitHub Issues / Discussions：直接在本仓库提交 Issue 或参与讨论

- 维护者会在1-3个工作日内回复，感谢你的理解与配合

再次感谢所有贡献者的支持，携手优化跑跑卡丁车工具项目！🚗
