package models


// UsageData 用量查询结果
type UsageData struct {
	Plan     string      `json:"plan"`
	Level    string      `json:"level"`
	Limits   []LimitItem `json:"limits"`
	Problems []Problem   `json:"problems,omitempty"`
}

// LimitItem 单个限额条目
type LimitItem struct {
	Type          string        `json:"type"`
	Unit          int           `json:"unit,omitempty"`
	Number        int           `json:"number,omitempty"`
	Usage         int           `json:"usage,omitempty"`
	CurrentValue  int           `json:"currentValue,omitempty"`
	Remaining     int           `json:"remaining,omitempty"`
	Percentage    int           `json:"percentage"`
	NextResetTime int64         `json:"nextResetTime,omitempty"`
	UsageDetails  []UsageDetail `json:"usageDetails,omitempty"`
}

// UsageDetail 分项使用详情
type UsageDetail struct {
	ModelCode string `json:"modelCode"`
	Usage     int    `json:"usage"`
}

// Problem 问题信息
type Problem struct {
	Code    int    `json:"code"`
	Message string `json:"message"`
}

// FormattedLimit 格式化后的限额（用于输出）
type FormattedLimit struct {
	Type       string        `json:"type"`
	Used       int           `json:"used,omitempty"`
	Total      int           `json:"total,omitempty"`
	Remaining  int           `json:"remaining,omitempty"`
	Percentage int           `json:"percentage"`
	ResetAt    string        `json:"resetAt,omitempty"`
	ResetTS    int64         `json:"-"` // 毫秒时间戳，用于倒计时
	Details    []UsageDetail `json:"details,omitempty"`
}

// QueryResult 查询输出结果
type QueryResult struct {
	Timestamp string           `json:"timestamp"`
	Plan      string           `json:"plan"`
	Limits    []FormattedLimit `json:"limits"`
}

// UsageResponse 用量查询 API 响应
type UsageResponse struct {
	Code    int       `json:"code"`
	Success bool      `json:"success"`
	Message string    `json:"message"`
	Data    UsageData `json:"data"`
}
