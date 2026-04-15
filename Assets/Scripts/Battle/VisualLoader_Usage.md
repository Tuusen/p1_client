# VisualLoader 使用指南

## 概述
`VisualLoader` 是一个用于动态加载角色视觉表现的组件，支持：
- 根据 `RoleConfig` 配置替换槽位 Sprite
- 动态加载自定义 AnimatorController
- 资源兜底机制（找不到资源时自动隐藏）

## 使用步骤

### 1. 配置 Unity Prefab

#### 1.1 在 VisualRoot 下创建槽位节点
在你的 `Hero/Monster/Boss/Summon` 预制体中，按照以下规范创建槽位节点：

```
VisualRoot
  ├── 0_Slot (SpriteRenderer)     ← 槽位类型 0（例如：身体）
  ├── 1_Slot (SpriteRenderer)     ← 槽位类型 1（例如：头部）
  ├── 2_Slot (SpriteRenderer)     ← 槽位类型 2（例如：武器）
  └── 3_Slot (SpriteRenderer)     ← 槽位类型 3（例如：特效）
```

**命名规范**：`{slotType}_Slot` 或 `Slot_{slotType}`，其中 `slotType` 必须是整数。

#### 1.2 挂载 VisualLoader 组件
1. 选中 `VisualRoot` 节点
2. 添加组件：`VisualLoader`
3. （可选）手动指定 `slotRenderers` 列表，或让它自动查找

### 2. 配置 role.xlsx

在配置表中设置以下字段：

| 字段 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `id` | int | 角色ID | `101` |
| `name` | string | 角色名称 | `哥布林` |
| `prefabPath` | string | 基础预制体路径 | `Prefabs/actors/Monster` |
| `animatorPath` | string | 动画控制器路径（可选） | `Animations/Monster/goblin_anim` |
| `sprite_set` | JSON数组 | 槽位贴图配置 | 见下方示例 |
| `head` | string | 头像路径（对话用） | `Portrait/goblin` |

#### sprite_set 配置示例
```json
[
  {"slotType": 0, "spritePath": "Sprites/Monster/goblin_body"},
  {"slotType": 1, "spritePath": "Sprites/Monster/goblin_head"},
  {"slotType": 2, "spritePath": "Sprites/Monster/goblin_axe"}
]
```

**Excel 中的写法**（JSON 字符串）：
```
[{"slotType":0,"spritePath":"Sprites/Monster/goblin_body"},{"slotType":1,"spritePath":"Sprites/Monster/goblin_head"}]
```

### 3. 资源路径规范

#### Sprite 路径
- 相对于 `Assets/Resources/` 目录
- 不需要写扩展名（`.png`）
- 示例：`Sprites/Monster/goblin_body` → `Assets/Resources/Sprites/Monster/goblin_body.png`

#### Animator 路径
- 相对于 `Assets/Resources/` 目录
- 示例：`Animations/Monster/goblin_anim` → `Assets/Resources/Animations/Monster/goblin_anim.controller`

### 4. 工作流程

#### 初始化流程
```
UnitController.Init()
  └─ InitComponents()
  │    └─ 获取 VisualLoader 组件
  └─ InitVisual(roleId)
       ├─ 读取 RoleConfig
       ├─ VisualLoader.LoadVisual(config)
       │    ├─ 加载 Animator（如果有配置）
       │    └─ 替换槽位 Sprite
       │         ├─ 遍历所有槽位节点
       │         ├─ 根据 slotType 匹配配置
       │         ├─ 加载并替换 Sprite
       │         └─ 兜底：找不到资源则隐藏槽位
       └─ 完成
```

### 5. 兜底机制

- **资源不存在**：自动隐藏该槽位，打印警告日志
- **配置中未指定槽位**：自动隐藏该槽位
- **未配置 animatorPath**：使用 Prefab 默认的 Animator

### 6. 注意事项

#### 6.1 轴心点（Pivot）统一
所有同类槽位的图片**必须使用相同的轴心点**，否则替换后会出现位置偏移。

**建议**：
- 身体类：轴心点在脚底中心 `(0, 0)`
- 头部类：轴心点在颈部位置 `(0.5, 0.2)`
- 武器类：轴心点在握持位置 `(0.3, 0.5)`

#### 6.2 Animator 参数规范
如果多个角色共用一个基础 Prefab 但使用不同的 Animator，确保所有 Animator 的**参数名一致**：

```
通用参数：
  - Trigger: Attack, Die, Hurt, Charge
  - Bool: IsMoving
  - Float: Speed (如果需要)
```

#### 6.3 Sorting Order
`CharacterFacing` 组件会自动设置所有 `SpriteRenderer` 的 `sortingOrder = 5`。如果需要调整层级，可以在 `VisualLoader` 加载完成后手动修改。

### 7. 调试技巧

#### 查看加载信息
`VisualLoader` 会在 Console 中输出：
```
[VisualLoader] 已收集 4 个槽位
[VisualLoader] 已加载自定义 Animator: Animations/Monster/goblin_anim
[VisualLoader] 已替换 3 个槽位 Sprite (roleId=101)
```

#### 查看运行时数据
在 Unity Editor 中选中运行时的 Unit，查看 `VisualLoader` 组件的 `currentRoleId` 字段。

#### 常见问题
- **槽位没有显示**：检查 `spritePath` 是否正确，或查看 Console 警告
- **位置偏移**：检查 Sprite 的轴心点是否统一
- **动画不播放**：检查 Animator 参数名是否匹配

### 8. 扩展示例

#### 手动注册槽位（动态生成场景）
```csharp
VisualLoader loader = GetComponentInChildren<VisualLoader>();
SpriteRenderer customSlot = CreateCustomSlot();
loader.RegisterSlot(99, customSlot);  // 注册槽位类型 99
```

#### 运行时切换外观
```csharp
RoleConfig newConfig = Cfg.Role.Get(newRoleId);
visualLoader.LoadVisual(newConfig);
```

## API 参考

### VisualLoader 公共方法

| 方法 | 说明 | 参数 |
|------|------|------|
| `LoadVisual(RoleConfig config)` | 加载视觉表现 | `config`: 角色配置 |
| `RegisterSlot(int slotType, SpriteRenderer renderer)` | 手动注册槽位 | `slotType`: 槽位类型<br>`renderer`: 渲染器 |
| `ClearSlots()` | 清空所有槽位 | 无 |

## 相关文件
- `Assets/Scripts/Battle/VisualLoader.cs` - 视觉加载器核心逻辑
- `Assets/Scripts/Battle/UnitController.cs` - 单位控制器基类（集成 InitVisual）
- `Assets/Scripts/Data/Configs/RoleConfig.cs` - 角色配置数据结构
- `Assets/Scripts/Core/GameHelper.cs` - 资源加载工具（LoadSprite）
