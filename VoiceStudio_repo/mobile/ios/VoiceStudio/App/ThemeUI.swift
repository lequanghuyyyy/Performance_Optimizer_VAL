import SwiftUI
import VoiceStudioKit

private struct ThemeKey: EnvironmentKey { static let defaultValue = Theme() }
extension EnvironmentValues {
    var vsTheme: Theme { get { self[ThemeKey.self] } set { self[ThemeKey.self] = newValue } }
}
extension Theme {
    func color(_ role: String) -> Color { let c = self[role]; return Color(.sRGB, red: c.r, green: c.g, blue: c.b, opacity: c.a) }
}
enum VSButtonKind { case primary, secondary, tertiary }
struct VSButtonStyle: ButtonStyle {
    @Environment(\.vsTheme) private var theme
    @Environment(\.isEnabled) private var enabled
    var kind: VSButtonKind = .secondary
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.custom("InterVariable", size: Theme.controlTypeSize, relativeTo: .body))
            .frame(minWidth: 44, minHeight: 44)
            .frame(maxWidth: kind == .primary ? .infinity : nil)
            .padding(.horizontal, kind == .tertiary ? 0 : VSTokens.spacingGap)
            .foregroundStyle(theme.color(!enabled ? "mutedForeground" : (configuration.role == .destructive ? "error" : (kind == .primary ? "messageActionForeground" : "text"))))
            .background {
                if kind != .tertiary {
                    RoundedRectangle(cornerRadius: VSTokens.radiusControl)
                        .fill(theme.color(!enabled ? "muted" : (kind == .primary ? (configuration.isPressed ? "messageActionHover" : "messageAction") : (configuration.isPressed ? "muted" : "surface"))))
                }
            }
            .overlay {
                if kind == .secondary { RoundedRectangle(cornerRadius: VSTokens.radiusControl).strokeBorder(theme.color("border")) }
            }
    }
}
struct VSFieldStyle: TextFieldStyle {
    @Environment(\.vsTheme) private var theme
    func _body(configuration: TextField<Self._Label>) -> some View {
        configuration.padding(Theme.compactInset).frame(minHeight: 44)
            .background(theme.color("input"), in: RoundedRectangle(cornerRadius: VSTokens.radiusControl))
            .tint(theme.color("focus"))
    }
}
struct Page<Content: View>: View {
    @Environment(\.vsTheme) private var theme
    @Environment(AppModel.self) private var model
    let content: Content
    let backgroundRole: String
    init(backgroundRole: String = "canvas", @ViewBuilder content: () -> Content) { self.backgroundRole = backgroundRole; self.content = content() }
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: VSTokens.spacingGap) {
                if model.loading {
                    ProgressView(model.t("common.loading")).frame(minHeight: 44).accessibilityIdentifier("page.loading")
                }
                content
            }
                .padding(Theme.compactInset).frame(maxWidth: .infinity, alignment: .leading)
        }
        .background(theme.color(backgroundRole))
        .keyboardAccessible()
    }
}
struct Card<Content: View>: View {
    @Environment(\.vsTheme) private var theme
    let content: Content
    init(@ViewBuilder content: () -> Content) { self.content = content() }
    var body: some View {
        VStack(alignment: .leading, spacing: VSTokens.spacingGap) { content }
            .frame(maxWidth: .infinity, alignment: .leading).padding(Theme.compactInset)
            .background(theme.color("surface"), in: RoundedRectangle(cornerRadius: VSTokens.radiusLg))
            .overlay(RoundedRectangle(cornerRadius: VSTokens.radiusLg).strokeBorder(theme.color("border")))
    }
}
struct RootView: View {
    @Environment(AppModel.self) private var model
    @Environment(\.colorScheme) private var scheme
    var body: some View {
        let theme = Theme(palette: model.theme.palette(system: scheme == .dark ? .dark : .light), appearance: model.theme.appearance(system: scheme == .dark ? .dark : .light))
        Group {
            TabView {
                if model.connection.client == nil {
                    NavigationStack { ConnectView() }.tabItem { Label(model.t("mobile.tabs.connect"), systemImage: "link") }
                } else {
                    NavigationStack { SpeakView() }.tabItem { Label(model.t("mobile.tabs.speak"), systemImage: "waveform") }
                    NavigationStack { VoicesView() }.tabItem { Label(model.t("mobile.tabs.voices"), systemImage: "person.wave.2") }
                    NavigationStack { DubView() }.tabItem { Label(model.t("mobile.tabs.dub"), systemImage: "film") }
                    NavigationStack { DictateView() }.tabItem { Label(model.t("mobile.tabs.dictate"), systemImage: "mic") }
                }
                NavigationStack { SettingsView() }.tabItem { Label(model.t("mobile.tabs.settings"), systemImage: "gearshape") }
            }
            .id(model.hostState.scopeID)
            .toolbarBackground(theme.color("chrome"), for: .tabBar)
            .toolbarBackground(.visible, for: .tabBar)
        }
        .sheet(isPresented: Binding(get: { !model.consentAnswered }, set: { presented in
            if !presented && !model.consentAnswered { model.chooseConsent(false) }
        }), onDismiss: {
            if !model.consentAnswered { model.chooseConsent(false) }
        }) {
            ConsentView()
                .presentationBackground(theme.color("surfaceOverlay"))
                .presentationCornerRadius(VSTokens.radiusLg)
                .presentationDragIndicator(.visible)
        }
        .onAppear { NativeTheme.apply(theme) }
        .onChange(of: model.theme) { _, _ in NativeTheme.apply(theme) }
        .onChange(of: scheme) { _, _ in NativeTheme.apply(theme) }
        .environment(\.vsTheme, theme)
        .font(.custom("InterVariable", size: VSTokens.textEditor.size, relativeTo: .body))
        .foregroundStyle(theme.color("text")).tint(theme.color("messageAction"))
        .buttonStyle(VSButtonStyle()).textFieldStyle(VSFieldStyle())
        .background(theme.color("canvas"))
        .toolbarBackground(theme.color("chrome"), for: .navigationBar)
        .safeAreaInset(edge: .top) {
            if let error = model.error {
                Card {
                    Text(model.t(error.key, error.params)).foregroundStyle(theme.color("error"))
                    HStack {
                        Button(model.t("common.close"), systemImage: "xmark") { model.error = nil }
                            .buttonStyle(VSButtonStyle(kind: .tertiary))
                        if error.key == "mobile.permissions.denied" {
                            Button(model.t("mobile.permissions.open_settings")) { if let url = URL(string: UIApplication.openSettingsURLString) { UIApplication.shared.open(url) } }
                        }
                    }
                }.padding(Theme.compactInset).environment(\.vsTheme, theme).accessibilityElement(children: .contain)
            }
        }
    }
}
struct ConsentView: View {
    @Environment(AppModel.self) private var model
    var body: some View {
        Page(backgroundRole: "surfaceOverlay") {
            Text(model.t("consent.title"))
                .font(.custom("InterVariable", size: Theme.titleTypeSize, relativeTo: .title).weight(.semibold))
                .accessibilityAddTraits(.isHeader).accessibilityIdentifier("consent.title")
            Text(model.t("consent.body"))
            Text(model.t("mobile.android.analytics_local"))
            ViewThatFits(in: .horizontal) {
                HStack(spacing: VSTokens.spacingGap) { choice(true); choice(false) }
                VStack(spacing: VSTokens.spacingGap) { choice(true); choice(false) }
            }
        }
        .accessibilityAction(.escape) { model.chooseConsent(false) }
    }
    @ViewBuilder func choice(_ value: Bool) -> some View {
        let button = Button { model.chooseConsent(value) } label: {
            Text(model.t(value ? "consent.yes" : "consent.no"))
                .multilineTextAlignment(.center).fixedSize(horizontal: false, vertical: true)
                .frame(maxWidth: .infinity)
        }.buttonStyle(VSButtonStyle(kind: .secondary)).accessibilityIdentifier(value ? "consent.yes" : "consent.no")
        if value { button }
        else { button.keyboardShortcut(.cancelAction) }
    }
}
