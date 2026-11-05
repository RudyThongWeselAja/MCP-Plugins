using System.Text;
using System.Text.Json;

namespace WeselAjaMcpServer.Generators;

public sealed class ShopifyWeselAjaGenerator
{
  public const string GeneratorVersion = "shopify-full-app-v4";

  public GenerationResult Generate(
      string projectName,
      bool includeExample,
      bool includeTests,
      bool overwrite)
  {
    var safeProjectName = SanitizeProjectName(projectName);

    var configuredOutputRoot =
        Environment.GetEnvironmentVariable(
            "WESELAJA_GENERATOR_OUTPUT_DIR");

    var outputRoot =
        string.IsNullOrWhiteSpace(configuredOutputRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "generated")
            : configuredOutputRoot.Trim();

    outputRoot = Path.GetFullPath(outputRoot);

    var shopifyRoot = Path.GetFullPath(
        Path.Combine(outputRoot, "shopify"));

    var projectDirectory = Path.GetFullPath(
        Path.Combine(shopifyRoot, safeProjectName));

    EnsureInsideDirectory(projectDirectory, shopifyRoot);

    if (Directory.Exists(projectDirectory))
    {
      if (!overwrite)
      {
        throw new InvalidOperationException(
            $"Generated project already exists: {projectDirectory}. " +
            "Set overwrite=true to replace it.");
      }

      Directory.Delete(projectDirectory, recursive: true);
    }

    Directory.CreateDirectory(projectDirectory);

    var files = BuildFiles(
        safeProjectName,
        includeExample,
        includeTests);

    var generatedFiles = new List<GeneratedFile>();

    foreach (var file in files)
    {
      var filePath = Path.GetFullPath(
          Path.Combine(projectDirectory, file.Key));

      EnsureInsideDirectory(filePath, projectDirectory);

      var directory = Path.GetDirectoryName(filePath);

      if (!string.IsNullOrWhiteSpace(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var content = file.Value.Replace(
          "__PROJECT_NAME__",
          safeProjectName,
          StringComparison.Ordinal);

      File.WriteAllText(
          filePath,
          content,
          new UTF8Encoding(false));

      generatedFiles.Add(
          new GeneratedFile
          {
            Path = Path.GetRelativePath(projectDirectory, filePath),
            Bytes = new FileInfo(filePath).Length
          });
    }

    return new GenerationResult(
        safeProjectName,
        projectDirectory,
        generatedFiles);
  }

  private static Dictionary<string, string> BuildFiles(
      string projectName,
      bool includeExample,
      bool includeTests)
  {
    var files = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase)
    {
      ["GENERATOR_VERSION.txt"] =
          GeneratorVersion + Environment.NewLine,

      ["package.json"] =
          PackageTemplate(projectName, includeTests),

      ["shopify.app.toml"] =
          ShopifyAppTomlTemplate,

      ["shopify.web.toml"] =
          ShopifyWebTomlTemplate,

      ["vite.config.ts"] =
          ViteConfigTemplate,

      ["tsconfig.json"] =
          TsConfigTemplate,

      ["env.d.ts"] =
          EnvTypesTemplate,

      [".gitignore"] =
          GitIgnoreTemplate,

      [".env.example"] =
          EnvTemplate,

      ["app/routes.ts"] =
          RoutesTemplate,

      ["app/root.tsx"] =
          RootTemplate,

      ["app/entry.server.tsx"] =
          EntryServerTemplate,

      ["app/db.server.ts"] =
          DbServerTemplate,

      ["app/shopify.server.ts"] =
          ShopifyServerTemplate,

      ["app/lib/weselaja.server.ts"] =
          WeselAjaClientTemplate,

      ["app/lib/weselaja-webhook.server.ts"] =
          WeselAjaWebhookTemplate,

      ["app/lib/shopify-order-sync.server.ts"] =
          ShopifyOrderSyncTemplate,

      ["app/routes/app.tsx"] =
          AppLayoutTemplate,

      ["app/routes/app._index.tsx"] =
          AppIndexTemplate,

      ["app/routes/_index/route.tsx"] =
          IndexRouteTemplate,

      ["app/routes/auth.$.tsx"] =
          AuthRouteTemplate,

      ["app/routes/payment.weselaja.callback.tsx"] =
          CallbackRouteTemplate,

      ["app/routes/payment.weselaja.return.tsx"] =
          ReturnRouteTemplate,

      ["app/routes/webhooks.app.uninstalled.tsx"] =
          UninstalledWebhookTemplate,

      ["app/routes/webhooks.app.scopes_update.tsx"] =
          ScopeUpdateWebhookTemplate,

      ["prisma/schema.prisma"] =
          PrismaSchemaTemplate,

      ["README.md"] =
          ReadmeTemplate
    };

    if (includeTests)
    {
      files["tests/weselaja.test.ts"] = TestsTemplate;
    }

    if (includeExample)
    {
      files["scripts/verify-config.ps1"] = VerifyConfigScript;
    }

    return files;
  }

  private static string PackageTemplate(
      string projectName,
      bool includeTests)
  {
    var scripts = new Dictionary<string, string>
    {
      ["dev"] = "shopify app dev",
      ["build"] = "react-router build",
      ["start"] = "react-router-serve ./build/server/index.js",
      ["setup"] = "prisma generate && prisma db push",
      ["db:studio"] = "prisma studio",
      ["typecheck"] = "react-router typegen && tsc --noEmit",
      ["config:link"] = "shopify app config link"
    };

    if (includeTests)
    {
      scripts["test"] = "tsx --test tests/weselaja.test.ts";
    }

    var scriptsJson = JsonSerializer.Serialize(scripts);

    return $$"""
        {
          "name": "{{projectName}}",
          "private": true,
          "type": "module",
          "engines": {
            "node": ">=20.19 <22 || >=22.12"
          },
          "scripts": {{scriptsJson}},
          "dependencies": {
            "@prisma/client": "^6.19.3",
            "@react-router/dev": "7.18.2",
            "@react-router/fs-routes": "7.18.2",
            "@react-router/node": "7.18.2",
            "@react-router/serve": "7.18.2",
            "@shopify/app-bridge-react": "^4.2.4",
            "@shopify/shopify-app-react-router": "^1.1.0",
            "@shopify/shopify-app-session-storage-prisma": "^9.0.0",
            "isbot": "^5.1.31",
            "prisma": "^6.19.3",
            "react": "^18.3.1",
            "react-dom": "^18.3.1",
            "react-router": "7.18.2",
            "vite-tsconfig-paths": "^6.0.4"
          },
          "devDependencies": {
            "@types/node": "^22.19.0",
            "@types/react": "^18.3.25",
            "@types/react-dom": "^18.3.7",
            "tsx": "^4.20.0",
            "typescript": "^5.9.3",
            "vite": "^7.3.1"
          },
          "overrides": {
            "qs": "^6.16.0"
          }
        }
        """;
  }

  private static string SanitizeProjectName(string? projectName)
  {
    if (string.IsNullOrWhiteSpace(projectName))
    {
      return "weselaja-shopify";
    }

    var builder = new StringBuilder();
    var separatorPending = false;

    foreach (var character in projectName.Trim().ToLowerInvariant())
    {
      if (char.IsAsciiLetterOrDigit(character))
      {
        if (separatorPending && builder.Length > 0)
        {
          builder.Append('-');
        }

        builder.Append(character);
        separatorPending = false;
      }
      else if (builder.Length > 0)
      {
        separatorPending = true;
      }
    }

    var result = builder.ToString().Trim('-');

    if (string.IsNullOrWhiteSpace(result))
    {
      result = "weselaja-shopify";
    }

    if (result.Length > 64)
    {
      result = result[..64].TrimEnd('-');
    }

    return result;
  }

  private static void EnsureInsideDirectory(
      string path,
      string root)
  {
    var fullPath = Path.GetFullPath(path);
    var fullRoot = Path.GetFullPath(root);
    var relative = Path.GetRelativePath(fullRoot, fullPath);

    if (
        relative == ".." ||
        relative.StartsWith(
            ".." + Path.DirectorySeparatorChar,
            StringComparison.Ordinal) ||
        relative.StartsWith(
            ".." + Path.AltDirectorySeparatorChar,
            StringComparison.Ordinal) ||
        Path.IsPathRooted(relative))
    {
      throw new InvalidOperationException(
          "Generated path is outside the configured directory.");
    }
  }

  private const string GitIgnoreTemplate = """
    node_modules/
    build/
    .react-router/
    .shopify/
    .env
    prisma/dev.sqlite
    prisma/dev.sqlite-journal
    *.sqlite-journal
    """;

  private const string EnvTemplate = """
    SHOPIFY_API_KEY=
    SHOPIFY_API_SECRET=
    SCOPES=read_orders,write_orders,write_draft_orders
    SHOPIFY_APP_URL=

    WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
    WESELAJA_API_KEY=
    WESELAJA_SECRET_KEY=
    WESELAJA_CUSTOMER_REFERENCE=
    WESELAJA_CURRENCY=IDR
    WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT

    WESELAJA_CALLBACK_URL=
    WESELAJA_REDIRECT_URL=

    WEB_SIGNATURE_SECRET=
    WESELAJA_CALLBACK_TIMESTAMP_HEADER=x-middleware-timestamp
    WESELAJA_CALLBACK_SIGNATURE_HEADER=x-middleware-signature
    WESELAJA_CALLBACK_SIGNED_URI=
    WESELAJA_CALLBACK_SIGNED_BASE_URL=
    """;

  private const string EnvTypesTemplate = """
    /// <reference types="vite/client" />
    """;

  private const string ShopifyAppTomlTemplate = """
    name = "__PROJECT_NAME__"
    client_id = ""
    application_url = "https://example.com"
    embedded = true

    [build]
    automatically_update_urls_on_dev = true

    [access_scopes]
    scopes = "read_orders,write_orders,write_draft_orders"

    [auth]
    redirect_urls = ["https://example.com/api/auth"]

    [webhooks]
    api_version = "2026-10"

    [[webhooks.subscriptions]]
    topics = ["app/uninstalled"]
    uri = "/webhooks/app/uninstalled"

    [[webhooks.subscriptions]]
    topics = ["app/scopes_update"]
    uri = "/webhooks/app/scopes_update"
    """;

  private const string ShopifyWebTomlTemplate = """
    roles = ["frontend", "backend"]

    [commands]
    predev = "npx prisma generate"
    dev = "npx react-router dev"
    build = "npx react-router build"
    """;

  private const string ViteConfigTemplate = """
    import { reactRouter } from "@react-router/dev/vite";
    import { defineConfig } from "vite";
    import tsconfigPaths from "vite-tsconfig-paths";

    if (
      process.env.HOST &&
      (!process.env.SHOPIFY_APP_URL ||
        process.env.SHOPIFY_APP_URL === process.env.HOST)
    ) {
      process.env.SHOPIFY_APP_URL = process.env.HOST;
      delete process.env.HOST;
    }

    const appUrl = process.env.SHOPIFY_APP_URL || "http://localhost";
    const host = new URL(appUrl).hostname;

    const hmrConfig =
      host === "localhost"
        ? {
            protocol: "ws" as const,
            host: "localhost",
            port: 64999,
            clientPort: 64999,
          }
        : {
            protocol: "wss" as const,
            host,
            port: Number(process.env.FRONTEND_PORT || 8002),
            clientPort: 443,
          };

    export default defineConfig({
      server: {
        allowedHosts: [host, ".trycloudflare.com"],
        port: Number(process.env.PORT || 3000),
        hmr: hmrConfig,
        cors: {
          preflightContinue: true,
        },
      },
      plugins: [
        reactRouter(),
        tsconfigPaths(),
      ],
      build: {
        assetsInlineLimit: 0,
      },
      optimizeDeps: {
        include: ["@shopify/app-bridge-react"],
      },
    });
    """;

  private const string TsConfigTemplate = """
    {
      "include": [
        "env.d.ts",
        "**/*.ts",
        "**/*.tsx",
        ".react-router/types/**/*"
      ],
      "compilerOptions": {
        "lib": ["DOM", "DOM.Iterable", "ES2022"],
        "strict": true,
        "skipLibCheck": true,
        "isolatedModules": true,
        "allowSyntheticDefaultImports": true,
        "removeComments": false,
        "forceConsistentCasingInFileNames": true,
        "noEmit": true,
        "allowJs": true,
        "resolveJsonModule": true,
        "jsx": "react-jsx",
        "module": "ESNext",
        "moduleResolution": "Bundler",
        "target": "ES2022",
        "baseUrl": ".",
        "types": ["@react-router/node", "vite/client"],
        "rootDirs": [".", "./.react-router/types"]
      }
    }
    """;

  private const string RoutesTemplate = """
    import { flatRoutes } from "@react-router/fs-routes";

    export default flatRoutes();
    """;

  private const string RootTemplate = """
    import {
      Links,
      Meta,
      Outlet,
      Scripts,
      ScrollRestoration,
    } from "react-router";

    export default function App() {
      return (
        <html lang="en">
          <head>
            <meta charSet="utf-8" />
            <meta
              name="viewport"
              content="width=device-width,initial-scale=1"
            />
            <Meta />
            <Links />
          </head>
          <body>
            <Outlet />
            <ScrollRestoration />
            <Scripts />
          </body>
        </html>
      );
    }
    """;

  private const string EntryServerTemplate = """
    import { PassThrough } from "node:stream";
    import { renderToPipeableStream } from "react-dom/server";
    import { ServerRouter } from "react-router";
    import { createReadableStreamFromReadable } from "@react-router/node";
    import type { EntryContext } from "react-router";
    import { isbot } from "isbot";
    import { addDocumentResponseHeaders } from "./shopify.server";

    export const streamTimeout = 5000;

    export default async function handleRequest(
      request: Request,
      responseStatusCode: number,
      responseHeaders: Headers,
      reactRouterContext: EntryContext
    ) {
      addDocumentResponseHeaders(request, responseHeaders);

      const userAgent = request.headers.get("user-agent");
      const callbackName = isbot(userAgent ?? "")
        ? "onAllReady"
        : "onShellReady";

      return new Promise<Response>((resolve, reject) => {
        const { pipe, abort } = renderToPipeableStream(
          <ServerRouter context={reactRouterContext} url={request.url} />,
          {
            [callbackName]: () => {
              const body = new PassThrough();
              const stream = createReadableStreamFromReadable(body);

              responseHeaders.set("Content-Type", "text/html");

              resolve(
                new Response(stream, {
                  headers: responseHeaders,
                  status: responseStatusCode,
                }),
              );

              pipe(body);
            },
            onShellError(error) {
              reject(error);
            },
            onError(error) {
              responseStatusCode = 500;
              console.error(error);
            },
          },
        );

        setTimeout(abort, streamTimeout + 1000);
      });
    }
    """;

  private const string DbServerTemplate = """
    import { PrismaClient } from "@prisma/client";

    declare global {
      var prismaGlobal: PrismaClient | undefined;
    }

    const prisma =
      process.env.NODE_ENV !== "production"
        ? (global.prismaGlobal ??= new PrismaClient())
        : new PrismaClient();

    export default prisma;
    """;

  private const string ShopifyServerTemplate = """
    import "@shopify/shopify-app-react-router/adapters/node";

    import {
      AppDistribution,
      shopifyApp,
    } from "@shopify/shopify-app-react-router/server";

    import type {
      ApiVersion,
    } from "@shopify/shopify-app-react-router/server";

    import { PrismaSessionStorage } from
      "@shopify/shopify-app-session-storage-prisma";

    import prisma from "./db.server";

    const SHOPIFY_API_VERSION =
      "2026-10" as unknown as ApiVersion;

    const shopify = shopifyApp({
      apiKey: process.env.SHOPIFY_API_KEY!,
      apiSecretKey: process.env.SHOPIFY_API_SECRET || "",
      apiVersion: SHOPIFY_API_VERSION,
      scopes: process.env.SCOPES?.split(","),
      appUrl: process.env.SHOPIFY_APP_URL || "",
      authPathPrefix: "/auth",
      sessionStorage: new PrismaSessionStorage(prisma),
      distribution: AppDistribution.AppStore,
      future: {
        expiringOfflineAccessTokens: true,
      },
      ...(process.env.SHOP_CUSTOM_DOMAIN
        ? { customShopDomains: [process.env.SHOP_CUSTOM_DOMAIN] }
        : {}),
    });

    export default shopify;

    export const apiVersion = SHOPIFY_API_VERSION;
    export const addDocumentResponseHeaders =
      shopify.addDocumentResponseHeaders;
    export const authenticate = shopify.authenticate;
    export const unauthenticated = shopify.unauthenticated;
    export const login = shopify.login;
    export const registerWebhooks = shopify.registerWebhooks;
    export const sessionStorage = shopify.sessionStorage;
    """;

  private const string WeselAjaClientTemplate = """
    import { createHmac, randomUUID } from "node:crypto";

    export const WESELAJA_CALLBACK_PATH =
      "/payment/weselaja/callback";

    export const WESELAJA_RETURN_PATH =
      "/payment/weselaja/return";

    export interface WeselAjaConfig {
      apiUrl: string;
      apiKey: string;
      secretKey: string;
      customerReference: string;
      currency: string;
      paymentMethod: string;
      callbackUrl: string;
      redirectUrl: string;
    }

    export interface CreatePaymentInput {
      amount: number;
      bank: string;
      customerName: string;
      referenceCode: string;
      idempotencyKey?: string;
      customerPhoneNumber?: string;
      description?: string;
    }

    export type JsonObject = Record<string, unknown>;

    function isJsonObject(value: unknown): value is JsonObject {
      return (
        typeof value === "object" &&
        value !== null &&
        !Array.isArray(value)
      );
    }

    export function resolveBankChannel(bank: string): string {
      const normalized = bank.trim().toUpperCase();

      const channels: Record<string, string> = {
        BRI: "BRI.VA",
        MANDIRI: "MDR.VA",
        MDR: "MDR.VA",
        BNI: "BNI.VA",
        PERMATA: "PTB.VA",
        PTB: "PTB.VA",
        CIMB: "CIMB.VA",
        BCA: "BCA.VA"
      };

      if (Object.values(channels).includes(normalized)) {
        return normalized;
      }

      const channel = channels[normalized];

      if (!channel) {
        throw new Error(`Unsupported WeselAja bank: ${bank}`);
      }

      return channel;
    }

    export function createSignature(
      method: string,
      uri: string,
      timestamp: string,
      body: string,
      secretKey: string
    ): string {
      const payload = [
        method,
        uri,
        timestamp,
        body
      ].join("\n");

      return createHmac("sha256", secretKey)
        .update(payload, "utf8")
        .digest("base64");
    }

    function requiredEnv(
      env: NodeJS.ProcessEnv,
      name: string
    ): string {
      const value = env[name]?.trim();

      if (!value) {
        throw new Error(`Missing environment variable: ${name}`);
      }

      return value;
    }

    function resolvePublicUrl(
      env: NodeJS.ProcessEnv,
      variableName: string,
      path: string
    ): string {
      const explicit = env[variableName]?.trim();

      if (explicit) {
        return explicit;
      }

      const appUrl = env.SHOPIFY_APP_URL?.trim().replace(/\/+$/, "");

      if (!appUrl) {
        throw new Error(
          `Set ${variableName} or provide SHOPIFY_APP_URL. ` +
          "Leave callback URLs blank during Shopify CLI development " +
          "to derive them from the current app URL."
        );
      }

      return `${appUrl}${path}`;
    }

    function resolveV1PayinsUrl(apiUrl: string): string {
      const baseUrl = apiUrl
        .trim()
        .replace(/\/+$/, "")
        .replace(/\/v[12](?:\/payins)?$/i, "");

      if (!baseUrl) {
        throw new Error("WESELAJA_API_URL is invalid.");
      }

      return `${baseUrl}/v1/payins`;
    }

    export function configFromEnvironment(
      env: NodeJS.ProcessEnv = process.env
    ): WeselAjaConfig {
      return {
        apiUrl:
          env.WESELAJA_API_URL?.trim() ||
          "https://sandbox.checkout.weselaja.id",
        apiKey: requiredEnv(env, "WESELAJA_API_KEY"),
        secretKey: requiredEnv(env, "WESELAJA_SECRET_KEY"),
        customerReference: requiredEnv(
          env,
          "WESELAJA_CUSTOMER_REFERENCE"
        ),
        currency: env.WESELAJA_CURRENCY?.trim() || "IDR",
        paymentMethod:
          env.WESELAJA_PAYMENT_METHOD?.trim() ||
          "VIRTUAL_ACCOUNT",
        callbackUrl: resolvePublicUrl(
          env,
          "WESELAJA_CALLBACK_URL",
          WESELAJA_CALLBACK_PATH
        ),
        redirectUrl: resolvePublicUrl(
          env,
          "WESELAJA_REDIRECT_URL",
          WESELAJA_RETURN_PATH
        ),
      };
    }

    export class WeselAjaClient {
      constructor(
        private readonly config: WeselAjaConfig,
        private readonly fetchImpl: typeof fetch = fetch
      ) {}

      async createPayment(
        input: CreatePaymentInput
      ): Promise<JsonObject> {
        if (!Number.isSafeInteger(input.amount) || input.amount <= 0) {
          throw new Error(
            "Payment amount must be a positive integer."
          );
        }

        if (!input.customerName.trim()) {
          throw new Error("Customer name is required.");
        }

        if (!input.referenceCode.trim()) {
          throw new Error("Reference code is required.");
        }

        const uri = "/v1/payins";

        const redirectUrl = new URL(this.config.redirectUrl);
        redirectUrl.searchParams.set(
          "referenceCode",
          input.referenceCode.trim(),
        );

        const payload: JsonObject = {
          initiatedAmount: input.amount,
          currency: this.config.currency,
          paymentMethod: this.config.paymentMethod,
          paymentChannel: resolveBankChannel(input.bank),
          referenceCode: input.referenceCode.trim(),
          customerReference: this.config.customerReference,
          customerName: input.customerName.trim(),
          callbackUrl: this.config.callbackUrl,
          redirectUrl: redirectUrl.toString(),
        };

        if (input.customerPhoneNumber?.trim()) {
          payload.customerPhoneNumber =
            input.customerPhoneNumber.trim();
        }

        if (input.description?.trim()) {
          payload.description = input.description.trim();
        }

        const body = JSON.stringify(payload);
        const timestamp = new Date().toISOString();

        const signature = createSignature(
          "POST",
          uri,
          timestamp,
          body,
          this.config.secretKey
        );

        const idempotencyKey =
          input.idempotencyKey?.trim() || `shopify-${randomUUID()}`;

        const url = resolveV1PayinsUrl(this.config.apiUrl);

        const response = await this.fetchImpl(url, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "Accept": "application/json",
            "User-Agent": "WeselAja-Shopify/1.0",
            "Xenith-Api-Key": this.config.apiKey,
            "Xenith-Request-Timestamp": timestamp,
            "Xenith-Request-Signature": signature,
            "X-Idempotency-Key": idempotencyKey,
          },
          body,
        });

        const responseBody = await response.text();
        let decoded: unknown;

        try {
          decoded = JSON.parse(responseBody) as unknown;
        } catch {
          throw new Error(
            `WeselAja returned HTTP ${response.status} ` +
            "with a non-JSON response: " +
            responseBody.slice(0, 500)
          );
        }

        if (!response.ok) {
          let detail = responseBody;

          if (isJsonObject(decoded)) {
            const data = isJsonObject(decoded.data)
              ? decoded.data
              : decoded;

            const message = data.message ?? data.error ?? data.detail;

            if (typeof message === "string") {
              detail = message;
            }
          }

          throw new Error(
            `WeselAja returned HTTP ${response.status}: ${detail}`
          );
        }

        if (!isJsonObject(decoded)) {
          throw new Error("Unexpected WeselAja response format.");
        }

        if (isJsonObject(decoded.data)) {
          return decoded.data;
        }

        return decoded;
      }
    }

    export function createWeselAjaClientFromEnvironment(): WeselAjaClient {
      return new WeselAjaClient(configFromEnvironment());
    }
    """;

  private const string WeselAjaWebhookTemplate = """
    import { createHmac, timingSafeEqual } from "node:crypto";

    export const WESELAJA_CALLBACK_PATH =
      "/payment/weselaja/callback";

    const DEFAULT_UPSTREAM_WEBHOOK_PATH = "/api/webhook/xenith";
    const MAX_TIMESTAMP_SKEW_MS = 5 * 60 * 1000;

    export type SignatureVerification =
      | { valid: true }
      | {
          valid: false;
          reason:
            | "missing_header"
            | "missing_secret"
            | "invalid_timestamp"
            | "expired_timestamp"
            | "invalid_signature";
        };

    type NamedValue = {
      name: string;
      value: string;
    };

    function uniqueNamedValues(values: NamedValue[]): NamedValue[] {
      const seen = new Set<string>();

      return values.filter(({ value }) => {
        if (!value || seen.has(value)) {
          return false;
        }

        seen.add(value);
        return true;
      });
    }

    function parseTimestampMs(timestamp: string): number {
      if (/^\d{10}$/.test(timestamp)) {
        return Number(timestamp) * 1000;
      }

      if (/^\d{13}$/.test(timestamp)) {
        return Number(timestamp);
      }

      return Date.parse(timestamp);
    }

    function getBodyCandidates(rawBody: Buffer): NamedValue[] {
      const rawText = rawBody.toString("utf8");
      let reserializedJson = "";

      try {
        reserializedJson =
          JSON.stringify(JSON.parse(rawText)) ?? "";
      } catch {
        reserializedJson = "";
      }

      return uniqueNamedValues([
        {
          name: "raw",
          value: rawText,
        },
        {
          name: "trimmed",
          value: rawText.trim(),
        },
        {
          name: "normalized-line-endings",
          value: rawText.replace(/\r\n/g, "\n"),
        },
        {
          name: "reserialized-json",
          value: reserializedJson,
        },
      ]);
    }

    function getUriCandidates(): NamedValue[] {
      const configuredUris = (
        process.env.WESELAJA_CALLBACK_SIGNED_URI ?? ""
      )
        .split(",")
        .map((value) => value.trim())
        .filter(Boolean)
        .map((value, index) => ({
          name: `configured-uri-${index + 1}`,
          value,
        }));

      const upstreamBaseUrl =
        process.env.WESELAJA_CALLBACK_SIGNED_BASE_URL
          ?.trim()
          .replace(/\/+$/, "") ?? "";

      const upstreamAbsoluteUrl = upstreamBaseUrl
        ? `${upstreamBaseUrl}${DEFAULT_UPSTREAM_WEBHOOK_PATH}`
        : "";

      return uniqueNamedValues([
        ...configuredUris,
        {
          name: "upstream-absolute-url",
          value: upstreamAbsoluteUrl,
        },
        {
          name: "upstream-webhook-path",
          value: DEFAULT_UPSTREAM_WEBHOOK_PATH,
        },
        {
          name: "shopify-callback-path",
          value: WESELAJA_CALLBACK_PATH,
        },
      ]);
    }

    type PayloadFormat = {
      name: string;
      build: (
        method: string,
        uri: string,
        body: string,
        timestamp: string,
      ) => string;
    };

    const PAYLOAD_FORMATS: PayloadFormat[] = [
      {
        name: "method-uri-body-timestamp",
        build: (method, uri, body, timestamp) =>
          `${method}\n${uri}\n${body}\n${timestamp}`,
      },
      {
        name: "literal-backslash-n",
        build: (method, uri, body, timestamp) =>
          [method, uri, body, timestamp].join("\\n"),
      },
      {
        name: "method-uri-timestamp-body",
        build: (method, uri, body, timestamp) =>
          `${method}\n${uri}\n${timestamp}\n${body}`,
      },
    ];

    function matchesBase64Signature(
      payload: string,
      signature: string,
      secret: string,
    ): boolean {
      const expected = createHmac("sha256", secret)
        .update(payload, "utf8")
        .digest("base64");

      const expectedBuffer = Buffer.from(expected, "utf8");
      const suppliedBuffer = Buffer.from(signature, "utf8");

      return (
        expectedBuffer.length === suppliedBuffer.length &&
        timingSafeEqual(expectedBuffer, suppliedBuffer)
      );
    }

    export function verifyWeselAjaCallbackSignature(
      rawBody: Buffer,
      timestamp: string,
      signature: string,
      secret: string,
      nowMs = Date.now(),
    ): SignatureVerification {
      if (!timestamp || !signature) {
        return { valid: false, reason: "missing_header" };
      }

      if (!secret) {
        return { valid: false, reason: "missing_secret" };
      }

      const timestampMs = parseTimestampMs(timestamp);

      if (!Number.isFinite(timestampMs)) {
        return { valid: false, reason: "invalid_timestamp" };
      }

      if (
        Math.abs(nowMs - timestampMs) >
        MAX_TIMESTAMP_SKEW_MS
      ) {
        return { valid: false, reason: "expired_timestamp" };
      }

      if (!/^[A-Za-z0-9+/]+={0,2}$/.test(signature)) {
        return { valid: false, reason: "invalid_signature" };
      }

      for (const uriCandidate of getUriCandidates()) {
        for (const bodyCandidate of getBodyCandidates(rawBody)) {
          for (const payloadFormat of PAYLOAD_FORMATS) {
            const signedPayload = payloadFormat.build(
              "POST",
              uriCandidate.value,
              bodyCandidate.value,
              timestamp,
            );

            if (
              matchesBase64Signature(
                signedPayload,
                signature,
                secret,
              )
            ) {
              console.info(
                "[WeselAja signature] Candidate matched",
                {
                  uriCandidate: uriCandidate.name,
                  bodyCandidate: bodyCandidate.name,
                  payloadFormat: payloadFormat.name,
                },
              );

              return { valid: true };
            }
          }
        }
      }

      return { valid: false, reason: "invalid_signature" };
    }

    export function verifyWeselAjaCallbackFromEnvironment(
      rawBody: Buffer,
      timestamp: string,
      signature: string,
      nowMs = Date.now(),
    ): SignatureVerification {
      const secret = process.env.WEB_SIGNATURE_SECRET?.trim() ?? "";

      return verifyWeselAjaCallbackSignature(
        rawBody,
        timestamp,
        signature,
        secret,
        nowMs,
      );
    }
    """;

  private const string ShopifyOrderSyncTemplate = """
    import type { WeselAjaPayment } from "@prisma/client";
    import prisma from "../db.server";
    import { unauthenticated } from "../shopify.server";

    export interface OrderSyncResult {
      success: boolean;
      retryable: boolean;
      message: string;
    }

    async function rejectSync(
      paymentId: string,
      message: string,
      retryable = false,
    ): Promise<OrderSyncResult> {
      await prisma.weselAjaPayment.update({
        where: { id: paymentId },
        data: {
          shopifyOrderSyncStatus: retryable ? "FAILED" : "REJECTED",
          shopifyOrderSyncError: message,
        },
      });

      return { success: false, retryable, message };
    }

    export async function syncShopifyOrderPaid(
      payment: WeselAjaPayment,
    ): Promise<OrderSyncResult> {
      if (!payment.shopifyOrderId || !payment.shopifyDomain) {
        return rejectSync(
          payment.id,
          "Shopify order ID or shop domain is missing.",
        );
      }

      try {
        const { admin } = await unauthenticated.admin(
          payment.shopifyDomain,
        );

        const orderResponse = await admin.graphql(
          `#graphql
          query WeselAjaOrderForPayment($id: ID!) {
            order(id: $id) {
              id
              displayFinancialStatus
            }
          }`,
          {
            variables: {
              id: payment.shopifyOrderId,
            },
          },
        );

        const orderJson = await orderResponse.json() as {
          data?: {
            order?: {
              id: string;
              displayFinancialStatus: string;
            } | null;
          };
          errors?: Array<{ message: string }>;
        };

        if (orderJson.errors?.length) {
          throw new Error(
            orderJson.errors.map(error => error.message).join("; "),
          );
        }

        const order = orderJson.data?.order;

        if (!order) {
          return rejectSync(
            payment.id,
            "Shopify order was not found.",
          );
        }

        if (order.displayFinancialStatus === "PAID") {
          await prisma.weselAjaPayment.update({
            where: { id: payment.id },
            data: {
              status: "SUCCESS",
              shopifyOrderPaidAt:
                payment.shopifyOrderPaidAt ?? new Date(),
              shopifyOrderSyncStatus: "SYNCED",
              shopifyOrderSyncError: null,
            },
          });

          return {
            success: true,
            retryable: false,
            message: "Shopify order was already paid.",
          };
        }

        const markResponse = await admin.graphql(
          `#graphql
          mutation WeselAjaMarkOrderPaid($input: OrderMarkAsPaidInput!) {
            orderMarkAsPaid(input: $input) {
              order {
                id
                displayFinancialStatus
              }
              userErrors {
                field
                message
              }
            }
          }`,
          {
            variables: {
              input: {
                id: payment.shopifyOrderId,
              },
            },
          },
        );

        const markJson = await markResponse.json() as {
          data?: {
            orderMarkAsPaid?: {
              order?: {
                id: string;
                displayFinancialStatus: string;
              } | null;
              userErrors?: Array<{
                field?: string[] | null;
                message: string;
              }>;
            };
          };
          errors?: Array<{ message: string }>;
        };

        if (markJson.errors?.length) {
          throw new Error(
            markJson.errors.map(error => error.message).join("; "),
          );
        }

        const mutation = markJson.data?.orderMarkAsPaid;
        const userErrors = mutation?.userErrors ?? [];

        if (userErrors.length) {
          return rejectSync(
            payment.id,
            userErrors.map(error => error.message).join("; "),
          );
        }

        if (mutation?.order?.displayFinancialStatus !== "PAID") {
          throw new Error(
            "Shopify did not confirm the order as paid.",
          );
        }

        await prisma.weselAjaPayment.update({
          where: { id: payment.id },
          data: {
            status: "SUCCESS",
            shopifyOrderPaidAt: new Date(),
            shopifyOrderSyncStatus: "SYNCED",
            shopifyOrderSyncError: null,
          },
        });

        return {
          success: true,
          retryable: false,
          message: "Shopify order was marked as paid.",
        };
      } catch (error) {
        return rejectSync(
          payment.id,
          error instanceof Error
            ? error.message
            : "Unknown Shopify synchronization error.",
          true,
        );
      }
    }
    """;

  private const string AppLayoutTemplate = """
    import type { HeadersFunction, LoaderFunctionArgs } from "react-router";
    import { Link, Outlet, useRouteError, useLoaderData } from "react-router";
    import { AppProvider } from "@shopify/shopify-app-react-router/react";
    import { boundary } from "@shopify/shopify-app-react-router/server";
    import { authenticate } from "../shopify.server";

    export async function loader({ request }: LoaderFunctionArgs) {
      await authenticate.admin(request);

      return {
        apiKey: process.env.SHOPIFY_API_KEY || "",
      };
    }

    export default function App() {
      const { apiKey } = useLoaderData<typeof loader>();

      return (
        <AppProvider embedded apiKey={apiKey}>
          <nav>
            <Link to="/app">WeselAja Payments</Link>
          </nav>
          <Outlet />
        </AppProvider>
      );
    }

    export function ErrorBoundary() {
      return boundary.error(useRouteError());
    }

    export const headers: HeadersFunction = (args) =>
      boundary.headers(args);
    """;

  private const string AppIndexTemplate = """
    import type {
      ActionFunctionArgs,
      LoaderFunctionArgs,
    } from "react-router";
    import { useFetcher, useLoaderData } from "react-router";
    import prisma from "../db.server";
    import {
      configFromEnvironment,
      WeselAjaClient,
    } from "../lib/weselaja.server";
    import { authenticate } from "../shopify.server";

    function errorMessage(error: unknown): string {
      return error instanceof Error
        ? error.message
        : "Unexpected error.";
    }

    function failure(
      error: string,
      shopifyOrderName: string | null = null
    ) {
      return {
        ok: false as const,
        error,
        shopifyOrderName,
        payment: null,
      };
    }

    export async function loader({ request }: LoaderFunctionArgs) {
      const { session } = await authenticate.admin(request);

      const payments = await prisma.weselAjaPayment.findMany({
        where: { shopifyDomain: session.shop },
        orderBy: { createdAt: "desc" },
        take: 20,
      });

      return {
        payments: payments.map(payment => ({
          id: payment.id,
          orderName: payment.shopifyOrderName,
          amount: payment.shopifyOrderAmount,
          currency: payment.shopifyOrderCurrency,
          referenceCode: payment.referenceCode,
          status: payment.status,
          paymentCode: payment.paymentCode,
          channel: payment.paymentChannel,
          syncStatus: payment.shopifyOrderSyncStatus,
          syncError: payment.shopifyOrderSyncError,
          createdAt: payment.createdAt.toISOString(),
        })),
      };
    }

    export async function action({ request }: ActionFunctionArgs) {
      const { admin, session } = await authenticate.admin(request);
      const formData = await request.formData();

      const customerName = String(
        formData.get("customerName") ?? ""
      ).trim();

      const customerEmail = String(
        formData.get("customerEmail") ?? ""
      ).trim();

      const itemTitle = String(
        formData.get("itemTitle") ?? "WeselAja order"
      ).trim();

      const bank = String(
        formData.get("bank") ?? "BCA"
      ).trim();

      const amount = Number(
        String(formData.get("amount") ?? "").trim()
      );

      if (!customerName) {
        return failure("Customer name is required.");
      }

      if (!Number.isSafeInteger(amount) || amount <= 0) {
        return failure("Amount must be a positive whole number in IDR.");
      }

      if (!itemTitle) {
        return failure("Order item title is required.");
      }

      let createdOrderName: string | null = null;

      try {
        const config = configFromEnvironment();

        if (config.currency !== "IDR") {
          return failure(
            "This test order flow currently requires IDR currency."
          );
        }

        const draftResponse = await admin.graphql(
          `#graphql
          mutation WeselAjaCreateDraftOrder($input: DraftOrderInput!) {
            draftOrderCreate(input: $input) {
              draftOrder {
                id
                name
              }
              userErrors {
                field
                message
              }
            }
          }`,
          {
            variables: {
              input: {
                email: customerEmail || undefined,
                note: `WeselAja payment for ${customerName}`,
                tags: ["weselaja", "payment-pending"],
                lineItems: [
                  {
                    title: itemTitle,
                    quantity: 1,
                    originalUnitPriceWithCurrency: {
                      amount: String(amount),
                      currencyCode: config.currency,
                    },
                    requiresShipping: false,
                    taxable: false,
                  },
                ],
              },
            },
          }
        );

        const draftJson = await draftResponse.json() as {
          data?: {
            draftOrderCreate?: {
              draftOrder?: {
                id: string;
                name: string;
              } | null;
              userErrors?: Array<{ message: string }>;
            };
          };
          errors?: Array<{ message: string }>;
        };

        if (draftJson.errors?.length) {
          throw new Error(
            draftJson.errors.map(error => error.message).join("; ")
          );
        }

        const draftPayload = draftJson.data?.draftOrderCreate;

        if (!draftPayload?.draftOrder || draftPayload.userErrors?.length) {
          throw new Error(
            draftPayload?.userErrors
              ?.map(error => error.message)
              .join("; ") || "Shopify did not create the draft order."
          );
        }

        const completeResponse = await admin.graphql(
          `#graphql
          mutation WeselAjaCompleteDraftOrder($id: ID!) {
            draftOrderComplete(id: $id, paymentPending: true) {
              draftOrder {
                order {
                  id
                  name
                  displayFinancialStatus
                  totalOutstandingSet {
                    shopMoney {
                      amount
                      currencyCode
                    }
                  }
                }
              }
              userErrors {
                field
                message
              }
            }
          }`,
          {
            variables: {
              id: draftPayload.draftOrder.id,
            },
          }
        );

        const completeJson = await completeResponse.json() as {
          data?: {
            draftOrderComplete?: {
              draftOrder?: {
                order?: {
                  id: string;
                  name: string;
                  displayFinancialStatus: string;
                  totalOutstandingSet?: {
                    shopMoney?: {
                      amount?: string;
                      currencyCode?: string;
                    };
                  };
                } | null;
              } | null;
              userErrors?: Array<{ message: string }>;
            };
          };
          errors?: Array<{ message: string }>;
        };

        if (completeJson.errors?.length) {
          throw new Error(
            completeJson.errors.map(error => error.message).join("; ")
          );
        }

        const completePayload = completeJson.data?.draftOrderComplete;

        if (
          !completePayload?.draftOrder?.order ||
          completePayload.userErrors?.length
        ) {
          throw new Error(
            completePayload?.userErrors
              ?.map(error => error.message)
              .join("; ") || "Shopify did not complete the draft order."
          );
        }

        const order = completePayload.draftOrder.order;
        createdOrderName = order.name;

        const outstanding = order.totalOutstandingSet?.shopMoney;

        if (
          !outstanding?.amount ||
          outstanding.currencyCode !== config.currency
        ) {
          throw new Error(
            "Shopify order currency or outstanding balance is invalid."
          );
        }

        const orderAmount = Number(outstanding.amount);

        if (!Number.isSafeInteger(orderAmount) || orderAmount <= 0) {
          throw new Error(
            "Shopify outstanding amount must be a positive whole IDR amount."
          );
        }

        const orderNumericId = order.id.split("/").pop();

        if (!orderNumericId) {
          throw new Error("Shopify order ID is invalid.");
        }

        const referenceCode = `SHOPIFY-${orderNumericId}`;

        const savedPayment = await prisma.weselAjaPayment.create({
          data: {
            shopifyDomain: session.shop,
            shopifyOrderId: order.id,
            shopifyOrderName: order.name,
            shopifyOrderAmount: outstanding.amount,
            shopifyOrderCurrency: outstanding.currencyCode,
            customerName,
            customerEmail: customerEmail || null,
            bank,
            referenceCode,
            initiatedAmount: outstanding.amount,
            currency: config.currency,
            paymentMethod: config.paymentMethod,
            paymentChannel: bank,
            callbackUrl: config.callbackUrl,
            redirectUrl: config.redirectUrl,
            status: "CREATING",
            shopifyOrderSyncStatus: "PENDING",
          },
        });

        try {
          const client = new WeselAjaClient(config);

          const result = await client.createPayment({
            amount: orderAmount,
            bank,
            customerName,
            referenceCode,
            idempotencyKey: referenceCode,
            description: `WeselAja payment for Shopify ${order.name}`,
          });

          const readString = (
            value: unknown,
            fallback: string
          ): string =>
            typeof value === "string" && value.length > 0
              ? value
              : fallback;

          const expirationTime =
            typeof result.expirationTime === "string" &&
            Number.isFinite(Date.parse(result.expirationTime))
              ? new Date(result.expirationTime)
              : null;

          const updatedPayment = await prisma.weselAjaPayment.update({
            where: { id: savedPayment.id },
            data: {
              payinId:
                typeof result.id === "string" ? result.id : null,
              initiatedAmount: readString(
                result.initiatedAmount,
                outstanding.amount
              ),
              paymentAmount: readString(result.paymentAmount, "0"),
              feeAmount:
                typeof result.feeAmount === "string"
                  ? result.feeAmount
                  : null,
              currency: readString(result.currency, config.currency),
              paymentMethod: readString(
                result.paymentMethod,
                config.paymentMethod
              ),
              paymentChannel: readString(
                result.paymentChannel,
                bank
              ),
              paymentCode:
                typeof result.paymentCode === "string"
                  ? result.paymentCode
                  : null,
              paymentCodeType:
                typeof result.paymentCodeType === "string"
                  ? result.paymentCodeType
                  : null,
              status: readString(
                result.status,
                "PENDING"
              ).toUpperCase(),
              expirationTime,
              creationError: null,
            },
          });

          return {
            ok: true as const,
            error: null,
            shopifyOrderName: order.name,
            payment: {
              id: updatedPayment.id,
              referenceCode: updatedPayment.referenceCode,
              status: updatedPayment.status,
              paymentCode: updatedPayment.paymentCode,
              paymentChannel: updatedPayment.paymentChannel,
            },
          };
        } catch (error) {
          await prisma.weselAjaPayment.update({
            where: { id: savedPayment.id },
            data: {
              status: "CREATE_FAILED",
              creationError: errorMessage(error),
            },
          });

          return failure(
            `Shopify order ${order.name} was created, but WeselAja payment creation failed: ${errorMessage(error)}`,
            order.name
          );
        }
      } catch (error) {
        return failure(
          `Could not create Shopify order and payment: ${errorMessage(error)}`,
          createdOrderName
        );
      }
    }

    export default function Index() {
      const { payments } = useLoaderData<typeof loader>();
      const fetcher = useFetcher<typeof action>();
      const busy = fetcher.state !== "idle";

      return (
        <main style={{ padding: 24, maxWidth: 960, margin: "0 auto" }}>
          <h1>WeselAja Payments</h1>

          <p>
            Create a Shopify order and a corresponding WeselAja Pay In
            transaction. An order is marked paid only after the callback signature
            is verified and the transaction status confirms successful payment.
          </p>

          <fetcher.Form method="post">
            <fieldset disabled={busy} style={{ display: "grid", gap: 12 }}>
              <label>
                Customer name
                <input name="customerName" required />
              </label>

              <label>
                Customer email
                <input name="customerEmail" type="email" />
              </label>

              <label>
                Item title
                <input
                  name="itemTitle"
                  defaultValue="WeselAja test order"
                  required
                />
              </label>

              <label>
                Amount (IDR)
                <input
                  name="amount"
                  type="number"
                  min="1"
                  step="1"
                  required
                />
              </label>

              <label>
                Payment bank
                <select name="bank" defaultValue="BCA">
                  <option value="BRI">BRI</option>
                  <option value="MANDIRI">Mandiri</option>
                  <option value="BNI">BNI</option>
                  <option value="PERMATA">Permata</option>
                  <option value="CIMB">CIMB</option>
                  <option value="BCA">BCA</option>
                </select>
              </label>

              <button type="submit">
                {busy ? "Creating order and payment..." : "Create order and Pay In"}
              </button>
            </fieldset>
          </fetcher.Form>

          {fetcher.data?.error && (
            <p role="alert">{fetcher.data.error}</p>
          )}

          {fetcher.data?.payment && (
            <section>
              <h2>Payment created</h2>
              <p>Order: {fetcher.data.shopifyOrderName}</p>
              <p>Reference: {fetcher.data.payment.referenceCode}</p>
              <p>Status: {fetcher.data.payment.status}</p>
              <p>Channel: {fetcher.data.payment.paymentChannel}</p>

              {fetcher.data.payment.paymentCode && (
                <p>
                  <a
                    href={fetcher.data.payment.paymentCode}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Open payment page
                  </a>
                </p>
              )}
            </section>
          )}

          <h2>Recent payments</h2>

          {payments.length === 0 ? (
            <p>No payments have been created yet.</p>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Order</th>
                  <th>Amount</th>
                  <th>Bank</th>
                  <th>Payment status</th>
                  <th>Shopify sync</th>
                  <th>Reference</th>
                </tr>
              </thead>
              <tbody>
                {payments.map(payment => (
                  <tr key={payment.id}>
                    <td>{payment.orderName}</td>
                    <td>{payment.currency} {payment.amount}</td>
                    <td>{payment.channel}</td>
                    <td>{payment.status}</td>
                    <td>
                      {payment.syncStatus}
                      {payment.syncError
                        ? `: ${payment.syncError}`
                        : ""}
                    </td>
                    <td>{payment.referenceCode}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </main>
      );
    }
    """;

  private const string IndexRouteTemplate = """
    import type { LoaderFunctionArgs } from "react-router";
    import { redirect } from "react-router";

    export async function loader({ request }: LoaderFunctionArgs) {
      const url = new URL(request.url);

      if (url.searchParams.has("shop")) {
        throw redirect(`/app?${url.searchParams.toString()}`);
      }

      return null;
    }

    export default function Index() {
      return (
        <main
          style={{
            maxWidth: "640px",
            margin: "64px auto",
            padding: "24px",
            fontFamily: "system-ui, sans-serif",
          }}
        >
          <h1>WeselAja Payment App</h1>
          <p>
            Aplikasi integrasi pembayaran WeselAja untuk Shopify.
          </p>
          <p>
            Silakan buka aplikasi melalui Shopify Admin untuk melanjutkan.
          </p>
        </main>
      );
    }
    """;

  private const string AuthRouteTemplate = """
    import type { LoaderFunctionArgs } from "react-router";
    import { authenticate } from "../shopify.server";

    export async function loader({ request }: LoaderFunctionArgs) {
      await authenticate.admin(request);
      return null;
    }
    """;

  private const string CallbackRouteTemplate = """
    import { createHash } from "node:crypto";
    import type {
      ActionFunctionArgs,
      LoaderFunctionArgs,
    } from "react-router";

    import prisma from "../db.server";
    import {
      verifyWeselAjaCallbackSignature,
    } from "../lib/weselaja-webhook.server";
    import { syncShopifyOrderPaid } from "../lib/shopify-order-sync.server";

    type JsonObject = Record<string, unknown>;

    type HeaderAttempt = {
      label: string;
      timestamp: string;
      signature: string;
      result: ReturnType<typeof verifyWeselAjaCallbackSignature>;
    };

    function isJsonObject(value: unknown): value is JsonObject {
      return (
        typeof value === "object" &&
        value !== null &&
        !Array.isArray(value)
      );
    }

    function getString(
      payload: JsonObject,
      ...keys: string[]
    ): string | null {
      for (const key of keys) {
        const value = payload[key];

        if (typeof value === "string" && value.trim()) {
          return value.trim();
        }

        if (typeof value === "number" && Number.isFinite(value)) {
          return String(value);
        }
      }

      return null;
    }

    function getPayloadString(
      root: JsonObject,
      data: JsonObject,
      ...keys: string[]
    ): string | null {
      return getString(data, ...keys) ?? getString(root, ...keys);
    }

    function getTimestampFormat(timestamp: string): string {
      if (!timestamp) return "missing";
      if (/^\d{10}$/.test(timestamp)) return "unix-seconds";
      if (/^\d{13}$/.test(timestamp)) return "unix-milliseconds";

      return Number.isFinite(Date.parse(timestamp))
        ? "date-string"
        : "unrecognized";
    }

    function getSignatureFormat(signature: string): string {
      if (!signature) return "missing";
      if (/^[A-Fa-f0-9]{64}$/.test(signature)) return "hex-64";

      if (/^[A-Za-z0-9+/]+={0,2}$/.test(signature)) {
        return "base64-like";
      }

      return "other";
    }

    export function loader(_args: LoaderFunctionArgs) {
      return Response.json(
        { success: false, error: "POST required." },
        {
          status: 405,
          headers: { Allow: "POST" },
        },
      );
    }

    export async function action({ request }: ActionFunctionArgs) {
      const requestUrl = new URL(request.url);

      console.info("[WeselAja callback] Request received", {
        method: request.method,
        path: requestUrl.pathname,
      });

      if (request.method !== "POST") {
        return Response.json(
          { success: false, error: "POST required." },
          {
            status: 405,
            headers: { Allow: "POST" },
          },
        );
      }

      const rawBody = Buffer.from(await request.arrayBuffer());

      if (rawBody.byteLength > 256 * 1024) {
        return Response.json(
          { success: false, error: "Callback body is too large." },
          { status: 413 },
        );
      }

      console.info("[WeselAja callback] Request details", {
        bodyBytes: rawBody.byteLength,
        relevantHeaderNames: [...request.headers.keys()].filter(
          (name) =>
            /timestamp|signature|middleware|webhook|weselaja|xenith/i.test(
              name,
            ),
        ),
      });

      const secret = process.env.WEB_SIGNATURE_SECRET?.trim() ?? "";

      const headerPairDefinitions: Array<{
        label: string;
        timestampHeader?: string;
        signatureHeader?: string;
      }> = [
        {
          label: "configured",
          timestampHeader:
            process.env.WESELAJA_CALLBACK_TIMESTAMP_HEADER?.trim(),
          signatureHeader:
            process.env.WESELAJA_CALLBACK_SIGNATURE_HEADER?.trim(),
        },
        {
          label: "x-middleware",
          timestampHeader: "x-middleware-timestamp",
          signatureHeader: "x-middleware-signature",
        },
        {
          label: "x-webhook",
          timestampHeader: "x-webhook-timestamp",
          signatureHeader: "x-webhook-signature",
        },
        {
          label: "x-xenith",
          timestampHeader: "x-xenith-timestamp",
          signatureHeader: "x-xenith-signature",
        },
        {
          label: "x-weselaja",
          timestampHeader: "x-weselaja-timestamp",
          signatureHeader: "x-weselaja-signature",
        },
        {
          label: "x-client",
          timestampHeader: "x-client-timestamp",
          signatureHeader: "x-client-signature",
        },
        {
          label: "client",
          timestampHeader: "client-timestamp",
          signatureHeader: "client-signature",
        },
        {
          label: "xenith-request",
          timestampHeader: "xenith-request-timestamp",
          signatureHeader: "xenith-request-signature",
        },
      ];

      const seenHeaderPairs = new Set<string>();
      const headerAttempts: HeaderAttempt[] = [];
      let validPair: HeaderAttempt | undefined;

      for (const pair of headerPairDefinitions) {
        if (!pair.timestampHeader || !pair.signatureHeader) {
          continue;
        }

        const pairKey =
          `${pair.timestampHeader.toLowerCase()}|` +
          pair.signatureHeader.toLowerCase();

        if (seenHeaderPairs.has(pairKey)) {
          continue;
        }

        seenHeaderPairs.add(pairKey);

        const timestamp =
          request.headers.get(pair.timestampHeader) ?? "";

        const signature =
          request.headers.get(pair.signatureHeader) ?? "";

        if (!timestamp && !signature) {
          continue;
        }

        const attempt: HeaderAttempt = {
          label: pair.label,
          timestamp,
          signature,
          result: verifyWeselAjaCallbackSignature(
            rawBody,
            timestamp,
            signature,
            secret,
          ),
        };

        headerAttempts.push(attempt);

        if (attempt.result.valid) {
          validPair = attempt;
          break;
        }
      }

      if (!validPair) {
        const failure =
          headerAttempts.find(
            (attempt) =>
              !attempt.result.valid &&
              attempt.result.reason !== "missing_header",
          )?.result ??
          headerAttempts[0]?.result ??
          verifyWeselAjaCallbackSignature(rawBody, "", "", secret);

        const reason = failure.valid
          ? "invalid_signature"
          : failure.reason;

        console.warn(
          "[WeselAja callback] Signature verification failed",
          {
            reason,
            secretConfigured: Boolean(secret),
            attemptedHeaderPairs: headerAttempts.map((attempt) => ({
              headerPair: attempt.label,
              timestampFormat: getTimestampFormat(attempt.timestamp),
              signatureFormat: getSignatureFormat(attempt.signature),
              result: attempt.result.valid
                ? "valid"
                : attempt.result.reason,
            })),
          },
        );

        return Response.json(
          {
            success: false,
            error: "Invalid callback signature.",
            reason,
          },
          { status: 401 },
        );
      }

      console.info("[WeselAja callback] Signature verified", {
        headerPair: validPair.label,
      });

      let decoded: unknown;

      try {
        decoded = JSON.parse(rawBody.toString("utf8")) as unknown;
      } catch {
        return Response.json(
          { success: false, error: "Invalid callback JSON." },
          { status: 400 },
        );
      }

      if (!isJsonObject(decoded)) {
        return Response.json(
          { success: false, error: "Invalid callback payload." },
          { status: 400 },
        );
      }

      const root = decoded;
      const data = isJsonObject(root.data) ? root.data : root;

      const payinId = getPayloadString(
        root,
        data,
        "id",
        "payinId",
        "payInId",
        "paymentId",
      );

      const referenceCode = getPayloadString(
        root,
        data,
        "referenceCode",
        "reference_code",
      );

      const status = (
        getPayloadString(
          root,
          data,
          "status",
          "paymentStatus",
          "payment_status",
        ) ?? ""
      ).toUpperCase();

      const paidAmount = getPayloadString(
        root,
        data,
        "paymentAmount",
        "paidAmount",
        "amount",
      );

      const feeAmount = getPayloadString(root, data, "feeAmount");

      const initiatedAmount = getPayloadString(
        root,
        data,
        "initiatedAmount",
      );

      const callbackCurrency = getPayloadString(
        root,
        data,
        "currency",
        "currencyCode",
      );

      if (!status || (!payinId && !referenceCode)) {
        console.warn(
          "[WeselAja callback] Missing transaction identity",
          {
            hasStatus: Boolean(status),
            hasPayinId: Boolean(payinId),
            hasReferenceCode: Boolean(referenceCode),
          },
        );

        return Response.json(
          {
            success: false,
            error:
              "Callback is missing transaction identity or status.",
          },
          { status: 400 },
        );
      }

      const eventKey = createHash("sha256")
        .update(rawBody)
        .digest("hex");

      const existingEvent =
        await prisma.weselAjaCallback.findUnique({
          where: { eventKey },
        });

      if (existingEvent?.processingResult === "synced") {
        return Response.json({
          success: true,
          duplicate: true,
          processingResult: "synced",
        });
      }

      let payment = payinId
        ? await prisma.weselAjaPayment.findUnique({
            where: { payinId },
          })
        : null;

      if (!payment && referenceCode) {
        payment = await prisma.weselAjaPayment.findUnique({
          where: { referenceCode },
        });
      }

      const event = await prisma.weselAjaCallback.upsert({
        where: { eventKey },
        create: {
          eventKey,
          paymentId: payment?.id ?? null,
          payinId,
          referenceCode,
          status,
          signatureValid: true,
          payload: rawBody.toString("utf8"),
          processingResult: "received",
        },
        update: {
          paymentId: payment?.id ?? null,
          payinId,
          referenceCode,
          status,
          signatureValid: true,
          payload: rawBody.toString("utf8"),
          processingResult: "received",
        },
      });

      if (!payment) {
        console.warn("[WeselAja callback] No matching payment", {
          hasPayinId: Boolean(payinId),
          hasReferenceCode: Boolean(referenceCode),
          status,
        });

        await prisma.weselAjaCallback.update({
          where: { id: event.id },
          data: {
            processingResult: "no_matching_payment",
            processedAt: new Date(),
          },
        });

        return Response.json(
          {
            success: true,
            matched: false,
            referenceCode,
          },
          { status: 202 },
        );
      }

      const successStatuses = [
        "SUCCESS",
        "SUCCESSFUL",
        "PAID",
        "COMPLETED",
        "SETTLED",
      ];

      const isSuccess = successStatuses.includes(status);

      await prisma.weselAjaPayment.update({
        where: { id: payment.id },
        data: {
          status,
          ...(paidAmount ? { paymentAmount: paidAmount } : {}),
          ...(feeAmount ? { feeAmount } : {}),
          ...(initiatedAmount ? { initiatedAmount } : {}),
        },
      });

      if (!isSuccess) {
        await prisma.weselAjaCallback.update({
          where: { id: event.id },
          data: {
            processingResult: `received_${status.toLowerCase()}`,
            processedAt: new Date(),
          },
        });

        console.info(
          "[WeselAja callback] Non-success status received",
          { status },
        );

        return Response.json({
          success: true,
          status,
          orderSync: "not_required",
        });
      }

      const currency = callbackCurrency ?? payment.currency;

      console.info(
        "[WeselAja callback] Starting Shopify order sync",
        {
          payinId,
          referenceCode,
          providerPaymentAmount: paidAmount,
          providerFeeAmount: feeAmount,
          providerInitiatedAmount: initiatedAmount,
          currency,
        },
      );

      const syncResult = await syncShopifyOrderPaid(payment);

      const processingResult = syncResult.success
        ? "synced"
        : syncResult.retryable
          ? "shopify_sync_failed"
          : "rejected";

      await prisma.weselAjaCallback.update({
        where: { id: event.id },
        data: {
          processingResult,
          processedAt: new Date(),
        },
      });

      console.info(
        "[WeselAja callback] Shopify sync finished",
        {
          success: syncResult.success,
          retryable: syncResult.retryable,
          processingResult,
          providerPaymentAmount: paidAmount,
          providerFeeAmount: feeAmount,
          currency,
        },
      );

      return Response.json(
        {
          success: true,
          status,
          orderSync: syncResult.success
            ? "synced"
            : syncResult.retryable
              ? "failed"
              : "rejected",
          message: syncResult.message,
        },
        { status: syncResult.retryable ? 500 : 200 },
      );
    }
    """;

  private const string ReturnRouteTemplate = """
    import type { LoaderFunctionArgs } from "react-router";
    import { useLoaderData } from "react-router";
    import prisma from "../db.server";

    export async function loader({ request }: LoaderFunctionArgs) {
      const url = new URL(request.url);

      const referenceCode =
        url.searchParams.get("referenceCode") ??
        url.searchParams.get("reference_code") ??
        url.searchParams.get("reference");

      const payment = referenceCode
        ? await prisma.weselAjaPayment.findUnique({
            where: { referenceCode },
          })
        : null;

      return {
        payment: payment
          ? {
              orderName: payment.shopifyOrderName,
              referenceCode: payment.referenceCode,
              status: payment.status,
              syncStatus: payment.shopifyOrderSyncStatus,
            }
          : null,
      };
    }

    export default function PaymentReturn() {
      const { payment } = useLoaderData<typeof loader>();

      return (
        <main
          style={{
            maxWidth: 640,
            margin: "40px auto",
            padding: 24,
            fontFamily: "Arial, sans-serif",
            lineHeight: 1.6,
          }}
        >
          <h1>WeselAja payment result</h1>

          {!payment ? (
            <p>
              Payment details are not available from this return URL.
              Open the Shopify app to check the transaction status.
            </p>
          ) : (
            <>
              <p>Order: {payment.orderName}</p>
              <p>Reference: {payment.referenceCode}</p>
              <p>Payment status: {payment.status}</p>
              <p>Shopify synchronization: {payment.syncStatus}</p>
            </>
          )}

          <p>
            The return page displays stored payment information. The
            browser redirect itself does not confirm payment success.
          </p>
        </main>
      );
    }
    """;

  private const string UninstalledWebhookTemplate = """
    import type { ActionFunctionArgs } from "react-router";
    import prisma from "../db.server";
    import { authenticate } from "../shopify.server";

    export async function action({ request }: ActionFunctionArgs) {
      const { shop, topic } = await authenticate.webhook(request);

      if (topic === "APP_UNINSTALLED") {
        await prisma.session.deleteMany({
          where: { shop },
        });
      }

      return new Response(null, { status: 200 });
    }
    """;

  private const string ScopeUpdateWebhookTemplate = """
    import type { ActionFunctionArgs } from "react-router";
    import prisma from "../db.server";
    import { authenticate } from "../shopify.server";

    export async function action({ request }: ActionFunctionArgs) {
      const { shop, topic, payload } =
        await authenticate.webhook(request);

      if (topic === "APP_SCOPES_UPDATE") {
        const current = (payload as { current?: string[] }).current;

        if (Array.isArray(current)) {
          await prisma.session.updateMany({
            where: { shop },
            data: { scope: current.join(",") },
          });
        }
      }

      return new Response(null, { status: 200 });
    }
    """;

  private const string PrismaSchemaTemplate = """
    generator client {
      provider = "prisma-client-js"
    }

    datasource db {
      provider = "sqlite"
      url      = "file:dev.sqlite"
    }

    model Session {
      id                  String    @id
      shop                String
      state               String
      isOnline            Boolean   @default(false)
      scope               String?
      expires              DateTime?
      accessToken         String
      userId              BigInt?
      firstName           String?
      lastName            String?
      email               String?
      accountOwner        Boolean   @default(false)
      locale              String?
      collaborator        Boolean?  @default(false)
      emailVerified       Boolean?  @default(false)
      refreshToken        String?
      refreshTokenExpires DateTime?
    }

    model WeselAjaPayment {
      id                     String   @id @default(cuid())
      shopifyDomain          String
      shopifyOrderId         String
      shopifyOrderName       String
      shopifyOrderAmount     String
      shopifyOrderCurrency   String
      customerName           String
      customerEmail          String?
      bank                   String
      referenceCode          String   @unique
      payinId                String?  @unique
      initiatedAmount        String
      paymentAmount          String?
      feeAmount              String?
      currency               String
      paymentMethod          String
      paymentChannel         String
      paymentCode            String?
      paymentCodeType        String?
      callbackUrl            String
      redirectUrl            String
      expirationTime         DateTime?
      status                 String   @default("CREATING")
      shopifyOrderPaidAt     DateTime?
      shopifyOrderSyncStatus String   @default("PENDING")
      shopifyOrderSyncError  String?
      creationError          String?
      createdAt              DateTime @default(now())
      updatedAt              DateTime @updatedAt

      callbacks WeselAjaCallback[]

      @@unique([shopifyDomain, shopifyOrderId])
      @@index([shopifyDomain, createdAt])
    }

    model WeselAjaCallback {
      id                String   @id @default(cuid())
      eventKey           String   @unique
      paymentId          String?
      payinId            String?
      referenceCode      String?
      status             String?
      signatureValid     Boolean  @default(false)
      signatureReason    String?
      payload            String
      processingResult   String?
      receivedAt         DateTime @default(now())
      processedAt        DateTime?

      payment WeselAjaPayment? @relation(fields: [paymentId], references: [id], onDelete: SetNull)

      @@index([referenceCode])
      @@index([payinId])
    }
    """;

  private const string TestsTemplate = """
    import assert from "node:assert/strict";
    import { createHmac } from "node:crypto";
    import { test } from "node:test";

    import { resolveBankChannel } from "../app/lib/weselaja.server.js";
    import {
      verifyWeselAjaCallbackSignature,
    } from "../app/lib/weselaja-webhook.server.js";

    function callbackSignature(
      body: Buffer,
      timestamp: string,
      secret: string,
    ): string {
      const prefix = Buffer.from(
        "POST\n/payment/weselaja/callback\n",
        "utf8",
      );
      const suffix = Buffer.from(`\n${timestamp}`, "utf8");

      return createHmac("sha256", secret)
        .update(Buffer.concat([prefix, body, suffix]))
        .digest("base64");
    }

    test("maps supported bank names to payment channels", () => {
      assert.equal(resolveBankChannel("BRI"), "BRI.VA");
      assert.equal(resolveBankChannel("Mandiri"), "MDR.VA");
      assert.equal(resolveBankChannel("BNI"), "BNI.VA");
      assert.equal(resolveBankChannel("Permata"), "PTB.VA");
      assert.equal(resolveBankChannel("CIMB"), "CIMB.VA");
      assert.equal(resolveBankChannel("BCA"), "BCA.VA");
    });

    test("accepts an explicit supported payment channel", () => {
      assert.equal(resolveBankChannel("BCA.VA"), "BCA.VA");
      assert.equal(resolveBankChannel("MDR.VA"), "MDR.VA");
    });

    test("rejects unsupported bank names", () => {
      assert.throws(
        () => resolveBankChannel("UNKNOWN"),
        /Unsupported WeselAja bank/,
      );
    });

    test("accepts a callback using the standard newline payload", () => {
      const timestamp = "2026-01-01T00:00:00.000Z";
      const body = Buffer.from(
        "{\"status\":\"SUCCESS\"}",
        "utf8",
      );
      const secret = "test-webhook-secret";

      assert.deepEqual(
        verifyWeselAjaCallbackSignature(
          body,
          timestamp,
          callbackSignature(body, timestamp, secret),
          secret,
          Date.parse(timestamp),
        ),
        { valid: true },
      );
    });

    test("accepts a callback using literal backslash-n separators", () => {
      const timestamp = "2026-01-01T00:00:00.000Z";
      const body = Buffer.from(
        "{\"status\":\"SUCCESS\"}",
        "utf8",
      );
      const secret = "test-webhook-secret";

      const payload = [
        "POST",
        "/payment/weselaja/callback",
        body.toString("utf8"),
        timestamp,
      ].join("\\n");

      const signature = createHmac("sha256", secret)
        .update(payload, "utf8")
        .digest("base64");

      assert.deepEqual(
        verifyWeselAjaCallbackSignature(
          body,
          timestamp,
          signature,
          secret,
          Date.parse(timestamp),
        ),
        { valid: true },
      );
    });

    test("rejects an incorrect callback signature", () => {
      const timestamp = "2026-01-01T00:00:00.000Z";

      assert.deepEqual(
        verifyWeselAjaCallbackSignature(
          Buffer.from("{}", "utf8"),
          timestamp,
          "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
          "test-secret",
          Date.parse(timestamp),
        ),
        { valid: false, reason: "invalid_signature" },
      );
    });

    test("rejects a callback signed with a different secret", () => {
      const timestamp = "2026-01-01T00:00:00.000Z";
      const body = Buffer.from(
        "{\"status\":\"SUCCESS\"}",
        "utf8",
      );

      assert.deepEqual(
        verifyWeselAjaCallbackSignature(
          body,
          timestamp,
          callbackSignature(body, timestamp, "sender-secret"),
          "different-secret",
          Date.parse(timestamp),
        ),
        { valid: false, reason: "invalid_signature" },
      );
    });

    test("rejects a callback when its raw body changes", () => {
      const timestamp = "2026-01-01T00:00:00.000Z";
      const signedBody = Buffer.from(
        "{\"status\":\"SUCCESS\"}",
        "utf8",
      );
      const receivedBody = Buffer.from(
        "{\"status\":\"PENDING\"}",
        "utf8",
      );
      const secret = "test-secret";

      assert.deepEqual(
        verifyWeselAjaCallbackSignature(
          receivedBody,
          timestamp,
          callbackSignature(signedBody, timestamp, secret),
          secret,
          Date.parse(timestamp),
        ),
        { valid: false, reason: "invalid_signature" },
      );
    });

    test("rejects an expired timestamp", () => {
      const timestamp = "2020-01-01T00:00:00.000Z";

      assert.deepEqual(
        verifyWeselAjaCallbackSignature(
          Buffer.from("{}", "utf8"),
          timestamp,
          "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
          "test-secret",
          Date.parse("2026-01-01T00:00:00.000Z"),
        ),
        { valid: false, reason: "expired_timestamp" },
      );
    });

    test("rejects a missing webhook secret", () => {
      const timestamp = "2026-01-01T00:00:00.000Z";

      assert.deepEqual(
        verifyWeselAjaCallbackSignature(
          Buffer.from("{}", "utf8"),
          timestamp,
          "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
          "",
          Date.parse(timestamp),
        ),
        { valid: false, reason: "missing_secret" },
      );
    });
    """;

  private const string VerifyConfigScript = """
    $ErrorActionPreference = "Stop"

    $requiredFiles = @(
        "shopify.app.toml",
        "shopify.web.toml",
        "vite.config.ts",
        "app/root.tsx",
        "app/routes/_index/route.tsx",
        "app/routes/app._index.tsx",
        "app/routes/payment.weselaja.callback.tsx",
        "app/routes/payment.weselaja.return.tsx",
        "app/lib/weselaja-webhook.server.ts",
        "app/lib/shopify-order-sync.server.ts",
        "prisma/schema.prisma",
        "README.md"
    )

    foreach ($file in $requiredFiles) {
        if (-not (Test-Path $file)) {
            throw "Missing generated file: $file"
        }
    }

    if (-not (Test-Path "GENERATOR_VERSION.txt")) {
        throw "Missing generator version marker."
    }

    Write-Output "Required Shopify integration files are present."
    Write-Output "Run npm install, npm run setup, npm run typecheck, npm test, and npm run build."
    Write-Output "Run npm run db:studio to inspect the local Prisma database."
    """;

  private const string ReadmeTemplate = """
    # __PROJECT_NAME__

    Shopify React Router app scaffold with a WeselAja Pay In integration,
    generated by the WeselAja MCP Code Generator.

    ## 1. Requirements

    - Node.js 22.12 or newer
    - npm
    - Shopify CLI
    - Shopify development store
    - Shopify app credentials
    - WeselAja sandbox API credentials

    ## 2. Create the environment file

    On Windows PowerShell:

    ```powershell
    Copy-Item .env.example .env
    ```

    Configure the Shopify and WeselAja credentials in `.env`.

    - `SHOPIFY_API_KEY`: Shopify app API key.
    - `SHOPIFY_API_SECRET`: Shopify app API secret.
    - `SCOPES`: Shopify permissions required by the app.
    - `SHOPIFY_APP_URL`: public URL provided by the Shopify app development setup.
    - `WESELAJA_API_URL`: WeselAja API base URL.
    - `WESELAJA_API_KEY`: API key for outgoing Pay In requests.
    - `WESELAJA_SECRET_KEY`: secret used to sign outgoing Pay In requests.
    - `WESELAJA_CUSTOMER_REFERENCE`: configured merchant/customer reference.
    - `WEB_SIGNATURE_SECRET`: secret used by the callback signing system.

    `WESELAJA_SECRET_KEY` and `WEB_SIGNATURE_SECRET` have different purposes.
    Do not assume their values are interchangeable.

    ## 3. Callback and return URLs

    During Shopify CLI development, leave these values blank to derive the
    callback and return URLs from `SHOPIFY_APP_URL`:

    ```env
    WESELAJA_CALLBACK_URL=
    WESELAJA_REDIRECT_URL=
    ```

    The generated app uses these paths:

    ```text
    /payment/weselaja/callback
    /payment/weselaja/return
    ```

    Explicit values in `WESELAJA_CALLBACK_URL` and `WESELAJA_REDIRECT_URL`
    override the derived URLs.

    Shopify CLI development requires a public URL so the payment provider
    can reach the callback route. If the public tunnel changes, restart the
    app and create a new sandbox payment. Existing transactions retain the
    URLs supplied when they were created.

    Do not use a localhost URL as an external payment provider callback URL.

    ## 4. Install and link the Shopify app

    Run these commands from the generated project directory:

    ```powershell
    npm install
    npm run config:link
    ```

    Follow the Shopify CLI prompts to authenticate and link the project
    to the intended Shopify app.

    Review `shopify.app.toml` and confirm that the configured scopes match
    what the application needs.

    ## 5. Initialize the database

    Run:

    ```powershell
    npm run setup
    ```

    This generates the Prisma client and pushes the schema to the
    development database. The SQLite database is stored at `prisma/dev.sqlite`.

    To inspect tables and records in Prisma Studio, run this in another terminal
    from the generated project directory:

    ```powershell
    npm run db:studio
    ```

    Prisma Studio opens a local browser page, normally at `http://localhost:5555`.
    Keep that terminal running while viewing the database.

    ## 6. Check the generated project

    Run:

    ```powershell
    npm run typecheck
    npm test
    ```

    The tests check bank channel mappings and callback signature verification
    using local test data. They do not create a payment and cannot prove that
    the real callback sender uses the configured secret and signing format.

    ## 7. Start development

    Run:

    ```powershell
    npm run dev
    ```

    Follow the Shopify CLI prompts to select the development store and launch
    the application.

    Open the embedded app in the development store and use the WeselAja
    Payments page to create a test order and Pay In transaction.

    ## 8. Payment flow

    1. The app creates a Shopify order with a pending payment.
    2. The app creates a WeselAja Pay In transaction using `POST /v1/payins`.
    3. The payment and Shopify order references are stored in Prisma.
    4. WeselAja sends a callback to `/payment/weselaja/callback`.
    5. The callback signature is verified before the payload is processed.
    6. The callback is matched to the stored payment by Pay In ID or reference code.
    7. A recognized successful transaction status triggers Shopify order synchronization.
    8. The app calls Shopify `orderMarkAsPaid` and stores the result.

    A valid signature proves that the callback matches the configured signing
    scheme. The payment status is checked independently. Signed `PENDING` or
    failed transactions must not be treated as successful payments.

    Before marking an order paid, the generated flow verifies that the signed
    callback amount matches the stored order amount and Shopify's current
    outstanding balance. Missing or mismatched amounts are rejected.

    ## 9. Callback signature configuration

    The callback route tests timestamp/signature header pairs together.
    The configured defaults are:

    ```env
    WESELAJA_CALLBACK_TIMESTAMP_HEADER=x-middleware-timestamp
    WESELAJA_CALLBACK_SIGNATURE_HEADER=x-middleware-signature
    ```

    `WEB_SIGNATURE_SECRET` must match the secret used by the callback signer.

    The verifier supports multiple candidate URI, raw-body and payload formats
    to accommodate the callback signing format confirmed during local testing.
    Its candidate-matching log records the selected format, not the secret or
    signature itself.

    Optional URI configuration:

    ```env
    WESELAJA_CALLBACK_SIGNED_URI=
    WESELAJA_CALLBACK_SIGNED_BASE_URL=
    ```

    Leave those variables empty unless the actual callback signing system
    requires a different URI. Never bypass signature verification simply to
    make an unverified callback pass.

    ## 10. Return page

    The generated client appends the payment reference to the redirect URL.
    The `/payment/weselaja/return` route uses that reference to find and show
    the locally stored transaction.

    The return page is informational. A browser redirect or query-string
    status is not proof of payment.

    ## Important notes

    - The app is a development-oriented integration scaffold.
    - This does not register a native payment method directly in Shopify checkout.
    - Use sandbox credentials for initial tests.
    - Ensure the accepted successful status values match the actual provider payload.
    - Verify the complete callback-to-order flow before production deployment.
    - Shopify `orderMarkAsPaid` is called only after callback signature, status,
      currency, and payment amount checks pass. Test the complete flow before
      production deployment.
    """;
}