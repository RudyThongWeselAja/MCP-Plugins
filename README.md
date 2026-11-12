# WeselAja MCP

WeselAja MCP is a Model Context Protocol (MCP) server built with .NET 10 that integrates WeselAja payment services with AI clients. It provides payment tools and code generation capabilities through a unified MCP interface.

The server supports two deployment methods:

- **Docker Compose** — recommended for running the server in the background.
- **Local .NET execution** — suitable for development, debugging, and running the server without Docker.

WeselAja MCP adalah server Model Context Protocol (MCP) berbasis .NET 10 yang mengintegrasikan layanan pembayaran WeselAja dengan AI client. Server ini menyediakan tools pembayaran dan kemampuan generator kode melalui satu antarmuka MCP.

Server dapat dijalankan dengan dua cara:

- **Docker Compose** — direkomendasikan untuk menjalankan server di background.
- **Menjalankan .NET secara lokal** — cocok untuk development, debugging, dan penggunaan tanpa Docker.

## Features / Fitur

- Check whether the WeselAja integration is enabled.
- Retrieve configured payment method and channel information.
- Create WeselAja Pay In transactions.
- Generate integration projects for supported platforms.
- Support STDIO and Streamable HTTP transports.
- Run with Docker Compose or directly through the .NET SDK.
- Save generated projects to the configured output directory.

Fitur yang tersedia:

- Memeriksa status integrasi WeselAja.
- Mengambil informasi metode dan kanal pembayaran yang dikonfigurasi.
- Membuat transaksi Pay In WeselAja.
- Menghasilkan proyek integrasi untuk platform yang didukung.
- Mendukung transport STDIO dan Streamable HTTP.
- Menjalankan server menggunakan Docker Compose atau .NET SDK secara langsung.
- Menyimpan proyek hasil generator ke direktori output yang dikonfigurasi.

## Requirements / Persyaratan

### Docker deployment

- Docker Desktop.
- Docker Compose.
- WeselAja API credentials.
- A configured `.env` file.

### Local development

- .NET 10 SDK.
- Git.
- WeselAja API credentials.
- A configured `.env` file.

Untuk deployment Docker, kamu membutuhkan Docker Desktop dan Docker Compose. Untuk menjalankan server secara lokal tanpa Docker, kamu cukup memerlukan .NET 10 SDK beserta konfigurasi API WeselAja yang valid.

## Project Structure / Struktur Proyek

```text
MCP-Plugins/
├── .gitignore
├── README.md
└── mcp/
    └── WeselAjaMcpServer/
        ├── .dockerignore
        ├── Dockerfile
        ├── docker-compose.yml
        ├── docker-compose.yml.example
        ├── WeselAjaMcpServer.csproj
        ├── Program.cs
        ├── Configuration/
        ├── Models/
        ├── Security/
        ├── Services/
        ├── Tools/
        ├── Generators/
        ├── .env
        └── generated/
```

The structure above is representative. Some directories may contain additional files as the project evolves.

Struktur di atas merupakan gambaran umum. Beberapa direktori dapat berisi file tambahan seiring perkembangan proyek.

## Configuration / Konfigurasi

Create a `.env` file inside the MCP server directory:

```text
mcp/WeselAjaMcpServer/.env
```

Buat file `.env` di direktori server MCP.

Example configuration / Contoh konfigurasi:

```env
XENITH_ENABLED=true
XENITH_TITLE=WeselAja
XENITH_DESCRIPTION=Pay securely using WeselAja payment gateway.

XENITH_API_KEY=your_sandbox_api_key
XENITH_SECRET_KEY=your_sandbox_secret_key
WESELAJA_API_URL=your_sandbox_api_base_url

MCP_TRANSPORT=http
MCP_HTTP_URL=http://127.0.0.1:3001
```

Replace the placeholder values with the credentials and API base URL assigned to your WeselAja account. The example above configures HTTP transport for local development.

Ganti nilai placeholder dengan kredensial dan base URL API yang sesuai dengan akun WeselAja kamu. Contoh tersebut menggunakan transport HTTP untuk development lokal.

### Environment Variables

| Variable | Description |
|---|---|
| `XENITH_ENABLED` | Enables or disables the integration. |
| `XENITH_TITLE` | Integration title. |
| `XENITH_DESCRIPTION` | Integration description. |
| `XENITH_API_KEY` | API key used for authentication. |
| `XENITH_SECRET_KEY` | Secret key used for request signing. |
| `WESELAJA_API_URL` | WeselAja API base URL. |
| `MCP_TRANSPORT` | Transport mode: `stdio` or `http`. |
| `MCP_HTTP_URL` | HTTP server binding address when HTTP transport is enabled. |

**Security notes / Catatan keamanan**

