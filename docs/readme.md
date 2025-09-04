# 📌 MyTasks

**MyTasks** is a modern task management application combining a **React + Vite frontend** with a robust **Azure Functions backend**.  
The system leverages **Microsoft Entra ID (External Identities)** for secure authentication, persists data in **SQL Server via Entity Framework Core (Code First)**, and follows a **mobile-first design philosophy**.

---

## 🚀 Core Technologies

### 🔹 Backend (Azure Functions)

- **Azure Functions Isolated v4** (.NET 9) for scalable serverless APIs.
- **Entity Framework Core (Code First)** with SQL Server for data persistence.
- **Microsoft Entra ID** for authentication and authorization, with two dedicated App Registrations (frontend and backend) to ensure secure token flows.
- **Microsoft Graph API** integration to securely fetch and provision user data.
- Architectural patterns:
  - **Repository Pattern** for clean data access abstraction.
  - **Factory Pattern** to flexibly create different notification types.
  - **Dependency Injection** for modular, testable services.
- Custom middleware for JWT validation and global exception handling, designed to be replaceable by Azure API Management (APIM) when available.
- Pagination and filtering implemented on API endpoints for efficient data retrieval.
- Notification system designed with a publisher abstraction, currently using a no-op publisher but ready to extend to SignalR, email, or push notifications.

### 🔹 Frontend (React + Vite)

- **React 18** with **Vite** for fast, modern frontend development.
- **Mobile-first** responsive UI design.
- **TanStack Query** for efficient data fetching, caching, and synchronization.
- **React Hook Form** for robust form validation and management.
- Custom React hooks to encapsulate API calls and avoid repetitive code.
- Authentication integrated with Microsoft Entra ID via a dedicated frontend App Registration, enabling secure token acquisition and API access.

---

## 📂 Project Structure Overview

```
api/
├── Data/
│   ├── Repositories/
│   │   ├── Implementations/   # EF Core repository implementations
│   │   └── Interfaces/        # Repository interfaces
├── Functions/                 # Azure Functions endpoints (Dashboard, Tasks, Notifications, Users)
├── Helpers/
│   ├── Middlewares/           # JwtValidationMiddleware, GlobalExceptionMiddleware
│   └── Validations/           # Input validation helpers
├── Migrations/                # EF Core migrations
├── Models/
│   ├── Dtos/                  # Data Transfer Objects
│   ├── Entities/              # Database entities
│   └── Factories/             # Notification factory implementations
├── Properties/                # Project settings
├── Services/
│   ├── Implementations/       # Business logic services
│   └── Interfaces/            # Service interfaces
└── Program.cs                 # Application entry point

client/
├── public/                    # Static assets
└── src/
    ├── app/
    │   ├── context/           # React Context API for global state
    │   ├── helpers/           # Utility functions
    │   ├── hooks/             # Custom React hooks for API calls
    │   ├── routes/            # Application routing
    │   ├── services/          # API service layer
    │   └── types/             # TypeScript types and interfaces
    ├── assets/                # Icons and static assets
    └── components/
        ├── atoms/             # Small reusable UI components (buttons, inputs)
        ├── molecules/         # Compositions of atoms
        ├── organisms/         # Complex UI components (e.g., PreviewGrid, TopBar)
        └── pages/             # Page-level components (views)
```

---

## ⚙️ Getting Started

### Backend Setup

1. Restore dependencies:

   ```bash
   cd api
   dotnet restore
   ```

