# WeselAja MCP

<<<<<<< HEAD
MCP server untuk mengintegrasikan pembayaran WeselAja dengan AI client yang mendukung Model Context Protocol (MCP).
=======
WeselAja MCP is a multi-implementation Model Context Protocol (MCP) server for WeselAja payment integration.

The project provides a unified MCP interface across multiple implementations:

`C#` `PHP` `Python` `WooCommerce` `Odoo`

It also includes a standalone Go API client in
[`mcp/WeselAjaMcpServer/generated/go/weselaja-golang`](mcp/WeselAjaMcpServer/generated/go/weselaja-golang).
The standalone Python client is available in
[`mcp/WeselAjaMcpServer/generated/python/weselaja-python`](mcp/WeselAjaMcpServer/generated/python/weselaja-python).

The MCP implementations expose the same tools:

`is_weselaja_active`  
`get_weselaja_payment_method`  
`create_weselaja_payment`

## Requirements

### Central MCP

`.NET 10 SDK` `Node.js` `npm`

Check:

```bash
dotnet --version
node --version
npm --version
```

### PHP

Required when using the PHP implementation:

`PHP` `PHP cURL extension`

Check:

```bash
php --version
```

### Python

Required when using the Python implementation:

`Python 3` `requests`

Install:

```bash
pip install requests
```

Check:

```bash
python --version
```

### Go

The standalone Go client requires Go 1.23 or later and uses only the standard
library. See the [Go integration README](mcp/WeselAjaMcpServer/generated/go/weselaja-golang/README.md)
for configuration and usage.

### Standalone Python client

The standalone Python integration requires Python 3.11 or later. Setup and
usage instructions are in the [Python integration README](mcp/WeselAjaMcpServer/generated/python/weselaja-python/README.md).

### WooCommerce

Required when using the WooCommerce implementation:

`WordPress` `WooCommerce` `WooCommerce WeselAja plugin`

### Odoo

Required when using the Odoo implementation:

`Odoo 18` `PostgreSQL` `Odoo payment module` `weselaja_payment custom module included in this repository`

---

