import csv
import re

input_file = 'result_cleaned.csv'

def snake_to_pascal(name_str):
    if not name_str: return ""
    # 如果已经没有下划线，尝试检查是否已经是 PascalCase
    if '_' not in name_str:
        # 如果全是大写（如 TAUNT），则转为 Taunt
        if name_str.isupper():
            return name_str.capitalize()
        # 否则假设已经是正确的 PascalCase（如 BurningPact），直接返回
        return name_str
        
    words = name_str.lower().split('_')
    return ''.join(word.capitalize() for word in words)

try:
    with open(input_file, 'r', encoding='utf-8-sig') as f:
        reader = csv.DictReader(f)
        fieldnames = list(reader.fieldnames)
        rows = list(reader)

    for row in rows:
        internal_name = row['InternalName'].strip()
        row['InternalName'] = snake_to_pascal(internal_name)

    with open(input_file, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)
        
    print("成功统一 InternalName 命名格式（PascalCase）。")
except Exception as e:
    print(f"处理出错: {e}")
