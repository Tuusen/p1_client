---
name: window-generator
description: 根据用户描述生成符合AutoBindUIComponents命名规则的窗口CS文件和对应的prefab文件。当用户需要创建新的UI窗口时调用。
---

# 窗口生成器

## 功能描述

此技能根据用户提供的窗口描述和功能需求，自动生成：
1. 继承自BaseWin的CS文件
2. 对应的prefab文件
3. 符合AutoBindUIComponents命名规则的UI组件绑定

## 命名规则

生成的文件和组件将遵循以下命名规则：

### CS文件命名
- 文件名：`[窗口名称]Win.cs`
- 类名：`[窗口名称]Win`
- 继承自：`BaseWin`

### UI组件命名
- 文本：`txt_[名称]`
- 按钮：`btn_[名称]`
- 复选框：`toggle_[名称]`
- 节点：`node_[名称]`
- 图片：`sp_[名称]`

### Prefab文件命名
- 文件名：`[窗口名称]Win.prefab`

## 使用方法

1. 调用此技能
2. 提供窗口的描述和功能需求
3. 技能会自动生成CS文件和prefab文件

## 示例

### 输入示例
```
创建一个登录窗口，包含用户名输入框、密码输入框、登录按钮和注册按钮。
```

### 输出示例
- CS文件：`LoginWin.cs`
- Prefab文件：`LoginWin.prefab`

## 生成规则

1. 根据用户描述识别UI组件类型
2. 生成符合命名规则的字段和组件
3. 在CS文件中实现必要的方法
4. 生成包含相应UI组件的prefab文件

## 注意事项

- 生成的文件会放在合适的目录中
- 确保BaseWin.cs文件存在于项目中
- 生成的代码会自动调用AutoBindUIComponents方法进行UI绑定