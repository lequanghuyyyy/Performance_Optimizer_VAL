import SwiftUI
import VoiceStudioKit

struct ConnectView: View {
    @Environment(AppModel.self) private var model
    @State private var url = ""
    @State private var pin = ""
    @State private var apiKey = ""
    @State private var label = ""
    @State private var scanning = false
    var body: some View {
        Page {
            Text(model.t("mobile.connect.subtitle"))
            Button(model.t("mobile.connect.scan_qr"), systemImage: "qrcode.viewfinder") { scanning = true }
            Text(model.t("mobile.connect.scan_help"))
            Card {
                TextField(model.t("mobile.connect.url_label"), text: $url).textContentType(.URL).keyboardType(.URL).textInputAutocapitalization(.never).autocorrectionDisabled().accessibilityIdentifier("connect.url")
                TextField(model.t("mobile.connect.pin_label"), text: $pin).keyboardType(.numberPad).textContentType(.oneTimeCode)
                SecureField(model.t("mobile.connect.api_key_label"), text: $apiKey).textInputAutocapitalization(.never).autocorrectionDisabled()
                Text(model.t("mobile.connect.api_key_help"))
                TextField(model.t("mobile.ios.host_label"), text: $label)
                Button(model.t(model.connection.connecting ? "mobile.connect.connecting" : "mobile.connect.connect")) { Task { await connect() } }
                    .buttonStyle(VSButtonStyle(kind: .primary)).disabled(model.connection.connecting || model.busy || BackgroundUpload.shared.pending != nil || url.isEmpty).accessibilityIdentifier("connect.submit")
            }
            if let e = model.connection.error { Text(model.t(e.key, e.params)) }
            Text(model.t("mobile.connect.saved_hosts"))
            ForEach(model.connection.hosts) { host in
                Card {
                    Text(host.label); Text(host.url.absoluteString)
                    Text(model.t("mobile.connect.last_seen", ["time": host.lastSeen.formatted()]))
                    Button(model.t("mobile.connect.connect")) { url = host.url.absoluteString; label = host.label; Task { await connect() } }
                    Button(model.t("mobile.connect.forget"), role: .destructive) { Task { await model.perform(connectionChange: true) { try await model.connection.forget(host) } } }
                }
            }
        }.navigationTitle(model.t("mobile.connect.title")).navigationBarTitleDisplayMode(.large)
        .sheet(isPresented: $scanning) {
            QRScanner(onCode: { code in
                scanning = false
                do { let address = try PairingAddress(code, qr: true); url = address.baseURL.absoluteString; pin = address.pin ?? ""; Task { await connect() } }
                catch let e as ClientError { model.error = e } catch { model.error = ClientError("mobile.connect.error_qr") }
            }, onError: { e in scanning = false; model.error = e })
                .presentationCornerRadius(VSTokens.radiusLg).ignoresSafeArea().overlay(alignment: .bottom) { Button(model.t("common.cancel")) { scanning = false } }
        }
    }
    private func connect() async {
        guard !model.busy, BackgroundUpload.shared.pending == nil else { model.error = ClientError("mobile.errors.upload_active"); return }
        model.pauseMonitor()
        defer { model.monitor() }
        let key = apiKey; apiKey = ""
        await model.perform(connectionChange: true) {
            _ = try await model.connection.connect(address: PairingAddress(url, pin: pin), key: key, label: label)
            pin = ""
            await model.perform { try await model.refresh() }
            model.acceptUpload()
            model.monitor()
        }
    }
}
