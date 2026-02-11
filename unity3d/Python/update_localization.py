import re
import sys

# Translation mapping
translations = {
    "切线模式": "Tangent mode",
    "平滑度来源": "Smoothness source",
    "平滑度": "Smoothness",
    "法线导入模式": "Normal import mode",
    "法线贴图模式": "Normal map mode",
    "高度贴图模式": "Height map mode",
    "材质重定向映射": "Material remaps",
}

def update_file(filepath):
    """Update MethodKey constructors with L.T() bilingual support"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Pattern: new MethodXxx("param", "中文描述")
    pattern = r'(new Method\w+\("[^"]+",\s*)"([^\x00-\x7F][^"]+)"'
    
    def replace_func(match):
        prefix = match.group(1)
        chinese = match.group(2)
        
        # Get English translation
        english = translations.get(chinese, chinese)
        
        # Return updated string with L.T()
        return f'{prefix}L.T("{english}", "{chinese}")'
    
    updated_content = re.sub(pattern, replace_func, content)
    
    # Write back
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(updated_content)
    
    print(f"Updated: {filepath}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python update_localization.py <filepath>")
        sys.exit(1)
    
    filepath = sys.argv[1]
    update_file(filepath)
