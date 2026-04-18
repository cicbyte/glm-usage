package output

import (
	"fmt"
	"strings"

	"github.com/cicbyte/glm-usage/internal/models"
	"github.com/mattn/go-runewidth"
)

// TableFormatter 表格格式输出
type TableFormatter struct{}

func (f *TableFormatter) Format(result *models.QueryResult) (string, error) {
	if len(result.Limits) == 0 {
		return "暂无用量数据", nil
	}

	colWidths := map[string]int{
		"TYPE": 8,
		"USED": 8,
		"TOTAL": 8,
		"PERCENT": 8,
		"RESET": 16,
	}

	var sb strings.Builder
	headers := []string{"TYPE", "USED", "TOTAL", "PERCENT", "RESET"}

	sb.WriteString("┌")
	for i, h := range headers {
		sb.WriteString(strings.Repeat("─", colWidths[h]))
		if i < len(headers)-1 {
			sb.WriteString("┬")
		}
	}
	sb.WriteString("┐\n")

	sb.WriteString("│")
	for i, h := range headers {
		sb.WriteString(padCenter(h, colWidths[h]))
		if i < len(headers)-1 {
			sb.WriteString("│")
		}
	}
	sb.WriteString("│\n")

	sb.WriteString("├")
	for i, h := range headers {
		sb.WriteString(strings.Repeat("─", colWidths[h]))
		if i < len(headers)-1 {
			sb.WriteString("┼")
		}
	}
	sb.WriteString("┤\n")

	for _, limit := range result.Limits {
		used := "-"
		total := "-"
		if limit.Total > 0 {
			used = fmt.Sprintf("%d", limit.Used)
			total = fmt.Sprintf("%d", limit.Total)
		}
		pct := fmt.Sprintf("%d%%", limit.Percentage)
		reset := "-"
		if limit.ResetAt != "" {
			reset = limit.ResetAt
		}

		values := []string{limit.Type, used, total, pct, reset}
		sb.WriteString("│")
		for i, v := range values {
			w := colWidths[headers[i]]
			sb.WriteString(padLeft(v, w))
			if i < len(headers)-1 {
				sb.WriteString("│")
			}
		}
		sb.WriteString("│\n")
	}

	sb.WriteString("└")
	for i, h := range headers {
		sb.WriteString(strings.Repeat("─", colWidths[h]))
		if i < len(headers)-1 {
			sb.WriteString("┴")
		}
	}
	sb.WriteString("┘\n")

	plan := "Free"
	if result.Plan != "" {
		plan = strings.ToUpper(result.Plan[:1]) + result.Plan[1:]
	}
	sb.WriteString(fmt.Sprintf("Plan: %s\n", plan))

	return sb.String(), nil
}

func (f *TableFormatter) FormatError(err string) string {
	return fmt.Sprintf("Error: %s\n", err)
}

func padLeft(s string, width int) string {
	w := runewidth.StringWidth(s)
	if w >= width {
		return s
	}
	return s + strings.Repeat(" ", width-w)
}

func padCenter(s string, width int) string {
	w := runewidth.StringWidth(s)
	if w >= width {
		return s
	}
	left := (width - w) / 2
	right := width - w - left
	return strings.Repeat(" ", left) + s + strings.Repeat(" ", right)
}
