# Insurance Platform — Frontend

React 18 + Vite SPA for the InsurancePlatform .NET 10 microservices backend.

## Tech Stack

| Concern | Library |
|---|---|
| Framework | React 18 (JSX) |
| Bundler | Vite 8 |
| Routing | react-router-dom v7 |
| Server state | @tanstack/react-query v5 |
| HTTP | axios (with JWT interceptor + refresh) |
| Forms | react-hook-form + yup |
| UI | MUI v9 + @mui/icons-material |
| Charts | recharts |
| Mocking | MSW v2 |

## Roles

| Role | Description |
|---|---|
| `Admin` | Full platform access, user management, reports |
| `Customer` | Browse/buy policies, file claims, payments |
| `ClaimsSpecialist` | Review and approve/reject claim tickets |
| `SupportSpecialist` | Handle support tickets |

## Local Development

```bash
# From D:\Project\frontend
npm install
npm run dev        # http://localhost:5173
```

### Environment

Copy `.env.example` to `.env`:

```
VITE_API_BASE_URL=http://localhost:5000
VITE_AUTH_STRATEGY=memory
VITE_ENV=development
```

When `VITE_ENV=development`, MSW intercepts all API calls — no backend needed.

### Mock Credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@example.com | any8chars |
| Customer | john@example.com | any8chars |
| Register | any email | any8chars → Customer role |

## Scripts

```bash
npm run dev        # Start dev server (http://localhost:5173)
npm run build      # Production build → dist/
npm run preview    # Preview production build
npm run lint       # ESLint
npm run format     # Prettier
npm run test       # Vitest (single run)
npm run e2e        # Playwright E2E
```

## Project Structure

```
src/
  api/           # axios functions per resource
  app/           # AuthContext, ToastContext, theme, routes, roles
  components/
    layout/      # AppShell, Sidebar, TopBar
    ui/          # StatCard, StatusChip, ConfirmDialog, PageLoader
  hooks/         # React Query hooks (usePolicies, useTickets, ...)
  mocks/         # MSW handlers + browser worker
  pages/
    public/      # Landing, Login, Register, ForgotPassword, Unauthorized
    admin/       # AdminDashboard, AdminUsers, AdminPolicies, AdminReports
    customer/    # CustomerDashboard, BrowsePolicies, MyPolicies, Tickets, Payments, Notifications
    specialist/  # SpecialistDashboard, TicketsManagement, TicketDetail
```

## API → Frontend Mapping

| Endpoint | API Function | Hook |
|---|---|---|
| POST /identity/auth/login | `auth.login()` | `useAuth().login()` |
| POST /identity/auth/register | `auth.register()` | `useAuth().register()` |
| GET /identity/admin/users | `users.getUsers()` | `useUsers()` |
| POST /identity/admin/create-claims-specialist | `users.createClaimsSpecialist()` | `useCreateClaimsSpecialist()` |
| POST /identity/admin/create-support-specialist | `users.createSupportSpecialist()` | `useCreateSupportSpecialist()` |
| PATCH /identity/admin/users/:id/status | `users.updateUserStatus()` | `useUpdateUserStatus()` |
| GET /policies | `policies.getPolicies()` | `usePolicies()` |
| GET /customer-policies | `policies.getCustomerPolicies()` | `useCustomerPolicies()` |
| POST /policies | `policies.createPolicy()` | `useCreatePolicy()` |
| PUT /policies/:id | `policies.updatePolicy()` | `useUpdatePolicy()` |
| POST /purchase | `policies.purchasePolicy()` | `usePurchasePolicy()` |
| POST /customer-policies/:id/renew | `policies.renewPolicy()` | `useRenewPolicy()` |
| GET /tickets | `tickets.getTickets()` | `useTickets()` |
| POST /tickets | `tickets.createTicket()` | `useCreateTicket()` |
| PUT /tickets/:id/status | `tickets.updateTicketStatus()` | `useUpdateTicketStatus()` |
| PUT /tickets/:id/assign | `tickets.assignTicket()` | `useAssignTicket()` |
| POST /tickets/:id/comments | `tickets.addComment()` | `useAddComment()` |
| POST /tickets/:id/approve | `tickets.approveClaim()` | `useApproveClaim()` |
| POST /tickets/:id/reject | `tickets.rejectClaim()` | `useRejectClaim()` |
| GET /payments | `payments.getPayments()` | `usePayments()` |
| POST /payments | `payments.createPayment()` | `useCreatePayment()` |
| GET /notifications | `notifications.getNotifications()` | `useNotifications()` |
| POST /notifications/:id/read | `notifications.markNotificationRead()` | `useMarkNotificationRead()` |
| GET /admin/dashboard | `reports.getDashboard()` | `useDashboard()` |
| GET /admin/reports/* | `reports.*()` | `useTicketReports()`, etc. |

## Role → Route Matrix

| Route | Admin | Customer | ClaimsSpecialist | SupportSpecialist |
|---|---|---|---|---|
| `/` (Landing) | ✓ | ✓ | ✓ | ✓ |
| `/login` | ✓ | ✓ | ✓ | ✓ |
| `/register` | ✓ | ✓ | ✓ | ✓ |
| `/admin/dashboard` | ✓ | ✗ | ✗ | ✗ |
| `/admin/users` | ✓ | ✗ | ✗ | ✗ |
| `/admin/policies` | ✓ | ✗ | ✗ | ✗ |
| `/admin/reports` | ✓ | ✗ | ✗ | ✗ |
| `/dashboard` | ✗ | ✓ | ✗ | ✗ |
| `/browse-policies` | ✗ | ✓ | ✗ | ✗ |
| `/my-policies` | ✗ | ✓ | ✗ | ✗ |
| `/tickets` | ✗ | ✓ | ✗ | ✗ |
| `/payments` | ✗ | ✓ | ✗ | ✗ |
| `/notifications` | ✗ | ✓ | ✗ | ✗ |
| `/specialist/dashboard` | ✓ | ✗ | ✓ | ✓ |
| `/specialist/tickets` | ✓ | ✗ | ✓ | ✓ |
| `/specialist/tickets/:id` | ✓ | ✗ | ✓ | ✓ |

## Security Notes

- Access token stored **in-memory** (not localStorage) — mitigates XSS token theft
- Refresh token expected as **httpOnly cookie** from backend
- Axios interceptor auto-refreshes on 401 and retries the original request
- `RoleGuard` component protects all authenticated routes
- CORS: backend must allow `http://localhost:5173` in development
