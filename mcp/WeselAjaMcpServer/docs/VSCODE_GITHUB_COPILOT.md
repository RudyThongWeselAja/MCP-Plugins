# Menggunakan WeselAja MCP di VS Code dan GitHub Copilot

Panduan ini menghubungkan GitHub Copilot di VS Code ke MCP generator WeselAja yang di-host di Railway. MCP ini menghasilkan proyek integrasi di server; hasilnya perlu diunduh ke workspace setelah proses generate selesai.

## Prasyarat

- Visual Studio Code dengan GitHub Copilot Chat aktif.
- Mode **Agent** tersedia di Copilot Chat.
- Akses ke MCP server dan token Bearer yang diberikan oleh pengelola server.
- Jika Anda memakai Copilot Business atau Enterprise, kebijakan organisasi harus mengizinkan MCP server.

Endpoint MCP:

```text
https://mcp-plugins-production.up.railway.app/mcp
```

Endpoint `/health` dapat dipakai untuk memeriksa ketersediaan server, tetapi tidak menghubungkan Copilot ke tool MCP. Endpoint `/mcp` dan `/downloads` memerlukan header `Authorization: Bearer <token>`.

## Konfigurasi di VS Code

Cara paling cepat adalah membuka Command Palette (`Ctrl+Shift+P`), menjalankan **MCP: Add Server**, memilih server HTTP, lalu mengisi URL di atas dan header autentikasi. Untuk konfigurasi manual, buat atau buka `.vscode/mcp.json` di workspace dan gunakan:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "weselaja-mcp-token",
      "description": "Token akses WeselAja MCP",
      "password": true
    }
  ],
  "mcpServers": {
    "weselaja": {
      "type": "http",
      "url": "https://mcp-plugins-production.up.railway.app/mcp",
      "headers": {
        "Authorization": "Bearer ${MCP_ACCESS_TOKEN}"
      },
      "tools": ["*"]
    }
  }
}
```

Jangan mengganti `${input:weselaja-mcp-token}` dengan token sungguhan di file konfigurasi yang akan dibagikan atau di-commit. VS Code meminta token saat server mulai dan menyembunyikan inputnya. Untuk konfigurasi yang hanya dipakai oleh Anda, Anda juga dapat menambahkan server melalui konfigurasi MCP di profil pengguna VS Code.

> Catatan kompatibilitas: variabel input interaktif pada format `.vscode/mcp.json` tidak diteruskan ke beberapa sesi Copilot berbasis Agent Host. Jika server tidak tersedia di sesi tersebut, gunakan konfigurasi portabel `~/.copilot/mcp-config.json` dengan environment variable `${WESELAJA_MCP_TOKEN}` pada header, dan sediakan variabel itu di lingkungan proses Copilot. Jangan menaruh token langsung di file konfigurasi.

Setelah menyimpan konfigurasi:

1. Klik **Start** di atas konfigurasi MCP. Saat diminta, masukkan token dari pengelola server.
2. Buka Copilot Chat, pilih mode **Agent**, lalu buka daftar tools.
3. Pastikan server `weselaja-generator` aktif dan tool `generate_weselaja_integration` terlihat.
4. Jika server tidak muncul, jalankan **MCP: List Servers** dari Command Palette dan periksa output server.

## Membuat proyek integrasi

Jelaskan platform, nama proyek, dan apakah Anda ingin contoh serta tes. Contoh prompt:

```text
Buat proyek integrasi WeselAja untuk Python dengan nama weselaja-python-demo.
Sertakan contoh penggunaan dan unit test. Gunakan konfigurasi sandbox.
```

Target yang tersedia saat panduan ini ditulis: Python, C#, PHP, Go, JavaScript, TypeScript, Java, Android Kotlin, Shopify, Odoo 18.0, dan WooCommerce. Setiap permintaan generator memakai workspace server yang terisolasi. Anda dapat menggunakan nama proyek yang sama tanpa menimpa hasil pengguna lain.

Jika tool mengembalikan `success: true` dan `downloadUrl`, unduh ZIP tersebut dengan metode GET dan Bearer token yang sama. Ekstrak ke folder yang Anda pilih di workspace. Bila Copilot tidak mengunduh dan mengekstrak hasil secara otomatis, jalankan langkah berikut di terminal PowerShell pada workspace Anda, ganti placeholder dengan nilai persis dari respons tool:

```powershell
$secureToken = Read-Host "Token akses WeselAja MCP" -AsSecureString
$token = [pscredential]::new("mcp", $secureToken).GetNetworkCredential().Password
$headers = @{ Authorization = "Bearer $token" }
$downloadUrl = Read-Host "Tempel downloadUrl dari hasil tool"
$zipFile = Read-Host "Nama file ZIP dari hasil tool"
$destination = Read-Host "Folder tujuan di workspace, misalnya generated/weselaja-python"
Invoke-WebRequest -Uri $downloadUrl -Headers $headers -OutFile $zipFile
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Expand-Archive -LiteralPath $zipFile -DestinationPath $destination
Remove-Variable token, secureToken, headers
```

Setelah file berada di workspace, buka README proyek hasil generate, isi `.env` dari `.env.example`, lalu jalankan tes dan contoh sesuai instruksi proyek. Contoh bawaan ditujukan untuk sandbox; periksa `WESELAJA_API_URL` dan kredensial sebelum mencoba lingkungan lain.

## Pengelolaan token

Deployment saat ini memeriksa satu nilai `MCP_ACCESS_TOKEN` yang disetel pada server. Pengelola memberikan token tersebut kepada pengguna; URL saja tidak memberikan akses. Perlakukan token sebagai rahasia, jangan menaruhnya di prompt Copilot, source control, atau tangkapan layar. Karena implementasi saat ini memakai satu token bersama, token itu belum dapat dicabut hanya untuk satu pengguna. Untuk uji coba terbatas, bagikan hanya kepada pengguna yang dipercaya; sebelum distribusi luas, siapkan autentikasi dan pencabutan akses per pengguna.

## Dokumentasi resmi

- [Menambahkan dan mengelola MCP server di VS Code](https://code.visualstudio.com/docs/agent-customization/mcp-servers)
- [Referensi konfigurasi MCP VS Code](https://code.visualstudio.com/docs/agents/reference/mcp-configuration)
- [MCP di GitHub Copilot Chat dalam IDE](https://docs.github.com/en/copilot/how-tos/copilot-in-your-ide/customize-copilot/extend-copilot-chat-with-tools-and-context/extend-copilot-chat-with-mcp)
