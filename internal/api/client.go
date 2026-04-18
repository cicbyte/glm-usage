package api

import (
	"context"
	"fmt"
	"math"
	"time"

	"github.com/cicbyte/glm-usage/internal/glm"
	"github.com/cicbyte/glm-usage/internal/log"
	"github.com/cicbyte/glm-usage/internal/models"
	"go.uber.org/zap"
)

const (
	maxRetries     = 3
	baseRetryDelay = 1 * time.Second
	requestTimeout = 10 * time.Second
)

// QueryUsage 查询 API 用量（带重试）
func QueryUsage(ctx context.Context, cfg *models.AppConfig) (*models.UsageData, error) {
	apiKey := cfg.APIKey

	if apiKey == "" {
		return nil, fmt.Errorf("API Key 未配置，请运行 'glm-usage config api-key <value>' 设置")
	}

	var lastErr error
	for i := 0; i < maxRetries; i++ {
		if i > 0 {
			delay := time.Duration(math.Pow(2, float64(i-1))) * baseRetryDelay
			log.Info("重试请求",
				zap.Int("attempt", i+1),
				zap.Duration("delay", delay))
			select {
			case <-time.After(delay):
			case <-ctx.Done():
				return nil, ctx.Err()
			}
		}

		queryCtx, cancel := context.WithTimeout(ctx, requestTimeout)
		result, err := glm.QueryUsage(queryCtx, apiKey)
		cancel()

		if err == nil {
			return result, nil
		}

		lastErr = err
		log.Warn("查询失败",
			zap.Int("attempt", i+1),
			zap.Error(err))
	}

	return nil, fmt.Errorf("重试 %d 次后仍然失败: %w", maxRetries, lastErr)
}

// TestConnection 测试 API 连接
func TestConnection(ctx context.Context, cfg *models.AppConfig) (latency time.Duration, err error) {
	start := time.Now()
	_, err = QueryUsage(ctx, cfg)
	latency = time.Since(start)
	return
}
