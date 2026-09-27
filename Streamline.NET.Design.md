# Streamline.NET 设计规范

## 1. 产品定义

Streamline.NET 是完整、独立的 C# Streamline wrapper，保留官方接口语义，并提供常用的引用、字符串和 Span 重载。

| 项目 | 规格 |
|---|---|
| 框架 | .NET 10 |
| 发布 | 单一纯托管 NuGet 包 `Streamline.NET` |
| 托管依赖 | .NET 基础类库，无额外运行时 NuGet 依赖 |
| 接口基准 | 提取时的官方最新正式 Release |
| 工程风格 | Metal.NET 的代码排版、功能分组和生成方式 |

绑定范围包含官方面向应用的接口、数据类型、回调、类型成员和辅助能力。构造、析构、复制与赋值、转换、运算符、索引器、模板、inline 和具有公开语义的宏均须有对应表达；保留仍公开的弃用声明。

范围由官方公开内容自动发现，不以文档中的文件名或示例限定。内部实现、测试、插件开发模板和编译控制内容按声明用途分类；同一文件中的应用接口及其必要依赖仍须纳入。

## 2. 工程结构与代码规范

```text
Streamline.NET/
├── .github/                         # 接口提取自动化
├── Streamline.NET.Generator/
│   ├── Streamline.NET.Generator.csproj
│   ├── Program.cs
│   ├── AstJsonParser.cs
│   ├── TypeMapper.cs
│   ├── CSharpEmitter.cs
│   ├── Models.cs
│   └── streamline-ast.json
├── Streamline.NET/
│   ├── Streamline.NET.csproj
│   ├── Interop/
│   ├── Core/
│   ├── <功能分组>/
│   └── Helpers/
├── Directory.Build.props
├── Directory.Packages.props
├── Streamline.NET.slnx
├── .gitignore
├── LICENSE
└── Streamline.NET.Design.md
```

`Streamline.NET.Generator` 读取接口快照，由 `Program` 串联 `AstJsonParser → TypeMapper → CSharpEmitter`，模型放在 `Models.cs`。`Streamline.NET` 提供绑定、便利重载、辅助实现和加载功能。内部文件按实际体量拆分，`<功能分组>` 根据公开模块生成。

库使用 `namespace Streamline.NET;`，生成器使用 `Streamline.NET.Generator`。枚举与结构体按功能合并，保留顶层类型名；核心函数放在 `SL` 的 partial 文件中，功能函数及其专属辅助函数放在 `SL.DLSS`、`SL.Reflex` 等平级嵌套静态 partial 类中。原生入口、结果码便利重载与直接返回值重载相邻，类型成员保留在所属类型中。固定导出的内部声明集中在 `SLNative`，加载实现放在 `Interop`。

生成文件与手写文件分开。生成代码的问题通过修改 Generator 解决；复杂辅助算法可按官方源码手写，不要求生成器翻译任意 C++ 程序。

| 项目 | 规则 |
|---|---|
| 项目配置 | 公共属性和包版本集中管理；启用 Nullable、ImplicitUsings、AllowUnsafeBlocks、EnforceCodeStyleInBuild。库启用 AOT／裁剪兼容分析和 Release 打包。 |
| 分析器 | Roslynator 为开发期依赖，不向使用者传递。 |
| 排版 | 四空格、Allman 大括号、文件范围命名空间、成员间空行；生成 C# 使用 UTF-8 BOM。 |
| 命名 | 类型和公开成员 PascalCase，参数和私有字段 camelCase；保留 DLSS 等官方缩写。 |
| 类型与初始化 | 原生值使用 `struct`，借用句柄使用 `readonly struct`；采用目标类型 `new()` 和集合表达式。 |
| 局部声明 | 以显式类型为主；匿名类型及 LINQ 分组可用 `var`。 |
| 注释 | 保留来源及弃用信息；XML 注释说明单位、默认值、线程、所有权和指针有效期。 |

代码风格参考 Metal.NET，接口语义以 Streamline 为准。[1]

## 3. 接口提取与生成

### 3.1 提取流程

**接口提取仅在 GitHub Actions 执行，本地不搭建或执行提取环境。** 自动化文件和脚本的组织由实现者决定。

每次提取解析一次 `NVIDIA-RTX/Streamline` 的 Latest 正式 Release，将 tag 解析为 commit；从该来源取得公开头文件、相关实现及必要依赖，使用 Clang 提取声明、数据布局和调用约定。唯一数据产物为 `Streamline.NET.Generator/streamline-ast.json`。[2]

