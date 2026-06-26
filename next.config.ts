import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./app/i18n/request.ts");

const nextConfig: NextConfig = {
  // Emit a minimal standalone server bundle so the Docker image only ships
  // the files needed to run (see deploy: ghcr.io/geneflow-app/geneflow-frontend).
  output: "standalone",
};

export default withNextIntl(nextConfig);
