"""查询智谱 AI API 用量。配置文件与 Go CLI 共享 ~/.cicbyte/glm-usage/config/config.yaml"""

import os
import sys
import json
import urllib.request
import urllib.error
from pathlib import Path

if sys.platform == "win32":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")

CONFIG_DIR = Path.home() / ".cicbyte" / "glm-usage" / "config"
CONFIG_PATH = CONFIG_DIR / "config.yaml"
API_URL = "https://open.bigmodel.cn/api/monitor/usage/quota/limit"


def ensure_config():
    """确保配置文件存在，不存在则自动创建并提示用户"""
    if CONFIG_PATH.exists():
        return True

    CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    CONFIG_PATH.write_text("api_key: \"\"\n", encoding="utf-8")

    print(f"配置文件已创建: {CONFIG_PATH}")
    print(f"请设置 API Key 后重试：")
    print(f"  glm-usage config api-key \"your-key\"")
    print(f"  或手动编辑 {CONFIG_PATH}，填入 api_key 字段")
    return False


def load_api_key():
    text = CONFIG_PATH.read_text(encoding="utf-8")
    for line in text.strip().splitlines():
        if line.startswith("api_key:"):
            key = line.split(":", 1)[1].strip().strip('"').strip("'")
            return key
    return ""


def query_usage(api_key):
    req = urllib.request.Request(API_URL)
    key = api_key
    if not key.startswith("Bearer "):
        key = "Bearer " + key
    req.add_header("Authorization", key)
    req.add_header("Content-Type", "application/json")
    req.add_header("User-Agent", "glm-usage-skill/1.0")

    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        if e.code == 401:
            print(f"错误: 认证失败 (401)，请检查 API Key 是否正确")
            print(f"  glm-usage config api-key \"your-key\"")
        else:
            body = e.read().decode("utf-8", errors="replace")
            print(f"错误: HTTP {e.code} - {body}")
        sys.exit(1)
    except urllib.error.URLError as e:
        print(f"错误: 网络请求失败 - {e.reason}")
        sys.exit(5)


def format_usage(data):
    result = data.get("data", {})
    level = result.get("level", "Unknown")
    limits = result.get("limits", [])

    type_names = {"TIME_LIMIT": "MCP", "TOKENS_LIMIT": "Token", "REQUEST_LIMIT": "Request"}
    order = {"TOKENS_LIMIT": 0, "TIME_LIMIT": 1, "REQUEST_LIMIT": 2}
    limits.sort(key=lambda x: order.get(x.get("type"), 99))

    lines = [f"套餐: {level.upper()}", ""]

    for item in limits:
        tp = item.get("type", "")
        name = type_names.get(tp, tp)
        pct = item.get("percentage", 0)
        used = item.get("currentValue", 0)
        total = item.get("usage", 0)
        remaining = item.get("remaining", 0)
        reset_ts = item.get("nextResetTime", 0)

        if pct > 80:
            color = "\033[91m"
        elif pct > 60:
            color = "\033[93m"
        else:
            color = "\033[92m"
        reset = "\033[0m"

        bar_len = 20
        filled = round(pct / 100 * bar_len)
        bar = "\u2588" * filled + "\u2591" * (bar_len - filled)

        line = f"{name:8s} {color}{pct:3d}%{reset} {bar}"
        if total > 0:
            line += f"  {used}/{total}"
        if remaining > 0:
            line += f"  (剩余 {remaining})"
        lines.append(line)

        if reset_ts > 0:
            from datetime import datetime
            t = datetime.fromtimestamp(reset_ts / 1000)
            lines.append(f"{'':8s}  重置: {t.strftime('%m-%d %H:%M')}")

        details = item.get("usageDetails", [])
        if details:
            for d in details:
                lines.append(f"{'':8s}  {d.get('modelCode', '?'):20s} {d.get('usage', 0)}")

        lines.append("")

    return "\n".join(lines)


def main():
    if not ensure_config():
        sys.exit(3)

    api_key = load_api_key()
    if not api_key:
        print(f"API Key 未设置，请先配置：")
        print(f"  glm-usage config api-key \"your-key\"")
        print(f"  或手动编辑 {CONFIG_PATH}")
        sys.exit(1)

    data = query_usage(api_key)

    if data.get("code") != 200 or not data.get("success"):
        print(f"API 错误: {data.get('msg', data.get('message', '未知'))}")
        sys.exit(9)

    print(format_usage(data))


if __name__ == "__main__":
    main()
