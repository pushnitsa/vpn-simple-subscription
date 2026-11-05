# Agent Guidelines for vpn-simple-subscription

## 1. Project Overview & Architecture
`vpn-simple-subscription` is a lightweight .NET 10 ASP.NET Core service providing subscription endpoints for VPN clients (e.g., Shadowrocket, v2rayN, Sing-box, Clash).
- **Architecture**: Employs the Strategy pattern (`ISubscriptionEngine`) to resolve configurations via either:
  - `StaticSubscriptionEngine`: Resolves local connection strings configured in `subscriptions.json`.
  - `FetchSubscriptionEngine`: Fetches dynamic multi-node subscriptions from upstream endpoints (e.g., 3x-UI) and applies targeted node overrides matching `matchSni` or `matchHost`.
- **Header Propagation**: Delivers subscription headers including user quota metrics (`Subscription-Userinfo`), `Profile-Title`, and `Profile-Update-Interval` (defaulting to 12 hours).
- **Endpoint Structure**: `{SubscriptionRoute}/{clientId}/plain` (mapped to `SubscriptionController.Index`).
- **Caching**: Thread-safe in-memory caching with 5-minute TTL and `SemaphoreSlim` synchronization.
- **Detailed Reference**: See `docs/Architecture.md` for full design and request flows.

## 2. Technology Stack & Tooling
- **Target Framework**: .NET 10.0 (`net10.0`, C# 13)
- **Framework SDK**: `Microsoft.NET.Sdk.Web`
- **Testing Framework**: xUnit v3 (`xunit.v3`, `Microsoft.NET.Test.Sdk`)
- **CI/CD**: GitHub Actions workflow (`.github/workflows/build-and-test.yml`)
- **Containerization**: Docker multi-stage build and Docker Compose
- **Tooling Rule**: Exclusively use .NET CLI (`dotnet`) and standard shell commands. Never use Python, Node, or external script runtimes for repo checks or tasks.

## 3. Coding Guidelines & Standards
Follow the Karpathy guidelines:
1. **Simplicity First**: Keep services minimal and single-purpose; avoid speculative abstractions.
2. **Surgical Changes**: Only modify lines required for the specific task or fix.
3. **Thread Safety**: Ensure proper double-checked locking patterns with `SemaphoreSlim` when managing shared mutable state or caches.
4. **Header Constants**: Always reference `SubscriptionHeaders.*` constants rather than raw string literals for subscription headers.
5. **Idiomatic C#**:
   - Utilize modern C# features (primary constructors, nullable reference types, file-scoped namespaces, pattern matching).
   - Maintain async/await hygiene across all I/O operations (`File.ReadAllTextAsync`, `WaitAsync`).

## 4. Secret & Credential Protection
- **Protect VPN Credentials**: The `subscriptions.json` file and `.env` files contain sensitive connection URIs (e.g., UUIDs, private keys, server IPs). **Never** inspect, read, or print these files in logs or chat responses.
- **Sanitized Fixtures for Testing**: Always use sanitized mock data (e.g. dummy domains like `example.com` and placeholder UUIDs) when writing tests or documentation.
- **No Real Infrastructure Data in Code, Tests, or Documentation**:
  - Never use real hostnames, public IP addresses, real domain names, or subscription URLs provided in user prompts or external endpoints in tests, example configuration files, or documentation.
  - Exclusively use RFC 2606 / RFC 6761 reserved domains (`*.example.com`, `*.example.org`), RFC 5737 documentation IP ranges (`192.0.2.0/24`, `198.51.100.0/24`, `203.0.113.0/24`), RFC 3849 IPv6 (`2001:db8::/32`), and RFC 4122 nil/placeholder UUIDs (`00000000-0000-0000-0000-000000000000`).
- **Configuration Hygiene**: Do not commit actual secrets to `appsettings*.json` or source control.

## 5. Git Operations
- **Never run `git push`**: Do NOT execute `git push` or attempt to push commits to remote repositories under any circumstances. Local commits (`git commit`) are allowed when requested, but pushing must always be left to the user.

## 6. Build, Test & Run Commands
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

## 7. Pre-Modification & Execution Rules
- **Preview Changes**: Always display file paths and unified diffs/code blocks in chat before applying file edits or writes.
- **Command Confirmation**: Display exact commands and rationales before running terminal operations.
- **Verification**: Run `dotnet build` and `dotnet test` to verify changes prior to concluding tasks.

## 8. Documentation Maintenance
- **Update Architecture Documentation**: Whenever system design, architecture, service interfaces, or configuration schemas are changed or refactored, always update `docs/Architecture.md` to reflect the latest design before completing the task.
- **Update Agent Guidelines (`AGENTS.md`)**: Keep `AGENTS.md` accurate and up-to-date. Whenever project workflows, operational rules, toolchains, dependencies, or conventions evolve, proactively update `AGENTS.md` to prevent documentation drift.
