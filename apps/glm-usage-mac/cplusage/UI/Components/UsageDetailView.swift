import SwiftUI

struct UsageDetailView: View {
    let details: [UsageDetail]

    var body: some View {
        if details.isEmpty {
            EmptyView()
        } else {
            VStack(alignment: .leading, spacing: 4) {
                Text("MCP 工具调用详情")
                    .font(.caption.bold())
                    .foregroundStyle(.secondary)
                    .padding(.top, 4)

                ForEach(details) { detail in
                    HStack {
                        Text(detail.toolName)
                            .font(.system(.caption, design: .monospaced))
                        Spacer()
                        Text("\(detail.usage) 次")
                            .font(.caption.monospacedDigit())
                            .foregroundStyle(.secondary)
                    }
                }
            }
            .padding(.leading, 8)
        }
    }
}
