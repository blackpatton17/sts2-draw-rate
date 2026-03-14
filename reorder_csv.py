import csv

input_file = 'result_cleaned.csv'
output_file = 'result_cleaned.csv'

with open(input_file, 'r', encoding='utf-8-sig') as f:
    reader = csv.DictReader(f)
    fieldnames = list(reader.fieldnames)
    
    # 确保列名正确
    if 'InternalName' in fieldnames and '卡牌名称' in fieldnames:
        # 重排列名：InternalName 第一，卡牌名称第二
        new_fieldnames = ['InternalName', '卡牌名称']
        for fn in fieldnames:
            if fn not in new_fieldnames:
                new_fieldnames.append(fn)
        
        rows = list(reader)
    else:
        print("未找到对应的列")
        exit(1)

with open(output_file, 'w', encoding='utf-8-sig', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=new_fieldnames)
    writer.writeheader()
    writer.writerows(rows)

print("CSV列顺序已重排，InternalName 现在是第一列。")
