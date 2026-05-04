# InsurancePlatform.UnitTests

This project contains unit tests for the main business logic layers that can be tested without starting SQL Server, RabbitMQ, or the ASP.NET hosts.

Current coverage focuses on:
- Identity registration, login, status updates, password hashing, and JWT creation
- Ticket workflow rules like claim validation, assignment rules, and comments
- Policy workflow rules like create, purchase, renew, and ticket-policy validation
- Payment workflow rules for manual and policy-driven payments
- Admin reporting aggregations from event audit data

Run locally from `D:\Project`:

```powershell
$env:DOTNET_CLI_HOME='D:\Project\.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:NUGET_PACKAGES='D:\Project\.nuget\packages'

dotnet test .\InsurancePlatform.UnitTests\InsurancePlatform.UnitTests.csproj --configfile .\NuGet.Config
```
