```markdown
# 📌 MyTasks

**MyTasks** is a task management application that combines a **React + Vite frontend** with an **Azure Functions backend**.  
The system integrates authentication with **Microsoft Entra ID (External Identities)**, persistence in **SQL using Entity Framework (Code First)**, and a design focused on **mobile first**.

---

## 🚀 Main Technologies

### 🔹 Backend (Azure Functions)

- **Azure Functions Isolated v4** (.NET 9)
- **Entity Framework Core (Code First)** with SQL
- **Microsoft Entra ID** for authentication/authorization
- **Graph API** for user data and provisioning
- Patterns: **Factory**, **Repository**, **Dependency Injection**
- Custom middleware for request validation (designed to migrate to APIM)

### 🔹 Frontend (React + Vite)

- **React 18 + Vite**
- **TanStack Query** (data fetching + caching)
- **React Hook Form** for forms
- Authentication integrated with **frontend app registration** in Microsoft Entra

---

## 📂 Project Structure
```

api/
│
│
├── Data/
│ └── Repositories/
│ ├── Implementations/ # Repository implementations
│ └── Interfaces/ # Repository interfaces
│
├── Functions/ # Azure Functions endpoints
│
├── Helpers/
│ ├── Middlewares/ # Custom middlewares
│ └── Validations/ # Input validation helpers
│
├── Migrations/ # Entity Framework migrations
│
├── Models/
│ ├── Dtos/ # Data Transfer Objects
│ ├── Entities/ # Database entities
│ └── Factories/ # Factory pattern implementations
│
├── Properties/ # Project settings
│
├── Services/
│ ├── Implementations/ # Service implementations
│ └── Interfaces/ # Service interfaces
│
└── README.md # Project documentation

client/
│
├───public/ # Static public files
└───src/
├───app/ # Core application logic
│ ├───context/ # Global state (React Context API)
│ ├───helpers/ # Utility functions and helpers
│ ├───hooks/ # Custom React hooks
│ ├───routes/ # Application routes
│ ├───services/ # API and external service logic
│ └───types/ # TypeScript types and interfaces
│
├───assets/ # Static assets
│ └───icons/ # Application icons
│
└───components/ # UI components
├───atoms/ # Smallest building blocks (buttons, inputs, etc.)
├───molecules/ # Compositions of atoms
├───organisms/ # More complex UI components
│ ├───PreviewGrid/ # Specific UI module
│ └───TopBar/ # Top navigation bar
└───pages/ # Page-level components (views)

````

---

## ⚙️ Getting Started

### 1️⃣ Backend (Azure Functions)
1. Install dependencies:
   ```bash
   cd api
   dotnet restore
````

2. Configure environment variables in `local.settings.json`:

   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "SqlConnectionString": "",
       "AppRegistrationCliendId": "",
       "TenantId": "",
       "AppRegistrationClientSecret": ""
     },
     "Host": { "CORS": "*" }
   }
   ```

3. Run initial migrations:

   ```bash
   dotnet ef database update
   ```

4. Start the local server:

   ```bash
   func start
   ```

### 2️⃣ Frontend (React + Vite)

1. Install dependencies:

   ```bash
   cd client
   npm install
   ```

2. Configure environment variables in `.env`:

   ```env
    VITE_ENTRA_CLIENT_ID=
    VITE_ENTRA_CLIENT_AUTHORITY=
    VITE_ENTRA_SCOPE=
    VITE_ENTRA_REDIRECT_URI=
    VITE_ENTRA_LOGOUT_URI=
    VITE_SERVER_API_URL=
   ```

3. Start the local development server:

   ```bash
   npm run dev
   ```

---

## 🔑 Authentication

- **Frontend:** retrieves a JWT token from Microsoft Entra (email or Google account).
- **Backend:** validates the token, checking `aud` and `iss`.
- **Graph API:** is used to fetch user data and handle provisioning.

---

## 🛠️ Database & Migrations

The backend uses **EF Core Code First**.

- Create a new migration:

  ```bash
  dotnet ef migrations add MigrationName
  ```

- Apply migrations:

  ```bash
  dotnet ef database update
  ```

---

## 📈 Observability & CI/CD

Pending implementation:

- Structured logging in backend and frontend
- Basic metrics and distributed tracing
- CI/CD pipeline including:

  - Linting
  - Unit tests (Already integrated)
  - Migrations
  - Controlled deployment

---

## 🔒 Operational Security

- Prefer **Managed Identities** whenever possible
- Apply **least privilege** principle for Graph permissions
- Periodically rotate client secrets
- Backend enforces token validation
- Secure configuration for CORS, headers, and rate limiting

## 📜 License

This project is licensed under the **MIT License**.
See the [LICENSE](./LICENSE) file for details.
