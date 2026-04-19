---
name: glm-usage
description: 查询智谱 AI (BigModel) API 用量，显示 Token 和 MCP 调用配额。当用户想查看 GLM 用量、API 用量、Token 消耗、MCP 调用次数时触发。
---

# GLM 用量查询

运行 `scripts/query_usage.py` 查询用量，配置文件与 Go CLI 共享（`~/.cicbyte/glm-usage/config/config.yaml`）。

## 执行流程

1. 运行 `python scripts/query_usage.py`
2. 如果配置文件不存在，脚本会自动创建并提示用户设置 API Key，**不要向用户索要 Key**
3. 将脚本输出直接展示给用户

## 输出格式

脚本输出包含：套餐等级、Token/MCP 用量百分比、彩色进度条、重置时间、分模型详情。
