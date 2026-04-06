# Insurance Platform Microservices

Services and default ports:
- ApiGateway: http://localhost:5000
- IdentityService: http://localhost:5001
- TicketService: http://localhost:5002
- PolicyService: http://localhost:5003
- PaymentService: http://localhost:5004
- NotificationService: http://localhost:5005
- AdminService: http://localhost:5006

Implemented areas:
- JWT authentication with seeded admin in IdentityService
- Ticket and claim handling with comments in TicketService
- Policy catalog and purchase flow in PolicyService
- Payment processing in PaymentService
- RabbitMQ-backed notification consumption in NotificationService
- Event audit dashboard in AdminService
- Ocelot routing and JWT validation in ApiGateway
- EF Core Code First models and initial migrations for each data-owning service

Setup:
1. Start SQL Server LocalDB or update each service appsettings connection string.
2. Start RabbitMQ locally on the default guest/guest connection or update appsettings.
3. Run the services in this order: Identity, Ticket, Policy, Payment, Notification, Admin, ApiGateway.
4. Register a customer with POST /identity/auth/register through the gateway.
5. Sign in with POST /identity/auth/login and use the returned Bearer token.
6. Use the seeded admin account from IdentityService/appsettings.json to create specialists.

Build command:
- dotnet build .\InsurancePlatform.sln --no-restore -m:1