- Never commit `.env` or API credentials to Git.
- Use sandbox credentials during development.
- Do not publish secret keys in issues, logs, or screenshots.
- Do not expose the MCP endpoint publicly without suitable authentication and network security.

Jangan commit file `.env` maupun kredensial API ke Git. Gunakan kredensial sandbox untuk pengujian dan jangan membuka endpoint MCP ke publik tanpa pengamanan yang sesuai.

## Option 1: Run with Docker Compose / Menjalankan dengan Docker Compose

Docker Compose is recommended when you want the MCP server to run in the background without keeping a terminal session open.

Docker Compose direkomendasikan jika kamu ingin server berjalan di background tanpa harus membiarkan terminal server tetap terbuka.

### 1. Open the MCP server directory

```powershell
cd C:\Kerja\XenithPay\mcp\WeselAjaMcpServer
```

Adjust the path if the repository is located elsewhere.

### 2. Build and start the server

For the first run, or after changing application source code or Docker build configuration:

```powershell
docker compose up -d --build
```

For subsequent runs when a rebuild is not required:

```powershell
docker compose up -d
```

The `-d` option starts the container in the background.

Opsi `-d` menjalankan container di background sehingga server tetap berjalan tanpa terminal interaktif.

### 3. Check container status

```powershell
docker compose ps
```

The container should show a running status.

Container seharusnya menunjukkan status berjalan, seperti `Up`.

### 4. Check the health endpoint

```powershell
Invoke-RestMethod http://localhost:3001/health
```

A successful response should look similar to:

```json
{
  "success": true,
  "service": "WeselAja MCP",
  "transport": "Streamable HTTP"
}
```

A successful health check confirms that the HTTP application is responding. It does not by itself verify payment API authentication or transaction creation.

Health check yang berhasil memastikan aplikasi HTTP merespons, tetapi tidak otomatis membuktikan autentikasi API atau pembuatan transaksi pembayaran berhasil.

### 5. Docker Compose commands

Run these commands from the MCP server directory.

**Start the server**

```powershell
docker compose up -d
```

**Rebuild and start**

```powershell
docker compose up -d --build
```

**Check status**

```powershell
docker compose ps
```

**View logs**

```powershell
docker compose logs -f
```

Press `Ctrl+C` to stop following the logs. The container continues running.

Tekan `Ctrl+C` untuk berhenti mengikuti log. Container tetap berjalan.

**Restart the server**

```powershell
docker compose restart
```

**Stop the server**

```powershell
docker compose down
```

The `down` command stops and removes the Compose-managed container and network. Files in the host's mounted `generated` directory remain available.

Perintah `down` menghentikan dan menghapus container serta jaringan yang dikelola Compose. File pada direktori `generated` di komputer host tetap tersedia.

### Automatic restart / Restart otomatis

The Compose configuration uses `restart: unless-stopped`. Docker can restart the container after an unexpected stop or Docker restart, provided Docker Desktop is running and the container has not been explicitly stopped.

Konfigurasi Compose menggunakan `restart: unless-stopped`. Docker dapat menjalankan kembali container setelah berhenti secara tidak terduga atau setelah Docker direstart, selama Docker Desktop aktif dan container tidak dihentikan secara eksplisit.

### Generated output / Hasil generator

The Compose configuration mounts the host's `generated` directory into the container at:

```text
/app/generated
```

Generated projects should therefore be available on the host under:

```text
mcp/WeselAjaMcpServer/generated/
```

Hasil generator akan tersedia di direktori host tersebut selama generator menulis ke lokasi output yang dikonfigurasi.

## Option 2: Run Locally Without Docker / Menjalankan Secara Lokal Tanpa Docker

This option runs the MCP server directly through the .NET 10 SDK. Docker Desktop is not required.

Opsi ini menjalankan server MCP secara langsung menggunakan .NET 10 SDK. Docker Desktop tidak diperlukan.

### 1. Install the .NET 10 SDK

Install the .NET 10 SDK on your development machine.

Verify the installation:

```powershell
dotnet --version
```

The installed SDK should be version 10.x.

### 2. Open the MCP server directory

```powershell
cd C:\Kerja\XenithPay\mcp\WeselAjaMcpServer
```

### 3. Configure the `.env` file

Make sure `.env` exists in the current directory and contains valid credentials and the correct API base URL.

Untuk menjalankan server secara lokal melalui HTTP, gunakan konfigurasi berikut di `.env`:

```env
MCP_TRANSPORT=http
MCP_HTTP_URL=http://127.0.0.1:3001
```

Keep the other WeselAja environment variables from the configuration section.

Pertahankan juga environment variable WeselAja lainnya, termasuk API key, secret key, status integrasi, dan base URL.

### 4. Restore dependencies

```powershell
dotnet restore
```

This downloads and restores the project's NuGet dependencies.

Perintah ini memulihkan dependensi NuGet yang dibutuhkan proyek.