输入按声明用途分类，保留排除依据。条件编译内容在合法配置下解析并去重；依赖沿签名、类型成员和辅助实现补全。提取配置及同名声明的差异写入 JSON，不使用开发机默认布局替代原生定义。

### 3.2 JSON 数据契约

| 数据 | 内容 |
|---|---|
| 格式与来源 | `schemaVersion`、Release／commit、依赖来源、工具链和提取配置。 |
| 覆盖记录 | 输入文件、声明身份、来源、分类及排除依据。 |
| 类型 | 字段、基类、私有布局、联合体、数组、大小、对齐、偏移、枚举数值及底层类型。 |
| 初始化与成员 | 默认值、嵌套构造、析构与复制限制、类型标识、结构版本、方法、转换、运算符和索引。 |
| 调用 | 原生签名、导出名或插件查询名、调用约定、回调及虚方法信息。 |
| 辅助实现 | 生成所需表达式与常量；手写逻辑的来源、行为及变更信息。 |
| 重载依据 | 输入输出方向、数量关系、编码、可空性和指针有效期。 |

JSON 只供 Generator 使用，必须包含生成所需数据，不能用源码地址或哈希代替字段、表达式和默认值。已提交的 JSON 与 Generator 应能离线重建全部生成文件。

### 3.3 生成规则

同一输入产生相同输出，Generator 只更新自己负责的文件。每个范围内声明对应生成实现、手写实现或等价 C# 语言表达，报告新增、变更、删除、未分类和未处理项。

未知类型、缺失数据或未解析的调用约定阻止生成完成，通过修正提取任务重新取得。手写辅助逻辑须随官方函数体变化同步，文件存在不代表实现正确。

重载依据已核实的接口契约生成；无法确认其传参条件时，保留完整原生入口并注明原因，不据此遗漏接口。

## 4. 绑定接口

### 4.1 命名与调用

```csharp
namespace Streamline.NET;

public static unsafe partial class SL
{
}
```

| 官方表达 | C# 表达 |
|---|---|
| `slInit`、`slDLSSSetOptions` | `SL.Init`、`SL.DLSS.SetOptions` |
| `sl::Result`、`eOk` | `SLResult`、`SLResult.Ok` |
| `sl::Boolean`、`sl::Version` | `SLBoolean`、`SLVersion` |
| 类型、字段、枚举成员 | PascalCase；枚举成员去除命名用的 `e` 前缀。 |
| `kFeatureDLSS`、`kSDKVersion` | `SL.FeatureDLSS`、`SL.SDKVersion` |

整数别名使用底层整数和命名常量；嵌套类型保留归属。构造、比较、转换和索引在所属类型表达，配套 `Equals`、`GetHashCode` 遵循相同语义。标志位的“任一位命中”用 `(value & mask) != 0`，不与“全部位命中”混用。

固定导出使用 internal `SLNative` 中的 `LibraryImport`；插件函数通过官方 `GetFeatureFunction` 取得 unmanaged 函数指针。没有独立导出的 inline 包装按实际逻辑实现。[3]

原生入口保留值传递、指针层级、可空性及默认参数。已知值结构的指针和 C++ 引用使用 `T*`，不透明对象使用原生地址；不能表示为 C# 常量的默认参数通过转发重载提供。`SL.SDKVersion` 来自绑定头文件。

插件函数按官方调用前提查询，查询失败返回原始结果，成功地址缓存；关闭 SDK 或改变插件装载状态时清理相关缓存。所有重载共用这一调用实现。

官方返回 `Result` 的函数，其原生入口和显式引用／缓冲重载返回 `SLResult`，SDK 结果不自动转为异常。直接返回输出值的便利重载按第 4.2 节处理非成功结果；其他函数保持自身返回语义。加载失败和缺少固定导出使用原有 .NET 异常。绑定不增加参数纠正、重试、功能回退、自动初始化或 GPU 等待。

### 4.2 便利重载

每个适用接口保留原生入口，并生成一套常用便利形式，不穷举参数组合。签名不能仅靠 `in`、`ref`、`out` 互相区分。

| 原生参数 | 便利形式 |
|---|---|
| 调用期只读／读写的普通值结构引用 | `in T`／`ref T`。 |
| 已确认的纯输出标量或地址 | `out`。 |
| 连续元素区及数量 | `ReadOnlySpan<T>`／`Span<T>`，保留容量与实际数量的区别。 |
| 编码明确、仅在调用期使用的字符指针 | `string`。 |
| 不透明 SDK 对象引用 | 按值传入句柄包装，内部使用对象地址，不改变所有权。 |
| 长期保存的指针、回调和异构结构链 | 保留原生表达。 |

