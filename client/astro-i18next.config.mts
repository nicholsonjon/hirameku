/** @type {import('astro-i18next').AstroI18nextConfig} */
export default {
  defaultLocale: "en",
  defaultNamespace: "common",
  load: ["client", "server"],
  locales: ["en", "jp"],
  namespaces: [
    "common",
    "contact-form",
  ],
};
