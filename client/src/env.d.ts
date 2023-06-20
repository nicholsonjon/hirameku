/// <reference types="astro/client" />

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

interface ImportMetaEnv {
  readonly PUBLIC_RECAPTCHA_V3_SITE_KEY: string;
}
