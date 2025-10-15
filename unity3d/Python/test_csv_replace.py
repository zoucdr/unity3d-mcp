#!/usr/bin/env python
# -*- coding: utf-8 -*-

import csv
import json
import os

def csv_to_dict(csv_file):
    """将CSV文件转换为翻译字典"""
    translation_dict = {}
    with open(csv_file, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        next(reader)  # 跳过表头
        
        for row in reader:
            if len(row) >= 3 and row[1] and row[2]:  # 确保有中文和替换值
                chinese_str = row[1]
                replacement = row[2]
                # 如果中文字符串被引号包围，去掉引号
                if chinese_str.startswith('"') and chinese_str.endswith('"'):
                    chinese_str = chinese_str[1:-1]
                translation_dict[chinese_str] = replacement
    
    return translation_dict

def main():
    # 测试CSV文件路径
    csv_file = "Python/chinese_strings.csv"
    
    # 将CSV转换为字典
    translation_dict = csv_to_dict(csv_file)
    
    # 输出结果
    print(f"从CSV文件中读取了 {len(translation_dict)} 个翻译条目")
    
    # 输出前5个条目作为示例
    print("\n前5个翻译条目示例:")
    for i, (key, value) in enumerate(list(translation_dict.items())[:5]):
        print(f"{i+1}. '{key}' -> '{value}'")
    
    # 将字典保存为JSON文件，以便replace_chinese.py使用
    json_file = "Python/test_translation.json"
    with open(json_file, 'w', encoding='utf-8') as f:
        json.dump(translation_dict, f, ensure_ascii=False, indent=4)
    
    print(f"\n已将翻译字典保存为JSON文件: {json_file}")
    print(f"现在可以使用replace_chinese.py进行替换测试:")
    print(f"python Python/replace_chinese.py <目标目录> {json_file}")

if __name__ == "__main__":
    main()