2. Configure environment variables in `local.settings.json`:

   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "SqlConnectionString": "<Your SQL Connection String>",
       "AppRegistrationClientId": "<Backend App Registration Client ID>",
       "TenantId": "<Azure Tenant ID>",
       "AppRegistrationClientSecret": "<Backend Client Secret>",
       "EnableJwtMiddleware": "<Boolean>"
     },
     "Host": { "CORS": "*" }
   }
   ```

3. Apply database migrations:

   ```bash
   dotnet ef database update
   ```

4. Start the Azure Functions host locally:

   ```bash
   func start
   ```

   > **Note:** Setting the environment variable `"EnableJwtMiddleware"` to `false` will bypass the authentication middleware, effectively disabling JWT validation. Use this only for local development or testing purposes, as it disables security checks.

### Frontend Setup

1. Install dependencies:

   ```bash
   cd client
   npm install
   ```

2. Configure environment variables in `.env`:

   ```env
   VITE_ENTRA_CLIENT_ID=<Frontend App Registration Client ID>
   VITE_ENTRA_CLIENT_AUTHORITY=https://login.microsoftonline.com/<Tenant ID>
   VITE_ENTRA_SCOPE=<Backend API scope>
   VITE_ENTRA_REDIRECT_URI=http://localhost:3000
   VITE_ENTRA_LOGOUT_URI=http://localhost:3000
   VITE_SERVER_API_URL=http://localhost:7071/api
   ```

3. Start the development server:

   ```bash
   npm run dev
   ```

---

## ☁️ Azure App Registration & Environment Configuration

To properly configure authentication and authorization, you need to create two separate App Registrations in Azure Active Directory: one for the **backend API** and one for the **frontend client**. This setup ensures secure token issuance and validation between the frontend and backend.

### 1. Create App Registrations

- **Backend API App Registration**

  - Represents your protected API.
  - Used to expose scopes and permissions.
  - Used by the backend to validate tokens and call Microsoft Graph.

- **Frontend Client App Registration**
  - Represents your React frontend application.
  - Used to request tokens for accessing the backend API.

### 2. Configure Backend App Registration

- Go to **Expose an API** tab.
- Set the **Application ID URI** (usually `api://{backend-app-client-id}`).
- Add a new **scope** by filling out the form:

  - Scope name (e.g., `access_as_user`)
  - Who can consent (Admins only or Admins and users)
  - Admin consent display name and description
  - User consent display name and description
  - Enable the scope (State: Enabled)

- To get the **Client Secret**:
  - Navigate to **Certificates & secrets**.
  - Create a new **Client Secret** and copy its value (you won’t be able to see it again).

### 3. Configure Frontend App Registration

- Go to **API permissions** tab.
- Click **Add a permission** → **APIs my organization uses**.
- Search and select your backend API app registration.
- Select the scope you created (e.g., `access_as_user`).
- Grant admin consent if required.

---

## 🔐 Authentication Flow

- The frontend authenticates users via Microsoft Entra ID, supporting email and Google accounts.
- Upon successful login, the frontend obtains a JWT token scoped for the backend API.
- The backend validates the token’s signature, audience, and issuer.
- User profile data is securely fetched from Microsoft Graph API using backend App Registration permissions.
- This separation ensures secure, scalable, and maintainable authentication flows.

---

## 🗄️ Database & Migrations

The backend uses the **Entity Framework Core Code First** approach.

- Define or update your entities in code.
- Create migrations using the CLI:

  ```bash
  dotnet ef migrations add <MigrationName>
  ```

- **Applying migrations is automatic on application startup** thanks to the following code in `Program.cs`:

  ```csharp
  using (var scope = app.Services.CreateScope())
  {
      var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
      dataContext.Database.Migrate();
  }
  ```

  This means you do **not** need to run `dotnet ef database update` manually; the app will apply any pending migrations when it starts, ensuring the database schema is always up to date.

- Additionally, the database schema is automatically updated on application startup by applying any pending migrations, including the creation of the stored procedure (`sp_AddNotification`) which efficiently inserts notifications and returns the inserted record in a single operation, enhancing performance and consistency.

---

## 🛠️ Development Workflow & CI/CD

- Branching strategy:
  - `main`: stable production-ready code.
  - `develop`: integration and testing.
- GitHub Actions runs unit tests on every pull request targeting `develop`.
- Future plans include adding linting, integration tests, and automated deployments.

---

## 🚧 Roadmap & Improvements

- Implement soft-delete for data persistence to allow safe record removal.
- Add optimistic UI updates to improve user experience.
- Replace polling with SignalR or WebSockets for real-time notifications.
- Introduce API Management (APIM) for advanced security, rate limiting, and monitoring.
- Add telemetry (logging, metrics, distributed tracing) for observability.
- Harden role-based access control (RBAC) with fine-grained permissions.
- Expand user profile management with additional fields (avatar, phone, address).
