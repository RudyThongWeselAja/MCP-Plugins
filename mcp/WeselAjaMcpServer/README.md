# WeselAja MCP Server

MCP server ini menyediakan tool untuk menghasilkan proyek integrasi WeselAja untuk berbagai bahasa dan platform. Hasil generator mencakup source code dan, sesuai target, README, contoh konfigurasi, contoh penggunaan, serta tes atau checklist.

## Menggunakan dengan VS Code dan GitHub Copilot

Untuk menghubungkan Copilot ke deployment Railway, konfigurasi server HTTP dan token Bearer diperlukan. Ikuti [panduan konfigurasi VS Code dan GitHub Copilot](docs/VSCODE_GITHUB_COPILOT.md).

## Transport

- **Streamable HTTP**: cocok untuk MCP server jarak jauh. Endpoint deployment Railway saat ini adalah `https://mcp-plugins-production.up.railway.app/mcp`.
- **STDIO**: cocok untuk menjalankan server secara lokal dari klien MCP.

Untuk transport HTTP, set `MCP_TRANSPORT=http` dan sediakan `MCP_ACCESS_TOKEN`. Endpoint MCP dan download ZIP meminta autentikasi Bearer. Endpoint `/health` tidak memerlukan token.

## Target generator

Python, C#, PHP, Go, JavaScript, TypeScript, Java, Android Kotlin, Shopify, Odoo 18.0, dan WooCommerce.

Setiap permintaan generator dibuat dalam workspace server yang terisolasi. Untuk penggunaan MCP jarak jauh, tool mengembalikan URL ZIP; pengguna perlu mengunduh dan mengekstrak ZIP itu ke workspace lokalnya.

## Konfigurasi deployment

Jangan commit file `.env` atau nilai rahasia. Isi variabel deployment melalui secret/environment settings platform hosting. Variabel utama:

| Variabel | Fungsi |
|---|---|
| `MCP_TRANSPORT` | Pilih `http` atau `stdio`; default `stdio`. |
| `MCP_HTTP_URL` | Alamat bind HTTP; default `http://0.0.0.0:3001`. |
| `MCP_ACCESS_TOKEN` | Token Bearer wajib untuk transport HTTP. |
| `MCP_PUBLIC_BASE_URL` | Base URL publik HTTPS untuk membentuk link download, misalnya `https://mcp-plugins-production.up.railway.app`. |
| `WESELAJA_GENERATOR_OUTPUT_DIR` | Root folder keluaran generator; default `generated` di direktori kerja server. |

Variabel layanan pembayaran WeselAja lainnya hanya diperlukan jika tool pembayaran diaktifkan. Jangan membagikan kredensial merchant melalui konfigurasi MCP pengguna; generator menghasilkan proyek dan kredensial harus diisi oleh pemilik integrasi pada proyek hasilnya.

## Keamanan token

Deployment saat ini memakai satu `MCP_ACCESS_TOKEN` bersama untuk semua klien yang diberi akses. Jangan publikasikan token atau menyimpannya dalam source control. Konfigurasi ini cocok untuk uji coba terbatas; distribusi luas sebaiknya memakai identitas dan pencabutan akses per pengguna.
