# GeneFlow ApiNet2 - Session Memory

## Project Overview
- **Backend:** .NET 8 ASP.NET Core Minimal APIs with Clean Architecture
- **Frontend:** Next.js with App Router (localhost:3000)
- **Database:** PostgreSQL 16 (Docker container: geneflow_postgres)
- **Backend Port:** 5286

## Database Configuration
- **Connection String:** `Host=127.0.0.1;Port=5432;Database=geneflow;Username=geneflow;Password=geneflow;Timeout=60;Command Timeout=120`
- **Important:** Use `127.0.0.1` instead of `localhost` to avoid Npgsql timeout issues
- **Schema:** `identity` (for User-related tables)
- **Tables:** users, external_logins, refresh_tokens, two_factor_codes

## EF Core Tools
- **Version:** 8.0.11 (must match project's EF Core version)
- **Install command:** `dotnet tool install --global dotnet-ef --version 8.0.11`
- **Migration command:** `dotnet ef database update --context UserContext --startup-project ../GeneFlow.ApiNet2.API`

## OAuth Configuration
- **Google Client ID:** Configured in appsettings.json
- **GitHub Client ID:** Pending configuration
- **Frontend env:** `.env.local` with `NEXT_PUBLIC_GOOGLE_CLIENT_ID` and `NEXT_PUBLIC_GITHUB_CLIENT_ID`

## Key Files Modified (OAuth Implementation)
### Backend (GeneFlow.ApiNet2)
- `API/appsettings.json` - OAuth settings, TwoFactor settings, Lockout settings
- `API/Endpoints/Identity/AuthEndpoints.cs` - OAuth login endpoint
- `API/Endpoints/Identity/UserEndpoints.cs` - 2FA setup, external logins endpoints
- `Application/Identity/Commands/OAuthLogin/` - OAuth login command
- `Application/Identity/Commands/SetupTwoFactor/` - TOTP 2FA setup
- `Application/Identity/Commands/ConfirmTwoFactorSetup/` - Confirm 2FA with code
- `Application/Identity/Commands/LinkExternalLogin/` - Link OAuth account
- `Application/Identity/Commands/UnlinkExternalLogin/` - Unlink OAuth account
- `Application/Identity/Queries/GetUserExternalLogins/` - List linked providers
- `Infrastructure/Identity/Services/OAuth/` - OAuth validators (Google, GitHub)
- `Infrastructure/Identity/Services/TwoFactorAuthenticator.cs` - TOTP implementation

### Frontend (geneflow-frontend)
- `app/stores/auth-store.ts` - oAuthLogin function
- `app/[locale]/(auth)/login/page.tsx` - Google/GitHub sign-in buttons
- `app/[locale]/(auth)/register/page.tsx` - Google/GitHub sign-up buttons
- `app/auth/callback/github/page.tsx` - GitHub OAuth callback (popup flow)
- `app/lib/error-messages.ts` - OAuth error messages
- `app/components/auth/TwoFactorSetup.tsx` - 2FA setup component
- `app/components/auth/ExternalLogins.tsx` - Manage linked OAuth accounts

## Common Issues & Solutions

### PostgreSQL Connection Timeout
- **Symptom:** `Npgsql.NpgsqlException: Exception while reading from stream`
- **Solution:** Use `127.0.0.1` instead of `localhost` in connection string

### PostgreSQL Authentication Failed
- **Symptom:** `28P01: password authentication failed for user "geneflow"`
- **Solution:** Delete Docker volume and recreate container:
  ```bash
  docker compose down postgres
  docker volume rm geneflowapinet_pgdata
  docker compose up -d postgres
  ```

### EF Core Tools Version Mismatch
- **Symptom:** Migration errors or unexpected behavior
- **Solution:** Match EF tools version with project:
  ```bash
  dotnet tool uninstall --global dotnet-ef
  dotnet tool install --global dotnet-ef --version 8.0.11
  ```

### Port Already in Use
- **Symptom:** `Failed to bind to address http://127.0.0.1:5286: address already in use`
- **Solution:** Kill dotnet processes: `taskkill /F /IM dotnet.exe`

### Swashbuckle v10 Compatibility
- **Issue:** OpenAPI types changed in Swashbuckle v10
- **Solution:** Downgraded to Swashbuckle v6.5.0

## Docker Services
```bash
# Start all services
cd C:\develop\geneflow\geneflow-backend\GeneFlow.ApiNet
docker compose up -d

# Key containers
- geneflow_postgres (PostgreSQL 16)
- geneflow_redis (Redis 7)
- geneflow_mailpit (Email testing - http://localhost:8025)
```

## Running the Application
```bash
# Backend
cd C:\develop\geneflow\geneflow-backend\GeneFlow.ApiNet2\GeneFlow.ApiNet2.API
dotnet run --launch-profile http

# Frontend
cd C:\develop\geneflow\geneflow-frontend
npm run dev
```

## API Endpoints (OAuth/2FA)
- `POST /api/v1/auth/oauth/{provider}` - OAuth login (google/github)
- `GET /api/v1/users/2fa/setup` - Get TOTP setup (QR code URI)
- `POST /api/v1/users/2fa/confirm` - Confirm 2FA setup with code
- `DELETE /api/v1/users/2fa` - Disable 2FA
- `GET /api/v1/users/external-logins` - List linked OAuth providers
- `POST /api/v1/users/external-logins` - Link OAuth account
- `DELETE /api/v1/users/external-logins/{provider}` - Unlink OAuth account

## Migration Progress

### Completed Bounded Contexts

| BC | Domain | Application | Infrastructure | API | Tests | Status |
|----|:------:|:-----------:|:--------------:|:---:|:-----:|:------:|
| SharedKernel | ✅ | ✅ | ✅ | - | - | 100% |
| Identity | ✅ | ✅ | ✅ | ✅ | ✅ | 100% |
| Profiles | ✅ | ✅ | ✅ | ✅ | ✅ | 100% |
| Plans | ✅ | ✅ | ✅ | ✅ | ✅ | 100% |
| Subscriptions | ✅ | ✅ | ✅ | ✅ | ✅ | 100% |

### Pending Bounded Contexts
- Studies
- Traces
- Alignments
- Pipelines
- Settings

### Plans & Subscriptions Implementation Details

**Endpoints Plans (`/api/v1/plans`):**
- `GET /` - Get all active plans
- `GET /{planId}` - Get plan by ID

**Endpoints Subscriptions (`/api/v1/subscriptions`):**
- `GET /current` - Get current user subscription
- `GET /history` - Get subscription history
- `POST /` - Create new subscription
- `POST /cancel` - Cancel subscription
- `POST /change-plan` - Change to different plan

**Tests Added (2024-04-11):**
- Domain/Plans: PlanTests, PlanNameTests, PlanPricingTests, PlanLimitsTests
- Domain/Subscriptions: SubscriptionTests, SubscriptionPeriodTests, SubscriptionStatusTests
- Application/Plans: GetAllPlansQueryHandlerTests
- Application/Subscriptions: CreateSubscriptionCommandHandlerTests, CancelSubscriptionCommandHandlerTests, ChangePlanCommandHandlerTests, GetCurrentSubscriptionQueryHandlerTests

**Total Tests:** 218 (all passing)

## Plan File Location
`C:\Users\video\.claude\plans\replicated-honking-hummingbird.md`
