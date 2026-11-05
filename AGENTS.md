# Agent Guidelines for vpn-simple-subscription

## 1. Project Overview & Architecture
`vpn-simple-subscription` is a lightweight .NET 10 ASP.NET Core service providing subscription endpoints for VPN clients (e.g., Shadowrocket, v2rayN, Sing-box).
- **Core Workflow**: Reads client definitions from a JSON storage file (`subscriptions.json`), joins connection strings into newline-delimited lists, Base64-encodes the result, and returns it with custom subscription headers (`Profile-Update-Interval`).
- **Endpoint Structure**: `{SubscriptionRoute}/{clientId}/plain` (mapped to `SubscriptionController.Index`).
- **Caching**: In-memory caching with TTL and `SemaphoreSlim` synchronization.

## 2. Technology Stack & Tooling
- **Target Framework**: .NET 10.0 (`net10.0`, C# 13)
- **Framework SDK**: `Microsoft.NET.Sdk.Web`
- **Testing Framework**: xUnit v3 (`xunit.v3`, `Microsoft.NET.Test.Sdk`)
- **Containerization**: Docker multi-stage build (`mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0`) and Docker Compose

## 3. Coding Guidelines & Standards
Follow the Karpathy guidelines:
1. **Simplicity First**: Keep services minimal and single-purpose; avoid speculative abstractions or third-party dependencies unless strictly necessary.
2. **Surgical Changes**: Only modify lines required for the specific task or fix.
3. **Thread Safety**: Ensure proper double-checked locking patterns when managing shared mutable state or in-memory caches with `SemaphoreSlim`.
4. **Idiomatic C#**:
   - Utilize modern C# features (primary constructors, nullable reference types, file-scoped namespaces, pattern matching).
   - Maintain async/await hygiene across all I/O operations (`File.ReadAllTextAsync`, `WaitAsync`).

## 4. Secret & Credential Protection
- **Protect VPN Credentials**: The `subscriptions.json` file and `.env` files contain sensitive connection URIs (e.g., UUIDs, private keys, server IPs). **Never** inspect, read, or print these files in logs or chat responses.
- **Sanitized Fixtures for Testing**: Always use sanitized mock data (e.g. dummy domains like `example.com` and placeholder UUIDs) when writing tests or documentation.
- **No Real Infrastructure Data in Code, Tests, or Documentation**:
  - Never use real hostnames, public IP addresses, real domain names, or subscription URLs provided in user prompts or external endpoints in tests, example configuration files, or documentation.
  - Exclusively use RFC 2606 / RFC 6761 reserved domains (`*.example.com`, `*.example.org`), RFC 5737 documentation IP ranges (`192.0.2.0/24`, `198.51.100.0/24`, `203.0.113.0/24`), RFC 3849 IPv6 (`2001:db8::/32`), and RFC 4122 nil/placeholder UUIDs (`00000000-0000-0000-0000-000000000000`).
- **Configuration Hygiene**: Do not commit actual secrets to `appsettings*.json` or source control.

## 5. Build, Test & Run Commands
- **Build Solution**:
  ```bash
  dotnet build VPN.SimpleSub.sln
  ```
- **Run Tests**:
  ```bash
  dotnet test test/VPN.SimpleSub.Test/VPN.SimpleSub.Test.csproj
  ```
- **Run Web Project Locally**:
  ```bash
  dotnet run --project src/VPN.SimpleSub.Web/VPN.SimpleSub.Web.csproj
  ```
- **Docker Compose**:
  ```bash
  cd src/VPN.SimpleSub.Web && docker compose build && docker compose up -d
  ```

## 6. Pre-Modification & Execution Rules
- **Preview Changes**: Always display file paths and unified diffs/code blocks in chat before applying file edits or writes.
- **Command Confirmation**: Display exact commands and rationales before running terminal operations.
- **Verification**: Run `dotnet build` and `dotnet test` to verify changes prior to concluding tasks.

## 7. Documentation Maintenance
- **Update Architecture Documentation**: Whenever the system design, architecture, service interfaces, or configuration schemas are changed or refactored, always update `docs/Architecture.md` to reflect the latest design before completing the task.
