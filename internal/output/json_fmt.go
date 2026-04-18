package output

import (
	"encoding/json"

	"github.com/cicbyte/glm-usage/internal/models"
)

// JSONFormatter JSON 格式输出
type JSONFormatter struct{}

func (f *JSONFormatter) Format(result *models.QueryResult) (string, error) {
	data, err := json.MarshalIndent(result, "", "  ")
	if err != nil {
		return "", err
	}
	return string(data) + "\n", nil
}

func (f *JSONFormatter) FormatError(err string) string {
	data, _ := json.MarshalIndent(map[string]string{"error": err}, "", "  ")
	return string(data) + "\n"
}
