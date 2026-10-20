# VoiceStudio for iOS — M1 companion

Native SwiftUI companion for a paired VoiceStudio desktop/server. iOS 17+, iPhone and iPad. No inference, accounts, telemetry transmission, push services, third-party packages, or app requests to services other than the paired host.

## Prerequisites

- Swift 6 / Swift 6.2.3 Command Line Tools for the macOS package checks; Python 3 for resource generation and the real loopback mock-server test.
- XcodeGen 2.46 (`brew install xcodegen`).
- Full Xcode with the iOS SDK and an iOS 17+ simulator/device to compile/run the app and UI tests. Signing requires your own development team for a device.
- A running VoiceStudio host with Network sharing, Tailscale, or an HTTPS URL enabled. The phone never enables host sharing or downloads models. Install required models and accept their licenses on the host.

Run these from the repository root:

```sh
mobile/ios/scripts/check-swift.sh build
mobile/ios/scripts/check-swift.sh test
mobile/ios/scripts/check-platform.sh
xcodegen generate --spec mobile/ios/VoiceStudio/project.yml
python3 mobile/ios/scripts/prepare_resources.py \
  --output mobile/ios/.generated/bundle \
  --info-output mobile/ios/.generated/Info.plist
swiftc -frontend -parse mobile/ios/VoiceStudio/App/*.swift \
  mobile/ios/VoiceStudio/Features/*.swift mobile/ios/VoiceStudio/Platform/*.swift \
  mobile/ios/VoiceStudioUITests/*.swift
```

`check-swift.sh` runs `swift build` / `swift test` with package and scratch paths under `mobile/ios/`, redirects module caches there, and disables SwiftPM's nested sandbox for restricted development environments. For CLT installations, it supplies the installed `Testing.framework` search/link/rpath flags, disables the missing optional `_Testing_Foundation` cross-import overlay, and selects Swift Testing without XCTest; no test dependency download is necessary. On ordinary Xcode installations, `cd mobile/ios/VoiceStudioKit && swift build && swift test` also works.

With full Xcode:

```sh
open mobile/ios/VoiceStudio/VoiceStudio.xcodeproj
xcodebuild -project mobile/ios/VoiceStudio/VoiceStudio.xcodeproj \
  -scheme VoiceStudio -sdk iphonesimulator -destination 'generic/platform=iOS Simulator' \
  CODE_SIGNING_ALLOWED=NO build
xcodebuild -project mobile/ios/VoiceStudio/VoiceStudio.xcodeproj \
  -scheme VoiceStudio -destination 'platform=iOS Simulator,name=iPhone 16' test
```

Choose an installed simulator name (`xcrun simctl list devices available`). Run the VoiceStudio scheme, decline or accept the local analytics preference, then scan the desktop Network QR or enter the host URL/PIN/API key. API keys are exchanged once for an administrator session and never saved. A PIN authorizes HTTP consumption, not remote dictation; dictation needs a session or a host-configured trusted network.

## Layout

- `VoiceStudioKit/`: Swift 6 package, iOS 17 and macOS 14. Typed HTTP client/models, redirect and cleartext policy, pairing parser, Keychain vault, observable session store, localization, theme resolution, dictation WebSocket, background upload/delegate/receipts, and persisted dub workflow.
- `VoiceStudioKit/Tests/`: Swift Testing contract tests, mock-transport integration tests, and a real Python loopback HTTP server exercising URLSession.
- `VoiceStudio/App/`: app lifecycle, Observation model, token-based native theme, consent and navigation.
- `VoiceStudio/Features/`: Connect, Speak, Voices/clone, Dub, Dictate, Settings/Privacy/About/licenses.
- `VoiceStudio/Platform/`: AVFoundation recording, PCM streaming, AVPlayer authenticated range loading and waveform extraction, AVCaptureSession QR, deterministic UI-smoke transport.
- `VoiceStudioUITests/`: launch → consent → mocked pairing → generate a valid WAV → play; consent choices/swipe dismissal, editor labeling, search sizing/empty states, shared Dub copy, one Dictate control, palette choices, compact history actions, and immediate tab content during delayed host inventory. ScreenshotTour captures the screens and picker sheets for visual review. `--ui-smoke` selects a transport that opens no sockets.
- `scripts/prepare_resources.py`: build-phase resource and version generation.

