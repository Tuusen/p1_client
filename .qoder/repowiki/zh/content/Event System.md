# 事件系统

<cite>
**本文档引用的文件**
- [EventExecutor.cs](file://Assets/Scripts/Battle/EventExecutor.cs)
- [BulletEventExecutor.cs](file://Assets/Scripts/Battle/BulletEventExecutor.cs)
- [EventEffectManager.cs](file://Assets/Scripts/Battle/EventEffectManager.cs)
- [EventConfig.cs](file://Assets/Scripts/Data/Configs/EventConfig.cs)
- [BulletEventConfig.cs](file://Assets/Scripts/Data/Configs/BulletEventConfig.cs)
- [EventEffectConfig.cs](file://Assets/Scripts/Data/Configs/EventEffectConfig.cs)
- [GameTypes.cs](file://Assets/Scripts/Data/GameTypes.cs)
- [Cfg.cs](file://Assets/Scripts/Core/Cfg.cs)
- [DamageCalculator.cs](file://Assets/Scripts/Battle/DamageCalculator.cs)
- [BuffSystem.cs](file://Assets/Scripts/Battle/BuffSystem.cs)
- [PassiveSystem.cs](file://Assets/Scripts/Battle/PassiveSystem.cs)
- [event_config.json](file://Assets/Resources/Configs/event_config.json)
- [bullet_event_config.json](file://Assets/Resources/Configs/bullet_event_config.json)
</cite>

## 更新摘要
**所做更改**
- 更新了事件配置文件优化部分，反映了击退事件描述文本的优化
- 新增了UI文本一致性改进的详细说明
- 更新了相关配置文件和数据结构的描述

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

事件系统是几何塔防游戏的核心机制，负责处理战斗中的各种效果和交互。该系统采用配置驱动的设计模式，通过JSON配置文件定义事件类型、参数和行为，实现了高度可扩展的游戏机制。

系统主要包含三个核心功能模块：
- **基础事件执行器**：处理伤害、治疗、护盾等基础战斗效果
- **子弹事件执行器**：管理子弹的特殊行为如穿透、爆炸、追踪等
- **事件效果管理器**：负责视觉特效的触发和播放，现已增强支持基于爆炸半径的动态缩放

## 项目结构

事件系统位于项目的Battle目录下，采用分层架构设计：

```mermaid
graph TB
subgraph "事件系统架构"
subgraph "执行器层"
EE[EventExecutor<br/>基础事件执行器]
BE[BulletEventExecutor<br/>子弹事件执行器]
EM[EventEffectManager<br/>事件效果管理器]
end
subgraph "配置层"
EC[EventConfig<br/>事件配置]
BEC[BulletEventConfig<br/>子弹事件配置]
EEC[EventEffectConfig<br/>事件效果配置]
end
subgraph "数据层"
GT[GameTypes<br/>运行时数据结构]
DC[DamageCalculator<br/>伤害计算器]
BS[BuffSystem<br/>增益效果系统]
PS[PassiveSystem<br/>被动效果系统]
end
subgraph "配置文件"
EJ[event_config.json<br/>事件配置文件]
BEJ[bullet_event_config.json<br/>子弹事件配置文件]
end
end
EE --> EC
BE --> BEC
EM --> EEC
EE --> DC
BS --> EE
PS --> EE
EC --> EJ
BEC --> BEJ
```

**图表来源**
- [EventExecutor.cs:1-233](file://Assets/Scripts/Battle/EventExecutor.cs#L1-L233)
- [BulletEventExecutor.cs:1-98](file://Assets/Scripts/Battle/BulletEventExecutor.cs#L1-L98)
- [EventEffectManager.cs:1-147](file://Assets/Scripts/Battle/EventEffectManager.cs#L1-L147)

**章节来源**
- [EventExecutor.cs:1-233](file://Assets/Scripts/Battle/EventExecutor.cs#L1-L233)
- [BulletEventExecutor.cs:1-98](file://Assets/Scripts/Battle/BulletEventExecutor.cs#L1-L98)
- [EventEffectManager.cs:1-147](file://Assets/Scripts/Battle/EventEffectManager.cs#L1-L147)

## 核心组件

### 事件执行器 (EventExecutor)

事件执行器是整个事件系统的核心控制器，负责解析事件配置并执行相应的游戏逻辑。

**主要功能**：
- 解析事件ID并获取对应的配置信息
- 根据事件类型调用相应的处理方法
- 管理事件上下文（施法者、目标、位置等）

**事件类型支持**：
- 伤害事件：造成固定或百分比伤害
- 治疗事件：恢复生命值
- 护盾事件：添加护盾值
- 击退事件：将目标击退指定距离
- 经验事件：为技能增加经验值
- 能量事件：为奥术系统添加能量
- 增益事件：为目标添加增益效果
- 被动事件：注册被动技能
- 召唤事件：生成友方单位
- 洗髓事件：移除目标身上的效果

**章节来源**
- [EventExecutor.cs:13-66](file://Assets/Scripts/Battle/EventExecutor.cs#L13-L66)
- [EventExecutor.cs:68-231](file://Assets/Scripts/Battle/EventExecutor.cs#L68-L231)

### 子弹事件执行器 (BulletEventExecutor)

专门处理子弹特殊行为的执行器，将配置转换为运行时数据结构。

**支持的子弹事件**：
- 穿透：子弹可以穿透多个目标
- 爆炸：子弹命中后产生范围伤害，现已支持基于爆炸半径的动态缩放
- 追踪：子弹自动追踪目标
- 散布：子弹分裂成多发
- 跳弹：子弹在场景中反弹
- 簇射：同时发射多颗子弹
- 附着：将事件附加到目标或施法者

**章节来源**
- [BulletEventExecutor.cs:6-95](file://Assets/Scripts/Battle/BulletEventExecutor.cs#L6-L95)

### 事件效果管理器 (EventEffectManager)

负责根据事件类型触发相应的视觉特效，现已增强支持基于爆炸半径的动态缩放功能。

**功能特性**：
- 从配置表获取特效信息
- 实例化预制件进行特效播放
- 支持多种特效类型和持续时间
- **新增**：基于爆炸半径的动态缩放功能
- **新增**：支持自定义缩放比例参数

**爆炸半径缩放增强功能**：
- 自动检测爆炸类特效的半径参数
- 根据爆炸半径动态调整特效尺寸
- 提供平滑的缩放过渡效果
- 支持自定义缩放系数调节

**章节来源**
- [EventEffectManager.cs:8-31](file://Assets/Scripts/Battle/EventEffectManager.cs#L8-L31)

## 架构概览

事件系统的整体架构采用分层设计，确保了良好的可维护性和扩展性：

```mermaid
sequenceDiagram
participant Player as 玩家输入
participant Battle as 战斗管理器
participant Executor as 事件执行器
participant Target as 目标对象
participant Effect as 特效系统
Player->>Battle : 发起技能使用
Battle->>Executor : ExecuteEvent(eventId, context)
Executor->>Executor : 解析事件配置
Executor->>Target : 执行相应效果
Target->>Effect : 触发视觉特效
Effect->>Effect : 检测爆炸半径参数
Effect->>Effect : 应用动态缩放
Effect-->>Target : 播放特效动画
Target-->>Battle : 更新状态信息
Battle-->>Player : 显示结果反馈
```

**图表来源**
- [EventExecutor.cs:15-66](file://Assets/Scripts/Battle/EventExecutor.cs#L15-L66)
- [EventEffectManager.cs:13-19](file://Assets/Scripts/Battle/EventEffectManager.cs#L13-L19)

## 详细组件分析

### 事件配置系统

事件系统采用配置驱动的方式，所有事件行为都由外部配置文件定义：

```mermaid
classDiagram
class EventConfig {
+int id
+int type
+string name
+string des
+int[] args
}
class BulletEventConfig {
+int id
+int type
+string name
+string des
+int[] args
}
class EventEffectConfig {
+int eventType
+float duration
+string target
+string prefabPath
}
class Cfg {
+Event Get(int id)
+BulletEvent Get(int id)
+EventEffect Get(int id)
}
EventConfig --> Cfg : 使用
BulletEventConfig --> Cfg : 使用
EventEffectConfig --> Cfg : 使用
```

**图表来源**
- [EventConfig.cs:11-24](file://Assets/Scripts/Data/Configs/EventConfig.cs#L11-L24)
- [BulletEventConfig.cs:11-25](file://Assets/Scripts/Data/Configs/BulletEventConfig.cs#L11-L25)
- [EventEffectConfig.cs:11-23](file://Assets/Scripts/Data/Configs/EventEffectConfig.cs#L11-L23)
- [Cfg.cs:7-34](file://Assets/Scripts/Core/Cfg.cs#L7-L34)

### 伤害计算系统

伤害系统实现了复杂的战斗平衡机制：

```mermaid
flowchart TD
Start([开始伤害计算]) --> HitCheck["命中检定<br/>命中率 - 闪避率"]
HitCheck --> Hit{"是否命中?"}
HitCheck --> Miss["闪避效果"]
Hit --> |否| Miss
Hit --> |是| BaseCalc["基础伤害计算<br/>攻击力 × 技能倍率 / 10000"]
BaseCalc --> ElemBonus["元素伤害加成<br/>攻击方元素伤害 + 全元素加成"]
ElemBonus --> ElemReduce["元素伤害减免<br/>防御方元素减免 + 全元素减免"]
ElemReduce --> CritCheck["暴击检定<br/>暴击率 - 暴击抵抗"]
CritCheck --> Crit{"是否暴击?"}
CritCheck --> Normal["普通伤害"]
Crit --> |是| CritCalc["暴击伤害计算<br/>基础伤害 × (1 + 暴击伤害 - 暴击抗性)"]
Crit --> |否| Normal
Normal --> BossElite["Boss/精英加成<br/>Boss伤害加成 + 精英伤害加成"]
CritCalc --> BossElite
BossElite --> Final["最终伤害<br/>Max(0, 计算结果)"]
Miss --> End([结束])
Final --> End
```

**图表来源**
- [DamageCalculator.cs:24-117](file://Assets/Scripts/Battle/DamageCalculator.cs#L24-L117)

**章节来源**
- [DamageCalculator.cs:22-118](file://Assets/Scripts/Battle/DamageCalculator.cs#L22-L118)

### 增益效果系统

增益效果系统提供了复杂的状态管理机制：

```mermaid
classDiagram
class BuffEntry {
+int buffConfigId
+BuffConfig cachedConfig
+int stackCount
+float remainingTime
+float tickTimer
+IBuffTarget caster
}
class BuffSystem {
+BuffEntry[] buffs
+AddBuff(int, IBuffTarget, IBuffTarget)
+RemoveBuffByConfigId(int, int)
+RemoveBuffsByType(int, int)
+Tick(float, IBuffTarget)
+HasSpecialEffect(int) bool
}
class IBuffTarget {
<<interface>>
+Attrs AttrComponent
+BuffSystem BuffSystem
+PassiveSystem PassiveSystem
+OnBuffDamage(float)
+OnBuffHeal(float)
+AddShield(int)
+bool IsDead
+Vector3 Position
}
BuffSystem --> BuffEntry : 管理
BuffEntry --> IBuffTarget : 影响
BuffSystem --> IBuffTarget : 接口
```

**图表来源**
- [BuffSystem.cs:6-30](file://Assets/Scripts/Battle/BuffSystem.cs#L6-L30)
- [BuffSystem.cs:30-84](file://Assets/Scripts/Battle/BuffSystem.cs#L30-L84)
- [BuffSystem.cs:16-28](file://Assets/Scripts/Battle/BuffSystem.cs#L16-L28)

**章节来源**
- [BuffSystem.cs:30-378](file://Assets/Scripts/Battle/BuffSystem.cs#L30-L378)

### 被动效果系统

被动系统实现了基于触发条件的效果机制：

```mermaid
sequenceDiagram
participant Trigger as 触发源
participant Passive as 被动系统
participant Config as 配置检查
participant Condition as 条件检定
participant EventExec as 事件执行器
Trigger->>Passive : OnTrigger(triggerCode, context)
Passive->>Config : 检查事件目标匹配
Config-->>Passive : 返回匹配结果
Passive->>Condition : 检查触发条件
Condition-->>Passive : 返回条件结果
Passive->>EventExec : ExecuteEvents(events, context)
EventExec-->>Passive : 执行完成
Passive->>Passive : 更新触发计数
Passive->>Passive : 检查移除条件
```

**图表来源**
- [PassiveSystem.cs:41-69](file://Assets/Scripts/Battle/PassiveSystem.cs#L41-L69)

**章节来源**
- [PassiveSystem.cs:14-150](file://Assets/Scripts/Battle/PassiveSystem.cs#L14-L150)

### 爆炸特效缩放系统

**新增功能**：基于爆炸半径的动态特效缩放

爆炸特效缩放系统是事件效果管理器的重要增强功能，为爆炸类特效提供了智能的尺寸调整能力：

```mermaid
flowchart TD
Start([爆炸特效触发]) --> CheckType{"检查特效类型"}
CheckType --> |爆炸类| ExtractRadius["提取爆炸半径参数"]
CheckType --> |非爆炸类| DefaultScale["使用默认缩放比例"]
ExtractRadius --> CalcScale["计算缩放比例<br/>scale = baseScale + radiusFactor × 半径"]
CalcScale --> ApplyScale["应用缩放比例到特效"]
ApplyScale --> PlayEffect["播放特效动画"]
DefaultScale --> PlayEffect
PlayEffect --> End([特效播放完成])
```

**图表来源**
- [EventEffectManager.cs:17-63](file://Assets/Scripts/Battle/EventEffectManager.cs#L17-L63)

**功能特点**：
- **智能缩放**：根据爆炸半径自动调整特效大小
- **平滑过渡**：提供自然的缩放动画效果
- **参数调节**：支持自定义缩放系数和基础比例
- **兼容性**：不影响现有非爆炸类特效

**章节来源**
- [EventEffectManager.cs:17-63](file://Assets/Scripts/Battle/EventEffectManager.cs#L17-L63)

### 事件配置文件优化

**更新**：击退事件描述文本优化，提升UI文本呈现的一致性和减少冗余

事件配置文件经过优化，特别是击退（knockback）事件的描述文本得到了改进：

**优化前的问题**：
- 击退事件描述统一为"击退目标力度"，缺乏具体数值信息
- UI展示时无法提供准确的数值反馈
- 文本表述过于简单，用户体验不佳

**优化后的改进**：
- 保持了统一的描述格式，确保UI文本一致性
- 击退事件现在能够正确显示具体的击退力度数值
- 减少了重复的UI文本，提升了信息呈现效率
- 为后续的UI本地化和国际化提供了更好的基础

**章节来源**
- [event_config.json:343-441](file://Assets/Resources/Configs/event_config.json#L343-L441)

## 依赖关系分析

事件系统各组件之间的依赖关系如下：

```mermaid
graph TB
subgraph "核心依赖"
Cfg[Cfg配置访问器] --> EventConfig
Cfg --> BulletEventConfig
Cfg --> EventEffectConfig
end
subgraph "执行器依赖"
EventExecutor --> Cfg
EventExecutor --> DamageCalculator
EventExecutor --> BuffSystem
EventExecutor --> PassiveSystem
BulletEventExecutor --> Cfg
EventEffectManager --> Cfg
end
subgraph "数据结构依赖"
GameTypes --> BulletEventData
BuffSystem --> IBuffTarget
PassiveSystem --> EventExecutor
end
subgraph "配置文件依赖"
EventConfig --> event_config.json
BulletEventConfig --> bullet_event_config.json
end
```

**图表来源**
- [Cfg.cs:9-33](file://Assets/Scripts/Core/Cfg.cs#L9-L33)
- [EventExecutor.cs:24-25](file://Assets/Scripts/Battle/EventExecutor.cs#L24-L25)
- [BulletEventExecutor.cs:19-20](file://Assets/Scripts/Battle/BulletEventExecutor.cs#L19-L20)

**章节来源**
- [Cfg.cs:7-35](file://Assets/Scripts/Core/Cfg.cs#L7-L35)

## 性能考虑

事件系统在设计时充分考虑了性能优化：

### 内存管理
- 使用对象池避免频繁的垃圾回收
- 缓存配置数据减少重复查询
- 合理的数据结构选择（数组vs列表）

### 执行效率
- 事件类型switch语句优化
- 条件检查的短路评估
- 批量处理相似事件

### 内存优化策略
- 配置数据只读缓存
- 事件上下文重用
- 特效实例化的延迟加载

### 爆炸特效缩放性能优化
- **缩放计算缓存**：避免重复计算相同的缩放比例
- **动画播放优化**：智能检测动画时长，避免不必要的等待
- **特效生命周期管理**：自动销毁不需要的特效实例

## 故障排除指南

### 常见问题及解决方案

**事件不生效**
- 检查事件ID是否正确配置
- 验证事件参数格式是否正确
- 确认目标对象是否存活

**伤害计算异常**
- 检查属性配置是否合理
- 验证元素相克关系
- 确认暴击计算逻辑

**特效不显示**
- 检查特效预制件路径
- 验证特效配置参数
- 确认特效生命周期管理

**爆炸特效缩放异常**
- 检查爆炸半径参数是否正确设置
- 验证缩放系数配置
- 确认特效预制件支持缩放变换

**UI文本显示问题**
- 检查事件描述文本格式
- 验证击退力度数值显示
- 确认UI布局和字体设置

**性能问题**
- 优化事件批量处理
- 减少不必要的配置查询
- 实施适当的对象池

**章节来源**
- [EventExecutor.cs:62-65](file://Assets/Scripts/Battle/EventExecutor.cs#L62-L65)
- [EventEffectManager.cs:23-29](file://Assets/Scripts/Battle/EventEffectManager.cs#L23-L29)

## 结论

事件系统通过其模块化设计和配置驱动的架构，为游戏提供了强大而灵活的效果机制。系统的主要优势包括：

1. **高度可扩展性**：新的事件类型可以通过配置轻松添加
2. **良好的性能表现**：优化的执行流程和内存管理
3. **清晰的架构分离**：职责明确的组件设计
4. **强大的配置系统**：外部配置文件便于平衡调整
5. **智能特效缩放**：新增的基于爆炸半径的动态缩放功能，提升了视觉效果的动态适应性
6. **UI文本优化**：击退事件描述文本的优化，提升了用户界面的一致性和用户体验

**最新增强功能**：
- **爆炸特效缩放**：自动根据爆炸半径调整特效尺寸
- **智能缩放算法**：提供平滑自然的缩放过渡效果
- **参数化配置**：支持自定义缩放系数和基础比例
- **UI文本一致性**：击退事件描述文本优化，减少冗余信息

该系统为几何塔防游戏的核心玩法提供了坚实的技术基础，支持复杂的游戏机制实现和快速的功能迭代。新增的爆炸特效缩放功能和UI文本优化进一步增强了视觉效果的表现力和用户体验，为玩家提供了更加沉浸式的游戏体验。