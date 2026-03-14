import json
import csv
import re

json_path = 'cards.json'
csv_path = 'result_cleaned.csv'
output_path = 'result_cleaned_with_id.csv'

# 工具函数：将 SNAKE_CASE (如 BURNING_PACT) 转换为 PascalCase (如 BurningPact)
def snake_to_pascal(snake_str):
    if not snake_str: return ""
    # 分割单词，每个单词首字母大写，然后拼接
    words = snake_str.lower().split('_')
    return ''.join(word.capitalize() for word in words)

# 1. 加载本地 JSON 映射并转换格式
name_to_internal_id = {}
try:
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    for key, value in data.items():
        if key.endswith('.title'):
            raw_id = key.replace('.title', '')
            # 转换为游戏内部名格式
            internal_id = snake_to_pascal(raw_id)
            name_to_internal_id[value.strip()] = internal_id
    print(f"Loaded {len(name_to_internal_id)} names from {json_path}")
except Exception as e:
    print(f"Error loading JSON: {e}")
    exit(1)

# 2. 处理 CSV 并追加 真实ID 列
updated_rows = []
try:
    with open(csv_path, 'r', encoding='utf-8') as f:
        content = f.read()
        if content.startswith('\ufeff'):
            content = content[1:]
        
        reader = csv.DictReader(content.splitlines())
        fieldnames = list(reader.fieldnames)
        if '真实ID' not in fieldnames:
            fieldnames.append('真实ID')
            
        for row in reader:
            card_name = row['卡牌名称'].strip()
            # 匹配 internal name
            row['真实ID'] = name_to_internal_id.get(card_name, '未知')
            updated_rows.append(row)
except Exception as e:
    print(f"Error processing CSV: {e}")
    exit(1)

# 3. 写入最终结果
with open(output_path, 'w', encoding='utf-8-sig', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(updated_rows)

print(f"Successfully generated {output_path}")
print(f"Total matched: {len([r for r in updated_rows if r['真实ID'] != '未知'])} / {len(updated_rows)}")

# 打印几个样例看看对不对
print("\n--- 转换样例 ---")
sample_names = ['燃烧契约', '被遗忘的仪式', '暴走']
for name in sample_names:
    print(f"{name} -> {name_to_internal_id.get(name, '未找到')}")
