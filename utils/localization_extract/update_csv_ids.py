import json
import csv
import os

# 1. 加载本地 JSON 映射
json_path = 'cards.json'
csv_path = 'result_cleaned.csv'
output_path = 'result_cleaned_with_id.csv'

name_to_id = {}
try:
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    for key, value in data.items():
        if key.endswith('.title'):
            real_id = key.replace('.title', '')
            name_to_id[value.strip()] = real_id
    print(f"Loaded {len(name_to_id)} IDs from {json_path}")
except Exception as e:
    print(f"Error loading JSON: {e}")
    exit(1)

# 2. 处理 CSV
updated_rows = []
try:
    with open(csv_path, 'r', encoding='utf-8') as f:
        # 自动检测 UTF-8-BOM
        content = f.read()
        if content.startswith('\ufeff'):
            content = content[1:]
        
        lines = content.splitlines()
        reader = csv.DictReader(lines)
        fieldnames = list(reader.fieldnames)
        if '真实ID' not in fieldnames:
            fieldnames.append('真实ID')
            
        for row in reader:
            card_name = row['卡牌名称'].strip()
            # 尝试直接匹配或去除特殊符号后再匹配
            row['真实ID'] = name_to_id.get(card_name, '未知')
            updated_rows.append(row)
    
    print(f"Processed {len(updated_rows)} rows from {csv_path}")
except Exception as e:
    print(f"Error processing CSV: {e}")
    exit(1)

# 3. 写入带 BOM 的 UTF-8 以便 Excel 直接打开
try:
    with open(output_path, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(updated_rows)
    print(f"Successfully generated {output_path}")
    
    matched_count = len([r for r in updated_rows if r['真实ID'] != '未知'])
    print(f"Matched IDs: {matched_count} / {len(updated_rows)}")
except Exception as e:
    print(f"Error writing output: {e}")
    exit(1)
