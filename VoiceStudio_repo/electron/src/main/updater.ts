import { app, BrowserWindow, ipcMain } from 'electron';
import updaterPackage, { type ProgressInfo, type UpdateInfo } from 'electron-updater';
import { rmSync } from 'node:fs';
import { join } from 'node:path';
import { isTrustedRenderer } from './trusted-renderer';
import type { UpdateReleaseInfo, UpdateState } from '../preload/index.d';
import { sendToLiveWindow } from './window-safety';
import { applicationUpdatesSupported, CURRENT_DISTRIBUTION } from './distribution';
import type { DistributionTarget } from '../../../config/distributions.mjs';

// electron-updater is CommonJS. Electron executes our main bundle as ESM, so
// named runtime imports fail before the app can create a window.
const { autoUpdater } = updaterPackage;

// Only tagged releases are published as an update feed. A Preview channel used
// to exist, but no workflow ever published its feed, so it silently served
// Stable; it was removed rather than left as a choice that does nothing.
const STABLE_FEED = 'https://github.com/debpalash/VoiceStudio/releases/latest/download';
const RELEASES_API = 'https://api.github.com/repos/debpalash/VoiceStudio/releases?per_page=30';

function feedChannelName(
  platform: NodeJS.Platform = process.platform,
  arch: string = process.arch,
  distribution: DistributionTarget = CURRENT_DISTRIBUTION,
): string {
  const prefix =
    distribution === 'microsoft-store' ? 'electron-microsoft-store-stable' : 'electron-stable';
  return `${prefix}-${platform}-${arch}`;
}

export function feedManifestName(
  platform: NodeJS.Platform = process.platform,
  arch: string = process.arch,
  distribution: DistributionTarget = CURRENT_DISTRIBUTION,
): string {
  const suffix = platform === 'darwin' ? '-mac' : platform === 'linux' ? '-linux' : '';
  return `${feedChannelName(platform, arch, distribution)}${suffix}.yml`;
}

export async function resolveUpdateFeedUrl(
  distribution: DistributionTarget,
  platform: NodeJS.Platform = process.platform,
  arch: string = process.arch,
  fetcher: typeof fetch = fetch,
): Promise<string> {
  if (distribution !== 'microsoft-store') return STABLE_FEED;
  const manifest = feedManifestName(platform, arch, distribution);
  const response = await fetcher(RELEASES_API, {
    headers: {
      Accept: 'application/vnd.github+json',
      'User-Agent': 'VoiceStudio',
    },
    signal: AbortSignal.timeout(10_000),
  });
  if (!response.ok) throw new Error(`Store update feed lookup failed (${response.status})`);
  const releases: unknown = await response.json();
  if (!Array.isArray(releases)) throw new Error('Store update feed lookup returned invalid data');
  const matchingReleases = releases.flatMap(
    (
      value,
    ): Array<{
      release: Record<string, unknown>;
      version: [number, number, number];
    }> => {
      if (!value || typeof value !== 'object') return [];
      const item = value as Record<string, unknown>;
      const version =
        typeof item.tag_name === 'string' ? item.tag_name.match(/^v(\d+)\.(\d+)\.(\d+)$/) : null;
      const publishesManifest =
        Array.isArray(item.assets) &&
        item.assets.some(
          (asset) =>
            asset &&
            typeof asset === 'object' &&
            (asset as Record<string, unknown>).name === manifest,
        );
      return version && publishesManifest
        ? [{ release: item, version: [Number(version[1]), Number(version[2]), Number(version[3])] }]
        : [];
    },
  );
  matchingReleases.sort((left, right) => {
    for (let index = 0; index < left.version.length; index += 1) {
      const difference = right.version[index] - left.version[index];
      if (difference) return difference;
    }
    return 0;
  });
  const release = matchingReleases[0]?.release;
  if (!release) throw new Error(`No Microsoft Store update feed publishes ${manifest}`);
  return `https://github.com/debpalash/VoiceStudio/releases/download/${encodeURIComponent(
    release.tag_name as string,
  )}`;
}

export const UPDATE_CHANNELS = {
  getState: 'updates:getState',
  check: 'updates:check',
  download: 'updates:download',
  dismiss: 'updates:dismiss',
  install: 'updates:install',
  listReleases: 'updates:listReleases',
  state: 'updates:state',
} as const;

export async function listDesktopReleases(
  fetcher: typeof fetch = fetch,
): Promise<UpdateReleaseInfo[]> {
  const response = await fetcher(RELEASES_API, {
    headers: {
      Accept: 'application/vnd.github+json',
      'User-Agent': 'VoiceStudio',
    },
    signal: AbortSignal.timeout(10_000),
  });
  if (!response.ok) throw new Error(`Releases request failed (${response.status})`);
  const payload: unknown = await response.json();
  if (!Array.isArray(payload)) throw new Error('Releases response was not a list');
  return payload.flatMap((value): UpdateReleaseInfo[] => {
    if (!value || typeof value !== 'object') return [];
    const item = value as Record<string, unknown>;
    const tag = typeof item.tag_name === 'string' ? item.tag_name.trim() : '';
    if (!tag) return [];
    const name = typeof item.name === 'string' && item.name.trim() ? item.name.trim() : tag;
    return [
      {
        version: tag.replace(/^v/, ''),
        name,
        date: typeof item.published_at === 'string' ? item.published_at : '',
        prerelease: item.prerelease === true,
        notes: typeof item.body === 'string' ? item.body : '',
      },
    ];
  });
}

