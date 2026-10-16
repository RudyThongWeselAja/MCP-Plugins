# WeselAja MCP

MCP server untuk mengintegrasikan pembayaran WeselAja dengan AI client yang mendukung Model Context Protocol (MCP).

## Configuration

Buat file `.env` di folder `mcp/WeselAjaMcpServer/`.

```env
XENITH_ENABLED=true
XENITH_TITLE=WeselAja
XENITH_DESCRIPTION=Pay using WeselAja Virtual Account

XENITH_API_KEY=your_api_key
XENITH_SECRET_KEY=your_secret_key
WESELAJA_API_URL=your_sandbox_api_base_url

WESELAJA_CURRENCY=IDR
WESELAJA_PAYMENT_METHOD=VIRTUAL_ACCOUNT
WESELAJA_CALLBACK_URL=https://your-domain.com/callback
WESELAJA_REDIRECT_URL=https://your-domain.com/payment/return

MCP_TRANSPORT=http
MCP_HTTP_URL=http://0.0.0.0:3001
```

Sesuaikan API key, secret key, API URL, callback URL, dan redirect URL dengan konfigurasi akun WeselAja kamu.

Gunakan kredensial sandbox saat pengembangan. Jangan commit file `.env` ke GitHub.

## Run MCP Server

Pastikan .NET SDK sudah terpasang, lalu jalankan dari root repository:

```powershell
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

Jika middleware Odoo menggunakan Bearer token, konfigurasikan token melalui pengaturan yang sesuai pada integrasi kamu. Jangan membagikan token tersebut ke repository publik.

## Troubleshooting

- **MCP tidak tersambung:** pastikan server berjalan dan URL `/mcp` benar.
- **API gagal tersambung:** periksa `WESELAJA_API_URL` dan koneksi jaringan.
- **Signature invalid:** periksa API key, secret key, timestamp, serta format signature.
- **Callback tidak diterima:** pastikan URL callback dapat dijangkau dari internet dan validasi signature sudah sesuai.
- **Generator gagal:** jalankan `dotnet build` dan pastikan target platform tersedia pada versi repository.

## Security

Jangan menyimpan API key, secret key, Bearer token, atau kredensial lainnya di source code atau repository publik.

Selalu gunakan sandbox untuk pengujian sebelum beralih ke production.
