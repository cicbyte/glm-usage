package cmd

import (
	"context"
	"fmt"
	"strings"
	"time"

	"github.com/cicbyte/glm-usage/internal/api"
	"github.com/cicbyte/glm-usage/internal/common"
	"github.com/cicbyte/glm-usage/internal/log"
	"github.com/cicbyte/glm-usage/internal/models"
	"github.com/cicbyte/glm-usage/internal/output"
	"github.com/cicbyte/glm-usage/internal/tui"
	"github.com/cicbyte/glm-usage/internal/utils"
	"github.com/charmbracelet/lipgloss"
	"github.com/spf13/cobra"
	"go.uber.org/zap"
)

var statusCmd = &cobra.Command{
	Use:   "status",
	Short: "显示配置状态和连接测试",
	Long:  `诊断工具状态，显示配置信息并测试 API 连接。`,
	Run: func(cmd *cobra.Command, args []string) {
		cfg := utils.GetEffectiveConfig(common.AppConfigModel, apiKey)
		printStatus(cfg)
	},
}

func init() {
	rootCmd.AddCommand(statusCmd)
}

func printStatus(cfg *models.AppConfig) {
	configPath := utils.ConfigInstance.GetConfigPath()

	fmt.Println(tui.TitleStyle.Render("glm-usage status"))
	fmt.Println()

	fmt.Println(tui.CardStyle.Render(
		fmt.Sprintf("  配置文件  %s\n  API Key   %s",
			configPath,
			maskKey(cfg.APIKey),
		)))
	fmt.Println()

	// 连接测试
	fmt.Print("  连接测试  ")
	ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	latency, err := api.TestConnection(ctx, cfg)
	cancel()

	if err != nil {
		fmt.Println(tui.RedStyle.Render("FAILED"))
		fmt.Printf("  错误: %s\n", err.Error())
		log.Error("连接测试失败", zap.Error(err))
	} else {
		fmt.Println(tui.GreenStyle.Render(fmt.Sprintf("OK  (%dms)", latency.Milliseconds())))
	}
	fmt.Println()

	if cfg.APIKey == "" {
		fmt.Println(tui.DimStyle.Render("    (未配置 API Key，无法查询)"))
		return
	}

	queryCtx, queryCancel := context.WithTimeout(context.Background(), 15*time.Second)
	data, err := api.QueryUsage(queryCtx, cfg)
	queryCancel()

	if err != nil {
		fmt.Printf("    %s\n", tui.RedStyle.Render("查询失败: "+err.Error()))
		return
	}

	result := output.FormatUsageData(data, defaultTimezone)

	if result.Plan != "" {
		fmt.Println("  " + tui.PlanDotStyle.Render("* ") + tui.PlanNameStyle.Render(strings.ToUpper(result.Plan)))
	}
	fmt.Println()

	for _, limit := range result.Limits {
		pct := limit.Percentage
		bar := tui.RenderProgressBar(pct, 25)
		barColored := lipgloss.NewStyle().Foreground(tui.ProgressColor(pct)).Render(bar)

		pctStr := fmt.Sprintf("%3d%%", pct)
		var pctRendered string
		switch {
		case pct >= 90:
			pctRendered = tui.RedStyle.Render(pctStr)
		case pct >= 70:
			pctRendered = tui.YellowStyle.Render(pctStr)
		default:
			pctRendered = tui.GreenStyle.Render(pctStr)
		}

		line := fmt.Sprintf("    %-8s %s %s", limit.Type, barColored, pctRendered)
		if limit.Total > 0 {
			line += fmt.Sprintf("  %s", tui.DimStyle.Render(fmt.Sprintf("%d/%d", limit.Used, limit.Total)))
		}
		fmt.Println(line)
		if limit.ResetAt != "" {
			fmt.Printf("              %s\n", tui.DimStyle.Render("重置: "+limit.ResetAt))
		}
	}
}