function releaseNotes(info: UpdateInfo): string | null {
  const notes = info.releaseNotes;
  if (typeof notes === 'string') return notes;
  if (!Array.isArray(notes)) return null;
  return (
    notes
      .map((entry) => entry.note)
      .filter(Boolean)
      .join('\n\n') || null
  );
}

function releaseSize(info: UpdateInfo): number | undefined {
  const total = info.files?.reduce((sum, file) => sum + (file.size || 0), 0) || 0;
  return total > 0 ? total : undefined;
}

/**
 * updates.json only ever stored the retired channel choice. Forget it so a
 * saved "preview" cannot linger; the updater always follows Stable now.
 */
export function forgetSavedUpdateChannel(userData: string = app.getPath('userData')): void {
  try {
    rmSync(join(userData, 'updates.json'), { force: true });
  } catch {
    // Best effort: an unreadable leftover is ignored either way.
  }
}

export class DesktopUpdater {
  private listeners = new Set<(state: UpdateState) => void>();
  private state: UpdateState;
  private checkInFlight: Promise<UpdateState> | null = null;
  private downloadInFlight: Promise<UpdateState> | null = null;

  constructor(
    private readonly distribution: DistributionTarget = CURRENT_DISTRIBUTION,
    private readonly feedFetcher: typeof fetch = fetch,
  ) {
    const supported = this.updatesSupported();
    this.state = {
      status: supported ? 'idle' : 'unsupported',
      currentVersion: app.getVersion(),
      progress: 0,
    };
    forgetSavedUpdateChannel();
    if (!supported) return;

    autoUpdater.autoDownload = false;
    autoUpdater.autoInstallOnAppQuit = false;
    autoUpdater.allowDowngrade = false;
    autoUpdater.on('checking-for-update', () =>
      this.patch({
        status: 'checking',
        progress: 0,
        transferredBytes: undefined,
        totalBytes: undefined,
        bytesPerSecond: undefined,
        etaSeconds: undefined,
        error: undefined,
      }),
    );
    autoUpdater.on('update-available', (info) =>
      this.patch({
        status: 'available',
        availableVersion: info.version,
        notes: releaseNotes(info),
        transferredBytes: 0,
        totalBytes: releaseSize(info),
        bytesPerSecond: undefined,
        etaSeconds: undefined,
        lastCheckedAt: Date.now(),
        error: undefined,
      }),
    );
    autoUpdater.on('update-not-available', () =>
      this.patch({
        status: 'idle',
        availableVersion: undefined,
        notes: null,
        progress: 0,
        transferredBytes: undefined,
        totalBytes: undefined,
        bytesPerSecond: undefined,
        etaSeconds: undefined,
        lastCheckedAt: Date.now(),
        error: undefined,
      }),
    );
    autoUpdater.on('download-progress', (progress: ProgressInfo) => {
      const totalBytes = Math.max(0, progress.total || 0);
      const transferredBytes = Math.max(0, progress.transferred || 0);
      const bytesPerSecond = Math.max(0, progress.bytesPerSecond || 0);
      this.patch({
        status: 'downloading',
        progress: Math.max(0, Math.min(100, progress.percent)),
        transferredBytes,
        totalBytes: totalBytes || this.state.totalBytes,
        bytesPerSecond: bytesPerSecond || undefined,
        etaSeconds:
          totalBytes > transferredBytes && bytesPerSecond > 0
            ? Math.ceil((totalBytes - transferredBytes) / bytesPerSecond)
            : 0,
      });
    });
    autoUpdater.on('update-downloaded', (info) => {
      const totalBytes = this.state.totalBytes || releaseSize(info);
      this.patch({
        status: 'downloaded',
        availableVersion: info.version,
        notes: releaseNotes(info),
        progress: 100,
        transferredBytes: totalBytes || this.state.transferredBytes || 0,
        totalBytes,
        bytesPerSecond: undefined,
        etaSeconds: 0,
        error: undefined,
      });
    });
    autoUpdater.on('error', (error) =>
      this.patch({ status: 'error', error: error.message || String(error) }),
    );
    if (this.distribution !== 'microsoft-store') this.configureFeed(STABLE_FEED);
  }

  snapshot(): UpdateState {
    return { ...this.state };
  }

