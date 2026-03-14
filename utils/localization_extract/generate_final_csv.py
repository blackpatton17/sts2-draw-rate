import json
import csv
import re

json_path = 'cards.json'
csv_path = 'result_cleaned.csv'
output_path = 'result_cleaned_with_id.csv'

# 工具函数：将 ID 转换为 纯小写且不含下划线 的格式
def normalize_id(id_str):
    if not id_str: return ""
    return re.sub(r'[^a-zA-Z0-9]', '', id_str).lower()

# 1. 加载本地 JSON 映射
name_to_normalized_id = {}
raw_id_map = {} # 存一份原始的 ID，万一以后有用
try:
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    for key, value in data.items():
        if key.endswith('.title'):
            raw_id = key.replace('.title', '')
            norm = normalize_id(raw_id)
            name_to_normalized_id[value.strip()] = norm
            raw_id_map[value.strip()] = raw_id
    print(f"Loaded {len(name_to_normalized_id)} names from {json_path}")
except Exception as e:
    print(f"Error loading JSON: {e}")
    exit(1)

# 2. 处理 CSV
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
            # 找到 JSON 里的 ID（原始格式，如 BURNING_PACT）
            original_id = raw_id_map.get(card_name, '未知')
            row['真实ID'] = original_id
            updated_rows.append(row)
except Exception as e:
    print(f"Error processing CSV: {e}")
    exit(1)

# 3. 写入
with open(output_path, 'w', encoding='utf-8-sig', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(updated_rows)

print(f"Successfully generated {output_path}")
print(f"Matched: {len([r for r in updated_rows if r['真实ID'] != '未知'])} / {len(updated_rows)}")
