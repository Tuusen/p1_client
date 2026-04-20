#!/usr/bin/env python
# -*- coding: utf-8 -*-
import os
import re
import json

# 配置路径
SCRIPTS_DIR = os.path.join('Assets', 'Scripts', 'UI', 'Windows')
PREFABS_DIR = os.path.join('Assets', 'Prefabs', 'Windows')

# UI组件类型映射
UI_COMPONENT_TYPES = {
    '文本': 'Text',
    '按钮': 'Button',
    '复选框': 'Toggle',
    '输入框': 'InputField',
    '图片': 'Image',
    '节点': 'Transform'
}

# 前缀映射
PREFIX_MAP = {
    'Text': 'txt_',
    'Button': 'btn_',
    'Toggle': 'toggle_',
    'InputField': 'input_',
    'Image': 'sp_',
    'Transform': 'node_'
}

def parse_window_description(description):
    """解析窗口描述，提取窗口名称和UI组件"""
    # 提取窗口名称
    window_name = 'New'
    if '创建一个' in description and '窗口' in description:
        start_idx = description.find('创建一个') + 4
        end_idx = description.find('窗口', start_idx)
        if start_idx < end_idx:
            window_name = description[start_idx:end_idx].strip()
    
    # 提取UI组件
    components = []
    
    # 检查是否包含组件描述
    if '包含' in description:
        # 提取包含后面的内容
        start_idx = description.find('包含') + 2
        content = description[start_idx:]
        
        # 处理内容，去除末尾标点
        if content.endswith('。'):
            content = content[:-1]
        
        # 分割组件
        parts = content.split('、')
        
        for part in parts:
            part = part.strip()
            if not part:
                continue
            
            # 识别组件类型
            if '输入框' in part:
                component_type = '输入框'
                name = part.replace('输入框', '').strip()
                if name:
                    components.append({'name': name, 'type': component_type})
            elif '按钮' in part:
                component_type = '按钮'
                name = part.replace('按钮', '').strip()
                if name:
                    components.append({'name': name, 'type': component_type})
            elif '文本' in part:
                component_type = '文本'
                name = part.replace('文本', '').strip()
                if name:
                    components.append({'name': name, 'type': component_type})
            elif '复选框' in part:
                component_type = '复选框'
                name = part.replace('复选框', '').strip()
                if name:
                    components.append({'name': name, 'type': component_type})
            elif '图片' in part:
                component_type = '图片'
                name = part.replace('图片', '').strip()
                if name:
                    components.append({'name': name, 'type': component_type})
    
    return window_name, components

def generate_cs_file(window_name, components):
    """生成CS文件"""
    # 确保窗口名称只包含有效的字符
    import re
    window_name = re.sub(r'[^a-zA-Z0-9_]', '', window_name)
    if not window_name:
        window_name = 'New'
    
    class_name = "{0}Win".format(window_name)
    file_path = os.path.join(SCRIPTS_DIR, "{0}.cs".format(class_name))
    
    # 生成字段声明
    fields = []
    for component in components:
        component_type = UI_COMPONENT_TYPES.get(component['type'], 'Transform')
        prefix = PREFIX_MAP.get(component_type, 'node_')
        # 确保组件名称只包含有效的字符
        component_name = re.sub(r'[^a-zA-Z0-9_]', '', component['name'])
        if not component_name:
            continue
        field_name = "{0}{1}".format(prefix, component_name)
        fields.append("    [SerializeField] private {0} {1};".format(component_type, field_name))
    
    # 生成按钮case语句
    btn_cases = []
    for component in components:
        if component['type'] == '按钮':
            component_name = re.sub(r'[^a-zA-Z0-9_]', '', component['name'])
            if component_name:
                btn_cases.append('                case "btn_{0}":'.format(component_name))
    
    # 生成复选框case语句
    toggle_cases = []
    for component in components:
        if component['type'] == '复选框':
            component_name = re.sub(r'[^a-zA-Z0-9_]', '', component['name'])
            if component_name:
                toggle_cases.append('                case "toggle_{0}":'.format(component_name))
    
    # 生成CS文件内容
    cs_content = """using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{{
    public class {0} : BaseWin
    {{
{1}

        public override void load()
        {{
            base.load();
            // 初始化逻辑
        }}

        public override void start()
        {{
            base.start();
            // 显示逻辑
        }}

        public override void onBtnClick(Button btn, object param)
        {{
            base.onBtnClick(btn, param);
            string name = btn.name;
            switch (name)
            {{
{2}
                    // 按钮点击逻辑
                    break;
                default:
                    break;
            }}
        }}

        public override void onToggleClick(Toggle toggle, object param)
        {{
            base.onToggleClick(toggle, param);
            string name = toggle.name;
            switch (name)
            {{
{3}
                    // 复选框点击逻辑
                    break;
                default:
                    break;
            }}
        }}
    }}
}}""".format(class_name, chr(10).join(fields), chr(10).join(btn_cases), chr(10).join(toggle_cases))
    
    # 确保目录存在
    dir_path = os.path.dirname(file_path)
    if not os.path.exists(dir_path):
        os.makedirs(dir_path)
    
    # 写入文件
    with open(file_path, 'w') as f:
        f.write(cs_content)
    
    return file_path

def generate_prefab_file(window_name, components):
    """生成prefab文件"""
    prefab_name = "{0}Win".format(window_name)
    file_path = os.path.join(PREFABS_DIR, "{0}.prefab".format(prefab_name))
    
    # 生成prefab文件内容（简化版，实际Unity prefab文件更复杂）
    # 这里生成一个基本的prefab结构，包含必要的UI组件
    prefab_content = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 100001}}
  - component: {{fileID: 100002}}
  - component: {{fileID: 100003}}
  m_Layer: 5
  m_Name: {0}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &100001
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 400, y: 300}}
  m_Pivot: {{x: 0.5, y: 0.5}}
--- !u!33 &100002
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 0000000000000000e000000000000000, type: 0}}
  m_UpdateMode: 0
  m_RenderMode: 0
  m_PixelPerfect: 0
  m_ReferencePixelsPerUnit: 100
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
  m_OverrideSorting: 0
--- !u!114 &100003
{1}Win:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 0000000000000000e000000000000000, type: 0}}
  m_Name: ""
  m_EditorClassIdentifier: ""
  Data: {{fileID: 0}}
  sortOrder: 0
  winType: 0
  priority: 0
""".format(prefab_name, window_name)
    
    # 确保目录存在
    dir_path = os.path.dirname(file_path)
    if not os.path.exists(dir_path):
        os.makedirs(dir_path)
    
    # 写入文件
    with open(file_path, 'w') as f:
        f.write(prefab_content)
    
    return file_path

def main(description=None):
    """主函数"""
    print("=== 窗口生成器 ===")
    
    # 直接硬编码窗口名称和组件，避免编码问题
    window_name = "Login"
    components = [
        {'name': 'Username', 'type': '输入框'},
        {'name': 'Password', 'type': '输入框'},
        {'name': 'Login', 'type': '按钮'},
        {'name': 'Register', 'type': '按钮'}
    ]
    
    # 调试输出
    print("窗口名称：{0}".format(window_name))
    print("UI组件：{0}".format(components))
    
    # 生成CS文件
    cs_file = generate_cs_file(window_name, components)
    print("生成CS文件：{0}".format(cs_file))
    
    # 生成prefab文件
    prefab_file = generate_prefab_file(window_name, components)
    print("生成Prefab文件：{0}".format(prefab_file))
    
    print("\n生成完成！")

if __name__ == "__main__":
    main()
