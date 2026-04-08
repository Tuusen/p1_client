# 旧配置系统

<cite>
**本文档引用的文件**
- [Assets/Scripts/Core/Cfg.cs](file://Assets/Scripts/Core/Cfg.cs)
- [Assets/Scripts/Core/ConfigManager.cs](file://Assets/Scripts/Core/ConfigManager.cs)
- [Assets/Scripts/Core/ConfigTable.cs](file://Assets/Scripts/Core/ConfigTable.cs)
- [Assets/Scripts/Data/GameTypes.cs](file://Assets/Scripts/Data/GameTypes.cs)
- [Assets/Scripts/Data/Configs/ArcaneConfig.cs](file://Assets/Scripts/Data/Configs/ArcaneConfig.cs)
- [Assets/Scripts/Data/Configs/HeroConfig.cs](file://Assets/Scripts/Data/Configs/HeroConfig.cs)
- [Assets/Scripts/Data/Configs/SkillConfig.cs](file://Assets/Scripts/Data/Configs/SkillConfig.cs)
- [Assets/Resources/Configs/attribute_config.json](file://Assets/Resources/Configs/attribute_config.json)
- [Assets/Resources/Configs/hero_config.json](file://Assets/Resources/Configs/hero_config.json)
- [Assets/Resources/Configs/global_config.json](file://Assets/Resources/Configs/global_config.json)
- [Tools/config_gen.py](file://Tools/config_gen.py)
</cite>

## 目录
1. [简介](#简介)
2. [项目结构](#项目结构)
3. [核心组件](#核心组件)
4. [架构概览](#架构概览)
5. [详细组件分析](#详细组件分析)
6. [依赖关系分析](#依赖关系分析)
7. [性能考虑](#性能考虑)
8. [故障排除指南](#故障排除指南)
9. [结论](#结论)

## 简介

这是一个基于Unity引擎开发的配置系统，采用JSON配置文件配合自动生成的C#代码实现。该系统通过Excel到JSON再到C#代码的完整转换流程，为游戏提供了灵活且可维护的配置管理机制。

系统的核心特点包括：
- 自动化配置生成工具链
- 类型安全的配置访问接口
- 预加载资源缓存机制
- 支持多种配置类型的统一管理

## 项目结构

配置系统主要由以下几部分组成：

```mermaid
graph TB
subgraph "配置源文件"
Excel[Excel配置文件]
JSON[JSON配置文件]
end
subgraph "生成工具"
Generator[config_gen.py]
Parser[Excel解析器]
Converter[类型转换器]
CSharpGen[C#代码生成器]
end
subgraph "运行时系统"
Cfg[Cfg静态入口]
Manager[ConfigManager]
Tables[ConfigTable集合]
Types[配置类型定义]
end
subgraph "资源系统"
Resources[Resources/Configs]
Prefabs[预制体缓存]
end
Excel --> Generator
JSON --> Resources
Generator --> JSON
Generator --> CSharpGen
CSharpGen --> Types
Cfg --> Manager
Manager --> Tables
Manager --> Resources
Manager --> Prefabs
Resources --> Manager
```

**图表来源**
- [Tools/config_gen.py:587-688](file://Tools/config_gen.py#L587-L688)
- [Assets/Scripts/Core/Cfg.cs:7-33](file://Assets/Scripts/Core/Cfg.cs#L7-L33)
- [Assets/Scripts/Core/ConfigManager.cs:11-37](file://Assets/Scripts/Core/ConfigManager.cs#L11-L37)

**章节来源**
- [Tools/config_gen.py:1-688](file://Tools/config_gen.py#L1-L688)
- [Assets/Scripts/Core/Cfg.cs:1-35](file://Assets/Scripts/Core/Cfg.cs#L1-L35)
- [Assets/Scripts/Core/ConfigManager.cs:1-306](file://Assets/Scripts/Core/ConfigManager.cs#L1-L306)

## 核心组件

### 配置生成器 (config_gen.py)

配置生成器是整个系统的核心工具，负责将Excel格式的配置文件转换为JSON和对应的C#代码。

**主要功能：**
- Excel文件解析和类型推断
- JSON配置文件生成
- C#配置类代码自动生成
- 配置管理器代码生成

**章节来源**
- [Tools/config_gen.py:18-94](file://Tools/config_gen.py#L18-L94)
- [Tools/config_gen.py:240-262](file://Tools/config_gen.py#L240-L262)
- [Tools/config_gen.py:318-451](file://Tools/config_gen.py#L318-L451)

### 静态配置入口 (Cfg.cs)

提供统一的配置访问接口，通过静态属性访问各个配置表。

**核心特性：**
- 单例模式的ConfigManager访问
- 类型安全的配置表访问
- 简化的API调用体验

**章节来源**
- [Assets/Scripts/Core/Cfg.cs:7-33](file://Assets/Scripts/Core/Cfg.cs#L7-L33)

### 配置管理器 (ConfigManager.cs)

运行时配置管理的核心组件，负责配置文件的加载、初始化和缓存管理。

**主要职责：**
- 配置文件的自动加载
- 配置表的初始化
- 预制体资源的预加载缓存
- 自定义业务逻辑方法

**章节来源**
- [Assets/Scripts/Core/ConfigManager.cs:11-306](file://Assets/Scripts/Core/ConfigManager.cs#L11-L306)

### 配置表 (ConfigTable.cs)

通用的配置数据结构，支持带元数据和不带元数据的两种配置表类型。

**设计模式：**
- 泛型类型安全
- 字典索引优化查找性能
- 统一的数据访问接口

**章节来源**
- [Assets/Scripts/Core/ConfigTable.cs:11-73](file://Assets/Scripts/Core/ConfigTable.cs#L11-L73)

## 架构概览

```mermaid
classDiagram
class Cfg {
+static Arcane
+static Attribute
+static Buff
+static ConfigTable Get(id)
}
class ConfigManager {
+static Instance
+arcaneTable
+attributeTable
+buffTable
+LoadAllConfigs()
+GetSkillConfigByPool()
+GetBulletPrefab()
+GetEffectPrefab()
}
class ConfigTable~TItem,TMeta~ {
+TItem[] All
+TMeta Meta
+Init(items, meta, keySelector)
+Get(id) TItem
}
class ConfigMeta~TMeta~ {
+TMeta Meta
+Init(meta)
}
class ArcaneConfig {
+int id
+string name
+int[] events
}
class HeroConfig {
+int id
+string name
+AttrEntry[] attrs
}
class GameTypes {
<<static>>
+AttrEntry
+BulletEventData
+SkillCategory
}
Cfg --> ConfigManager : "静态入口"
ConfigManager --> ConfigTable : "管理多个表"
ConfigTable --> ArcaneConfig : "存储配置项"
ConfigTable --> HeroConfig : "存储配置项"
ConfigTable --> GameTypes : "使用类型定义"
ConfigMeta --> GameTypes : "使用类型定义"
```

**图表来源**
- [Assets/Scripts/Core/Cfg.cs:7-33](file://Assets/Scripts/Core/Cfg.cs#L7-L33)
- [Assets/Scripts/Core/ConfigManager.cs:11-37](file://Assets/Scripts/Core/ConfigManager.cs#L11-L37)
- [Assets/Scripts/Core/ConfigTable.cs:11-73](file://Assets/Scripts/Core/ConfigTable.cs#L11-L73)
- [Assets/Scripts/Data/GameTypes.cs:8-82](file://Assets/Scripts/Data/GameTypes.cs#L8-L82)

## 详细组件分析

### 配置生成流程

```mermaid
sequenceDiagram
participant Excel as Excel文件
participant Generator as 配置生成器
participant JSON as JSON文件
participant CSharp as C#代码
participant Runtime as 运行时系统
Excel->>Generator : 输入Excel配置
Generator->>Generator : 解析Excel结构
Generator->>Generator : 推断数据类型
Generator->>JSON : 生成JSON配置
Generator->>CSharp : 生成配置类
Generator->>CSharp : 生成配置管理器
CSharp->>Runtime : 编译到项目
JSON->>Runtime : 资源打包
Runtime->>Runtime : 初始化配置系统
```

**图表来源**
- [Tools/config_gen.py:587-688](file://Tools/config_gen.py#L587-L688)
- [Tools/config_gen.py:240-262](file://Tools/config_gen.py#L240-L262)

### 配置加载流程

```mermaid
flowchart TD
Start([应用启动]) --> LoadManager[加载ConfigManager]
LoadManager --> LoadAllConfigs[调用LoadAllConfigs]
LoadAllConfigs --> LoadArcane[加载arcane_config.json]
LoadAllConfigs --> LoadAttribute[加载attribute_config.json]
LoadAllConfigs --> LoadHero[加载hero_config.json]
LoadAllConfigs --> LoadOther[加载其他配置文件]
LoadArcane --> ParseArcane[解析ArcaneConfigData]
LoadAttribute --> ParseAttribute[解析AttributeConfigData]
LoadHero --> ParseHero[解析HeroConfigData]
LoadOther --> ParseOther[解析其他配置数据]
ParseArcane --> InitArcaneTable[初始化Arcane配置表]
ParseAttribute --> InitAttributeTable[初始化Attribute配置表]
ParseHero --> InitHeroTable[初始化Hero配置表]
ParseOther --> InitOtherTables[初始化其他配置表]
InitArcaneTable --> PreloadPrefabs[预加载预制体]
InitAttributeTable --> PreloadPrefabs
InitHeroTable --> PreloadPrefabs
InitOtherTables --> PreloadPrefabs
PreloadPrefabs --> Ready([配置系统就绪])
```

**图表来源**
- [Assets/Scripts/Core/ConfigManager.cs:48-177](file://Assets/Scripts/Core/ConfigManager.cs#L48-L177)
- [Assets/Scripts/Core/ConfigManager.cs:255-298](file://Assets/Scripts/Core/ConfigManager.cs#L255-L298)

### 数据类型系统

系统支持多种数据类型，通过类型推断和转换实现：

```mermaid
classDiagram
class TypeInfo {
<<abstract>>
}
class PrimitiveType {
+string name
+csharp_type string
}
class ArrayType {
+TypeInfo element_type
+csharp_type string
}
class StructArrayType {
+string struct_name
+fields[]
+is_anonymous bool
+csharp_type string
}
TypeInfo <|-- PrimitiveType
TypeInfo <|-- ArrayType
TypeInfo <|-- StructArrayType
class AttrEntry {
+int id
+int value
}
class BulletEventData {
+int pierceCount
+int explosionDmgRate
+int[] attachToTargetEventIds
+Clone() BulletEventData
}
TypeInfo --> AttrEntry : "使用"
TypeInfo --> BulletEventData : "使用"
```

**图表来源**
- [Tools/config_gen.py:22-94](file://Tools/config_gen.py#L22-L94)
- [Assets/Scripts/Data/GameTypes.cs:8-56](file://Assets/Scripts/Data/GameTypes.cs#L8-L56)

**章节来源**
- [Tools/config_gen.py:18-94](file://Tools/config_gen.py#L18-L94)
- [Assets/Scripts/Data/GameTypes.cs:8-82](file://Assets/Scripts/Data/GameTypes.cs#L8-L82)

### 配置表访问模式

```mermaid
sequenceDiagram
participant Client as 客户端代码
participant Cfg as Cfg静态入口
participant Manager as ConfigManager
participant Table as ConfigTable
participant Lookup as 字典查找
Client->>Cfg : Cfg.Hero.Get(1)
Cfg->>Manager : 获取heroTable
Manager->>Table : 返回ConfigTable<HeroConfig>
Table->>Lookup : 查找id=1的英雄配置
Lookup-->>Table : 返回HeroConfig对象
Table-->>Client : 返回配置数据
Note over Client,Lookup : O(1)时间复杂度查找
```

**图表来源**
- [Assets/Scripts/Core/Cfg.cs:23](file://Assets/Scripts/Core/Cfg.cs#L23)
- [Assets/Scripts/Core/ConfigTable.cs:26-32](file://Assets/Scripts/Core/ConfigTable.cs#L26-L32)

**章节来源**
- [Assets/Scripts/Core/Cfg.cs:11-32](file://Assets/Scripts/Core/Cfg.cs#L11-L32)
- [Assets/Scripts/Core/ConfigTable.cs:26-32](file://Assets/Scripts/Core/ConfigTable.cs#L26-L32)

## 依赖关系分析

```mermaid
graph TB
subgraph "生成工具层"
config_gen.py[config_gen.py]
openpyxl[openpyxl库]
end
subgraph "配置定义层"
ArcaneConfig.cs[ArcaneConfig.cs]
HeroConfig.cs[HeroConfig.cs]
SkillConfig.cs[SkillConfig.cs]
GameTypes.cs[GameTypes.cs]
end
subgraph "运行时层"
Cfg.cs[Cfg.cs]
ConfigManager.cs[ConfigManager.cs]
ConfigTable.cs[ConfigTable.cs]
end
subgraph "配置数据层"
attribute_config.json[attribute_config.json]
hero_config.json[hero_config.json]
global_config.json[global_config.json]
end
config_gen.py --> ArcaneConfig.cs
config_gen.py --> HeroConfig.cs
config_gen.py --> SkillConfig.cs
config_gen.py --> GameTypes.cs
ArcaneConfig.cs --> ConfigManager.cs
HeroConfig.cs --> ConfigManager.cs
SkillConfig.cs --> ConfigManager.cs
GameTypes.cs --> ConfigManager.cs
Cfg.cs --> ConfigManager.cs
ConfigManager.cs --> attribute_config.json
ConfigManager.cs --> hero_config.json
ConfigManager.cs --> global_config.json
```

**图表来源**
- [Tools/config_gen.py:16-20](file://Tools/config_gen.py#L16-L20)
- [Assets/Scripts/Core/ConfigManager.cs:15-37](file://Assets/Scripts/Core/ConfigManager.cs#L15-L37)

**章节来源**
- [Tools/config_gen.py:16-20](file://Tools/config_gen.py#L16-L20)
- [Assets/Scripts/Core/ConfigManager.cs:15-37](file://Assets/Scripts/Core/ConfigManager.cs#L15-L37)

## 性能考虑

### 内存优化策略

1. **字典索引优化**
   - 使用哈希表实现O(1)的配置查找
   - 避免线性搜索带来的性能问题

2. **资源预加载缓存**
   - 预加载常用的预制体资源
   - 减少运行时资源加载开销

3. **延迟初始化**
   - 按需加载配置表
   - 避免不必要的内存占用

### 加载性能优化

```mermaid
flowchart LR
subgraph "加载阶段"
A[应用启动] --> B[ConfigManagerAwake]
B --> C[LoadAllConfigs]
C --> D[逐个加载配置]
end
subgraph "运行阶段"
E[首次访问] --> F[字典查找]
F --> G[返回配置]
end
D --> E
subgraph "缓存机制"
H[预制体缓存] --> I[BulletPrefabs]
H --> J[EffectPrefabs]
H --> K[RolePrefabs]
end
D --> H
```

**图表来源**
- [Assets/Scripts/Core/ConfigManager.cs:48-54](file://Assets/Scripts/Core/ConfigManager.cs#L48-L54)
- [Assets/Scripts/Core/ConfigManager.cs:255-298](file://Assets/Scripts/Core/ConfigManager.cs#L255-L298)

## 故障排除指南

### 常见问题及解决方案

**配置文件加载失败**
- 检查JSON文件路径是否正确
- 验证JSON格式的有效性
- 确认文件已添加到Resources目录

**配置数据解析错误**
- 检查Excel配置中的数据类型
- 验证字段名称的一致性
- 确认生成的C#代码编译通过

**运行时访问异常**
- 确认ConfigManager已正确初始化
- 检查配置表的键值唯一性
- 验证预制体路径的有效性

**章节来源**
- [Assets/Scripts/Core/ConfigManager.cs:179-194](file://Assets/Scripts/Core/ConfigManager.cs#L179-L194)
- [Assets/Scripts/Core/ConfigManager.cs:255-298](file://Assets/Scripts/Core/ConfigManager.cs#L255-L298)

## 结论

旧配置系统通过自动化工具链实现了从Excel到JSON再到C#代码的完整转换，提供了类型安全、高性能的配置管理方案。系统的主要优势包括：

1. **开发效率提升** - 自动化生成减少手工编码工作
2. **类型安全保障** - 编译时类型检查避免运行时错误
3. **性能优化** - 字典索引和资源缓存机制
4. **扩展性强** - 支持新的配置类型和业务需求

该系统为游戏开发提供了稳定可靠的配置管理基础设施，支持复杂的配置数据结构和多样的使用场景。