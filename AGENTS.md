# Agent Guidelines: vpn-simple-subscription

Lightweight .NET 10 subscription service. See `docs/Architecture.md` for design & request flow.

## 1. Safety & Boundaries
- **No Git Push**: NEVER execute `git push`. Local commits (`git commit`) only when requested.
- **Commit Messages**: Must start with conventional type (`feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`) followed by a concise message.
- **Secrets Protection**: Never inspect `.env`, `subscriptions.json`, or credentials files.
- **Sanitized Data Only**: Never use real IPs, domains, or upstream URLs in tests or examples. Exclusively use RFC 2606 (`*.example.com`), RFC 5737 IPs (`198.51.100.0/24`), and nil UUIDs.
- **Ask Before Implementation**: Always propose and confirm the plan with the user before writing or modifying code.

## 2. Tooling & Development Rules
- **Tooling**: Strictly use .NET CLI (`dotnet`) and bash commands. Never invoke Python, Node, or external script runtimes.
- **Constants**: Use `SubscriptionHeaders.*` constants rather than string literals for subscription headers.
- **Documentation**: Proactively update `docs/Architecture.md` whenever system design, interfaces, or schemas change.

## 3. Core Commands
- **Build**: `dotnet build VPN.SimpleSub.sln`
- **Test**: `dotnet test test/VPN.SimpleSub.Test/VPN.SimpleSub.Test.csproj`
- **Run**: `dotnet run --project src/VPN.SimpleSub.Web/VPN.SimpleSub.Web.csproj`
