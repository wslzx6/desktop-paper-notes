# 运行验证

项目根目录运行 `./build.ps1 -Test`。验证代码为 `src/SelfTests.cs`，构建与成品共用同一份实现；验证使用 `dist/test-results` 下独立的数据目录，不修改真实便签或开机启动设置。

结果位于 `dist/test-results/latest.txt`。每次运行单独保存位图、桌面窗口诊断和数据文件，方便复查。它会短暂创建真实桌面窗口和管理窗口，完成后自行退出。

界面手工验证可以运行 `dist/桌面便签.exe --test-mode`，隔离数据位于 `dist/test-data`。
