package tui

import (
	"context"
	"fmt"
	"strconv"
	"strings"
	"time"

	"github.com/cicbyte/glm-usage/internal/api"
	"github.com/cicbyte/glm-usage/internal/models"
	"github.com/cicbyte/glm-usage/internal/output"
	"github.com/charmbracelet/bubbles/spinner"
	"github.com/charmbracelet/bubbles/textinput"
	tea "github.com/charmbracelet/bubbletea"
	"github.com/charmbracelet/lipgloss"
)

type appResultMsg struct {
	data *models.UsageData
	err  error
}

type appTickMsg time.Time

func fetchAppUsage(cfg *models.AppConfig) tea.Msg {
	ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer cancel()
	data, err := api.QueryUsage(ctx, cfg)
	if err != nil {
		return appResultMsg{err: err}
	}
	return appResultMsg{data: data}
}

type AppModel struct {
	spinner       spinner.Model
	input         textinput.Model
	cfg           *models.AppConfig
	timezone      string
	watch         bool
	interval      int
	data          *models.UsageData
	err           error
	loading       bool
	paused        bool
	editing       bool
	quit          bool
	startTime     time.Time
	lastQueryTime time.Time
	queryCount    int
	width         int
	height        int
}

func NewAppModel(cfg *models.AppConfig, timezone string, watch bool, interval int) AppModel {
	s := spinner.New()
	s.Spinner = spinner.Dot
	s.Style = lipgloss.NewStyle().Foreground(lipgloss.Color("#7D56F4"))

	ti := textinput.New()
	ti.Prompt = "输入刷新间隔(秒): "
	ti.PromptStyle = lipgloss.NewStyle().Foreground(lipgloss.Color("#7D56F4"))
	ti.Placeholder = strconv.Itoa(interval)
	ti.CharLimit = 5
	ti.Focus()

	return AppModel{
		spinner:    s,
		input:      ti,
		cfg:        cfg,
		timezone:   timezone,
		watch:      watch,
		interval:   interval,
		loading:    true,
		startTime:  time.Now(),
	}
}

func (m AppModel) Init() tea.Cmd {
	return tea.Batch(m.spinner.Tick, func() tea.Msg {
		return fetchAppUsage(m.cfg)
	})
}

func (m AppModel) Update(msg tea.Msg) (tea.Model, tea.Cmd) {
	switch msg := msg.(type) {
	case tea.KeyMsg:
		if m.editing {
			switch msg.String() {
			case "enter":
				val := strings.TrimSpace(m.input.Value())
				if val != "" {
					if n, err := strconv.Atoi(val); err == nil && n >= 5 && n <= 86400 {
						m.interval = n
						m.lastQueryTime = time.Now()
					}
				}
				m.editing = false
				m.input.Blur()
				return m, nil
			case "esc":
				m.editing = false
				m.input.Blur()
				return m, nil
			}
			var cmd tea.Cmd
			m.input, cmd = m.input.Update(msg)
			return m, cmd
		}

		switch msg.String() {
		case "q", "ctrl+c":
			m.quit = true
			return m, tea.Quit
		case "r":
			m.loading = true
			m.err = nil
			return m, func() tea.Msg { return fetchAppUsage(m.cfg) }
		}

		// 以下快捷键仅监控模式有效
		if m.watch {
			switch msg.String() {
			case "p", " ":
				m.paused = !m.paused
				if !m.paused {
					m.loading = true
					return m, tea.Batch(
						tea.Tick(time.Second, func(t time.Time) tea.Msg { return appTickMsg(t) }),
						func() tea.Msg { return fetchAppUsage(m.cfg) },
					)
				}
				return m, tea.Tick(time.Second, func(t time.Time) tea.Msg { return appTickMsg(t) })
			case "i":
				m.input.SetValue(strconv.Itoa(m.interval))
				m.editing = true
				m.input.Focus()
				return m, nil
			}
		}

	case tea.WindowSizeMsg:
		m.width = msg.Width
		m.height = msg.Height

	case spinner.TickMsg:
		var cmd tea.Cmd
		m.spinner, cmd = m.spinner.Update(msg)
		return m, cmd

	case appTickMsg:
		return m, tea.Tick(time.Second, func(t time.Time) tea.Msg {
			return appTickMsg(t)
		})

	case appResultMsg:
		m.loading = false
		m.data = msg.data
		m.err = msg.err
		m.queryCount++
		if msg.err == nil {
			m.lastQueryTime = time.Now()
		}
		if msg.data != nil {
			m.data = msg.data
		}

		// 单次模式：数据加载完成后不再启动定时器
		if !m.watch {
			return m, nil
		}

		if m.paused {
			return m, tea.Tick(time.Second, func(t time.Time) tea.Msg { return appTickMsg(t) })
		}
		return m, tea.Batch(
			tea.Tick(time.Second, func(t time.Time) tea.Msg { return appTickMsg(t) }),
			tea.Tick(time.Duration(m.interval)*time.Second, func(t time.Time) tea.Msg {
				return fetchAppUsage(m.cfg)
			}),
		)
	}

	return m, nil
}