Consent is a dismissible sheet with the desktop consent title/body and the same local-only explanation as Android. Its title is a heading; Yes and No have the same style and width. Swiping, Escape, and the accessibility escape action record No. Speak shows the shared “Type what you want to hear…” placeholder only while its editor is empty, and uses that copy as the editor's accessibility label. It includes backend engine/language and profile choices, playback, waveform/seeking, system share/save, history star/delete. Voices includes search, previews, consent-gated microphone/file cloning, rename/delete. Dub supports device media or host URL ingest, multiple target tracks, an explicit engine-preset or saved-profile voice choice, persisted job/event polling, abort/history/download/share. The picked file's original name is used in the source row, staged multipart filename, pending upload and job title. Remove deselects a picked source and its accessibility label names the file; it does not delete it. The target-language row shows Choose until a language is selected. Dub history uses the shared mobile.dub.history_meta template with language and a minute:second duration, without a date, in compact rows with icon actions for Save to Files and system sharing. Missing language uses the same completed-stage fallback as Android; missing duration is 0:00. Picker labels keep their intrinsic width; current values fill the remaining width, align right and truncate to one line. At accessibility text sizes the value moves below its label. Every searchable list uses one token-styled search container around an icon and a plain text field, and shows a localized no-match message. Pages render their forms immediately and show inline loading feedback while host data arrives.

Translation uses the host-selected ready engine explicitly; online engines and remote or undiscoverable LLM destinations require equal-weight Allow/Decline consent scoped to that host, job and provider. Verified host-loopback LLM endpoints do not require cloud consent. Dictate has one primary control: tap to start/stop, or hold and release to finish; Hold to talk is its accessibility hint. It sends mono PCM16 at 16 kHz with single-use WebSocket tickets and EOF finalization. Utterance finals keep recording; the session summary replaces the live transcript. Stream termination is scoped to its own socket. Settings includes saved hosts, mode and independent light/dark palette rows with their desktop names and compact live color swatches, system/desktop language choices, local notification permission, privacy, app/font licenses and a link to the paired host for model licenses.

Permissions are requested at first use. InfoPlist localization uses the mobile permission keys. No Bonjour, APNs, FCM, analytics SDK or crash reporter is linked. Disconnect revokes an existing bearer session and clears local credentials even if the host is unavailable. PIN-only connections disconnect locally. Reconnecting preserves a saved server name unless a replacement name is entered; rejected pairing attempts leave the active server connected. Unreadable saved preferences use defaults, and unreadable credentials allow fresh pairing. Session/PIN secrets use Keychain with `AfterFirstUnlockThisDeviceOnly`; host metadata and job checkpoints stay in local preferences. Requests reject redirects and public cleartext; TLS uses system certificate validation. Reduced transparency uses opaque token surfaces throughout, and there are no custom motion effects. Inter uses Dynamic Type scaling; controls have at least 44-point targets and content scrolls at large sizes.

## Shared inputs and version

Do not edit `VoiceStudioKit/Sources/VoiceStudioKit/Theme/Tokens.generated.swift`.

The maintainer regenerates shared inputs from the repository root:

```sh
bun scripts/export-mobile-tokens.ts
uv run python scripts/export_openapi.py
```

Those commands update shared files and both platforms; they are intentionally not part of an iOS build. The package theme maps generated roles and falls back to the default palette for missing appearances. Dark/default is the initial preference.

Each app build copies the actual `electron/src/renderer/src/i18n/locales/*.json`, `mobile/contract/strings*.en.json`, and `electron/src/shared/languages.json` into `Catalogs/`; no catalog copies are committed. It also extracts the desktop `LANG_CODES` mapping from `electron/src/shared/utils/languages.js` into a derived `language-codes.json`. The same script creates localized permission descriptions and reads root `package.json` into the derived Info.plist `CFBundleShortVersionString`. There is no mobile version mirror. Existing translations use locale → language → English → key fallback. Chinese script/region tags select the desktop zh-CN or zh-TW catalog first; an explicit Hans/Hant script takes precedence over region. Catalogs are parsed once when the app model is created and reused for progress updates and notifications. New mobile strings fall back to the contract English until the maintainer merges translations into desktop catalogs. iOS additions live only in `mobile/contract/strings.ios-additions.en.json`.

