#!/usr/bin/env python
# -*- coding: utf-8 -*-

import os
import re
import csv
import json
from pathlib import Path

def is_chinese(char):
    """判断一个字符是否是中文"""
    return '\u4e00' <= char <= '\u9fff'

def extract_chinese_from_file(file_path):
    """从文件中提取中文字符串"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except UnicodeDecodeError:
        try:
            with open(file_path, 'r', encoding='gbk') as f:
                content = f.read()
        except Exception as e:
            print(f"无法读取文件 {file_path}: {e}")
            return []

    # 正则表达式匹配中文字符串
    # 匹配至少包含一个中文字符的连续字符串
    pattern = r'[^\n\r\t]*[\u4e00-\u9fff]+[^\n\r\t]*'
    mixed_strings = re.findall(pattern, content)
    
    # 提取每个匹配字符串中的纯中文部分
    chinese_strings = []
    for mixed in mixed_strings:
        # 提取连续的中文字符
        pure_chinese = re.findall(r'[\u4e00-\u9fff]+', mixed)
        chinese_strings.extend(pure_chinese)
    
    # 过滤空字符串并去重
    chinese_strings = [s for s in chinese_strings if s]
    chinese_strings = list(set(chinese_strings))
    
    # 按字符串长度从长到短排序
    chinese_strings.sort(key=len, reverse=True)
    
    return chinese_strings

def scan_directory(directory, extensions):
    """扫描目录下所有指定后缀的文件并提取中文"""
    results = {}
    extensions = [ext.lower() for ext in extensions]
    
    for root, dirs, files in os.walk(directory):
        # 忽略以点号开头的文件夹
        dirs[:] = [d for d in dirs if not d.startswith('.')]
        
        for file in files:
            file_path = os.path.join(root, file)
            file_ext = os.path.splitext(file)[1].lower()
            
            # 检查文件后缀是否在指定列表中
            if file_ext[1:] in extensions:  # 去掉点号
                print(f"处理文件: {file_path}")
                chinese_strings = extract_chinese_from_file(file_path)
                if chinese_strings:
                    rel_path = os.path.relpath(file_path, directory)
                    results[rel_path] = chinese_strings
    
    return results

def export_to_csv(results, output_file):
    """将结果导出到CSV文件"""
    with open(output_file, 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['文件', '中文字符串', '替换值'])
        
        for file_path, strings in results.items():
            for string in strings:
                writer.writerow([file_path, string, ''])

def export_to_json(results, output_file):
    """将结果导出到JSON文件"""
    # 转换为更适合翻译的格式
    translation_dict = {}
    
    for file_path, strings in results.items():
        for string in strings:
            if string not in translation_dict:
                translation_dict[string] = ""
    
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(translation_dict, f, ensure_ascii=False, indent=4)

def main():
    # 使用运行时输入代替命令行参数
    print("欢迎使用中文字符串提取工具")
    
    # 输入要扫描的目录
    directory = input("请输入要扫描的目录路径: ").strip()
    while not os.path.isdir(directory):
        print(f"错误：{directory} 不是有效的目录")
        directory = input("请重新输入有效的目录路径: ").strip()
    
    # 输入要扫描的文件后缀
    default_extensions = ['cs', 'py', 'js', 'ts', 'html', 'css', 'json', 'xml']
    extensions_input = input(f"请输入要扫描的文件后缀列表，用空格分隔 (默认: {' '.join(default_extensions)}): ").strip()
    extensions = extensions_input.split() if extensions_input else default_extensions
    
    # 输入输出文件名
    output = input("请输入输出文件名（不含后缀，默认: chinese_strings）: ").strip()
    if not output:
        output = "chinese_strings"
    
    # 选择输出格式
    format_choice = input("请选择输出格式 (csv/json，默认: json): ").strip().lower()
    output_format = format_choice if format_choice in ['csv', 'json'] else 'json'
    
    print(f"\n开始扫描目录: {directory}")
    print(f"扫描文件类型: {', '.join(extensions)}")
    
    results = scan_directory(directory, extensions)
    
    # 统计提取的中文字符串数量
    total_strings = sum(len(strings) for strings in results.values())
    unique_strings = len(set(s for strings in results.values() for s in strings))
    
    print(f"\n扫描完成！")
    print(f"处理的文件数量: {len(results)}")
    print(f"提取的中文字符串总数: {total_strings}")
    print(f"去重后的中文字符串数量: {unique_strings}")
    
    # 导出结果
    if output_format == 'csv':
        output_file = f"{output}.csv"
        export_to_csv(results, output_file)
    else:
        output_file = f"{output}.json"
        export_to_json(results, output_file)
    
    print(f"结果已导出到 {output_file}")

if __name__ == "__main__":
    main()
