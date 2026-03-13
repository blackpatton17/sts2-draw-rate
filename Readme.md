# main.py 使用说明（简版）

`main.py` 是桌面悬浮层工具：读取 `result_cleaned.csv`，按快捷键触发 OCR 分析并在屏幕上显示结果。

## 1. 启动前准备

1. 使用 Python 3.12（推荐）  
2. 安装依赖：

```bash
python -m venv .venv
.venv\Scripts\activate
python -m pip install -U pip
python -m pip install -r requirements.txt
```

3. 确保项目根目录存在 `result_cleaned.csv`（`main.py` 启动时会读取它）

## 2. 启动

在项目目录执行：

```bash
python main.py
```

## 3. 快捷键

- `Ctrl+Q`：开始分析
- `Ctrl+H`：显示/隐藏悬浮窗
- `Ctrl+Shift+X`：退出程序

## 4. 常见问题

### 启动后没数据

- 检查 `result_cleaned.csv` 是否存在
- 检查 CSV 列名是否与代码一致（当前代码会读取“卡牌名称”等列）

### 热键无响应

- 用“管理员权限”打开终端再运行 `main.py`
- 避免和其他软件的全局热键冲突

### OCR 报错（Paddle 相关）

- 先确认当前环境版本与 `requirements.txt` 一致
- 如果有旧版本残留，重装依赖后再试
