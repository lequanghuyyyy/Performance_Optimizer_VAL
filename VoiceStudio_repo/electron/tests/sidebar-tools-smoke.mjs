import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { chromium } from 'playwright';

const browser = await chromium.launch({
  ...(process.env.PLAYWRIGHT_BUNDLED === '1' ? {} : { channel: process.env.PLAYWRIGHT_CHANNEL || 'msedge' }),
  headless: true,
});
const ui = process.env.VOICESTUDIO_UI_URL || 'http://localhost:3912';
try {
  for (const platform of ['win32', 'darwin', 'linux']) {
    for (const locale of ['en', 'de', 'ar']) {
      const strings = JSON.parse(
        readFileSync(
          new URL(`../src/renderer/src/i18n/locales/${locale}.json`, import.meta.url),
          'utf8',
        ),
      );
      const page = await browser.newPage({ viewport: { width: 960, height: 600 } });
      await page.addInitScript(
        ({ platform, locale }) => {
          localStorage.setItem('voicestudio.setup.complete.v1', '1');
          localStorage.setItem('voicestudio.locale', locale);
          window.voicestudio = {
            app: {
              version: 'test',
              platform,
              isDev: true,
              onNavigate: () => () => {},
              onPersistenceFlush: () => () => {},
            },
            repair: {
              list: async () => [],
              getState: async () => ({ status: 'idle', output: '', workspaceAvailable: false }),
              onEvent: () => () => {},
            },
          };
        },
        { platform, locale },
      );
      await page.route('**/api/profiles', (route) =>
        route.fulfill({
          json: [
            {
              id: 'sidebar-layout-fixture',
              name: 'Sidebar layout fixture',
              kind: 'clone',
              ref_audio_path: 'fixture.wav',
              created_at: 0,
            },
          ],
        }),
      );
      await page.goto(ui + '/#/clone');
      const sidebar = page.locator('aside').first();
      await sidebar.locator('[data-slot=engine-view-toggle]').waitFor();
      const chooseView = async (level) => {
        await sidebar
          .getByRole('radiogroup', { name: strings.sidebarTools.title, exact: true })
          .getByRole('radio', {
            name: level === 'models' ? strings.modelSettings.models : strings.sidebarTools[level],
            exact: true,
          })
          .click();
      };
      const footer = sidebar.locator('footer');
      assert.equal(
        await footer.locator('[data-slot=engine-row]').count(),
        1,
        'Simple starts collapsed to voice readiness',
      );
      await chooseView('models');
      for (const family of ['tts', 'asr', 'llm', 'translation', 'dictation', 'diarisation']) {
        const tool = footer.locator(`a[href$="/settings/models/${family}"]`).first();
        assert.ok((await tool.innerText()).includes(strings.sidebarTools[family]));
        assert.ok(
          (await tool.innerText()).split('\n').length >= 2,
          'Status must be written, not color-only',
        );
        assert.ok((await tool.boundingBox()).height >= 44);
        assert.equal(await tool.evaluate((el) => el.scrollWidth <= el.clientWidth), true);
      }
      const speech = footer.locator('a[href$="/settings/models/tts"]').first();
      await speech.focus();
      assert.ok(await speech.evaluate((el) => el === document.activeElement));
      assert.equal(await page.locator('[data-slot=tooltip-content][data-open]').count(), 0);
      await speech.blur();
      // Speed / quality is a radiogroup of tiers (Fast/Balanced/Quality/Max) with an Auto toggle.
      const tiers = footer.getByRole('radiogroup', {
        name: strings.performanceProfile.title,
        exact: true,
      });
      await tiers.waitFor();
      assert.ok(await tiers.getAttribute('aria-describedby'));
      assert.equal(
        await tiers.locator('[role=radio][aria-checked=true]').count(),
        1,
        'Exactly one speed/quality tier is selected',
      );
      for (const level of ['simple', 'models', 'details']) {
        await chooseView(level);
        assert.equal(
          await footer.locator('#sidebar-engine-details').getAttribute('data-detail-level'),
          level,
        );
        assert.equal(
          await footer.locator('[data-slot=engine-row]').count(),
          level === 'simple' ? 1 : 6,
          'Views replace one another; engine rows are never duplicated',
        );
        if (level === 'details') {
          const disclosure = footer.locator('[data-slot=engine-row] > button');
          await disclosure.nth(1).click();
          assert.equal(await footer.locator('[data-slot=engine-diagnostics]').count(), 1);
          assert.equal(await disclosure.first().getAttribute('aria-expanded'), 'false');
        }
        const settings = sidebar.locator('a[href$="/settings"]').first();
        const bounds = await settings.boundingBox();
        assert.ok(
          bounds && bounds.y >= 0 && bounds.y + bounds.height <= 600,
          `${platform}/${locale}: Settings must remain on-screen`,
        );
        // A taller engine footer must leave a usable voice library, even when
        // navigation needs to scroll. Check its real visible bounds.
        const preview = sidebar
          .getByRole('button', { name: strings.clone.preview_voice, exact: true })
          .first();
        await preview.scrollIntoViewIfNeeded();
        const previewBounds = await preview.boundingBox();
        const tabBounds = await sidebar
          .getByRole('tab', { name: strings.clone.saved_profiles, exact: true })
          .boundingBox();
        const footerBounds = await footer.boundingBox();
        assert.ok(
          previewBounds.y >= tabBounds.y + tabBounds.height &&
            previewBounds.y + previewBounds.height <= footerBounds.y,
          `${platform}/${locale}/${level}: Voice preview must fit between tabs and engines`,
        );
        assert.ok(
          await footer.evaluate((el) => el.scrollWidth <= el.clientWidth),
          platform +
            '/' +
            locale +
            ': ' +
            JSON.stringify(
              await footer.evaluate((el) => ({
                width: el.clientWidth,
                scroll: el.scrollWidth,
                children: [...el.querySelectorAll('*')]
                  .filter((x) => x.scrollWidth > x.clientWidth && x.clientWidth > 0)
                  .map((x) => [x.tagName, x.className, x.clientWidth, x.scrollWidth]),
              })),
            ),
        );
      }
      await page.reload();
      await footer.locator('[data-detail-level=details]').waitFor();
      if (platform === 'win32' && locale === 'en') {
        await page.setViewportSize({ width: 1280, height: 900 });
        for (const level of ['simple', 'models', 'details']) {
          await chooseView(level);
          await footer.evaluate((el) => {
            el.scrollTop = 0;
          });
          await page.mouse.move(800, 100);
          await page.keyboard.press('Escape');
          await footer.screenshot({
            path: `.tmp-sidebar-tools-${level}.png`,
            animations: 'disabled',
          });
        }
      }
      await chooseView('models');
      await speech.focus();
      await page.keyboard.press('Enter');
      await page.waitForURL('**/#/settings/models/tts');
      await page.close();
    }
  }
  console.log(
    'Three persistent views, no duplicate rows, text status, keyboard navigation, slider descriptions, and short-window layout passed for Windows/macOS/Linux renderer layouts in English/German/Arabic.',
  );
} finally {
  await browser.close();
}
