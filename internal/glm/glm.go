package glm

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"strings"
	"time"

	"github.com/cicbyte/glm-usage/internal/models"
)

const defaultMonitorURL = "https://open.bigmodel.cn/api/monitor/usage/quota/limit"

var httpClient = &http.Client{
	Timeout: 15 * time.Second,
}

// QueryUsage 查询智谱 AI API 用量
func QueryUsage(ctx context.Context, apiKey string) (*models.UsageData, error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, defaultMonitorURL, nil)
	if err != nil {
		return nil, fmt.Errorf("创建请求失败: %w", err)
	}

	// 设置认证头
	key := strings.TrimPrefix(strings.TrimSpace(apiKey), "Bearer ")
	req.Header.Set("Authorization", "Bearer "+key)
	req.Header.Set("Content-Type", "application/json")

	resp, err := httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("请求失败: %w", err)
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(io.LimitReader(resp.Body, 1<<20)) // 限制 1MB
	if err != nil {
		return nil, fmt.Errorf("读取响应失败: %w", err)
	}

	if resp.StatusCode == http.StatusUnauthorized {
		return nil, fmt.Errorf("认证失败 (401)")
	}
	if resp.StatusCode == http.StatusTooManyRequests {
		return nil, fmt.Errorf("请求过于频繁 (429)")
	}
	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("请求失败 (%d): %s", resp.StatusCode, string(body))
	}

	var result models.UsageResponse
	if err := json.Unmarshal(body, &result); err != nil {
		return nil, fmt.Errorf("解析响应失败: %w", err)
	}

	if !result.Success {
		return nil, fmt.Errorf("API 错误: %s", result.Message)
	}

	return &result.Data, nil
}
