package output

import (
	"fmt"
	"strings"

	"github.com/cicbyte/glm-usage/internal/models"
)

// PlainFormatter 纯文本格式输出
type PlainFormatter struct{}

func (f *PlainFormatter) Format(result *models.QueryResult) (string, error) {
	var sb strings.Builder

	sb.WriteString(fmt.Sprintf("Timestamp: %s\n", result.Timestamp))
	sb.WriteString(fmt.Sprintf("Plan: %s\n", result.Plan))
	sb.WriteString("\n")

	for _, limit := range result.Limits {
		sb.WriteString(fmt.Sprintf("[%s]\n", limit.Type))
		if limit.Total > 0 {
			sb.WriteString(fmt.Sprintf("  Used: %d / %d (%d%%)\n", limit.Used, limit.Total, limit.Percentage))
			sb.WriteString(fmt.Sprintf("  Remaining: %d\n", limit.Remaining))
		} else {
			sb.WriteString(fmt.Sprintf("  Percentage: %d%%\n", limit.Percentage))
		}
		if limit.ResetAt != "" {
			sb.WriteString(fmt.Sprintf("  Reset: %s\n", limit.ResetAt))
		}
		if len(limit.Details) > 0 {
			sb.WriteString("  Details:\n")
			for _, d := range limit.Details {
				sb.WriteString(fmt.Sprintf("    - %s: %d\n", d.ModelCode, d.Usage))
			}
		}
		sb.WriteString("\n")
	}

	return sb.String(), nil
}

func (f *PlainFormatter) FormatError(err string) string {
	return fmt.Sprintf("Error: %s\n", err)
}
