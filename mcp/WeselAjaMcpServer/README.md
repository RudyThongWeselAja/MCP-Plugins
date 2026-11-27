# WeselAja MCP Server

MCP server ini menyediakan tool untuk menghasilkan proyek integrasi WeselAja untuk berbagai bahasa dan platform. Hasil generator mencakup source code dan, sesuai target, README, contoh konfigurasi, contoh penggunaan, serta tes atau checklist.

## Menggunakan dengan VS Code dan GitHub Copilot

Untuk menghubungkan Copilot ke deployment Railway, konfigurasi server HTTP dan token Bearer diperlukan. Ikuti [panduan konfigurasi VS Code dan GitHub Copilot](docs/VSCODE_GITHUB_COPILOT.md).

## Transport

- **Streamable HTTP**: cocok untuk MCP server jarak jauh. Endpoint deployment Railway saat ini adalah `https://mcp-plugins-production.up.railway.app/mcp`.
- **STDIO**: cocok untuk menjalankan server secara lokal dari klien MCP.

Untuk transport HTTP, set `MCP_TRANSPORT=http`, `DATABASE_URL`, dan `MCP_ADMIN_TOKEN`. Endpoint MCP meminta Bearer access token individual berawalan `wja_`; download ZIP memakai Bearer token pemilik atau ticket sekali pakai. Endpoint `/health` tidak memerlukan token, sedangkan `/ready` memeriksa koneksi database.

## Target generator

Python, C#, PHP, Go, JavaScript, TypeScript, Java, Android Kotlin, Shopify, Odoo 18.0, dan WooCommerce.

Setiap permintaan generator dibuat dalam workspace server yang terisolasi. Untuk penggunaan MCP jarak jauh, tool mengembalikan URL ZIP; pengguna perlu mengunduh dan mengekstrak ZIP itu ke workspace lokalnya.

## Konfigurasi deployment

Jangan commit file `.env` atau nilai rahasia. Isi variabel deployment melalui secret/environment settings platform hosting. Variabel utama:

| Variabel | Fungsi |
|---|---|
| `MCP_TRANSPORT` | Pilih `http` atau `stdio`; default `stdio`. |
| `MCP_HTTP_URL` | Alamat bind HTTP; default `http://0.0.0.0:3001`. |
| `DATABASE_URL` | Koneksi PostgreSQL untuk customer, token, request generator, dan ticket download. |
| `MCP_ADMIN_TOKEN` | Secret administrasi minimal 32 karakter untuk endpoint `/admin/*`; bukan token customer. |
| `MCP_PUBLIC_BASE_URL` | Base URL publik HTTPS untuk membentuk link download, misalnya `https://mcp-plugins-production.up.railway.app`. |
| `WESELAJA_GENERATOR_OUTPUT_DIR` | Root folder keluaran generator; default `generated` di direktori kerja server. |
| `WESELAJA_GENERATOR_MAX_CONCURRENCY` | Jumlah generator yang boleh aktif bersamaan per instance; default `2`, rentang `1`–`16`. |
| `WESELAJA_GENERATOR_RETENTION_HOURS` | Lama penyimpanan artifact lokal sebelum cleanup; default `26`, rentang `24`–`168` jam. |

Variabel layanan pembayaran WeselAja lainnya hanya diperlukan jika tool pembayaran diaktifkan. Jangan membagikan kredensial merchant melalui konfigurasi MCP pengguna; generator menghasilkan proyek dan kredensial harus diisi oleh pemilik integrasi pada proyek hasilnya.

## Keamanan token

Setiap customer memakai access token individual berawalan `wja_`. Server hanya menyimpan hash token; pengelola dapat mencabut satu token tanpa memengaruhi customer lain, dan ticket download yang diterbitkan token tersebut ikut tidak berlaku. Jangan publikasikan token atau menyimpannya dalam source control.
