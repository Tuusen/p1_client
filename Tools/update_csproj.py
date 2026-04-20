# -*- coding: utf-8 -*-
"""
update_csproj.py - 自动更新 Assembly-CSharp.csproj 文件
用于修复 Qoder/OmniSharp 标红问题
Usage: python update_csproj.py [--project-root PATH]
"""
from __future__ import print_function, unicode_literals
import os
import re
import sys
import codecs
import argparse
from collections import OrderedDict

# Set default encoding to utf-8 for Python 2
if sys.version_info[0] < 3:
    reload(sys)
    sys.setdefaultencoding('utf-8')

def find_cs_files(scripts_dir):
    """
    扫描 Assets/Scripts 目录下所有 .cs 文件
    返回相对路径列表(使用 Windows 路径分隔符)
    """
    cs_files = []
    
    if not os.path.exists(scripts_dir):
        print("错误: 找不到 Scripts 目录: {}".format(scripts_dir))
        sys.exit(1)
    
    for root, dirs, files in os.walk(scripts_dir):
        for file in files:
            if file.endswith('.cs'):
                full_path = os.path.join(root, file)
                # 转换为相对路径
                rel_path = os.path.relpath(full_path, scripts_dir.replace('/Assets/Scripts', '').replace('\\Assets\\Scripts', ''))
                # 统一使用反斜杠
                rel_path = rel_path.replace('/', '\\')
                cs_files.append(rel_path)
    
    # 排序以保持稳定的输出
    cs_files.sort()
    return cs_files


def generate_compile_entries(cs_files):
    """
    生成 <Compile Include="..." /> 条目
    """
    entries = []
    for file_path in cs_files:
        # 使用 5 个空格缩进(与原文件格式一致)
        entry = '     <Compile Include="{}" />'.format(file_path)
        entries.append(entry)
    return entries


def update_csproj(csproj_path, scripts_dir):
    """
    更新 .csproj 文件中的 Compile 条目
    """
    print("正在扫描 .cs 文件...")
    cs_files = find_cs_files(scripts_dir)
    print("找到 {} 个 .cs 文件".format(len(cs_files)))
    
    # 生成新的 Compile 条目
    new_entries = generate_compile_entries(cs_files)
    
    # 读取 .csproj 文件
    if not os.path.exists(csproj_path):
        print("错误: 找不到 .csproj 文件: {}".format(csproj_path))
        sys.exit(1)
    
    # 兼容 Python 2 和 3
    if sys.version_info[0] < 3:
        with codecs.open(csproj_path, 'r', encoding='utf-8-sig') as f:
            content = f.read()
    else:
        with open(csproj_path, 'r', encoding='utf-8-sig') as f:
            content = f.read()
    
    # 找到第一个 <ItemGroup> 中包含 <Compile> 的部分
    # 使用正则表达式匹配整个 ItemGroup 块
    pattern = r'(  <ItemGroup>\n)(     <Compile Include=".*?" />\n)+  </ItemGroup>'
    
    # 构建新的 ItemGroup 内容
    compile_block = '\n'.join(new_entries)
    new_itemgroup = '  <ItemGroup>\n{}\n  </ItemGroup>'.format(compile_block)
    
    # 替换旧的 ItemGroup
    new_content = re.sub(pattern, new_itemgroup, content)
    
    # 写回文件
    if sys.version_info[0] < 3:
        with codecs.open(csproj_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
    else:
        with open(csproj_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
    
    print("[OK] 成功更新: {}".format(csproj_path))
    print("[OK] 共更新 {} 个文件引用".format(len(cs_files)))
    
    # 显示变更摘要
    print("\n文件分布:")
    dir_stats = {}
    for file_path in cs_files:
        # 提取相对目录 (Assets\Scripts\Battle\xxx.cs -> Battle)
        parts = file_path.split('\\')
        if len(parts) >= 3:
            dir_name = parts[2]  # Battle, Core, Data, UI
            dir_stats[dir_name] = dir_stats.get(dir_name, 0) + 1
    
    for dir_name in sorted(dir_stats.keys()):
        print("  - {}: {} 个文件".format(dir_name, dir_stats[dir_name]))


def main():
    parser = argparse.ArgumentParser(description='更新 Assembly-CSharp.csproj 文件以修复 Qoder 标红问题')
    parser.add_argument('--project-root', type=str, default=None,
                        help='项目根目录路径(默认自动检测)')
    
    args = parser.parse_args()
    
    # 确定项目根目录
    if args.project_root:
        project_root = args.project_root
    else:
        # 默认: 脚本所在目录的上一级(Tools -> project root)
        script_dir = os.path.dirname(os.path.abspath(__file__))
        project_root = os.path.dirname(script_dir)
    
    print("=" * 60)
    print("Qoder .csproj 更新工具")
    print("=" * 60)
    print("项目根目录: {}".format(project_root))
    
    # 构建路径
    csproj_path = os.path.join(project_root, 'Assembly-CSharp.csproj')
    scripts_dir = os.path.join(project_root, 'Assets', 'Scripts')
    
    # 执行更新
    update_csproj(csproj_path, scripts_dir)
    
    print("\n" + "=" * 60)
    print("完成! 请在 Qoder 中重启 OmniSharp:")
    print("  1. 按 Ctrl+Shift+P")
    print("  2. 输入 'OmniSharp: Restart OmniSharp'")
    print("=" * 60)


if __name__ == '__main__':
    main()
