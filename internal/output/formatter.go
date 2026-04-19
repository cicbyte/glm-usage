package output

import (
	"fmt"
	"sort"
	"time"

	"github.com/cicbyte/glm-usage/internal/models"
)

// limitTypeOrder 定义限额类型的显示优先级，值越小越靠前
var limitTypeOrder = map[string]int{
	"TOKENS_LIMIT":  0,
	"TIME_LIMIT":    1,
	"REQUEST_LIMIT": 2,
}

// Formatter 输出格式化器接口
type Formatter interface {
	Format(result *models.QueryResult) (string, error)
	FormatError(err string) string
}

// GetFormatter 获取格式化器
func GetFormatter(format string) (Formatter, error) {
	switch format {
	case "table":
		return &TableFormatter{}, nil
	case "json":
		return &JSONFormatter{}, nil
	case "yaml":
		return &YAMLFormatter{}, nil
	case "plain":
		return &PlainFormatter{}, nil
	default:
		return nil, fmt.Errorf("不支持的输出格式: %s", format)
	}
}

// FormatUsageData 将 API 返回的 UsageData 转换为格式化用的 QueryResult
func FormatUsageData(data *models.UsageData, timezone string) *models.QueryResult {
	result := &models.QueryResult{
		Timestamp: time.Now().Format(time.RFC3339),
		Plan:      data.Level,
		Limits:    make([]models.FormattedLimit, 0, len(data.Limits)),
	}

	loc, _ := time.LoadLocation(timezone)
	if loc == nil {
		loc = time.Local
	}

	for _, limit := range data.Limits {
		fl := models.FormattedLimit{
			Type:       FormatLimitType(limit.Type),
			Used:       limit.CurrentValue,
			Total:      limit.Usage,
			Remaining:  limit.Remaining,
			Percentage: limit.Percentage,
			ResetTS:    limit.NextResetTime,
			Details:    limit.UsageDetails,
		}

		if limit.NextResetTime > 0 {
			t := time.UnixMilli(limit.NextResetTime).In(loc)
			fl.ResetAt = t.Format("2006-01-02 15:04")
		}

		result.Limits = append(result.Limits, fl)
	}

	sort.Slice(result.Limits, func(i, j int) bool {
		return limitTypeOrder[data.Limits[i].Type] < limitTypeOrder[data.Limits[j].Type]
	})

	return result
}

func FormatLimitType(t string) string {
	switch t {
	case "TIME_LIMIT":
		return "MCP"
	case "TOKENS_LIMIT":
		return "Token"
	case "REQUEST_LIMIT":
		return "Request"
	default:
		return t
	}
}
