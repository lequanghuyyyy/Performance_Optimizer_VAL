import assert from 'node:assert/strict';
import { chromium } from 'playwright';

const browser = await chromium.launch({
  ...(process.env.PLAYWRIGHT_BUNDLED === '1' ? {} : { channel: process.env.PLAYWRIGHT_CHANNEL || 'msedge' }),
  headless: true,
});
const page = await browser.newPage({ viewport: { width: 1280, height: 1000 } });
const ui = process.env.VOICESTUDIO_UI_URL || 'http://localhost:3912';
let changed = false;
let currentTier = 'fast';
let fail = false;
let releaseSave;
let releaseEngines;
const saveGate = new Promise((resolve) => {
  releaseSave = resolve;
});
const engineGate = new Promise((resolve) => {
  releaseEngines = resolve;
});
const writes = [];
let telemetryReads = 0;
const model = () =>
  currentTier === 'auto' ? 'Systran/faster-whisper-tiny' : changed ? 'test/medium' : 'test/small';
const ttsEngine = () => (currentTier === 'auto' ? 'omnivoice' : 'kitten');
const captureModel = () =>
  currentTier === 'auto' ? 'sherpa-parakeet-tdt-v3' : 'sherpa-whisper-tiny';
const captureLabel = () => (currentTier === 'auto' ? 'Parakeet TDT v3' : 'Whisper Tiny');
const profile = () => ({
  global: currentTier,
  overrides: {},
  effective: { asr: changed ? 'balanced' : 'fast' },
  families: ['tts', 'asr'],
  implemented_families: ['asr'],
  applicable_families: ['tts', 'asr', 'dictation', 'translation'],
  targets: { tts: {}, asr: { beam_size: changed ? 3 : 1 } },
  selections: {
    tts: { engine: ttsEngine(), model: currentTier === 'auto' ? 'k2-fsa/OmniVoice' : null },
    dictation: { engine: 'sherpa-onnx', model: captureModel(), label: captureLabel() },
    asr: {
      engine: 'faster-whisper',
      model: model(),
      label: changed ? 'Whisper medium' : 'Whisper small',
    },
  },
  plan: {
    resolved: currentTier === 'auto' ? 'quality' : currentTier,
    status: 'adjusted',
    max_status: 'adjusted',
    hardware: { device: 'cuda', ram_gb: 32, vram_gb: 8, cpu_threads: 16 },
    families: Object.fromEntries(
      ['tts', 'asr', 'translation', 'dictation', 'diarisation', 'llm'].map((name) => [
        name,
        {
          tier: 'quality',
          reason: ['tts', 'dictation'].includes(name)
            ? 'fits'
            : ['asr', 'translation'].includes(name)
              ? 'memory'
              : 'installed',
          selection: null,
        },
      ]),
    ),
  },
});
try {
  await page.addInitScript(() => localStorage.setItem('voicestudio.setup.complete.v1', '1'));
  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path.endsWith('/settings/performance-profile')) {
      if (request.method() === 'PUT') {
        writes.push(request.postDataJSON());
        if (fail) return route.fulfill({ status: 409, json: { detail: 'Test: engine busy' } });
        await saveGate;
        changed = true;
        currentTier = request.postDataJSON().tier;
      }
      return route.fulfill({ json: profile() });
    }
    assert.equal(request.method(), 'GET', 'The smoke test must never mutate the live backend');
    if (path.endsWith('/sysinfo')) {
      telemetryReads += 1;
      return route.fulfill({
        json: {
          cpu: telemetryReads === 1 ? 12 : 37,
          gpu_utilization: 61,
          ram: 18.5,
          total_ram: 32,
          vram: 6.2,
          total_vram: 8,
        },
      });
    }
    if (path.endsWith('/engines')) {
      if (changed) await engineGate;
      return route.fulfill({
        json: {
          tts: {
            active: ttsEngine(),
            active_model: currentTier === 'auto' ? 'k2-fsa/OmniVoice' : null,
            backends: [
              {
                id: ttsEngine(),
                display_name: currentTier === 'auto' ? 'OmniVoice' : 'KittenTTS',
                available: true,
              },
            ],
          },
          asr: {
            active: 'faster-whisper',
            active_model: model(),
            backends: [{ id: 'faster-whisper', display_name: 'Faster-Whisper', available: true }],
          },
          llm: { active: 'off', backends: [{ id: 'off', display_name: 'Off', available: true }] },
        },
      });
    }
    if (path.endsWith('/dictation/prefs'))
      return route.fulfill({ json: { enabled: true, model_id: captureModel() } });
    if (path.endsWith('/dictation/models'))
      return route.fulfill({
        json: {
          engine_available: true,
          models: [{ id: captureModel(), label: captureLabel(), installed: true }],
        },
      });
    if (path.endsWith('/engines/translation'))
      return route.fulfill({
        json: {
          active: 'argos',
          sandboxed: false,
          engines: [
            {
              id: 'argos',
              display_name: 'Argos',
              installed: true,
              ready: true,
              category: 'offline',
            },
          ],
        },
      });
    if (path.endsWith('/model/loaded')) return route.fulfill({ json: { models: [], count: 0 } });
    if (path.endsWith('/batch/jobs')) return route.fulfill({ json: [] });
    return route.continue();
  });
  await page.goto(ui + '/#/clone');
  const footer = page.locator('aside').first().locator('footer');
  // Speed / quality: a radiogroup of tiers (Fast/Balanced/Quality/Max) plus a separate Auto toggle.
  const tiers = footer.getByRole('radiogroup', { name: 'Speed / quality', exact: true });
  const tier = (name) => tiers.getByRole('radio', { name, exact: true });
  const checkedTier = () => tiers.locator('[role=radio][aria-checked=true]').innerText();
  await tiers.waitFor();
  await page.waitForFunction(
    () =>
      document.querySelector('footer [role=radiogroup][aria-describedby$="-help"] [role=radio]') &&
      !document.querySelector('footer [role=radiogroup][aria-describedby$="-help"] [role=radio]')
        .disabled,
  );
  await tier('Fast').focus();
  await page.keyboard.press('ArrowRight');
  await page.waitForFunction(
    () =>
      document.querySelector('footer [role=radiogroup][aria-describedby$="-help"] [role=radio]')
        ?.disabled,
  );
  releaseSave();
  await footer.locator('[data-detail-level=models]').waitFor();
  const asr = footer.locator('[data-slot=engine-row]').filter({ hasText: 'Speech to text' });
  await asr.getByRole('status').filter({ hasText: 'Switching' }).waitFor();
  assert.ok(
    (await asr.innerText()).includes('medium'),
    'Show the confirmed replacement while catalogue refresh is pending',
  );
  assert.equal(await asr.getAttribute('aria-busy'), 'true');
  releaseEngines();
  await asr.getByRole('status').filter({ hasText: 'Available' }).waitFor();
  assert.equal(await asr.getAttribute('aria-busy'), 'false');
  assert.equal(await checkedTier(), 'Balanced');
  assert.deepEqual(writes, [{ tier: 'balanced', family: null }]);
  fail = true;
  await tier('Balanced').focus();
  await page.keyboard.press('ArrowLeft');
  await footer.getByRole('alert').waitFor();
  assert.equal(
    await checkedTier(),
    'Balanced',
    'A rejected save restores the confirmed preset',
  );
  assert.ok(
    (await asr.innerText()).includes('medium'),
    'A rejected save preserves the confirmed model',
  );
  fail = false;
  await footer.getByRole('button', { name: 'Auto', exact: true }).click();
  await footer.getByRole('button', { name: 'Auto', exact: true, pressed: true }).waitFor();
  await page.waitForFunction(
    () =>
      !document.querySelector('footer [role=radiogroup][aria-describedby$="-help"] [role=radio]')
        ?.disabled,
  );
  assert.equal(telemetryReads, 0, 'Closed hardware panel does not poll');
  await footer.getByRole('button', { name: 'Adapted to your hardware' }).click();
  const live = page.getByRole('region', { name: 'Live usage' });
  await live.getByText('12%', { exact: true }).waitFor();
  await live.getByText('37%', { exact: true }).waitFor();
  await live.getByText('18.5 / 32 GB', { exact: true }).waitFor();
  await live.getByText('6.2 / 8 GB', { exact: true }).waitFor();
  const hardwareSpecs = page.getByText('32 GB RAM · 16 CPU threads · 8 GB VRAM');
  assert.equal(await hardwareSpecs.isVisible(), false, 'Secondary explanation starts collapsed');
  const details = page.locator('[data-slot=popover-content] summary');
  await details.focus();
  await details.press('Enter');
  await hardwareSpecs.waitFor();
  await details.press('Enter');
  assert.equal(await hardwareSpecs.isVisible(), false, 'Details collapse with the keyboard');
  await details.blur();
  await page.getByText('Memory limit', { exact: true }).first().waitFor();
  await footer.getByText('OmniVoice', { exact: true }).waitFor();
  await footer.getByText('Parakeet TDT v3', { exact: true }).waitFor();
  assert.deepEqual(writes.at(-1), { tier: 'auto', family: null });
  await page
    .locator('[data-slot=popover-content]')
    .evaluate((el) => Promise.all(el.getAnimations().map((animation) => animation.finished)));
  const panel = await footer.boundingBox();
  const explanation = await page.locator('[data-slot=popover-content]').boundingBox();
  await page.screenshot({
    path: '.tmp-sidebar-auto.png',
    clip: {
      x: panel.x,
      y: Math.min(panel.y, explanation.y),
      width: explanation.x + explanation.width - panel.x + 8,
      height:
        Math.max(panel.y + panel.height, explanation.y + explanation.height) -
        Math.min(panel.y, explanation.y),
    },
  });
  await page.keyboard.press('Escape');
  await live.waitFor({ state: 'hidden' });
  const afterClose = telemetryReads;
  await page.waitForTimeout(2_300);
  assert.equal(telemetryReads, afterClose, 'Polling stops when the panel closes');
  console.log(
    'Speed/quality tiers: confirmed model feedback, loading, rollback, Auto selection and hardware explanation passed; all writes mocked.',
  );
} finally {
  releaseSave();
  releaseEngines();
  await browser.close();
}
