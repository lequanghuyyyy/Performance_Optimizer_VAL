# VoiceStudio mobile — M1 companion app contract

Both native apps (iOS: Swift/SwiftUI, `mobile/ios/`; Android: Kotlin/Compose,
`mobile/android/`) implement **exactly this spec**. A feature ships on both or
on neither (CLAUDE.md → "Default features must work on every platform").

M1 is a **companion**: the phone drives the user's own VoiceStudio backend
(desktop app, Docker or server) over LAN, Tailscale or a remote URL. No
on-device inference in M1; no cloud relay; no accounts.

## Shared inputs (generated — never hand-edit)

| File | Source | Regenerate |
|---|---|---|
| `contract/openapi.json` | backend FastAPI app | `uv run python scripts/export_openapi.py` |
| `contract/tokens.json`, `Tokens.generated.swift`, `Tokens.generated.kt` | Electron theme | `bun scripts/export-mobile-tokens.ts` |
| Locale catalogs | `electron/src/renderer/src/i18n/locales/*.json` | copied into each app at build time |

## Theme

- Colors come only from `VSTokens.palettes` (roles = `VSTokens.roles`, same
  names as desktop `THEME_COLOR_ROLES`). No literal colors in UI code.
- Palettes: `default`, `signal`, `canopy`, `current`, `hearth`, `orchid`,
  `heritage` — same ids and labels as desktop `AVAILABLE_PALETTES`.
- Preferences mirror desktop `theme-preferences.ts`: `mode` (`light` | `dark` |
  `system`, default `dark`), a palette per appearance (default `default`). A
  palette lacking the requested appearance falls back to `default`.
- Role usage (match desktop): screen = `canvas`; cards/rows = `surface`;
  sheets/menus = `surfaceOverlay`; primary buttons = `messageAction` /
  `messageActionForeground`, pressed = `messageActionHover`; text = `text`,
  secondary = `textMuted`/`mutedForeground`; separators = `border`; fields =
  `input`; focus ring = `focus`; errors = `error`/`errorForeground`/`errorSurface`;
  warnings likewise; tab/nav bars = `chrome` + `toolbarForeground`.
- Type: Inter (bundled, SIL OFL — include its license). Desktop sizes
  (`VSTokens.text*`) are the base; scale with Dynamic Type / Android font
  scale. Body text on phones uses `editor` (16/28); controls use `ui` (14/20)
  scaled up to the platform minimum touch-readable size.
- Radii: `radiusLg` for cards, `radiusControl` for buttons/fields. Spacing:
  `spacingGap` between elements, `spacingInset` for screen padding (cap at 16
  on compact widths).
- Respect reduced motion and reduced transparency.

## Strings

- All user-facing text through a small `L10n.t(key, params)` that reads the
  bundled Electron catalogs: nested JSON, dotted keys, i18next `{{name}}`
  interpolation, `_one`/`_other` plurals, fallback chain
  `<locale> → <language> → en → key`. Locale chosen from the system, overridable
  in Settings using the desktop language list.
- Reuse existing keys where the meaning is identical (`common.*`, `tts.*`,
  `clone.*`, …). New mobile keys live under the `mobile.` namespace and are
  listed in `contract/strings.mobile.en.json` (single shared list — both apps
  use the same keys). If you need a key that is not there, add it to
  `contract/strings.<platform>-additions.en.json` instead; the maintainer merges
  those into the shared list and the Electron catalogs with translations.
- No hardcoded user-facing text in Swift/Kotlin. No CJK anywhere outside the
  catalogs.

## Screens (M1)

Bottom navigation: **Speak**, **Voices**, **Dub**, **Dictate**, **Settings**.
Before pairing, only the Connect flow and Settings → About/Privacy are reachable.

1. **First run**
   - Analytics consent: same copy keys as desktop, two equal-weight Yes/No
     buttons, never default-on, dismissing = No. M1 does not transmit
     analytics; it only records the choice (desktop parity for later).
   - Connect screen.
