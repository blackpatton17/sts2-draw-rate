import json

with open('cards.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

# 搜索图片里的三张牌
targets = ['燃烧契约', '被遗忘的仪式', '暴走']
found = {}
for key, value in data.items():
    if key.endswith('.title'):
        if value in targets:
            found[value] = key.replace('.title', '')

print("--- Image Targets Check ---")
for name in targets:
    print(f"{name}: {found.get(name, 'NOT FOUND')}")

print("\n--- Sample IDs from JSON (first 20) ---")
all_ids = [k.replace('.title', '') for k in data.keys() if k.endswith('.title')]
for i in range(min(20, len(all_ids))):
    print(all_ids[i])
