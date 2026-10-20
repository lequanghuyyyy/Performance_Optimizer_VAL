import { useState } from 'react';
import {
  BlocksIcon,
  BookOpenIcon,
  CopyIcon,
  EyeIcon,
  ExternalLinkIcon,
  MailIcon,
  PinIcon,
  XIcon,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  DialogDescription,
  DialogClose,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { getBridge } from '@/components/bridge';
import './sponsor-inquiry.css';

export const PARTNER_EMAIL = 'partner@voicestudio.sh';
export const SPONSOR_FORM_URL = 'https://forms.gle/2PYCvd39hbwijzX37';
export function sponsorMailto(subject: string, message: string) {
  return `mailto:${PARTNER_EMAIL}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(message)}`;
}

export function SponsorInquiry({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { t } = useTranslation();
  const emailTemplate = t('sponsorSlot.email_template');
  const [message, setMessage] = useState(emailTemplate);
  const [copied, setCopied] = useState(false);
  const [failed, setFailed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [mode, setMode] = useState<'form' | 'email'>('form');
  return (
    <Dialog
      open={open}
      onOpenChange={(value) => {
        setCopied(false);
        setFailed(false);
        onOpenChange(value);
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="sponsor-inquiry-dialog h-[min(90vh,1600px)] max-h-[min(90vh,1600px)] overflow-hidden rounded-2xl p-6 sm:max-w-2xl"
      >
        <DialogClose
          render={
            <Button
              variant="ghost"
              size="icon-sm"
              className="absolute right-3 top-3"
              aria-label={t('common.close')}
            />
          }
        >
          <XIcon aria-hidden="true" />
        </DialogClose>
        <div className="sponsor-inquiry-heading">
          <span className="sponsor-inquiry-hero-icon" aria-hidden="true">
            <PinIcon />
          </span>
          <div className="grid gap-1">
            <DialogTitle>{t('sponsorSlot.partner_heading')}</DialogTitle>
            <DialogDescription>{t('sponsorSlot.partner_subtitle')}</DialogDescription>
          </div>
        </div>
        <div className="sponsor-inquiry-perks">
          <span><EyeIcon aria-hidden="true" /><small>{t('sponsorSlot.app_placement')}</small></span>
          <span><BlocksIcon aria-hidden="true" /><small>{t('sponsorSlot.integration_page')}</small></span>
          <span><BookOpenIcon aria-hidden="true" /><small>{t('sponsorSlot.readme_exposure')}</small></span>
        </div>
        <div className="sponsor-inquiry-methods">
          <div className="sponsor-inquiry-tabs" role="tablist" aria-label={t('sponsorSlot.partner_heading')}>
            <Button type="button" size="sm" variant="ghost" className="sponsor-inquiry-tab"
              role="tab" aria-selected={mode === 'form'} onClick={() => setMode('form')}>
              {t('sponsorSlot.form')}
            </Button>
            <Button type="button" size="sm" variant="ghost" className="sponsor-inquiry-tab"
              role="tab" aria-selected={mode === 'email'} onClick={() => setMode('email')}>
              <MailIcon aria-hidden="true" />{t('sponsorSlot.email')}
            </Button>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="sponsor-inquiry-form-link"
            aria-label={t('network.open_in_browser')}
            title={t('network.open_in_browser')}
            onClick={() => {
              const bridge = getBridge();
              if (bridge) {
                void bridge.files.openExternal(SPONSOR_FORM_URL).catch(() => setFailed(true));
              } else {
                window.open(SPONSOR_FORM_URL, '_blank', 'noopener,noreferrer');
              }
            }}
          >
            <ExternalLinkIcon aria-hidden="true" />
            <span className="text-xs">{t('network.open_in_browser')}</span>
          </Button>
        </div>
        {mode === 'form' ? (
          <iframe
            title={t('sponsorSlot.book')}
            src={SPONSOR_FORM_URL}
            className="sponsor-inquiry-panel min-h-0 w-full flex-1 rounded-xl border bg-background"
            loading="lazy"
          />
        ) : (
          <form
            className="sponsor-inquiry-panel flex min-h-0 flex-1 flex-col gap-4"
            onSubmit={async (event) => {
              event.preventDefault();
              setFailed(false);
              setBusy(true);
              try {
                const href = sponsorMailto(
                  `VoiceStudio — ${t('support.sponsors_become')}`,
                  message.trim(),
                );
                const bridge = getBridge();
                if (bridge) await bridge.files.openExternal(href);
                else window.location.href = href;
              } catch {
                setFailed(true);
              } finally {
                setBusy(false);
              }
            }}
          >
            <label
              htmlFor="sponsor-message"
              className="grid min-h-0 flex-1 grid-rows-[auto_minmax(0,1fr)] gap-2 text-sm font-medium"
            >
              {t('sponsorSlot.message')}
            <textarea
                id="sponsor-message"
                value={message}
                maxLength={1500}
                rows={4}
                onChange={(event) => setMessage(event.target.value)}
                className="min-h-0 h-full w-full resize-none rounded-xl border border-sidebar-border bg-sidebar-control-surface p-3 text-sm font-normal outline-none focus-visible:ring-2 focus-visible:ring-primary"
              />
            </label>
            <div className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-sidebar-border bg-sidebar-control-surface px-3 py-2">
              <span className="select-text text-sm">{PARTNER_EMAIL}</span>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={async () => {
                  setFailed(false);
                  setCopied(false);
                  try {
                    await navigator.clipboard.writeText(PARTNER_EMAIL);
                    setCopied(true);
                  } catch {
                    setFailed(true);
                  }
                }}
              >
                <CopyIcon aria-hidden="true" />
                {t('sponsorSlot.copy_email')}
              </Button>
            </div>
            {copied && (
              <p role="status" className="text-xs text-muted-foreground">
                {t('transcriptions.copied')}
              </p>
            )}
            {failed && (
              <p role="alert" className="text-xs text-destructive">
                {t('common.error')}
              </p>
            )}
            <Button type="submit" disabled={busy}>
              <MailIcon aria-hidden="true" />
              {t('sponsorSlot.email_app')}
            </Button>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
