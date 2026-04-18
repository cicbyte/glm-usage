package utils

import (
	"fmt"
	"os"
)

// 初始化应用目录结构
func InitAppDirs() error {
	dirs := []string{
		ConfigInstance.GetConfigDir(),
		ConfigInstance.GetLogDir(),
	}

	for _, dir := range dirs {
		if err := os.MkdirAll(dir, 0755); err != nil {
			return fmt.Errorf("创建目录失败 %s: %w", dir, err)
		}
	}

	return nil
}
