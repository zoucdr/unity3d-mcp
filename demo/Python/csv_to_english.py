#!/usr/bin/env python
# -*- coding: utf-8 -*-

import csv
import os
import sys

def create_english_csv(input_csv, output_csv=None):
    """将中文CSV转换为英文翻译CSV文档"""
    if not output_csv:
        # 如果没有指定输出文件名，则使用CSV文件名作为基础
        base_name = os.path.splitext(input_csv)[0]
        output_csv = f"{base_name}_english.csv"
    
    # 读取CSV文件
    rows = []
    with open(input_csv, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        header = next(reader)  # 读取表头
        rows.append(header)  # 保留表头
        
        for row in reader:
            if len(row) >= 2:
                # 保留原始的文件路径和中文字符串
                new_row = row[:2]
                
                # 添加一个空的英文翻译列，如果已经有第三列则保留
                if len(row) >= 3:
                    new_row.append(row[2])
                else:
                    new_row.append("")
                
                rows.append(new_row)
    
    # 写入CSV文件
    with open(output_csv, 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerows(rows)
    
    print(f"已创建英文翻译CSV模板: {output_csv}")
    print(f"共包含 {len(rows)-1} 个条目")
    return output_csv

def main():
    print("中文CSV转英文翻译CSV工具")
    
    # 检查命令行参数
    if len(sys.argv) > 1:
        # 从命令行参数获取输入文件
        input_csv = sys.argv[1]
        
        # 从命令行参数获取输出文件（如果提供）
        output_csv = sys.argv[2] if len(sys.argv) > 2 else None
    else:
        # 如果没有命令行参数，则使用交互式输入
        try:
            # 获取输入文件
            input_csv = input("请输入中文CSV文件路径: ").strip()
            if not os.path.isfile(input_csv):
                print(f"错误：{input_csv} 不是有效的文件")
                return
            
            # 获取输出文件（可选）
            output_csv = input("请输入输出文件路径（留空则自动生成）: ").strip()
            if not output_csv:
                output_csv = None
        except EOFError:
            print("错误：无法读取输入，请提供命令行参数")
            print("用法: python csv_to_english.py <输入CSV文件路径> [输出CSV文件路径]")
            return
    
    if not os.path.isfile(input_csv):
        print(f"错误：{input_csv} 不是有效的文件")
        return
    
    # 转换文件
    result_file = create_english_csv(input_csv, output_csv)
    
    print("\n转换完成！")
    print(f"请打开 {result_file} 文件，在第三列添加对应的英文翻译")
    print("完成翻译后，可以使用 replace_chinese.py 进行替换")

if __name__ == "__main__":
    main()
