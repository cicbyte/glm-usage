package tui

import (
	"fmt"
	"os"
	"strings"
	"time"

	"github.com/cicbyte/glm-usage/internal/models"
	"github.com/charmbracelet/lipgloss"
	"github.com/mattn/go-isatty"
)

// IsTerminal 检查 stdout 是否为终端
func IsTerminal() bool {
	return isatty.IsTerminal(os.Stdout.Fd()) || isatty.IsCygwinTerminal(os.Stdout.Fd())
}

// 共享样式
var (
	// 标题样式
	TitleStyle = lipgloss.NewStyle().
			Bold(true).
			Foreground(lipgloss.Color("#7D56F4"))

	// 子标题
	SubtitleStyle = lipgloss.NewStyle().
			Foreground(lipgloss.Color("#666"))

	// Plan 指示器 ● Pro
	PlanDotStyle = lipgloss.NewStyle().
			Foreground(lipgloss.Color("#7D56F4"))
	PlanNameStyle = lipgloss.NewStyle().
			Bold(true).
			Foreground(lipgloss.Color("#FAFAFA"))

	// 正常值
	NormalStyle = lipgloss.NewStyle().
			Foreground(lipgloss.Color("#FAFAFA"))

	// 成功/绿色
	GreenStyle = lipgloss.NewStyle().
			Foreground(lipgloss.Color("#04B575"))

	// 警告/黄色
	YellowStyle = lipgloss.NewStyle().
			Foreground(lipgloss.Color("#E0AF68"))

	// 危险/红色
	RedStyle = lipgloss.NewStyle().
			Bold(true).
			Foreground(lipgloss.Color("#FF5F56"))

	// 淡色文字
	DimStyle = lipgloss.NewStyle().
			Foreground(lipgloss.Color("#666"))

	// 卡片边框
	CardStyle = lipgloss.NewStyle().
			Border(lipgloss.RoundedBorder()).
			BorderForeground(lipgloss.Color("#444")).
			Padding(1, 2)

	// 底部提示
	HelpStyle = lipgloss.NewStyle().
			Foreground(lipgloss.Color("#666")).
			PaddingTop(1)
)

// ProgressColor 根据百分比返回对应颜色
func ProgressColor(pct int) lipgloss.Color {
	switch {
	case pct >= 90:
		return lipgloss.Color("#FF5F56")
	case pct >= 70:
		return lipgloss.Color("#E0AF68")
	default:
		return lipgloss.Color("#04B575")
	}
}

// RenderProgressBar 渲染文本进度条
func RenderProgressBar(pct int, width int) string {
	if width <= 0 {
		width = 30
	}
	filled := pct * width / 100
	if filled > width {
		filled = width
	}
	bar := make([]rune, width)
	for i := 0; i < width; i++ {
		if i < filled {
			bar[i] = '█'
		} else {
			bar[i] = '░'
		}
	}
	return string(bar)
}

// Capitalize 首字母大写
func Capitalize(s string) string {
	if len(s) == 0 {
		return s
	}
	return strings.ToUpper(s[:1]) + s[1:]
}

// renderLimits 渲染用量限额进度条
func renderLimits(limits []models.FormattedLimit) string {
	var b strings.Builder
	for _, limit := range limits {
		pct := limit.Percentage
		barWidth := 25
		bar := RenderProgressBar(pct, barWidth)
		barColored := lipgloss.NewStyle().Foreground(ProgressColor(pct)).Render(bar)
		pctStr := fmt.Sprintf("%3d%%", pct)
		var pctColored string
		switch {
		case pct >= 90:
			pctColored = RedStyle.Render(pctStr)
		case pct >= 70:
			pctColored = YellowStyle.Render(pctStr)
		default:
			pctColored = GreenStyle.Render(pctStr)
		}
		numStr := ""
		if limit.Total > 0 {
			numStr = fmt.Sprintf("%d / %d", limit.Used, limit.Total)
		}
		typeLabel := NormalStyle.Render(fmt.Sprintf("  %-8s", limit.Type))
		line := typeLabel + " " + barColored + " " + pctColored
		if numStr != "" {
			line += "  " + DimStyle.Render(numStr)
		}
		b.WriteString(line + "\n")
		if limit.ResetTS > 0 {
			countdown := FormatCountdown(limit.ResetTS)
			resetInfo := DimStyle.Render(fmt.Sprintf("重置: %s  剩余: ", limit.ResetAt))
			countdownStr := NormalStyle.Render(countdown)
			b.WriteString("            " + resetInfo + countdownStr + "\n")
		}
	}
	return b.String()
}

// FormatCountdown 格式化倒计时
// type 为 MCP 时显示 "Xd Xh Xm"，否则显示 "Xh Xm"
func FormatCountdown(resetTS int64) string {
	if resetTS <= 0 {
		return "-"
	}
	dur := time.Until(time.UnixMilli(resetTS))
	if dur <= 0 {
		return "已重置"
	}
	dur = dur.Truncate(time.Second)
	days := int(dur.Hours()) / 24
	hours := int(dur.Hours()) % 24
	mins := int(dur.Minutes()) % 60
	secs := int(dur.Seconds()) % 60

	if days > 0 {
		return fmt.Sprintf("%dd %dh %02dm %02ds", days, hours, mins, secs)
	}
	return fmt.Sprintf("%dh %02dm %02ds", hours, mins, secs)
}