带类型与版本头的输出结构、原地替换的接口地址均使用 `ref`。异构结构指针数组保留 `BaseStructure**`，不当作连续结构数组。

对于已确认的单一输出值结构，另提供省略输出参数、直接返回该结构的同名重载。内部使用 `new T()` 初始化结构头、版本及官方默认值，再调用现有结果码重载；仅当结果为 `SLResult.Ok` 时返回结构，否则抛出 `SLException`，保留原始 `SLResult` 与对应原生函数名。非成功警告也按此规则抛出，调用方可选择结果码重载自行处理。

直接返回值重载采用默认版本和空 `Next` 链，需要预设输出结构、扩展链或检查非成功时输出内容的调用继续使用 `ref` 形式。新增重载依据 JSON 中已核实的输出契约生成，不根据 `ref` 所处位置自动推断；接口地址替换、需要调用方预填的真正输入输出参数、数组及多个输出不自动套用此规则。

引用和 Span 固定到原生调用返回，字符串按原编码转换并释放临时内存。外层固定不固定嵌套指针，也不延长资源有效期。原生入口始终保留官方允许的空参数和空缓冲形式。[4]

调用形态（初始化和设备关联已完成）：

```csharp
DLSSOptions options = new()
{
    Mode = DLSSMode.MaxQuality,
    OutputWidth = 1920,
    OutputHeight = 1080
};

DLSSOptimalSettings settings = SL.DLSS.GetOptimalSettings(in options);
```

需要自行处理 SDK 结果或提供输出结构时，使用同名结果码重载：

```csharp
DLSSOptimalSettings settings = new();
SLResult result = SL.DLSS.GetOptimalSettings(in options, ref settings);
```

直接控制地址时，在 unsafe 上下文使用同名指针入口。[3]

### 4.3 类型与生命周期

| 内容 | 映射规则 |
|---|---|
| 整数和枚举 | 保留宽度、符号性、数值和别名；明确的位掩码使用 `[Flags]`，`size_t` 使用 `nuint`。 |
| 布尔 | 原生 `bool` 使用单字节 `Bool8`；`sl::Boolean` 独立映射为 `SLBoolean`，保留无效状态。 |
| 字符指针 | 保留字符宽度和符号性；字符串重载使用接口规定的编码。 |
| 内嵌数组与联合体 | 使用 fixed、InlineArray 或 Explicit 布局，保持原地存储。 |
| 外部数据类型 | 生成接口需要的完整定义，不引入无关图形 API。 |

结构布局与 JSON 一致，包含基类前缀、私有字段及对齐。优先 Sequential，联合体和不规则布局采用 Explicit。版本化结构的构造函数设置 `Next`、`StructType`、`StructVersion` 和官方默认值，保留嵌套构造；静态类型标识为 `TypeId`，版本按 `size_t` 存储。`default(T)` 不执行自定义构造，零初始化不代表官方提供了有效默认值。[5]

对象和回调使用提取到的调用约定。`FrameToken` 是持有原生地址的只读借用包装，帧号访问通过原生虚方法；`ViewportHandle` 保留完整值结构。结构体返回回调不能仅按字段布局决定签名。[5]

析构或销毁语义对应显式释放操作，分配器分配的内存通过原分配器释放。不可复制类型的限制注明为调用约束；不提供复制便利接口，值副本不取得新的所有权。外层结构保留其拥有成员的释放责任。

COM 接口、函数地址和 SDK 数据按各接口的所有权处理。成员引用返回指向原对象；回调、字符串、结构链及资源保持规定的有效期。托管异常不得跨越原生回调边界。

## 5. 官方辅助能力

纯头文件算法在 C# 中实现，内部调用 SDK 的辅助包装转入对应原生入口。简单映射生成，复杂算法可手写；两者均计入声明覆盖。

| 类别 | 实现要求 |
|---|---|
| 字符串与预设 | 保留官方文本、未知值处理和解析行为。 |
| 结构链 | 泛型查询原生结构；头文件辅助函数的地址集合用 `List<nint>`，不将托管集合传作原生容器。 |
| 类型成员与语法宏 | 用对应的 C# 成员或控制流表达，保留单次求值及返回语义。 |
| 数学 | 保持公式、参数顺序、精度和已定义的边界行为，不隐式修改相机参数。 |
| 签名验证 | 保留信任与签名身份检查的各自语义；必要系统调用及资源释放封装在功能内部。 |
| Vulkan | 生成所需数据定义，保留字段、常量、名称匹配及合并规则，不扩展官方算法。 |

