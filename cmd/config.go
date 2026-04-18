package cmd

import (
	"fmt"
	"os"
	"strings"

	"github.com/cicbyte/glm-usage/internal/common"
	"github.com/cicbyte/glm-usage/internal/utils"
	"github.com/spf13/cobra"
)

var configCmd = &cobra.Command{
	Use:   "config",
	Short: "配置管理",
}

var configSetCmd = &cobra.Command{
	Use:   "api-key [value]",
	Short: "设置 API Key",
	Long:  `设置 API Key。提供 value 参数直接设置，否则交互式输入。`,
	Run: func(cmd *cobra.Command, args []string) {
		config := common.AppConfigModel
		var newKey string

		if len(args) > 0 {
			newKey = strings.TrimSpace(args[0])
		} else {
			fmt.Print("请输入新的 API Key: ")
			buf := make([]byte, 256)
			n, err := os.Stdin.Read(buf)
			if err != nil {
				fmt.Fprintln(os.Stderr, "Error: 读取输入失败")
				os.Exit(4)
			}
			newKey = strings.TrimSpace(string(buf[:n]))
		}

		if newKey == "" {
			fmt.Fprintln(os.Stderr, "Error: API Key 不能为空")
			os.Exit(4)
		}

		config.APIKey = newKey

		if err := utils.ConfigInstance.SaveConfig(config); err != nil {
			fmt.Fprintf(os.Stderr, "Error: 保存配置失败: %v\n", err)
			os.Exit(9)
		}

		common.AppConfigModel = config
		fmt.Println("API Key 已更新")
	},
}

var configPathCmd = &cobra.Command{
	Use:   "path",
	Short: "显示配置文件路径",
	Run: func(cmd *cobra.Command, args []string) {
		fmt.Println(utils.ConfigInstance.GetConfigPath())
	},
}

func init() {
	rootCmd.AddCommand(configCmd)
	configCmd.AddCommand(configSetCmd)
	configCmd.AddCommand(configPathCmd)
}