### 5. Build the project

```powershell
dotnet build
```

Confirm that the build completes successfully before starting the server.

Pastikan build berhasil sebelum menjalankan server.

### 6. Start the MCP server

```powershell
dotnet run
```

The server should start using the transport configured in `.env`.

Server akan berjalan menggunakan transport yang dikonfigurasi di `.env`.

Keep this terminal open while the server is running. Press `Ctrl+C` to stop the local process.

Biarkan terminal tetap terbuka selama server berjalan. Tekan `Ctrl+C` untuk menghentikan proses lokal.

### 7. Verify the HTTP server

Open another PowerShell window and run:

```powershell
Invoke-RestMethod http://localhost:3001/health
```

If the server is healthy, it should return a response similar to:

```json
{
  "success": true,
  "service": "WeselAja MCP",
  "transport": "Streamable HTTP"
}
```

The local MCP endpoint is:

```text
http://localhost:3001/mcp
```

### Local development workflow / Alur development lokal

When you change the source code, stop the running process with `Ctrl+C`, then start it again:

```powershell
dotnet run
```

If you modify project dependencies, restore them:

```powershell
dotnet restore
```

If you need to verify compilation independently:

```powershell
dotnet build
```

For local development, these commands do not require a Docker image rebuild.

Untuk development lokal, perubahan source code tidak memerlukan rebuild Docker image.

## Using STDIO Transport / Menggunakan Transport STDIO

The application also supports STDIO transport for MCP clients that launch the server as a local process.

Aplikasi juga mendukung transport STDIO untuk MCP client yang menjalankan server sebagai proses lokal.

To use STDIO, set this value in `.env`:

```env
MCP_TRANSPORT=stdio
```

Then launch the application using:

```powershell
dotnet run
```

Configure your MCP client to launch the application using the appropriate executable or .NET command and working directory. The exact client configuration depends on the MCP application you use.

Konfigurasikan MCP client agar menjalankan aplikasi melalui perintah .NET atau executable yang sesuai dengan working directory proyek. Format konfigurasinya bergantung pada aplikasi MCP client yang digunakan.

STDIO and HTTP are different transport modes. When STDIO is selected, do not expect the HTTP health endpoint or `/mcp` URL to be available from that process.

STDIO dan HTTP merupakan transport yang berbeda. Ketika menggunakan STDIO, endpoint health maupun URL `/mcp` tidak tersedia dari proses tersebut.

## MCP Endpoint / Endpoint MCP

When running with HTTP transport, the server exposes these endpoints.

Ketika server menggunakan transport HTTP, endpoint berikut tersedia.

| Purpose | URL |
|---|---|
| MCP endpoint | `http://localhost:3001/mcp` |
| Health endpoint | `http://localhost:3001/health` |

Use `http://localhost:3001/mcp` when connecting from an MCP client on the same computer.

Gunakan `http://localhost:3001/mcp` untuk menghubungkan MCP client yang berjalan pada komputer yang sama.

For Docker, the current port mapping binds to localhost, so the service is not directly exposed to other machines on the network.

Pada Docker, pemetaan port saat ini dibatasi ke localhost sehingga layanan tidak langsung terbuka untuk komputer lain melalui jaringan.

## MCP Tools

The server exposes four MCP tools.

Server menyediakan empat tools MCP berikut.

| Tool | Description |
|---|---|
| `is_weselaja_active` | Checks whether the WeselAja integration is enabled. |
| `get_weselaja_payment_method` | Retrieves configured payment method and channel information. |
| `create_weselaja_payment` | Creates a WeselAja payment transaction. |
| `generate_weselaja_integration` | Generates integration source code using a registered generator target. |

The exact inputs for each tool are defined by its MCP tool schema.

Input setiap tool mengikuti skema MCP yang disediakan oleh server.

### Testing with MCP Inspector

Start the MCP server using HTTP transport, then open another terminal:

```powershell
npx @modelcontextprotocol/inspector
```

Open the local URL printed by Inspector in your browser.

Configure the connection as follows:

| Setting | Value |
|---|---|
| Transport | Streamable HTTP |
| URL | `http://localhost:3001/mcp` |

Click **Connect** and verify that the four tools appear.

Klik **Connect** dan pastikan keempat tools muncul.

For a basic connectivity test, run `is_weselaja_active`. Only run the payment creation tool when you intend to create a transaction in the configured API environment.

Untuk menguji koneksi, jalankan `is_weselaja_active` terlebih dahulu. Jalankan tool pembuatan pembayaran hanya ketika memang ingin membuat transaksi pada lingkungan API yang dikonfigurasi.

## Code Generation / Pembuatan Kode

The `generate_weselaja_integration` tool generates integration projects using the targets registered in `CodeGeneratorTools.cs`.

