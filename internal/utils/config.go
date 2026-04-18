package utils

import (
	"fmt"
	"os"
	"path/filepath"

	"github.com/cicbyte/glm-usage/internal/models"
	"go.yaml.in/yaml/v3"
)

const (
	appSeriesName = "glm-usage"
	appBaseDir    = ".cicbyte"
)

var ConfigInstance = &configManager{}

type configManager struct{}

func (c *configManager) getHomeDir() (string, error) {
	dir, err := os.UserHomeDir()
	if err != nil {
		return "", fmt.Errorf("无法获取用户主目录: %w", err)
	}
	return dir, nil
}

func (c *configManager) GetAppSeriesDir() (string, error) {
	home, err := c.getHomeDir()
	if err != nil {
		return "", err
	}
	return filepath.Join(home, appBaseDir), nil
}

func (c *configManager) GetAppDir() (string, error) {
	home, err := c.getHomeDir()
	if err != nil {
		return "", err
	}
	return filepath.Join(home, appBaseDir, appSeriesName), nil
}

func (c *configManager) GetConfigDir() string {
	home, _ := c.getHomeDir()
	return filepath.Join(home, appBaseDir, appSeriesName, "config")
}

func (c *configManager) GetLogDir() string {
	home, _ := c.getHomeDir()
	return filepath.Join(home, appBaseDir, appSeriesName, "logs")
}

func (c *configManager) GetConfigPath() string {
	return filepath.Join(c.GetConfigDir(), "config.yaml")
}

func (c *configManager) GetLogPath() string {
	return filepath.Join(c.GetLogDir(), "app.log")
}

func (c *configManager) LoadConfig() *models.AppConfig {
	configPath := c.GetConfigPath()
	data, err := os.ReadFile(configPath)
	if err != nil {
		return &models.AppConfig{}
	}

	config := &models.AppConfig{}
	if err := yaml.Unmarshal(data, config); err != nil {
		fmt.Fprintf(os.Stderr, "警告: 配置文件解析失败，使用默认配置: %v\n", err)
		return &models.AppConfig{}
	}

	return config
}

func (c *configManager) SaveConfig(config *models.AppConfig) error {
	data, err := yaml.Marshal(config)
	if err != nil {
		return err
	}
	return os.WriteFile(c.GetConfigPath(), data, 0600)
}

// GetEffectiveConfig 合并运行时参数到配置中
func GetEffectiveConfig(base *models.AppConfig, apiKey string) *models.AppConfig {
	cfg := *base

	if apiKey != "" {
		cfg.APIKey = apiKey
	}

	return &cfg
}