InterVariable.ttf is from the official [Inter 4.1 release](https://github.com/rsms/inter/releases/tag/v4.1). Its SIL OFL is bundled as `Resources/Licenses/OFL.txt`; VoiceStudio's AGPL license is bundled alongside it and both appear in About.

## Verification and remaining device checks

Verified here: macOS Swift package build and 78 Swift Testing tests (several parameterized), strict-concurrency package compilation including the production upload coordinator, macOS type-checking of the media-loader and smoke-transport wrappers, all palette/role values against the contract JSON, interpolation/plural/fallback/key coverage, Chinese script/region mapping, QR/manual IPv4/IPv6 parsing, private/public cleartext boundaries, health fingerprint, PIN/session exchange/revoke/401 handling, corrupt-store recovery, reconnect labels, candidate-host failure isolation, upload refusal/cancellation classification, receipt consumption through relaunch, stale dictation termination, multipart TTS and dub upload, job progress, persisted multi-language dub pipeline, host-only redirect rejection, XcodeGen generation, resource/version generation and app-source syntax parsing. A real loopback server exercises the upload start/staging/delegate path with a file-backed foreground session: original filename reaches the host, successful completion is consumed once, and 401/500 responses produce actionable failure receipts. Package tests cover remote/loopback/undiscoverable LLM routing, per-job allow/decline and changed-endpoint reapproval, filename persistence, partial-file cleanup, localized history metadata, missing metadata and duration formatting.

This machine has Command Line Tools only. The SwiftUI app and XCTest UI smoke have **not been compiled or run with an iOS SDK**. Consent swipe/Escape, placeholder rendering/hiding, search appearance, Dictate tap/hold cancellation and palette swatches still require Xcode UI verification. Full Xcode build, simulator smoke, real-device QR/microphone/local-network permission prompts, VoiceOver/largest Dynamic Type, Inter rendering, authenticated media playback, background upload/relaunch, task scheduling, notification delivery, and live-host end-to-end TTS/clone/dub/dictation remain unverified. Simulator/device testing is a release gate, not implied by syntax parsing.

## Contract interpretations and platform limits

- Health has no named OpenAPI response schema: require `status=ok`, `device`, `version`, and the real backend's `x-omnivoice-backend` version marker (`backend/main.py`). This rejects unrelated `/health` services.
- The session exchange returns `token`; the WebSocket ticket response returns **`ticket`**, which is sent under the **`ws_ticket`** query name. These loose-schema fields come from `backend/api/routers/auth.py`.
- Non-streaming `/generate` returns WAV bytes and `X-Audio-Id`, not JSON. Recent WAV playback uses the actual `/audio/<id>.wav` StaticFiles mount in `backend/main.py`; static mounts are absent from FastAPI's OpenAPI. Using the documented `.ogg` conversion endpoint would require an additional decoder on iOS 17. WAV range requests keep credentials in headers.
- There is no generic `/languages` endpoint. Both pickers use the bundled desktop language names in desktop order. Speak starts with Auto (omitted from the request) and accepts a profile language as either a name or ISO code. Dub excludes Auto; target codes come from desktop `LANG_CODES`, with ISO names resolved by Foundation for the remaining languages. Names without a resolvable code remain visible but disabled with an explanation; the app does not invent codes. The host validates model and translation compatibility.
- There is no single “start dub” endpoint. The app follows upload/ingest → persisted prep-job events → transcribe → translate → generate per target → persisted render-job events. The polling key is the returned **task ID**, distinct from the dub media job ID. Acknowledged transitions and event sequence cursors are saved. The backend has no client idempotency key for render; termination between host acceptance and checkpoint persistence can require host-side inspection before retrying.
- iOS background URLSession continues file upload and persists completion receipts through relaunch. If the OS refuses the background session, the staged file and pending job survive while the app retries using a foreground URLSession. That fallback needs app runtime and does not promise upload continuation after suspension or termination. Cancelling an upload clears its pending state without a failure receipt. Receipts are consumed once for their paired host, so a failure is not redisplayed after foregrounding or relaunch. Host generation runs independently. BGProcessing submission refusal leaves the host job intact and foreground monitoring available. BGProcessing is opportunistic: iOS does not promise continuous polling or an immediate completion notification while suspended/force-quit. Monitoring and local completion notifications resume at the next granted runtime/foreground launch. There is no push relay.
- The translation inventory has a loose OpenAPI response. Its `active`, `engines`, `category`, `installed`, `ready`, `display_name`, and `configured_via` fields follow `backend/api/routers/engines.py` and `backend/services/translation_engines.py`. Missing readiness never implies permission to translate. For category `llm`, the app reads `/api/settings/llm-skills` and `/api/settings/llm-providers` to resolve the `dub_translation` override and effective base URL; an inventory identifying the legacy env fallback uses `/api/settings/llm-endpoint`. These read-only endpoints are in OpenAPI; their loose response fields follow `settings.py`, `llm_skills.py` and `llm_providers.py`. A provider's `local` label alone is insufficient because its endpoint can be overridden. Only HTTP(S) loopback endpoints on an OpenAI-compatible transport are treated as local. Private LAN/Tailnet destinations still require consent. If desktop admin restrictions, missing fields or discovery errors hide the destination, the app asks for per-job consent using the available provider/engine name. Cancellation and 401 retain their normal handling. LLM approval also covers a hash of the resolved provider and endpoint, so an observed route change asks again; raw endpoint credentials are not persisted. Existing checkpoints without matching approval require confirmation. Host settings can change after discovery; these endpoints do not provide an atomic route-and-translate operation.
- `/dub/history` is a loose schema returning database rows from `dub_core.py`. The model decodes `filename`, `language`/`language_code`, `duration` (seconds) and `created_at` (Unix seconds), as written by `dub_pipeline.py`. Presentation matches Android: filename (or ID), the shared history template using `language` and duration, and no date. Older rows lacking metadata remain readable.
- The synchronous `/dub/transcribe/{job_id}` path does not prepare source-speaker references. An empty profile selects the engine preset; a saved profile sends its ID. Source-voice cloning is not offered by this flow.
- Model-license navigation opens the paired host interface; M1 does not accept licenses or install models from the phone.