  subscribe(listener: (state: UpdateState) => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  async check(quiet = false): Promise<UpdateState> {
    if (
      !this.updatesSupported() ||
      this.downloadInFlight ||
      ['downloading', 'downloaded'].includes(this.state.status)
    )
      return this.snapshot();
    if (this.checkInFlight) return this.checkInFlight;
    const previousState = this.snapshot();
    this.patch({
      status: 'checking',
      progress: 0,
      transferredBytes: undefined,
      totalBytes: undefined,
      bytesPerSecond: undefined,
      etaSeconds: undefined,
      error: undefined,
    });
    const operation = (async () => {
      try {
        const feedUrl =
          this.distribution === 'microsoft-store'
            ? await resolveUpdateFeedUrl(
                this.distribution,
                process.platform,
                process.arch,
                this.feedFetcher,
              )
            : STABLE_FEED;
        this.configureFeed(feedUrl);
        await autoUpdater.checkForUpdates();
        if (this.state.status === 'checking') {
          this.patch({ status: 'idle', lastCheckedAt: Date.now() });
        }
      } catch (error) {
        if (quiet) {
          this.state = previousState;
          this.emit();
        } else
          this.patch({
            status: 'error',
            error: error instanceof Error ? error.message : String(error),
          });
      }
      return this.snapshot();
    })();
    this.checkInFlight = operation;
    try {
      return await operation;
    } finally {
      if (this.checkInFlight === operation) this.checkInFlight = null;
    }
  }

  async download(): Promise<UpdateState> {
    if (!this.updatesSupported() || this.checkInFlight || this.state.status !== 'available')
      return this.snapshot();
    if (this.downloadInFlight) return this.downloadInFlight;
    const operation = (async () => {
      this.patch({
        status: 'downloading',
        progress: 0,
        transferredBytes: 0,
        bytesPerSecond: undefined,
        etaSeconds: undefined,
        error: undefined,
      });
      try {
        await autoUpdater.downloadUpdate();
      } catch (error) {
        this.patch({
          status: 'error',
          error: error instanceof Error ? error.message : String(error),
        });
      }
      return this.snapshot();
    })();
    this.downloadInFlight = operation;
    try {
      return await operation;
    } finally {
      if (this.downloadInFlight === operation) this.downloadInFlight = null;
    }
  }

  dismiss(): UpdateState {
    if (this.state.status !== 'error') return this.snapshot();
    this.state = {
      status: 'idle',
      currentVersion: this.state.currentVersion,
      progress: 0,
      lastCheckedAt: this.state.lastCheckedAt,
    };
    this.emit();
    return this.snapshot();
  }

  install(): void {
    if (!this.updatesSupported() || this.state.status !== 'downloaded') return;
    autoUpdater.quitAndInstall(false, true);
  }

  private configureFeed(url: string): void {
    if (!this.updatesSupported()) return;
    autoUpdater.allowPrerelease = false;
    const feedChannel = feedChannelName(process.platform, process.arch, this.distribution);
    autoUpdater.channel = feedChannel;
    // electron-updater's channel setter silently enables downgrades; keep the
    // app monotonic.
    autoUpdater.allowDowngrade = false;
    // Explicit, so a build packaged with another feed URL still follows Stable.
    autoUpdater.setFeedURL({
      provider: 'generic',
      url,
      channel: feedChannel,
    });
  }

  private updatesSupported(): boolean {
    return applicationUpdatesSupported(app.isPackaged, this.distribution);
  }

  private patch(value: Partial<UpdateState>): void {
    this.state = { ...this.state, ...value };
    this.emit();
  }

  private emit(): void {
    const state = this.snapshot();
    for (const listener of this.listeners) listener(state);
  }
}

export function registerUpdateIpc(
  updater: DesktopUpdater,
  getMainWindow: () => BrowserWindow | null,
  beforeInstall: () => Promise<void>,
): () => void {
  const trusted = (event: Electron.IpcMainInvokeEvent) => {
    const owner = getMainWindow();
    if (
      !owner ||
      event.sender !== owner.webContents ||
      event.senderFrame !== owner.webContents.mainFrame ||
      !isTrustedRenderer(event.senderFrame.url, process.env.ELECTRON_RENDERER_URL)
    )
      throw new Error('Untrusted update request');
  };
  ipcMain.handle(UPDATE_CHANNELS.getState, (event) => {
    trusted(event);
    return updater.snapshot();
  });
  ipcMain.handle(UPDATE_CHANNELS.check, (event) => {
    trusted(event);
    return updater.check();
  });
  ipcMain.handle(UPDATE_CHANNELS.download, (event) => {
    trusted(event);
    return updater.download();
  });
  ipcMain.handle(UPDATE_CHANNELS.dismiss, (event) => {
    trusted(event);
    return updater.dismiss();
  });
  ipcMain.handle(UPDATE_CHANNELS.install, async (event) => {
    trusted(event);
    if (updater.snapshot().status !== 'downloaded') return;
    await beforeInstall();
    updater.install();
  });
  ipcMain.handle(UPDATE_CHANNELS.listReleases, (event) => {
    trusted(event);
    return listDesktopReleases();
  });
  const unsubscribe = updater.subscribe((state) => {
    sendToLiveWindow(getMainWindow(), UPDATE_CHANNELS.state, state);
  });
  return () => {
    unsubscribe();
    Object.values(UPDATE_CHANNELS)
      .filter((channel) => channel !== UPDATE_CHANNELS.state)
      .forEach((channel) => ipcMain.removeHandler(channel));
  };
}
