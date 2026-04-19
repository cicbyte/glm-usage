import SwiftUI

struct SettingsView: View {
    @ObservedObject var appState = AppState.shared
    @ObservedObject var config = ConfigService.shared
    @State private var apiKeyInput: String = ""
    @State private var showAPIKey: Bool = false
    @State private var isValidating: Bool = false
    @State private var validationMessage: String?

    var body: some View {
        Form {
            accountSection
            generalSection
            notificationSection
            aboutSection
        }
        .formStyle(.grouped)
        .frame(width: 400, height: 480)
        .onAppear {
            loadCurrentAPIKey()
        }
    }

    // MARK: - Account

    private var accountSection: some View {
        Section {
            HStack {
                if showAPIKey {
                    TextField("API Key", text: $apiKeyInput)
                        .textFieldStyle(.roundedBorder)
                } else {
                    SecureField("API Key", text: $apiKeyInput)
                        .textFieldStyle(.roundedBorder)
                }
                Button {
                    showAPIKey.toggle()
                } label: {
                    Image(systemName: showAPIKey ? "eye.slash" : "eye")
                }
                .buttonStyle(.plain)
            }

            Picker("服务器", selection: selectedServer) {
                ForEach(Constants.GLMServer.allCases) { server in
                    Text(server.label).tag(server)
                }
            }

            HStack {
                Button("保存") {
                    saveAPIKey()
                }
                .disabled(apiKeyInput.isEmpty)
                .buttonStyle(.borderedProminent)

                Button("验证") {
                    validateKey()
                }
                .disabled(apiKeyInput.isEmpty || isValidating)

                if isValidating {
                    ProgressView()
                        .scaleEffect(0.6)
                }

                Spacer()

                if let msg = validationMessage {
                    Text(msg)
                        .font(.caption)
                        .foregroundStyle(msg.contains("成功") ? .green : .red)
                }
            }
        } header: {
            Text("账户")
        }
    }

    // MARK: - General

    private var generalSection: some View {
        Section {
            Picker("刷新间隔", selection: selectedInterval) {
                ForEach(Constants.RefreshInterval.allCases) { interval in
                    Text(interval.label).tag(interval)
                }
            }

            Toggle("启动时自动刷新", isOn: Binding(
                get: { config.autoRefreshOnLaunch },
                set: { config.saveAutoRefreshOnLaunch($0) }
            ))
        } header: {
            Text("通用")
        }
    }

    // MARK: - Notification

    private var notificationSection: some View {
        Section {
            Toggle("启用通知", isOn: Binding(
                get: { config.notificationsEnabled },
                set: { config.saveNotificationsEnabled($0) }
            ))

            Picker("警告阈值", selection: Binding(
                get: { config.warningThreshold },
                set: { config.saveWarningThreshold($0) }
            )) {
                Text("70%").tag(70)
                Text("80%").tag(80)
                Text("90%").tag(90)
            }

            Picker("危险阈值", selection: Binding(
                get: { config.dangerThreshold },
                set: { config.saveDangerThreshold($0) }
            )) {
                Text("80%").tag(80)
                Text("90%").tag(90)
                Text("95%").tag(95)
            }
        } header: {
            Text("通知")
        }
    }

    // MARK: - About

    private var aboutSection: some View {
        Section {
            HStack {
                Text("版本")
                Spacer()
                Text("\(Constants.App.version) (Build \(Constants.App.build))")
                    .foregroundStyle(.secondary)
            }
        } header: {
            Text("关于")
        }
    }

    // MARK: - Helpers

    private var selectedServer: Binding<Constants.GLMServer> {
        Binding(
            get: { config.serverEndpoint },
            set: {
                config.saveServerEndpoint($0)
                appState.reconfigureAndRefresh()
            }
        )
    }

    private var selectedInterval: Binding<Constants.RefreshInterval> {
        Binding(
            get: { config.refreshInterval },
            set: { newValue in
                config.saveRefreshInterval(newValue)
                appState.restartAutoRefresh()
            }
        )
    }

    private func loadCurrentAPIKey() {
        apiKeyInput = KeychainService.shared.load(key: Constants.Keychain.glmApiKey) ?? ""
    }

    private func saveAPIKey() {
        appState.saveAPIKey(apiKeyInput)
        validationMessage = "已保存"
        Task {
            await appState.manualRefresh()
        }
    }

    private func validateKey() {
        isValidating = true
        validationMessage = nil
        let tempProvider = GLMProvider()
        tempProvider.configure(apiKey: apiKeyInput, baseURL: nil)

        Task {
            let valid = await tempProvider.validateConfig()
            isValidating = false
            validationMessage = valid ? "验证成功" : "验证失败"
        }
    }
}
