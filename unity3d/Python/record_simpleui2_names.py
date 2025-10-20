#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
记录SimpleUI2的组件名称映射到规则文件
"""

import json

# Figma节点ID到Unity组件名称的映射
names_mapping = {
    "50:80": "Shadow",
    "50:81": "Bounding Headline", 
    "50:82": "corner_TopRight",
    "50:83": "corner_TopLeft",
    "50:84": "corner_BottomRight",
    "50:85": "corner_BottomLeft",
    "50:86": "Figma basics",
    "630:9": "Frame 6"
}

# 转换为JSON字符串
names_json = json.dumps(names_mapping, ensure_ascii=False)

print("组件名称映射（JSON字符串格式）：")
print(names_json)

print("\n映射详情：")
for node_id, unity_name in names_mapping.items():
    print(f"  {node_id} -> {unity_name}")

print("\n使用MCP工具记录时，请使用以下命令：")
print(f'ui_rule_manage action=record_names name=SimpleUI2 names_data=\'{names_json}\'')