Tool `generate_weselaja_integration` menghasilkan proyek integrasi menggunakan target yang didaftarkan di `CodeGeneratorTools.cs`.

The repository contains generator implementations for various platforms. The targets exposed by the MCP tool depend on the current registration in the code generator tool. Refer to the implementation for the authoritative list of available targets.

Repository ini memiliki implementasi generator untuk berbagai platform. Target yang dapat dipanggil melalui MCP mengikuti daftar yang didaftarkan pada code generator tool. Gunakan implementasi tersebut sebagai acuan daftar target yang tersedia.

Generated source code is output, not a replacement for the MCP server. Inspect and test generated projects in their intended environments before using them in production.

Source code hasil generator merupakan output terpisah, bukan pengganti server MCP. Periksa dan uji proyek yang dihasilkan pada lingkungan target sebelum digunakan di sistem produksi.

## Creating Payments / Membuat Pembayaran

The `create_weselaja_payment` tool initiates a Pay In transaction through the configured WeselAja API.

Tool `create_weselaja_payment` memulai transaksi Pay In melalui API WeselAja yang dikonfigurasi.

Before creating a payment:

1. Verify that the API base URL is correct.
2. Use sandbox credentials during testing.
3. Provide valid transaction and customer information.
4. Check that the selected payment channel is supported.
5. Use a unique reference code for each transaction.

Sebelum membuat pembayaran, pastikan base URL benar, gunakan kredensial sandbox untuk pengujian, berikan data transaksi yang valid, periksa dukungan kanal pembayaran, dan gunakan reference code yang unik.

A successful health check confirms that the MCP application responds. It does not guarantee that the external WeselAja API is reachable or that payment creation will succeed.

Health check yang berhasil hanya memastikan aplikasi MCP merespons. Hal itu tidak menjamin API WeselAja dapat diakses atau pembuatan pembayaran akan berhasil.

## Troubleshooting / Pemecahan Masalah

### Docker container is not running

```powershell
docker compose ps
docker compose logs --tail 100
```

### Port 3001 is already in use

Check whether another service or container is using port `3001`. Stop the conflicting service or adjust the Docker port mapping if appropriate.

Periksa apakah port `3001` sedang digunakan oleh layanan atau container lain.

### Local .NET build fails

Verify the installed SDK:

```powershell
dotnet --version
```

Restore dependencies and build again:

```powershell
dotnet restore
dotnet build
```

Review the compiler output if the build still fails.

### MCP client cannot connect

For HTTP transport, verify that the server is running and check:

```powershell
Invoke-RestMethod http://localhost:3001/health
```

Confirm that the MCP client URL is:

```text
http://localhost:3001/mcp
```

For STDIO transport, ensure that the client launches the server using the correct command and working directory instead of attempting to connect through HTTP.

Untuk transport STDIO, pastikan client menjalankan server dengan command dan working directory yang benar, bukan mencoba terhubung melalui HTTP.

### WeselAja API request fails

Check the API base URL, credentials, request signature, request payload, and supported payment channel.

Periksa base URL API, kredensial, signature request, payload, dan kanal pembayaran yang digunakan.

### Generated files cannot be found

Check the generator response and the configured output directory. When using Docker Compose, also verify that the `generated` bind mount points to the intended host directory.

Periksa respons generator, direktori output yang dikonfigurasi, serta bind mount `generated` ketika menggunakan Docker Compose.

## Quick Reference / Referensi Cepat

### Docker Compose

```powershell
# Run from mcp/WeselAjaMcpServer

# Start the server
docker compose up -d

# Rebuild and start
docker compose up -d --build

# Check status
docker compose ps

# View logs
docker compose logs -f

# Restart the server
docker compose restart

# Stop the server
docker compose down
```

### Local .NET without Docker

```powershell
# Run from mcp/WeselAjaMcpServer

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Start the server
dotnet run
```

## Security / Keamanan

- Keep `.env` out of version control.
- Never publish API keys or secret keys.
- Use sandbox credentials during development.
- Restrict access to the MCP endpoint.
- Do not expose the HTTP endpoint publicly without suitable authentication, HTTPS, and network controls.
- Review generated source code before production use.

- Jangan masukkan `.env` ke version control.
- Jangan membagikan API key atau secret key.
- Gunakan kredensial sandbox selama development.
- Batasi akses ke endpoint MCP.
- Jangan membuka endpoint HTTP ke publik tanpa autentikasi, HTTPS, dan kontrol jaringan yang sesuai.
- Periksa source code hasil generator sebelum digunakan di produksi.

---

**WeselAja MCP supports both Docker Compose deployment and local .NET execution. Choose the method that best fits your development or deployment workflow.**

**WeselAja MCP mendukung deployment menggunakan Docker Compose maupun menjalankan .NET secara lokal. Pilih cara yang sesuai dengan kebutuhan development atau deployment kamu.**