2. **Connect / pairing**
   - Scan QR (camera) of the desktop "Network" share panel: URL
     `http(s)://<host>:<port>/?pin=<6 digits>`. Also manual entry: URL + optional
     PIN + optional API key.
   - Validate with `GET /health` (must be a VoiceStudio response).
   - PIN: send `X-OmniVoice-Pin` on every request.
   - API key: exchange once via `POST /api/auth/session` with
     `Authorization: Bearer <key>` and body `{"transport":"bearer"}`; store only
     the returned session (Keychain / Android Keystore-backed storage) and
     send it as `Authorization: Bearer <session>`; never persist the key.
     On 401 ask again. "Disconnect" calls `DELETE /api/auth/session`.
   - Saved hosts list (URL, label, last seen); one active host.
   - See `docs/api-auth.md`, `docs/sharing.md`.
3. **Speak** (TTS)
   - Text editor, voice picker (`GET /profiles`), engine/language pickers
     from `GET /engines` and backend language data, Generate → `POST /generate`.
   - Play result in-app (streamed from the host), waveform/progress scrubber,
     share/save via system share sheet. Recent generations: `GET /history`
     (star/delete).
4. **Voices**
   - List/search profiles, play preview, create a clone: record (mic) or pick
     audio file → upload as the clone endpoint expects (see OpenAPI
     `/profiles` POST), with the consent statement before cloning.
   - Delete/rename.
5. **Dub**
   - Pick video/audio from the device or paste a URL (`/dub/ingest-url`),
     upload (`/dub/upload`), choose target language(s) and voice, start,
     monitor progress (job events / polling per OpenAPI), abort, download/share
     results (`/dub/download*`). History: `/dub/history`.
   - Long jobs run on the host; the app reconnects and resumes monitoring.
     Upload continues in the background (iOS background URLSession / Android
     WorkManager). Local notification on finish if the user allowed
     notifications (no APNs/FCM).
6. **Dictate**
   - Hold/tap to record, stream to the host dictation WebSocket (ticket from
     `POST /api/auth/ws-ticket` when a session is used), show live transcript,
     copy/share. Readiness from `GET /dictation/readiness`.
7. **Settings**
   - Connection (hosts, disconnect), Appearance (mode + palette per appearance,
     live preview), Language, Notifications, Privacy (analytics choice, what
     leaves the phone: nothing but requests to the paired host), About
     (version from the root `package.json`, licenses incl. Inter, model
     licenses link to the host).

## Platform rules

- Networking: plain HTTP allowed **only** to local/private addresses
  (iOS `NSAllowsLocalNetworking` + `NSLocalNetworkUsageDescription`; Android
  `network_security_config` for loopback/RFC1918/CGNAT `100.64/10`/link-local
  and `.local`/`.ts.net`). Everything else HTTPS.
- Permissions requested only at first use, with a reason string from L10n:
  camera (QR), microphone (record/dictate), local network, notifications.
- Errors: every network/backend error maps to an actionable message (what
  happened + what to do), mirroring desktop error copy where it exists.
- Accessibility: every control labelled, Dynamic Type / font scale up to the
  largest size without clipping, minimum 44pt / 48dp touch targets, contrast
  from tokens.
- Minimum OS: iOS 17, Android API 29 (target the latest stable SDK).
- Version: read from the root `package.json` at build time (never hard-coded).

## Tests each app must have

- Token parity: every palette/role in `contract/tokens.json` resolves in the
  app's theme layer.
- L10n: interpolation, plurals, fallback chain, every key used in code exists
  in `en` (shared list + additions).
- Pairing URL parsing (QR forms, IPv4/IPv6, missing PIN), cleartext policy
  (private vs public hosts).
- API client against a mock server: health check, PIN header, session
  exchange/revoke, 401 handling, generate, dub upload + progress.
- UI smoke: launch → consent → connect (mocked) → Speak generates → play.
