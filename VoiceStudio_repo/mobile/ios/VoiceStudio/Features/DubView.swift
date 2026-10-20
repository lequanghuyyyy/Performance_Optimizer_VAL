import SwiftUI
import VoiceStudioKit
import UniformTypeIdentifiers

struct DubView: View {
    @Environment(AppModel.self) private var model
    @Environment(\.vsTheme) private var theme
    @State private var upload = BackgroundUpload.shared
    @State private var importing = false
    @State private var selectedFile: SelectedMedia?
    @State private var sourceURL = ""
    @State private var selected = Set<String>()
    @State private var profile = DubVoiceChoice.unselected
    @State private var selectingLanguages = false
    @State private var languageQuery = ""
    @State private var shared: SharedFile?
    @State private var saved: SharedFile?
    var languages: [DubLanguage] { model.languages.dubOptions }
    private var filteredLanguages: [MobileLanguage] { model.languages.search(languageQuery, includeAuto: false) }
    var body: some View {
        Page {
            Text(model.t("mobile.dub.runs_on_host"))
            Card {
                Button(model.t("mobile.dub.pick_media"), systemImage: "doc.badge.plus") { importing = true }
                if let selectedFile { SelectedFileRow(name: selectedFile.name) { self.selectedFile = nil } }
                TextField(model.t("mobile.dub.paste_url"), text: $sourceURL).keyboardType(.URL).textInputAutocapitalization(.never).autocorrectionDisabled().accessibilityIdentifier("dub.source_url")
                Button { selectingLanguages = true } label: {
                    let names = languages.filter { selected.contains($0.code) }.map(\.name)
                    SelectionRow(label: model.t("mobile.dub.target_languages"), value: names.isEmpty ? model.t("mobile.android.choose") : ListFormatter.localizedString(byJoining: names))
                }.buttonStyle(VSButtonStyle(kind: .tertiary)).accessibilityIdentifier("dub.languages")
                ScrollView(.horizontal) {
                    HStack(spacing: VSTokens.spacingGap) {
                        ForEach(languages.filter { selected.contains($0.code) }) { language in
                            Button { selected.remove(language.code) } label: {
                                HStack(spacing: VSTokens.spacingGap) { Text(language.name); Image(systemName: "xmark") }
                            }.accessibilityLabel(model.t("mobile.dub.remove_language", ["language": language.name]))
                        }
                    }
                }
                VoicePicker(selection: $profile, dubbing: true)
                Button(model.t("mobile.dub.start")) { Task { await start() } }
                    .buttonStyle(VSButtonStyle(kind: .primary)).disabled(profile == DubVoiceChoice.unselected || model.busy || upload.pending != nil || selected.isEmpty || (selectedFile == nil && sourceURL.isEmpty) || model.dub.monitoring)
            }
            if let pending = upload.pending {
                Card {
                    if let name = pending.filename { Text(name).lineLimit(1).truncationMode(.middle) }
                    Text(model.t("mobile.dub.uploading", ["percent": String(Int(upload.fraction * 100))]))
                    ProgressView(value: upload.fraction).accessibilityLabel(model.t("mobile.ios.progress"))
                    Text(model.t("mobile.ios.upload_pending"))
                    Button(model.t("mobile.ios.upload_cancel"), role: .destructive) { Task { await upload.cancel() } }
                }
            }
            if let engine = model.dub.pendingTranslation {
                Card {
                    Text(model.t("mobile.dub.translation_online_body", ["service": engine.display_name]))
                    // Both choices use the same native weight, size and style.
                    ViewThatFits(in: .horizontal) {
                        HStack(spacing: VSTokens.spacingGap) { translationChoice(true); translationChoice(false) }
                        VStack(spacing: VSTokens.spacingGap) { translationChoice(true); translationChoice(false) }
                    }
                }
            }
            if let cp = model.dub.checkpoint {
                Card {
                    Text(cp.title).lineLimit(1).truncationMode(.middle)
                    Text(model.t("mobile.ios.dub_stages." + cp.stage.rawValue))
                    ProgressView(value: model.dub.progress).accessibilityLabel(model.t("mobile.ios.progress"))
                    if cp.host != model.connection.active?.id { Text(model.t("mobile.ios.dub_resume_help")) }
                    else if cp.stage == .done { Button(model.t("mobile.dub.download")) { Task { await download(cp.jobID, name: cp.title) } } }
                    else if cp.stage != .cancelled {
                        if !model.dub.monitoring { Button(model.t("mobile.ios.upload_resume")) { model.monitor() } }
                        Button(model.t("mobile.dub.abort"), role: .destructive) { Task { await model.perform { model.pauseMonitor(); try await model.dub.abort(api: model.api()) } } }
                    }
                }
            }
            Text(model.t("mobile.dub.history"))
            if model.dubHistory.isEmpty && !model.loading { Text(model.t("mobile.dub.empty_history")) }
            if !model.dubHistory.isEmpty {
                Card {
                    ForEach(model.dubHistory) { item in
                        ViewThatFits(in: .horizontal) {
                            HStack(spacing: VSTokens.spacingGap) {
                                historyTitle(item)
                                historyActions(item)
                            }
                            VStack(alignment: .leading, spacing: VSTokens.spacingGap) {
                                historyTitle(item)
                                historyActions(item)
                            }
                        }
                        if item.id != model.dubHistory.last?.id { Divider().overlay(theme.color("border")) }
                    }
                }
            }
        }.navigationTitle(model.t("mobile.tabs.dub")).navigationBarTitleDisplayMode(.large)
        .fileImporter(isPresented: $importing, allowedContentTypes: [.movie, .audio]) { result in
            switch result { case .success(let url): selectedFile = SelectedMedia(url: url); sourceURL = ""; case .failure: model.error = ClientError("mobile.errors.file") }
        }
        .sheet(item: $shared) { file in ShareSheet(items: [file.url]).presentationCornerRadius(VSTokens.radiusLg) }
        .sheet(item: $saved) { file in SaveFileSheet(url: file.url).presentationCornerRadius(VSTokens.radiusLg) }
        .sheet(isPresented: $selectingLanguages) {
            NavigationStack {
                List {
                    SearchField(prompt: model.t("common.search"), text: $languageQuery).listRowBackground(theme.color("surfaceOverlay"))
                    if filteredLanguages.isEmpty { SearchEmptyState(query: languageQuery).listRowBackground(theme.color("surface")) }
                    ForEach(filteredLanguages) { language in
                    Button {
                        if let code = language.code {
                            if selected.contains(code) { selected.remove(code) } else { selected.insert(code) }
                        }
                    } label: {
                        HStack(spacing: VSTokens.spacingGap) {
                            VStack(alignment: .leading, spacing: VSTokens.spacingGap) {
                                Text(language.name).lineLimit(2).truncationMode(.tail)
                                if language.code == nil { Text(model.t("mobile.dub.language_unavailable")).foregroundStyle(theme.color("textMuted")) }
                            }
                            Spacer(minLength: 0)
                            Image(systemName: language.code.map { selected.contains($0) } == true ? "checkmark.circle.fill" : "circle")
                                .foregroundStyle(theme.color("messageAction")).accessibilityHidden(true)
                        }.frame(minHeight: 44).contentShape(Rectangle())
                    }.buttonStyle(VSButtonStyle(kind: .tertiary)).disabled(language.code == nil)
                        .accessibilityLabel(language.name)
                        .accessibilityValue(model.t(language.code.map { selected.contains($0) } == true ? "mobile.ios.selected" : "mobile.ios.not_selected"))
                        .listRowBackground(theme.color("surface"))
                    }
                }
                .scrollContentBackground(.hidden).background(theme.color("surfaceOverlay"))
                .keyboardAccessible()
                .navigationTitle(model.t("mobile.dub.target_languages"))
                .toolbar { Button(model.t("common.confirm")) { selectingLanguages = false }.buttonStyle(VSButtonStyle(kind: .tertiary)) }
            }.presentationBackground(theme.color("surfaceOverlay")).presentationCornerRadius(VSTokens.radiusLg)
        }
        .onChange(of: sourceURL) { _, value in if !value.isEmpty { selectedFile = nil } }
        .refreshable { await model.perform { model.dubHistory = try await model.api().dubHistory() }; model.monitor() }
    }
    private func translationChoice(_ allowed: Bool) -> some View {
        Button {
            model.pauseMonitor()
            model.dub.confirmTranslation(allowed)
            if allowed { model.monitor() }
        } label: {
            Text(model.t(allowed ? "mobile.dub.translation_allow" : "mobile.dub.translation_decline"))
                .frame(maxWidth: .infinity)
        }.buttonStyle(VSButtonStyle(kind: .secondary))
            .accessibilityIdentifier(allowed ? "dub.translation.allow" : "dub.translation.decline")
    }
    private func start() async {
        await model.perform {
            try DubVoiceChoice.validate(profile)
            let api = try model.api()
            let targets = languages.filter { selected.contains($0.code) }
            if let file = selectedFile {
                let access = file.url.startAccessingSecurityScopedResource(); defer { if access { file.url.stopAccessingSecurityScopedResource() } }
                let type = UTType(filenameExtension: file.url.pathExtension)
                try await upload.start(source: file.url, api: api, languages: targets, profile: profile, audioOnly: type?.conforms(to: .audio) == true, filename: file.uploadName)
                selectedFile = nil
            } else {
                let result = try await api.ingest(url: sourceURL, jobID: UUID().uuidString)
                try model.dub.begin(result, api: api, languages: targets, profile: profile)
                sourceURL = ""; model.monitor()
            }
        }
    }
    private func historyTitle(_ item: DubHistoryItem) -> some View {
        VStack(alignment: .leading, spacing: VSTokens.spacingGap) {
            Text(item.title).lineLimit(1).truncationMode(.middle)
            Text(item.metadata(l10n: model.l10n)).accessibilityIdentifier("dub.metadata." + item.id)
                .font(.custom("InterVariable", size: VSTokens.textLabel.size, relativeTo: .caption))
                .foregroundStyle(theme.color("textMuted")).lineLimit(1).truncationMode(.tail)
        }.frame(maxWidth: .infinity, alignment: .leading)
    }
    private func historyActions(_ item: DubHistoryItem) -> some View {
        HStack(spacing: VSTokens.spacingGap) {
            Button(model.t("mobile.dub.download"), systemImage: "arrow.down.to.line") {
                Task { await download(item.job_id ?? item.id, name: item.title) }
            }.accessibilityIdentifier("dub.download." + item.id)
                .accessibilityLabel(model.t("mobile.dub.download") + ": " + item.title)
            Button(model.t("mobile.speak.share"), systemImage: "square.and.arrow.up") {
                Task { await download(item.job_id ?? item.id, name: item.title, share: true) }
            }.accessibilityIdentifier("dub.share." + item.id)
                .accessibilityLabel(model.t("mobile.speak.share") + ": " + item.title)
        }.labelStyle(.iconOnly).buttonStyle(VSButtonStyle(kind: .tertiary)).disabled(model.busy)
    }
    private func download(_ id: String, name: String, share: Bool = false) async {
        await model.perform {
            let api = try model.api()
            let reply = try await api.download(api.request("/dub/download/\(try APIClient.component(id))"))
            let disposition = reply.headers["content-disposition"] ?? ""
            // Derive only an extension, never a server-provided local path.
            let ext = ["mp4", "mkv", "wav", "mp3", "m4a"].first { disposition.contains("." + $0) } ?? "m4a"
            let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            let candidate = URL(fileURLWithPath: name).deletingPathExtension().lastPathComponent.trimmingCharacters(in: .whitespacesAndNewlines)
            let basename = ["", ".", "..", "/"].contains(candidate) ? id : candidate
            let file = directory.appendingPathComponent(basename).appendingPathExtension(ext)
            try FileManager.default.moveItem(at: reply.file, to: file)
            if share { shared = SharedFile(url: file) } else { saved = SharedFile(url: file) }
        }
    }
}
struct SaveFileSheet: UIViewControllerRepresentable {
    let url: URL
    func makeUIViewController(context: Context) -> UIDocumentPickerViewController { UIDocumentPickerViewController(forExporting: [url], asCopy: true) }
    func updateUIViewController(_ uiViewController: UIDocumentPickerViewController, context: Context) {}
}
struct SharedFile: Identifiable { let id = UUID(); let url: URL }
struct ShareSheet: UIViewControllerRepresentable {
    let items: [URL]
    func makeUIViewController(context: Context) -> UIActivityViewController { UIActivityViewController(activityItems: items, applicationActivities: nil) }
    func updateUIViewController(_ uiViewController: UIActivityViewController, context: Context) {}
}
