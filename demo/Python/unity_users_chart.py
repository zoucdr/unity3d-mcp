#!/usr/bin/env python
# -*- coding: utf-8 -*-

import matplotlib.pyplot as plt
import numpy as np
import matplotlib
from matplotlib.font_manager import FontProperties
import os
import datetime

# 设置中文显示
matplotlib.rcParams['font.sans-serif'] = ['SimHei']  # 指定默认字体为黑体
matplotlib.rcParams['axes.unicode_minus'] = False  # 解决保存图像时负号'-'显示为方块的问题

# 创建数据
years = np.array([2010, 2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023, 2024])

# Unity使用者数量（百万）- 这里使用模拟数据
users = np.array([0.8, 1.3, 2.0, 3.2, 4.5, 5.5, 6.8, 8.0, 9.5, 11.0, 13.0, 15.0, 17.0, 19.0, 20.5])

# 创建图形和子图
plt.figure(figsize=(12, 7), dpi=100)

# 绘制曲线图
plt.plot(years, users, marker='o', linestyle='-', linewidth=2, markersize=8, color='#2196F3')

# 添加数据标签
for x, y in zip(years, users):
    plt.text(x, y + 0.3, f'{y}M', ha='center', va='bottom', fontsize=9)

# 填充曲线下方区域
plt.fill_between(years, users, color='#2196F3', alpha=0.2)

# 设置标题和标签
plt.title('Unity引擎使用者数量趋势 (2010-2024)', fontsize=16, fontweight='bold', pad=20)
plt.xlabel('年份', fontsize=12, labelpad=10)
plt.ylabel('使用者数量 (百万)', fontsize=12, labelpad=10)

# 设置网格
plt.grid(True, linestyle='--', alpha=0.7)

# 设置坐标轴范围
plt.xlim(years[0] - 0.5, years[-1] + 0.5)
plt.ylim(0, max(users) * 1.2)

# 添加注释
plt.annotate('Unity 5.0发布', xy=(2015, 5.5), xytext=(2015, 7),
             arrowprops=dict(facecolor='black', shrink=0.05, width=1.5, headwidth=8), 
             ha='center', fontsize=10)

plt.annotate('Unity 2017发布', xy=(2017, 8.0), xytext=(2017, 10),
             arrowprops=dict(facecolor='black', shrink=0.05, width=1.5, headwidth=8), 
             ha='center', fontsize=10)

plt.annotate('Unity 2022 LTS发布', xy=(2022, 17.0), xytext=(2022, 19),
             arrowprops=dict(facecolor='black', shrink=0.05, width=1.5, headwidth=8), 
             ha='center', fontsize=10)

# 添加水印
plt.figtext(0.5, 0.02, f'生成时间: {datetime.datetime.now().strftime("%Y-%m-%d")} (Unity MCP)', 
            ha='center', color='gray', fontsize=9)

# 添加图例
plt.legend(['Unity使用者数量'], loc='upper left')

# 美化图表
plt.style.use('seaborn-v0_8-whitegrid')
plt.tight_layout()

# 保存图表
save_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'unity_users_chart.png')
plt.savefig(save_path, dpi=300, bbox_inches='tight')
print(f"图表已保存到: {save_path}")

# 显示图表（在Unity中不会显示，但保存文件）
plt.close()

print("Unity使用者数量曲线图生成完成！")