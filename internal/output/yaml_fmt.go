package output

import (
	"fmt"

	"github.com/cicbyte/glm-usage/internal/models"
	"go.yaml.in/yaml/v3"
)

// YAMLFormatter YAML 格式输出
type YAMLFormatter struct{}

func (f *YAMLFormatter) Format(result *models.QueryResult) (string, error) {
	data, err := yaml.Marshal(result)
	if err != nil {
		return "", err
	}
	return string(data), nil
}

func (f *YAMLFormatter) FormatError(err string) string {
	return fmt.Sprintf("error: %s\n", err)
}
