// GLM_Usage.js - Scriptable 脚本

const API_KEY = "xxx"
const BASE_URL = "https://open.bigmodel.cn"

let widget = new ListWidget()
widget.backgroundColor = new Color("#1a1a1a")

async function fetchUsage() {
  let req = new Request(`${BASE_URL}/api/monitor/usage/quota/limit`)
  req.headers = {
    "Authorization": API_KEY,
    "User-Agent": "ios-widget/1.0"
  }
  
  try {
    let res = await req.loadJSON()
    if (res.code !== 200) throw new Error(res.msg)
    return res.data
  } catch (e) {
    return { error: e.message }
  }
}

function formatTime(timestamp) {
  if (!timestamp) return ""
  let date = new Date(timestamp)
  return `${date.getMonth()+1}/${date.getDate()} ${date.getHours()}:${String(date.getMinutes()).padStart(2,'0')}`
}

async function buildWidget() {
  let data = await fetchUsage()
  
  if (data.error) {
    let errorText = widget.addText("❌ " + data.error)
    errorText.textColor = Color.red()
    errorText.font = Font.systemFont(14)
    return
  }
  
  let limits = data.limits || []
  let timeLimit = limits.find(l => l.type === "TIME_LIMIT")
  let tokenLimit = limits.find(l => l.type === "TOKENS_LIMIT")
  
  // 标题
  let title = widget.addText("GLM " + (data.level || "Plan").toUpperCase())
  title.textColor = Color.white()
  title.font = Font.boldSystemFont(16)
  widget.addSpacer(8)
  
  // MCP 用量
  if (timeLimit) {
    let mcpStack = widget.addStack()
    mcpStack.layoutHorizontally()
    
    let mcpLabel = mcpStack.addText("MCP ")
    mcpLabel.textColor = Color.gray()
    mcpLabel.font = Font.systemFont(14)
    
    let mcpValue = mcpStack.addText(`${timeLimit.currentValue}/${timeLimit.usage}`)
    mcpValue.textColor = timeLimit.percentage > 80 ? Color.red() : timeLimit.percentage > 60 ? Color.yellow() : Color.green()
    mcpValue.font = Font.boldSystemFont(14)
    
    widget.addSpacer(4)
    
    // 进度条（修复：用 systemFont 代替 monospacedSystemFont）
    let progress = Math.round((timeLimit.currentValue / timeLimit.usage) * 10)
    let bar = "█".repeat(progress) + "░".repeat(10 - progress)
    let barText = widget.addText(bar)
    barText.textColor = timeLimit.percentage > 80 ? Color.red() : Color.gray()
    barText.font = Font.systemFont(12)  // ← 第76行，已修复
    
    widget.addSpacer(4)
    
    if (timeLimit.nextResetTime) {
      let reset = widget.addText("重置: " + formatTime(timeLimit.nextResetTime))
      reset.textColor = Color.gray()
      reset.font = Font.systemFont(10)
    }
  }
  
  widget.addSpacer(8)
  
  // Token 百分比
  if (tokenLimit) {
    let tokenStack = widget.addStack()
    tokenStack.layoutHorizontally()
    
    let tokenLabel = tokenStack.addText("Token ")
    tokenLabel.textColor = Color.gray()
    tokenLabel.font = Font.systemFont(14)
    
    let tokenValue = tokenStack.addText(`${tokenLimit.percentage}%`)
    tokenValue.textColor = tokenLimit.percentage > 80 ? Color.red() : tokenLimit.percentage > 60 ? Color.yellow() : Color.green()
    tokenValue.font = Font.boldSystemFont(14)
    
    if (tokenLimit.nextResetTime) {
      let reset = widget.addText("重置: " + formatTime(tokenLimit.nextResetTime))
      reset.textColor = Color.gray()
      reset.font = Font.systemFont(10)
    }
  }
  
  // 临界提醒
  if (timeLimit && timeLimit.percentage > 90) {
    let n = new Notification()
    n.title = "GLM 用量警告"
    n.body = `MCP 已用 ${timeLimit.percentage}%，即将耗尽！`
    n.sound = "default"
    n.schedule()
  }
  
  widget.refreshAfterDate = new Date(Date.now() + 5 * 60 * 1000)
}

await buildWidget()
Script.setWidget(widget)
Script.complete()