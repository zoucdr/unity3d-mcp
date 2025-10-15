#!/usr/bin/env python
# -*- coding: utf-8 -*-

import os
import re
import csv
import json
from pathlib import Path

# 可选依赖：deep_translator (Google 翻译)
try:
    from deep_translator import GoogleTranslator  # type: ignore
except Exception:  # noqa: BLE001 - 容忍环境缺少依赖
    GoogleTranslator = None  # type: ignore


def load_translation_dict(translation_file):
    """加载翻译字典。支持：
    - JSON: {"中文":"English"}
    - CSV:  文件,中文字符串,替换值  (与 extract_chinese.py 导出兼容)
    - key=value: 每行一条
    返回: dict[str,str]
    """
    translation_dict = {}
    ext = os.path.splitext(translation_file)[1].lower()

    if ext == '.json':
        with open(translation_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
            # 只收集非空翻译
            for k, v in data.items():
                if k and isinstance(v, str) and v.strip():
                    translation_dict[k] = v
        return translation_dict

    if ext == '.csv':
        with open(translation_file, 'r', encoding='utf-8', newline='') as f:
            reader = csv.reader(f)
            header = next(reader, None)
            # 兼容无表头
            if header and len(header) >= 3 and header[0] == '文件' and header[1] == '中文字符串':
                pass  # 已带标准表头
            else:
                # 没有标准表头，回退当作纯三列
                if header:
                    # 处理第一行
                    row = header
                    if len(row) >= 3 and row[1] and row[2]:
                        translation_dict[row[1]] = row[2]
                header = None

            for row in reader:
                if len(row) >= 3 and row[1] and row[2]:
                    chinese_str = row[1]
                    replacement = row[2]
                    if replacement:
                        translation_dict[chinese_str] = replacement
        return translation_dict

    # key=value
    with open(translation_file, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line or '=' not in line:
                continue
            key, value = line.split('=', 1)
            key = key.strip()
            value = value.strip()
            if key and value:
                translation_dict[key] = value
    return translation_dict


def sort_by_key_length(desc_map):
    """按键长度从长到短排序，返回排序后的 (key, value) 列表。"""
    return sorted(desc_map.items(), key=lambda kv: len(kv[0]), reverse=True)


def find_chinese_segments(text):
    """查找文本中所有连续中文片段，返回去重集合。"""
    segments = re.findall(r'[\u4e00-\u9fff]+', text)
    return set(s for s in segments if s)


def auto_translate_missing(chinese_set, existing_dict):
    """将 chinese_set 中现有词典缺失的条目机翻为英文，返回新增的 {cn: en} 字典。
    当 deep_translator 不可用时，返回空。
    """
    if not GoogleTranslator:
        return {}
    translator = GoogleTranslator(source='auto', target='en')
    added = {}
    for cn in chinese_set:
        if cn in existing_dict:
            continue
        try:
            en = translator.translate(cn)
            if en and isinstance(en, str):
                added[cn] = en
        except Exception:
            # 忽略单条翻译失败，继续其它
            continue
    return added


def safe_replace_all(content, translations):
    """使用占位符两阶段替换，避免重叠匹配导致的串改。
    translations: 已经按长度降序的 (chinese, replacement) 列表
    返回: new_content, num_rules_applied
    """
    placeholder_pairs = []
    rules_applied = 0

    # 第一步：将所有命中的中文替换为唯一占位符
    for idx, (cn, en) in enumerate(translations):
        if not en:
            continue
        if cn not in content:
            continue
        placeholder = f"__TRANS_PLACEHOLDER_{idx}__"
        pattern = re.escape(cn)
        new_content, count = re.subn(pattern, placeholder, content)
        if count > 0:
            placeholder_pairs.append((placeholder, en))
            rules_applied += 1
            content = new_content

    # 第二步：回填占位符为目标翻译
    for placeholder, en in placeholder_pairs:
        content = content.replace(placeholder, en)

    return content, rules_applied


def translate_file(input_file, translation_file, output_file=None, create_backup=True, auto_translate=False, save_new_dict_path=None):
    # 读入原文
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            content = f.read()
            original = content
    except UnicodeDecodeError:
        with open(input_file, 'r', encoding='gbk') as f:
            content = f.read()
            original = content

    # 加载词典
    tdict = load_translation_dict(translation_file)

    # 自动机翻补全缺失条目（基于当前文件中出现的中文片段）
    if auto_translate:
        chinese_in_file = find_chinese_segments(content)
        newly_added = auto_translate_missing(chinese_in_file, tdict)
        if newly_added:
            tdict.update(newly_added)
            # 可选保存新增译文
            if save_new_dict_path:
                try:
                    with open(save_new_dict_path, 'w', encoding='utf-8') as f:
                        json.dump(newly_added, f, ensure_ascii=False, indent=2)
                except Exception:
                    pass

    # 排序后替换
    sorted_pairs = sort_by_key_length(tdict)

    # 执行安全替换
    content, rules_applied = safe_replace_all(content, sorted_pairs)

    # 输出
    if rules_applied > 0:
        if create_backup:
            backup_path = f"{input_file}.bak"
            with open(backup_path, 'w', encoding='utf-8') as f:
                f.write(original)
        if not output_file:
            stem, ext = os.path.splitext(input_file)
            output_file = f"{stem}.translated{ext or ''}"
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(content)
    return rules_applied, output_file


def main():
    print("整文件翻译工具")
    input_file = input("请输入要翻译的文件路径: ").strip()
    while not os.path.isfile(input_file):
        print(f"错误：{input_file} 不是有效的文件")
        input_file = input("请重新输入有效的文件路径: ").strip()

    translation_file = input("请输入翻译词典路径（CSV/JSON/key=value）: ").strip()
    while not os.path.isfile(translation_file):
        print(f"错误：{translation_file} 不是有效的文件")
        translation_file = input("请重新输入有效的翻译词典路径: ").strip()

    out_path = input("请输入输出文件路径（留空自动生成 .translated 版本）: ").strip()
    backup_choice = input("是否创建原文件备份? (y/n, 默认 y): ").strip().lower()
    create_backup = backup_choice != 'n'

    auto_choice = input("缺失条目是否自动机翻为英文? (y/n, 默认 y): ").strip().lower()
    auto_translate = auto_choice != 'n'
    save_new = None
    if auto_translate:
        if GoogleTranslator:
            save_new = input("将新增机翻条目另存为JSON路径（留空不保存）: ").strip() or None
        else:
            print("提示：未检测到 deep_translator 库，无法自动机翻。可执行: pip install deep-translator")
            auto_translate = False

    applied, out_file = translate_file(
        input_file,
        translation_file,
        out_path or None,
        create_backup,
        auto_translate=auto_translate,
        save_new_dict_path=save_new,
    )
    print(f"应用了 {applied} 条翻译规则")
    if applied > 0:
        print(f"已写入: {out_file}")
    else:
        print("没有匹配到可应用的翻译项")


if __name__ == '__main__':
    main()