> > > > > > > e373e93 (1. https://github.com/RudyThongWeselAja/MCP-Plugins/issues/8)

## Configuration

Buat file `.env` di folder `mcp/WeselAjaMcpServer/`.

```env
XENITH_ENABLED=true
XENITH_API_KEY=
XENITH_SECRET_KEY=
WESELAJA_API_URL=https://sandbox.checkout.weselaja.id

XENITH_ODOO_URL=http://127.0.0.1:8069
XENITH_ODOO_API_TOKEN=

MCP_TRANSPORT=http
MCP_HTTP_URL=http://0.0.0.0:3001

WESELAJA_CURRENCY=IDR
WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT

WESELAJA_CALLBACK_URL=.../payment/weselaja/callback
WESELAJA_REDIRECT_URL=.../payment/weselaja/return
```

Sesuaikan API key, secret key, API URL, callback URL, dan redirect URL dengan konfigurasi akun WeselAja kamu.

Gunakan kredensial sandbox saat pengembangan. Jangan commit file `.env` ke GitHub.

## Run MCP Server

Pastikan .NET SDK sudah terpasang, lalu jalankan dari root repository:

<<<<<<< HEAD

````powershell
=======
```text
mcp/
└── WeselAjaMcpServer/
    ├── Program.cs
    ├── WeselAjaMcpServer.csproj
    ├── WeselAjaTools.cs
    ├── Models/
    ├── Clients/
    │   ├── CSharpMcpClient.cs
    │   ├── OdooWeselAjaClient.cs
    │   ├── PhpWeselAjaClient.cs
    │   ├── PythonWeselAjaClient.cs
    │   └── WooCommerceWeselAjaClient.cs
    ├── .env
    └── generated/
        └── go/
            └── weselaja-golang/
````

---

## WeselAja API

The MCP implementations communicate with the WeselAja Sandbox API:

```text
https://sandbox.checkout.weselaja.id
```

Payment endpoint:

```text
POST /v1/payins
```

### Request Signature

The request uses HMAC-SHA256 signature authentication.

The signature payload is generated using:

```text
POST
/v1/payins
TIMESTAMP
RAW_BODY
```

The generated HMAC-SHA256 signature is encoded using Base64.

### Required Headers

```text
Xenith-Api-Key
Xenith-Request-Timestamp
Xenith-Request-Signature
X-Idempotency-Key
```

---

## MCP Tools

The server exposes the following MCP tools:

| Tool                          | Description                                      |
| ----------------------------- | ------------------------------------------------ |
| `is_weselaja_active`          | Check whether the WeselAja integration is active |
| `get_weselaja_payment_method` | Get the configured WeselAja payment method       |
| `create_weselaja_payment`     | Create a new WeselAja payment                    |

---

## Running the MCP Server

Navigate to the central MCP directory:

```bash
>>>>>>> e373e93 (1. https://github.com/RudyThongWeselAja/MCP-Plugins/issues/8)
cd mcp/WeselAjaMcpServer
dotnet restore
dotnet build
dotnet run
```

MCP server menggunakan port `3001` sesuai konfigurasi di atas.

Endpoint MCP:

```text
http://127.0.0.1:3001/mcp
```

Untuk penggunaan lokal, pastikan MCP server berjalan sebelum menghubungkan AI client.

## Connect to MCP Client

Tambahkan konfigurasi berikut pada MCP client yang mendukung Streamable HTTP:

```json
{
  "mcpServers": {
    "weselaja": {
      "type": "http",
      "url": "http://127.0.0.1:3001/mcp"
    }
  }
}
```

Jika menggunakan MCP Inspector, jalankan:

```powershell
npx @modelcontextprotocol/inspector
```

Pilih koneksi Streamable HTTP dan masukkan URL MCP server di atas.

## Available Tools

### `is_weselaja_active`

Memeriksa apakah integrasi WeselAja sedang aktif.

### `get_weselaja_payment_method`

Melihat metode pembayaran dan kanal bank yang tersedia pada konfigurasi.

### `create_weselaja_payment`

Membuat transaksi pembayaran Virtual Account melalui WeselAja.

Contoh argumen:

```json
{
  "amount": 10000,
  "customerName": "Andi",
  "bank": "BCA",
  "customerPhoneNumber": "081234567890",
  "description": "Sandbox payment test",
  "referenceCode": "MCP-TEST-001"
}
```

Bank yang didukung dalam konfigurasi: `BRI`, `Mandiri`, `BNI`, `Permata`, `CIMB`, dan `BCA`.

Gunakan tool ini hanya ketika ingin membuat transaksi pembayaran. Nilai `amount` menggunakan rupiah, sehingga `10000` berarti Rp10.000.

### `generate_weselaja_integration`

Menghasilkan source code integrasi WeselAja untuk platform yang didukung.

Contoh untuk Odoo 18:

```json
{
  "target": "odoo",
  "projectName": "weselaja_odoo",
  "odooVersion": "18.0",
  "includeExample": true,
  "includeTests": true,
  "overwrite": false
}
```

Target generator yang tersedia pada versi repository saat ini:

- `python`
- `csharp`
- `php`
- `go`
- `javascript`
- `typescript`
- `java`
- `odoo`

Generator WooCommerce memerlukan implementasi dan registrasi target `woocommerce` sebelum dapat digunakan.

Hasil generate disimpan di folder `generated/` pada MCP server. Tool ini hanya menghasilkan source code dan tidak membuat transaksi pembayaran.

## Odoo Integration

Untuk menggunakan integrasi Odoo, pastikan addon WeselAja sudah terpasang dan konfigurasi API serta webhook sudah benar.

<<<<<<< HEAD
Jika middleware Odoo menggunakan Bearer token, konfigurasikan token melalui pengaturan yang sesuai pada integrasi kamu. Jangan membagikan token tersebut ke repository publik.
=======
The C# implementation is one of the payment implementations supported by the central MCP server.

The central MCP server starts the C# implementation through the configured project path.

Run the central MCP server:

```bash
cd mcp/WeselAjaMcpServer
dotnet build
dotnet run
```

The C# implementation communicates with the central MCP server through the configured MCP transport.

---

## PHP Implementation

The PHP implementation requires:

```text
PHP
PHP cURL extension
```

Check PHP:

```bash
php --version
```

The PHP implementation communicates with the WeselAja Sandbox API using the configured API credentials.

Make sure the following values are correctly configured:

```env
XENITH_API_KEY=...
XENITH_SECRET_KEY=...
WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
```

---

## Python Implementation

The Python implementation requires Python 3 and the `requests` package.

Install the dependency:

```bash
pip install requests
```

Check Python:

```bash
python --version
```

The Python implementation uses the configured WeselAja API credentials and API URL.

---

## Go Integration

The standalone Go client creates signed WeselAja pay-ins, maps supported banks
to payment channels, and reads credentials from environment variables or `.env`.
See the [Go integration README](mcp/WeselAjaMcpServer/generated/go/weselaja-golang/README.md)
for setup, API signing details, and examples.

---

## WooCommerce Implementation

The WooCommerce implementation requires:

```text
WordPress
WooCommerce
WooCommerce WeselAja plugin
```

The WooCommerce implementation provides the WeselAja payment integration through WooCommerce and communicates with the central MCP server using the configured MCP endpoint.

---

## Odoo Implementation

The Odoo implementation requires:

```text
Odoo 18
PostgreSQL
Odoo payment module
weselaja_payment
```

The custom Odoo module is included in:

```text
odoo/addons/weselaja_payment
```

### Odoo MCP Endpoint

The Odoo MCP endpoint is:

```text
http://localhost:8069/weselaja/mcp
```

The central MCP server connects to Odoo using:

```env
XENITH_ODOO_URL=...
XENITH_ODOO_API_TOKEN=...
```

The Odoo provider must be installed and configured before payment requests can be processed.

### Odoo Module

Module name:

```text
weselaja_payment
```

The module provides the WeselAja payment provider and MCP endpoint.

---

## Recommended Setup Order

```text
1. Configure .env
2. Configure WeselAja Sandbox credentials
3. Configure the required implementation
4. Install and configure Odoo / WooCommerce when required
5. Start the central MCP server
6. Verify the /health endpoint
7. Start MCP Inspector
8. Connect to /mcp
9. Test the available MCP tools
10. Test payment creation
```

---

> > > > > > > e373e93 (1. https://github.com/RudyThongWeselAja/MCP-Plugins/issues/8)

## Troubleshooting

- **MCP tidak tersambung:** pastikan server berjalan dan URL `/mcp` benar.
- **API gagal tersambung:** periksa `WESELAJA_API_URL` dan koneksi jaringan.
- **Signature invalid:** periksa API key, secret key, timestamp, serta format signature.
- **Callback tidak diterima:** pastikan URL callback dapat dijangkau dari internet dan validasi signature sudah sesuai.
- **Generator gagal:** jalankan `dotnet build` dan pastikan target platform tersedia pada versi repository.

## Security

Jangan menyimpan API key, secret key, Bearer token, atau kredensial lainnya di source code atau repository publik.

<<<<<<< HEAD
Selalu gunakan sandbox untuk pengujian sebelum beralih ke production.
=======
Make sure `.NET 10 SDK` is installed.

Then run:

```bash
cd mcp/WeselAjaMcpServer
dotnet build
dotnet run
```

---

### MCP Inspector Cannot Connect

Make sure the MCP server is running:

```text
http://127.0.0.1:3001
```

Verify the health endpoint:

```text
http://127.0.0.1:3001/health
```

Make sure MCP Inspector uses:

```text
http://127.0.0.1:3001/mcp
```

Do not use:

```text
http://0.0.0.0:3001/mcp
```

---

### Odoo Connection Problem

Check the Odoo URL:

```env
XENITH_ODOO_URL=http://localhost:8069
```

Check the Odoo MCP endpoint:

```text
http://localhost:8069/weselaja/mcp
```

Make sure:

```text
weselaja_payment
```

is installed and the Odoo provider is active.

---

### Payment Request Failed

Verify:

```env
XENITH_API_KEY=...
XENITH_SECRET_KEY=...
WESELAJA_API_URL=https://sandbox.checkout.weselaja.id
```

Also verify that:

- the request body is valid JSON
- the request timestamp is valid
- the HMAC signature is generated from the exact request body
- the `X-Idempotency-Key` header is present
- the configured payment channel is supported by WeselAja

---

## Quick Start

### Start the MCP Server

```bash
cd mcp/WeselAjaMcpServer
dotnet run
```

### Verify the Server

```bash
curl http://127.0.0.1:3001/health
```

### Start MCP Inspector

Open another terminal:

```bash
npx @modelcontextprotocol/inspector
```

Connect to:

```text
http://127.0.0.1:3001/mcp
```

Use:

```text
Transport: Streamable HTTP
```

Then test:

```text
is_weselaja_active
get_weselaja_payment_method
create_weselaja_payment
```

---

## Supported Implementations

| Implementation             | Status                |
| -------------------------- | --------------------- |
| C#                         | Supported             |
| PHP                        | Supported             |
| Python                     | Supported             |
| Python (standalone client) | Standalone API client |
| Go                         | Standalone API client |
| WooCommerce                | Supported             |
| Odoo                       | Supported             |

> > > > > > > e373e93 (1. https://github.com/RudyThongWeselAja/MCP-Plugins/issues/8)
