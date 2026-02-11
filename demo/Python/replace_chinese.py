#!/usr/bin/env python
# -*- coding: utf-8 -*-

import os
import re
import csv
# import argparse  # 不再使用命令行参数
import json
from pathlib import Path

def load_translation_dict(translation_file):
    """加载翻译字典"""
    translation_dict = {}
    file_specific_dict = {}  # 按文件路径组织的翻译字典
    
    # 根据文件扩展名选择加载方式
    if translation_file.endswith('.json'):
        # 加载JSON格式
        with open(translation_file, 'r', encoding='utf-8') as f:
            return json.load(f), None  # JSON格式不支持文件特定替换
    elif translation_file.endswith('.csv'):
        # 加载CSV格式（与extract_chinese.py导出的CSV兼容）
        with open(translation_file, 'r', encoding='utf-8', newline='') as f:
            reader = csv.reader(f)
            # 跳过标题行
            header = next(reader, None)
            if not header or len(header) < 3:
                raise ValueError("CSV文件格式不正确，应包含至少3列")
            
            # 读取每一行，格式：[文件路径, 中文字符串, 替换值]
            for row in reader:
                if len(row) >= 3 and row[1] and row[2]:  # 确保中文和替换值都不为空
                    file_path = row[0]
                    chinese_str = row[1]
                    replacement = row[2]
                    
                    # 将替换规则添加到全局字典
                    translation_dict[chinese_str] = replacement
                    
                    # 将替换规则添加到文件特定字典
                    if file_path not in file_specific_dict:
                        file_specific_dict[file_path] = {}
                    file_specific_dict[file_path][chinese_str] = replacement
            
            return translation_dict, file_specific_dict
    else:
        # 假设是简单的key=value格式
        with open(translation_file, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if line and '=' in line:
                    key, value = line.split('=', 1)
                    translation_dict[key.strip()] = value.strip()
            return translation_dict, None  # key=value格式不支持文件特定替换

def sort_by_length(translation_dict):
    """按字符串长度排序翻译字典（从长到短）"""
    return {k: v for k, v in sorted(translation_dict.items(), key=lambda item: len(item[0]), reverse=True)}

def replace_chinese_in_file(file_path, translation_dict, backup=True):
    """替换文件中的中文字符串"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            original_content = content
    except UnicodeDecodeError:
        try:
            with open(file_path, 'r', encoding='gbk') as f:
                content = f.read()
                original_content = content
        except Exception as e:
            print(f"无法读取文件 {file_path}: {e}")
            return False, 0
    
    # 替换中文字符串，从最长的开始
    replacements = 0
    for chinese_str, replacement in translation_dict.items():
        if replacement and chinese_str in content:
            # 使用正则表达式确保我们不会替换部分匹配
            # 例如，不会把"中文字符串测试"中的"中文字符串"替换掉
            pattern = re.escape(chinese_str)
            content = re.sub(pattern, replacement, content)
            replacements += 1
    
    # 如果有替换，写回文件
    if replacements > 0:
        # 备份原文件
        if backup:
            backup_file = f"{file_path}.bak"
            with open(backup_file, 'w', encoding='utf-8') as f:
                f.write(original_content)
            print(f"已创建备份: {backup_file}")
        
        # 写入替换后的内容
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        return True, replacements
    
    return False, 0

def scan_and_replace(directory, translation_dict, file_specific_dict, extensions, backup=True, file_specific_mode=True):
    """扫描目录下所有指定后缀的文件并替换中文"""
    extensions = [ext.lower() for ext in extensions]
    files_processed = 0
    files_modified = 0
    total_replacements = 0
    
    # 如果启用了文件特定模式并且有文件特定字典
    if file_specific_mode and file_specific_dict:
        print("使用文件特定替换模式")
        
        # 遍历文件特定字典中的每个文件路径
        for rel_file_path, file_dict in file_specific_dict.items():
            # 构建完整文件路径
            file_path = os.path.join(directory, rel_file_path)
            
            # 检查文件是否存在
            if not os.path.isfile(file_path):
                print(f"警告: 文件不存在: {file_path}")
                continue
                
            # 对文件特定字典按长度排序
            file_dict = sort_by_length(file_dict)
            
            files_processed += 1
            print(f"处理文件: {file_path}")
            modified, replacements = replace_chinese_in_file(file_path, file_dict, backup)
            if modified:
                files_modified += 1
                total_replacements += replacements
                print(f"  - 已替换 {replacements} 处中文")
    else:
        # 常规目录扫描模式
        print("使用常规目录扫描模式")
        for root, dirs, files in os.walk(directory):
            # 忽略以点号开头的文件夹
            dirs[:] = [d for d in dirs if not d.startswith('.')]
            for file in files:
                file_path = os.path.join(root, file)
                file_ext = os.path.splitext(file)[1].lower()
                
                # 检查文件后缀是否在指定列表中
                if file_ext[1:] in extensions:  # 去掉点号
                    files_processed += 1
                    print(f"处理文件: {file_path}")
                    modified, replacements = replace_chinese_in_file(file_path, translation_dict, backup)
                    if modified:
                        files_modified += 1
                        total_replacements += replacements
                        print(f"  - 已替换 {replacements} 处中文")
    
    return files_processed, files_modified, total_replacements

def main():
    # 使用运行时输入代替命令行参数
    print("欢迎使用中文字符串替换工具")
    
    # 输入要扫描的目录
    directory = input("请输入要扫描的目录路径: ").strip()
    while not os.path.isdir(directory):
        print(f"错误：{directory} 不是有效的目录")
        directory = input("请重新输入有效的目录路径: ").strip()
    
    # 输入翻译文件路径
    translation_file = input("请输入翻译文件路径（支持CSV、JSON或key=value格式）: ").strip()
    while not os.path.isfile(translation_file):
        print(f"错误：{translation_file} 不是有效的文件")
        translation_file = input("请重新输入有效的翻译文件路径: ").strip()
        
    # 显示文件格式提示
    file_ext = os.path.splitext(translation_file)[1].lower()
    if file_ext == '.csv':
        print("检测到CSV格式文件，将按extract_chinese.py导出的格式加载")
        file_specific_mode_default = 'y'  # CSV默认使用文件特定模式
    elif file_ext == '.json':
        print("检测到JSON格式文件，将按extract_chinese.py导出的格式加载")
        file_specific_mode_default = 'n'  # JSON默认不使用文件特定模式
    else:
        print("未识别的文件格式，将尝试按key=value格式加载")
        file_specific_mode_default = 'n'  # 其他格式默认不使用文件特定模式
    
    # 是否使用文件特定模式
    if file_ext == '.csv':
        file_specific_mode_input = input(f"是否仅替换CSV文件中指定的文件? (y/n, 默认: {file_specific_mode_default}): ").strip().lower()
        file_specific_mode = file_specific_mode_input if file_specific_mode_input else file_specific_mode_default
        file_specific_mode = file_specific_mode != 'n'  # 默认根据文件格式决定
    else:
        file_specific_mode = False
    
    # 输入要扫描的文件后缀（仅在非文件特定模式下需要）
    default_extensions = ['cs', 'py', 'js', 'ts', 'html', 'css', 'json', 'xml']
    if not file_specific_mode:
        extensions_input = input(f"请输入要扫描的文件后缀列表，用空格分隔 (默认: {' '.join(default_extensions)}): ").strip()
        extensions = extensions_input.split() if extensions_input else default_extensions
    else:
        extensions = default_extensions  # 在文件特定模式下，后缀列表不重要
    
    # 是否创建备份
    create_backup = input("是否创建备份文件? (y/n, 默认: y): ").strip().lower()
    create_backup = create_backup != 'n'  # 默认创建备份
    
    # 加载翻译字典
    try:
        translation_dict, file_specific_dict = load_translation_dict(translation_file)
        
        if file_specific_dict:
            total_files = len(file_specific_dict)
            total_entries = sum(len(file_dict) for file_dict in file_specific_dict.values())
            print(f"已加载 {len(translation_dict)} 个翻译条目，涉及 {total_files} 个文件，共 {total_entries} 个文件特定条目")
        else:
            print(f"已加载 {len(translation_dict)} 个翻译条目")
            if file_specific_mode:
                print("警告: 选择了文件特定模式，但翻译文件不支持或未包含文件路径信息")
                file_specific_mode = False
    except Exception as e:
        print(f"加载翻译文件时出错: {e}")
        return
    
    # 按字符串长度排序翻译字典（从长到短）
    translation_dict = sort_by_length(translation_dict)
    
    print(f"\n开始扫描目录: {directory}")
    if not file_specific_mode:
        print(f"扫描文件类型: {', '.join(extensions)}")
    print(f"替换模式: {'仅替换指定文件' if file_specific_mode else '扫描所有匹配文件'}")
    print(f"备份文件: {'是' if create_backup else '否'}")
    
    # 扫描并替换
    files_processed, files_modified, total_replacements = scan_and_replace(
        directory, translation_dict, file_specific_dict, extensions, create_backup, file_specific_mode
    )
    
    print(f"\n替换完成！")
    print(f"处理的文件数量: {files_processed}")
    print(f"修改的文件数量: {files_modified}")
    print(f"总替换次数: {total_replacements}")

if __name__ == "__main__":
    main()