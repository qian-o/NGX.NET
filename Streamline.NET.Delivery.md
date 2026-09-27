# Streamline.NET 交付记录

日期：2026-09-27。范围以 `Streamline.NET.Design.md` 和本次用户澄清为准：只交付 wrapper、Generator、接口 JSON 与纯托管包；直接在当前仓库 `master` 工作。

工作流范围更正：实现过程中擅自增加了 `.github/workflows/validate.yml`，现已撤销。目前仅保留接口提取工作流，构建、验证与打包手动执行。下文 CI 链接与结果保留为已发生的验证记录，不作为后续自动化安排。

## 2026-09-27 API 完整性复核与清理

已删除 `.github/validation/`、`.github/aot-smoke/` 和 `.github/scripts/verify_package.py`，同时删除测试项目的本地构建产物及 README 中的调用命令。`.github` 现仅保留接口提取工作流、提取脚本和 `overload-contracts.json` 输入契约。此次核对临时执行，不新增测试项目、验证脚本或工作流到版本库。

核对基准是 [Streamline v2.14.1](https://github.com/NVIDIA-RTX/Streamline/releases/tag/v2.14.1)，commit 为 `2122257e0fce486f91b385aa63b9a09b0a34b363`。复核时 GitHub Latest 仍指向此版本。独立查询该 commit 的完整文件树，确认 24 个公开输入文件均在快照中；逐个读取官方内容计算 SHA-256，与快照记录完全一致。没有在本地执行 Clang 提取，也没有引入头文件或原生运行文件。

完整性核对同时使用官方快照、声明覆盖记录和编译后程序集元数据，并对可独立执行的 wrapper 逻辑进行实际调用；没有以生成器的零未处理计数代替核对。

| 范围 | 核对结果 |
|---|---|
| 声明与排除项 | 1,706 个声明身份全部对应；其中 1,409 个应用声明、209 个必要依赖、88 个有理由的内部／插件模板／测试声明。未分类、未处理和缺失实现均为 0。 |
| SDK 入口 | 42 个原始入口全部存在；19 个固定导出的导入名、调用约定和签名一致；23 个功能入口的功能 ID、查询名、参数转发和结果码行为一致。 |
| 重载 | 40 个常用结果码重载和 13 个结构体返回值重载均有正确的公开签名；原始入口的两个默认参数与弃用信息保留。DirectSR 数量查询与 Span 填充形式实际调用通过。 |
| 值类型 | 55 个值类型的大小、对齐及 369 个字段位置逐项一致，包括展开的基类头和私有字段；6 个内嵌数组、联合体及 58 个公开构造函数签名已核对。 |
| 默认初始化 | 39 个版本化值结构的类型标识／版本、220 个显式字段默认值及 30 处嵌套初始化检查通过；浮点向量构造的无效值默认行为另经实际调用检查。 |
| 枚举与常量 | 31 个枚举的底层类型、标志及 357 个成员值一致；100 个公开常量和 3 个必要 Vulkan 结构类型常量一致。 |
| 回调及借用对象 | 42 个入口函数类型别名、4 个回调签名及回调字段的 Cdecl 元数据一致；FrameToken 与 IAllocator 按原生虚表槽实际调用托管模拟函数通过。 |
| 辅助与类型成员 | 15 个字符串辅助、2 个预设辅助、8 个数学辅助、4 个 Vulkan 辅助及结构链、类型转换／比较／索引、SLArray 分配与释放均已对照并调用检查。20 个原生枚举运算符由 C# 位运算及 4 个 HasAnyFlags 重载表达。 |
| 手写实现与宏 | 38 个手写声明记录的官方函数体哈希一致，并复核对应实现；37 条宏记录全部对应，其中 26 条应用语义、10 条编译控制、1 条内部实现。模板由泛型结构链、SLArray 及枚举底层类型转换表达。 |
| 签名辅助 | 两个公开签名辅助的身份／信任检查与资源释放逻辑已对照；15 个私有 Windows 值类型大小与 78 个字段位置一致。当前主机只执行了系统功能不可用时返回 false 的路径。 |

全部 SDK 入口按实际公开路径核对如下（表中省略 `SL.` 前缀）：

| 归属 | 方法 |
|---|---|
| 核心（19） | `Init`、`Shutdown`、`IsFeatureSupported`、`IsFeatureLoaded`、`SetFeatureLoaded`、`SetTagForFrame`、`SetTag`、`SetConstants`、`GetFeatureRequirements`、`GetFeatureVersion`、`AllocateResources`、`FreeResources`、`EvaluateFeature`、`UpgradeInterface`、`GetNativeInterface`、`GetFeatureFunction`、`GetNewFrameToken`、`SetD3DDevice`、`SetVulkanInfo` |
| `DLSS`（3） | `GetOptimalSettings`、`GetState`、`SetOptions` |
| `DLSSD`（3） | `GetOptimalSettings`、`GetState`、`SetOptions` |
| `DLSSG`（2） | `GetState`、`SetOptions` |
| `DeepDVC`（2） | `GetState`、`SetOptions` |
| `DirectSR`（3） | `GetOptimalSettings`、`GetVariantInfo`、`SetOptions` |
| `NIS`（2） | `GetState`、`SetOptions` |
| `PCL`（3） | `GetState`、`SetMarker`、`SetOptions` |
| `Reflex`（5） | `GetState`、`Sleep`、`SetOptions`、`SetCameraData`、`GetPredictedCameraData` |

此次实际执行的 8,042 项行为检查全部通过，包括全部功能入口的托管函数指针转发、11 个功能返回值重载的成功／异常／保留 ref 输出行为、218 个官方字符串映射及未知值、Vulkan 名称和布尔合并、矩阵与相机历史计算等。另两个核心结构体返回值重载完成了签名、初始化与调用路径核对，未冒称执行过真实固定导出。

两个产品项目 Debug／Release 构建均为 0 警告、0 错误。离线再生成的 55 个生成文件与覆盖记录逐字节不变，生成 C# 的 UTF-8 BOM 检查通过。

结论：在本次正式 Release 的应用 wrapper 范围内未发现缺失 API，无需补充 wrapper 实现。真实 NVIDIA SDK／GPU 行为及 Windows 信任链成功路径未在本次执行；JSON 的 ABI 基准仍为提取记录中的 Windows x64，不能将当前 macOS ARM64 的托管检查表述为 Windows 原生运行验证。下方早期测试项目和 CI 结果仅为历史记录。

## API 分组与直接返回值重载更新

用户确认同时采用功能分组与返回值重载。本次更新：

- 功能操作及其专属辅助方法采用 `SL.DLSS`、`SL.DLSSD`、`SL.DLSSG`、`SL.Reflex`、`SL.PCL`、`SL.NIS`、`SL.DeepDVC`、`SL.DirectSR`。核心操作及功能 ID 常量仍属于 `SL`；结构体与枚举保持原类型名。
- 为 13 个已核实的单一输出结构增加同名直接返回值重载，例如 `DLSSOptimalSettings settings = SL.DLSS.GetOptimalSettings(in options);`。
- 新重载用 `new()` 初始化默认结构头、版本及空扩展链，通过现有结果码实现调用。结果为 `SLResult.Ok` 时返回结构；其他结果，包括非成功警告，抛出 `SLException`，保留原始 `Result` 和 `NativeFunction`。
- 同组指针／`ref` 重载继续返回 `SLResult`，支持调用方扩展链与部分输出检查。接口地址替换等真正输入输出操作未转换为返回值重载。
- 分组属于公开方法路径变更；原平铺功能方法调用需迁移到分组路径。没有增加兼容转发层或工作流。

当前快照来自[输出契约提取运行](https://github.com/qian-o/Streamline.NET/actions/runs/36315215332)，SDK Release／commit 保持 v2.14.1／`2122257e0fce486f91b385aa63b9a09b0a34b363`。

本次手动本地验证：两个产品项目 Debug／Release 构建通过；55 个值类型布局及 517 项断言通过；macOS ARM64 NativeAOT／裁剪消费者实际发布并运行通过；独立 NuGet 消费者可引用两种重载。生成文件现为 55 个，BOM 与逐字节再生成检查通过。新增结果码测试使用托管 unmanaged-callable 函数指针模拟 SDK 返回，未执行真实 NVIDIA SDK 调用。

以下 Windows CI 记录保留为此前版本的历史验证结果，不代表本次重新运行了已撤销的工作流。

## 交付内容

- .NET 10 库与独立 C# Generator，工程设置及开发期包版本集中管理。
- 42 个 SDK 入口：19 个固定导出和 23 个通过 SDK 查询的功能入口。
- 类型、默认值、成员、借用句柄、回调签名、必要 Vulkan 定义及常用引用、字符串、Span 重载。
- 字符串、预设、结构链、数学、Vulkan 特性和 Windows 签名辅助。
- 绝对路径延迟加载、单模块解析、插件函数缓存及其失效逻辑。
- 55 个由 Generator 负责的生成文件，生成 C# 为 UTF-8 BOM。
- `Streamline.NET.0.1.0.nupkg`，本地输出在 `Streamline.NET/bin/Release/`；没有公开发布。

包内只有托管程序集、XML 文档、README、LICENSE 和 NuGet 元数据。运行时 NuGet 依赖为零，不含 SDK 头文件、NVIDIA 运行库或其他图形 API 绑定包。项目许可标识保持 MIT。

## 来源与提取

| 项目 | 实际来源 |
|---|---|
| Streamline | v2.14.1 |
| Commit | `2122257e0fce486f91b385aa63b9a09b0a34b363` |
| Vulkan 必要定义 | Vulkan-Headers v1.3.231，`98f440ce6868c94f5ec6e198cc1adda4760e8849` |
| 声明与布局工具 | libclang 18.1.1 |
| 调用降低与虚表记录 | Clang 20.1.8 |
| Windows SDK / MSVC | 10.0.26100.0 / 14.44.35207 |
| 提取目标 | `x86_64-pc-windows-msvc`，C++20；字符配置及差异写入 JSON |

接口提取始终在 GitHub Actions 执行。本地只消费提交的 JSON；未搭建或执行本地提取环境。

- [最终提取复核，快照逐字节一致](https://github.com/qian-o/Streamline.NET/actions/runs/36307088534)
- [已执行的 Windows 验证记录，工作流已撤销](https://github.com/qian-o/Streamline.NET/actions/runs/36307087960)

两次最终运行均成功，对应实现提交 `7b59300e69de7f65dd0387b107dcf75b3f26cb7d`。最终提取产物与提交的快照逐字节一致。

## 覆盖结果

`Streamline.NET.Generator/declaration-coverage.json` 包含全部对应关系，`ManualImplementations.json` 固定已核对的手写辅助函数与成员的上游函数体哈希。

| 记录 | 数量 |
|---|---:|
| 声明身份，包含参数、成员及依赖 | 1706 |
| 应用声明 | 1409 |
| 必要依赖声明 | 209 |
| 排除的内部实现声明 | 74 |
| 排除的插件模板声明 | 13 |
| 排除的上游测试声明 | 1 |
| 宏 | 37 |
| 宏分类 | 26 个应用语义、10 个编译控制、1 个内部辅助 |
| 未分类 / 未处理 | 0 / 0 |

排除项保留理由。NvPerf 的公开类型与常量保留；外部 Vulkan 只生成实际需要的类型与常量。C++ 语法设施按相应 C# 成员或控制流表达，不以空函数替代。上游手写实现对应的函数体变化、未知类型或调用约定会阻止生成。

## 初始交付的验证记录

- 两个产品项目 Debug、Release 构建通过，启用 warnings-as-errors，0 编译警告、0 编译错误。
- 55 个值类型的大小和字段偏移逐项与 Clang 输出比较。
- 492 项托管检查通过，覆盖结构头、嵌套及数组默认构造、原生文本、预设、位掩码、结构链、借用虚方法和原分配器释放。
- 数学检查包括非对角逆矩阵与独立 .NET 数学实现对照、奇异矩阵的原始行为、零向量、共享相机历史。
- Vulkan 检查包含原始名称匹配、C 字符串终止符、结构头和逻辑合并。
- 延迟加载检查包含未配置、相对路径、加载失败、缺少固定导出及加载后拒绝重新配置。缺导出检查使用现有系统库，没有下载测试 DLL 或 NVIDIA 运行文件。
- Windows CI 实际发布并运行 NativeAOT/裁剪消费者；包含托管 unmanaged-callable 结构返回回调和无 SDK 加载路径的检查。
- 本地和 CI 检查包内容，并以独立配置、独立缓存、仅本地包源构建新消费者，确认便利 API 可以直接引用。
- 已还原依赖的条件下运行 Generator `--no-restore`，结果稳定；再次生成报告新增、变更、删除均为零。
- 49 个生成文件逐一确认 UTF-8 BOM；生成文件与覆盖记录的逐字节重建一致。
- 调用约定缺失、手写实现来源函数体变更两种负向用例均在写入生成文件前被拒绝。
- `git diff --check` 通过。

## 官方行为与具体处理

- `FindStruct<T, TStop>` 的官方实现会在某些空链尾分支解引用空指针；wrapper 对该未定义情况返回 null，正常遍历与停止类型顺序保持一致。来源：快照中的 `sl_helpers.h` 对应函数体。
- 签名辅助保持原有信任策略、次级签名数量要求和 NVIDIA 公钥身份匹配；补齐官方部分提前返回路径遗漏的资源释放。来源：快照中的 `sl_security.h` 对应函数体。
- `RecalculateCameraMatrices` 保留原有共享历史及显式修改相机数据的行为，文档明确线程与 viewport 限制。
- DirectSR 变体查询与填充分开表达：空缓冲查询数量，有缓冲时数量为请求数量，不伪造实际写入数量，也不自动重试、截断或回退。

## 验证边界与未完成项

本次 wrapper 交付范围内没有未完成的声明实现。以下内容没有被标记为运行通过：

- 未加载、下载或分发 NVIDIA 运行库，未执行真实 SDK/GPU 功能调用，也未做图形效果或驱动兼容性验收。
- 未验证真实 NVIDIA 签名文件的正向结果；已验证缺失文件路径及相关托管/Windows 调用路径。
- 没有增加多架构原生运行库验收。JSON 中记录的是实际提取 ABI 配置。
- NuGet 包尚未公开发布，公开发布需要单独授权。

所有修改、提交、推送和 Actions 操作限定在当前仓库；未修改 GitHub 账号设置、仓库权限、保护规则或计费配置。