普通 Streamline 结构链节点实现以下契约，供 `FindStruct<T>` 以 `where T : unmanaged, ISLStructure` 使用；多态对象和 Vulkan 结构不实现它：

```csharp
public interface ISLStructure
{
    static abstract StructType TypeId { get; }
}
```

Vulkan 数据按官方布局生成：`VkBool32` 为 `uint`，`SType` 为 32 位 `int`，`PNext` 保留指针，结构类型常量按依赖生成。外部结构不附加 Streamline 结构头；与第三方绑定通过 ABI 一致的原生地址互操作。名称列表可提供 `ReadOnlySpan<string>` 重载。[6]

保留官方已定义的行为；发现缺陷记录来源，不把未定义行为作为稳定功能。纯数据及签名辅助不触发 interposer 加载；转调 SDK 的辅助遵守其入口前提。带共享历史的算法注明状态和线程限制。[6]

## 6. 运行库加载

```csharp
public static void SetLibraryPath(string libraryPath);
```

该方法接收 Streamline 主运行库的绝对文件路径，只保存配置。首次 SDK 原生调用加载指定文件；未配置路径或加载后重新配置时抛出 `InvalidOperationException`。

`SL` 注册程序集级 `DllImportResolver`，仅处理内部 Streamline 导入名，其他导入交给默认解析。加载采用同步，保存单个模块句柄，固定调用和内部导出解析使用同一模块；不修改全局 PATH 或工作目录。[7]

插件查找沿用 `Preferences.PathsToPlugins`，不由 wrapper 隐式改写。interposer 保留至进程结束；插件函数缓存按第 4 节失效。路径配置不触发加载，文件签名验证由调用者在加载前显式执行。

## 7. 交付与实施

### 7.1 包内容

NuGet 包包含托管程序集和 `LICENSE`；仓库与公开制品不包含 NVIDIA 原生运行文件。项目自有代码采用 MIT，`PackageLicenseExpression` 使用 `MIT`；实际采用的第三方内容按适用许可保留声明，包内内容须与许可标识相容。[8]

交付绑定库与 Generator 源码、提取 JSON、NuGet 包及本规范。必要声明保留在相关源码和随包的 `LICENSE` 中。

### 7.2 完成标准

| 项目 | 验收要求 |
|---|---|
| 覆盖 | 公开输入、依赖和排除依据有记录；范围内声明有实现对应，未分类和未处理项为零。 |
| 生成 | 离线重建结果稳定，官方辅助逻辑的变更已同步。 |
| 接口与类型 | 命名、重载、默认值、布局、回调、所有权及错误行为符合本规范和官方契约。 |
| 辅助实现 | 算法及边界对照完成，可独立执行的托管逻辑记录实际结果。 |
| 构建与包 | 两个项目构建通过，编译警告和兼容分析问题已处理，包内容及依赖正确。 |

AI 负责实现、核对和交付。报告列出来源 Release／commit、提取 Actions 结果、覆盖摘要、检查及构建打包结果、未完成项；未实际调用的原生功能不标记为运行通过。

### 7.3 自主操作

AI 可提交和推送任务相关代码、创建和更新 PR、操作提取 Actions，并在相关检查通过后合并 PR；保留仓库保护与无关修改。提取失败通过修正工作流重跑解决。

内部拆分、生成规则、辅助翻译和构建问题由实现者处理。只有具体事实要求改变范围、公开 API 或发布要求时，才提交证据与方案供用户决定。公开发布包及修改账号安全、计费或仓库保护不属于本授权。

## 参考资料

以下资料用于核对实现，不是输入清单；实际接口和算法以提取记录的 Release／commit 为准。

[1]: https://github.com/qian-o/Metal.NET "工程及生成器风格"
[2]: https://docs.github.com/en/rest/releases/releases#get-the-latest-release "GitHub Latest Release API"
[3]: https://github.com/NVIDIA-RTX/Streamline/tree/v2.14.1/include "核心和功能接口；按本次 Release 定位"
[4]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/fixed "C# 内存固定"
[5]: https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/include/sl_core_types.h "原生类型和回调；结构头见同目录 sl_struct.h"
[6]: https://github.com/NVIDIA-RTX/Streamline/tree/v2.14.1/include "官方辅助实现；按本次 Release 定位"
[7]: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading "原生库加载"
[8]: https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/license.txt "官方许可"