func (m AppModel) View() string {
	if m.quit {
		return ""
	}

	var b strings.Builder

	// 标题行
	b.WriteString(TitleStyle.Render("glm-usage"))
	if m.data != nil && m.data.Level != "" {
		b.WriteString("  " + PlanDotStyle.Render("* ") + PlanNameStyle.Render(Capitalize(m.data.Level)))
	}
	b.WriteString("\n")

	if m.loading {
		b.WriteString("\n  " + m.spinner.View() + " 查询中...\n")
	} else if m.err != nil {
		b.WriteString("\n  " + RedStyle.Render("x "+m.err.Error()) + "\n")
		b.WriteString(HelpStyle.Render("  按 r 重试, q 退出"))
	} else if m.data != nil {
		result := output.FormatUsageData(m.data, m.timezone)
		b.WriteString(renderLimits(result.Limits))

		// 详情
		for _, limit := range result.Limits {
			if len(limit.Details) > 0 {
				b.WriteString("\n")
				b.WriteString(DimStyle.Render("  " + limit.Type + " 详情:"))
				b.WriteString("\n")
				for _, d := range limit.Details {
					b.WriteString(fmt.Sprintf("    %s  %s\n", DimStyle.Render(d.ModelCode), NormalStyle.Render(fmt.Sprintf("%d", d.Usage))))
				}
			}
		}

		if m.loading {
			b.WriteString("\n  " + m.spinner.View() + DimStyle.Render(" 刷新中..."))
		}
	}

	// 输入框
	if m.editing {
		b.WriteString("\n\n  " + m.input.View())
	}

	// 帮助行
	var help string
	if m.watch {
		help = "按 i 编辑间隔 | p 暂停 | r 刷新 | q 退出"
	} else {
		help = "按 r 刷新 | q 退出"
	}
	b.WriteString(HelpStyle.Render(help))

	// 状态栏（仅监控模式）
	if m.watch {
		b.WriteString("\n")
		elapsed := time.Since(m.startTime).Truncate(time.Second)
		statusLine := fmt.Sprintf("  间隔: %ds", m.interval)
		if !m.paused && !m.lastQueryTime.IsZero() {
			remaining := time.Duration(m.interval)*time.Second - time.Since(m.lastQueryTime)
			if remaining < 0 {
				remaining = 0
			}
			remaining = remaining.Truncate(time.Second)
			statusLine += fmt.Sprintf("  |  下次刷新: %ds", int(remaining.Seconds()))
		}
		statusLine += fmt.Sprintf("  |  查询: %d次  |  运行: %s", m.queryCount, elapsed)
		if m.paused {
			statusLine += "  |  " + YellowStyle.Render("已暂停")
		}
		b.WriteString(CardStyle.Render(statusLine))
	}

	return b.String()
}

// RunApp 启动 TUI
func RunApp(cfg *models.AppConfig, timezone string, watch bool, interval int) error {
	m := NewAppModel(cfg, timezone, watch, interval)
	p := tea.NewProgram(m, tea.WithAltScreen())
	_, err := p.Run()
	return err
}
