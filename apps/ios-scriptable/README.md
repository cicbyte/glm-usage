# GLM Usage - iOS Scriptable 小组件

在 iPhone/iPad 桌面上实时显示智谱 AI (BigModel) 的 MCP 调用次数和 Token 用量，附带彩色进度条和用量预警通知。

## 效果预览

- 深色背景小组件，显示套餐等级
- MCP 用量：`已用/总量` + 进度条（绿/黄/红三色）
- Token 用量：百分比显示
- 超过 90% 自动弹出通知提醒
- 每 5 分钟自动刷新

## 前置条件

- iOS 16+ 设备
- [Scriptable](https://apps.apple.com/app/scriptable/id1405459188) App
- 智谱 AI 开放平台 API Key（[获取地址](https://open.bigmodel.cn/)）

## 安装

1. 打开 Scriptable App
2. 点击右上角 **+** 新建脚本
3. 将 `main.js` 的内容粘贴到编辑器中（或通过 AirDrop / iCloud 文件导入）
4. 替换脚本顶部的 API Key：

```javascript
const API_KEY = "你的智谱 API Key"
```

5. 关闭编辑器，脚本会自动保存

## 添加到桌面

1. 回到主屏幕，长按空白处进入编辑模式
2. 点击左上角 **+**，搜索 **Scriptable**
3. 选择小组件尺寸（建议 **小** 或 **中**）
4. 在配置面板中选择刚创建的脚本
5. 点击「添加小组件」

## 数据说明

| 指标 | 来源字段 | 显示格式 |
|------|---------|---------|
| MCP 调用 | `TIME_LIMIT` | `已用/总量` + 进度条 |
| Token 消耗 | `TOKENS_LIMIT` | 百分比 |
| 套餐等级 | `level` | 标题显示 |
| 重置时间 | `nextResetTime` | `月/日 时:分` |

进度条颜色规则：`≤60%` 绿色 / `60%-80%` 黄色 / `>80%` 红色。用量超过 90% 时会触发系统通知。

## API 端点

```
GET https://open.bigmodel.cn/api/monitor/usage/quota/limit
Authorization: {你的 API Key}
```

与 Go CLI 和 WPF 客户端共享同一端点。
