# WeselAja MCP

WeselAja MCP is a multi-implementation Model Context Protocol (MCP) server for WeselAja payment integration.

The project provides a unified MCP interface across multiple implementations:

`C#` `PHP` `Python` `WooCommerce` `Odoo`

All implementations expose the same MCP tools:

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

### WooCommerce

Required when using the WooCommerce implementation:

`WordPress` `WooCommerce` `WooCommerce WeselAja plugin`

### Odoo

Required when using the Odoo implementation:

`Odoo 18` `PostgreSQL` `Odoo payment module` `weselaja_payment custom module included in this repository`

---

## Configuration

Create:

```text
mcp/WeselAjaMcpServer/.env
```

Example:

```env
XENITH_ENABLED=true

XENITH_API_KEY=...

XENITH_SECRET_KEY=...

WESELAJA_API_URL=https://sandbox.checkout.weselaja.id

XENITH_ODOO_URL=...

XENITH_ODOO_API_TOKEN=...

MCP_TRANSPORT=http

MCP_HTTP_URL=http://0.0.0.0:3001
```

### Environment Variables

| Variable | Description |
|---|---|
| `XENITH_ENABLED` | Enable or disable the WeselAja integration |
| `XENITH_API_KEY` | WeselAja API key |
| `XENITH_SECRET_KEY` | WeselAja secret key |
| `WESELAJA_API_URL` | WeselAja API base URL |
| `XENITH_ODOO_URL` | Odoo base URL |
| `XENITH_ODOO_API_TOKEN` | Odoo API bearer token |
| `MCP_TRANSPORT` | MCP transport mode |
| `MCP_HTTP_URL` | MCP HTTP server binding address |

---

## Project Structure

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
    └── .env
```

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

| Tool | Description |
|---|---|
| `is_weselaja_active` | Check whether the WeselAja integration is active |
| `get_weselaja_payment_method` | Get the configured WeselAja payment method |
| `create_weselaja_payment` | Create a new WeselAja payment |

---

## Running the MCP Server

Navigate to the central MCP directory:

```bash
cd mcp/WeselAjaMcpServer
```

Run the server:

```bash
dotnet run
```

With the current configuration:

```env
MCP_TRANSPORT=http
MCP_HTTP_URL=http://0.0.0.0:3001
```

the MCP server will listen on:

```text
http://0.0.0.0:3001
```

The MCP endpoint is:

```text
http://127.0.0.1:3001/mcp
```

---

## Health Check

To verify that the MCP server is running:

```text
http://127.0.0.1:3001/health
```

Example:

```bash
curl http://127.0.0.1:3001/health
```

---

## MCP Inspector

MCP Inspector can be used to test the MCP server and its available tools.

### 1. Start the MCP Server

From the central MCP directory:

```bash
cd mcp/WeselAjaMcpServer
dotnet build
dotnet run
```

Make sure the server is running on:

```text
http://127.0.0.1:3001
```

### 2. Start MCP Inspector

Open another terminal:

```bash
npx @modelcontextprotocol/inspector
```

### 3. Connect to the MCP Server

Use the following configuration in MCP Inspector:

| Configuration | Value |
|---|---|
| Transport | `Streamable HTTP` |
| URL | `http://127.0.0.1:3001/mcp` |

Then click **Connect**.

> `0.0.0.0` is used as the server binding address. MCP Inspector should connect using `127.0.0.1`.

### 4. Available Tools

After connecting, the following tools should be available:

```text
is_weselaja_active
get_weselaja_payment_method
create_weselaja_payment
```

---

## Testing `is_weselaja_active`

Example arguments:

```json
{
  "implementation": "Odoo"
}
```

Expected usage:

```text
implementation = CSharp
implementation = PHP
implementation = Python
implementation = WooCommerce
implementation = Odoo
```

---

## Testing `get_weselaja_payment_method`

Example arguments:

```json
{
  "implementation": "Odoo"
}
```

---

## Testing `create_weselaja_payment`

Example arguments:

```json
{
  "implementation": "Odoo",
  "amount": 100000,
  "currency": "IDR",
  "paymentMethod": "VIRTUAL_ACCOUNT",
  "paymentChannel": "BRI.VA",
  "referenceCode": "REF-DIRECT-POSTMAN-2222",
  "customerReference": "LAMPUNGDEV123456789",
  "customerName": "BURHAN",
  "customerPhoneNumber": "123456789",
  "description": "deskripsi",
  "callbackUrl": "https://reborn-refocus-autistic.ngrok-free.dev/webhook/callback-payin",
  "redirectUrl": "https://weselaja.com"
}
```

---

## C# Implementation

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

## Troubleshooting

### MCP Server Cannot Be Started

Check the .NET SDK:

```bash
dotnet --version
```

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

| Implementation | Status |
|---|---|
| C# | Supported |
| PHP | Supported |
| Python | Supported |
| WooCommerce | Supported |
| Odoo | Supported |
