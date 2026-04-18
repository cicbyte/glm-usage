package cmd

import (
	"context"
	"fmt"
	"os"
	"strings"
	"time"

	"github.com/cicbyte/glm-usage/internal/api"
	"github.com/cicbyte/glm-usage/internal/common"
	"github.com/cicbyte/glm-usage/internal/log"
	"github.com/cicbyte/glm-usage/internal/models"
	"github.com/cicbyte/glm-usage/internal/output"
	"github.com/cicbyte/glm-usage/internal/tui"
	"github.com/cicbyte/glm-usage/internal/utils"
	"github.com/spf13/cobra"
	"go.uber.org/zap"
)

var (
	cfgFile   string
	outputFmt string
	apiKey    string
	verbose   bool
	watch    bool
	interval int
)

const defaultTimezone = "Asia/Shanghai"

var rootCmd = &cobra.Command{
	Use:   "glm-usage",
	Short: "CLI API 用量监控工具",
	Long:  `glm-usage - 查询、监控智谱 AI API 用量限额。`,
	PersistentPreRunE: func(cmd *cobra.Command, args []string) error {
		if err := utils.InitAppDirs(); err != nil {
			return err
		}
		common.AppConfigModel = utils.ConfigInstance.LoadConfig()
		log.Init(utils.ConfigInstance.GetLogPath(), verbose)
		return nil
	},
	Run: runRoot,
}

func Execute() {
	if err := rootCmd.Execute(); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}

func init() {
	rootCmd.PersistentFlags().StringVar(&cfgFile, "config", "", "指定配置文件路径")
	rootCmd.PersistentFlags().StringVarP(&outputFmt, "output", "o", "", "输出格式: table/json/yaml/plain")
	rootCmd.PersistentFlags().StringVar(&apiKey, "api-key", "", "临时指定 API Key（覆盖配置）")
	rootCmd.PersistentFlags().BoolVarP(&verbose, "verbose", "v", false, "详细日志输出")

	rootCmd.Flags().BoolVarP(&watch, "watch", "w", false, "持续监控模式")
	rootCmd.Flags().IntVarP(&interval, "interval", "i", 300, "刷新间隔（秒）")
}

func runRoot(cmd *cobra.Command, args []string) {
	cfg := utils.GetEffectiveConfig(common.AppConfigModel, apiKey)

	if watch && interval < 5 {
		fmt.Fprintln(os.Stderr, "Error: 刷新间隔不能小于 5 秒")
		os.Exit(4)
	}

	outFmt := getOutputFormat()

	// TUI 模式
	if outFmt == "table" && tui.IsTerminal() {
		if err := tui.RunApp(cfg, defaultTimezone, watch, interval); err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
		return
	}

	// 纯文本模式
	formatter, err := output.GetFormatter(outFmt)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(4)
	}

	if !watch {
		doSingleQuery(cfg, formatter)
		return
	}

	doWatchPlain(cfg, formatter)
}

func doSingleQuery(cfg *models.AppConfig, formatter output.Formatter) {
	ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer cancel()

	data, err := api.QueryUsage(ctx, cfg)
	if err != nil {
		log.Error("查询失败", zap.Error(err))
		fmt.Fprintln(os.Stderr, formatter.FormatError(err.Error()))
		os.Exit(exitCode(err))
	}

	result := output.FormatUsageData(data, defaultTimezone)
	out, err := formatter.Format(result)
	if err != nil {
		fmt.Fprintln(os.Stderr, formatter.FormatError(err.Error()))
		os.Exit(9)
	}
	fmt.Print(out)
}

func doWatchPlain(cfg *models.AppConfig, formatter output.Formatter) {
	ticker := time.NewTicker(time.Duration(interval) * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-ticker.C:
			ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
			data, err := api.QueryUsage(ctx, cfg)
			cancel()

			if err != nil {
				log.Error("查询失败", zap.Error(err))
				fmt.Fprintln(os.Stderr, formatter.FormatError(err.Error()))
				continue
			}

			result := output.FormatUsageData(data, defaultTimezone)
			out, err := formatter.Format(result)
			if err != nil {
				continue
			}
			fmt.Print(out)
		}
	}
}

func getOutputFormat() string {
	if outputFmt != "" {
		return outputFmt
	}
	return "table"
}

func maskKey(key string) string {
	if len(key) <= 8 {
		return "***"
	}
	return key[:4] + "****" + key[len(key)-4:]
}

func exitCode(err error) int {
	msg := err.Error()
	switch {
	case strings.Contains(msg, "401"), strings.Contains(msg, "认证"):
		return 1
	case strings.Contains(msg, "超时"), strings.Contains(msg, "timeout"):
		return 2
	case strings.Contains(msg, "网络"), strings.Contains(msg, "connection"):
		return 5
	default:
		return 9
	}
}
