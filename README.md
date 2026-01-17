# User Management System (3-Tier Architecture)

A robust, enterprise-grade User Management System built with .NET 10, MongoDB, and a clean 3-tier architecture. This project demonstrates best practices in modern web development, including the Result Pattern, Strategy Pattern for validation, secure password hashing, and comprehensive unit testing.

## 🏗️ Architecture Overview

The solution is divided into four main projects to ensure clear separation of concerns:

- **UserManagement.API**: The entry point. Handles HTTP requests, API documentation (Swagger), and global exception handling.
- **UserManagement.Services**: Contains the core business logic. Manages user registration orchestration, password hashing with BCrypt, and high-level validation.
- **UserManagement.Repository**: The data access layer. Implements a generic repository pattern for MongoDB, handling persistence and secondary indexes.
- **UserManagement.Shared**: A shared project containing cross-cutting concerns like DTOs, Entities, Interfaces, and the `Result` pattern.

## ✨ Key Features

- **3-Tier Architecture**: Explicit layers for Presentation (API), Business Logic (Services), and Data Access (Repository).
- **Advanced Validation**:
  - **DTO Validation**: Uses `FluentValidation` for structural and format checks (Regex, Length, etc.).
  - **Business Validation**: Strategy-based validation via `IBusinessValidator` for complex rules (Email/Phone uniqueness).
- **Security**: 
  - Industry-standard password hashing using **BCrypt.Net**.
  - Strict password complexity policies.
- **Reliable Data Access**:
  - **Generic Repository**: Abstractions for common CRUD operations.
  - **MongoDB Integration**: Flexible NoSQL storage with optimized indexing.
  - **Soft Delete**: Built-in support for safe record deletion.
- **Robust Error Handling**: Uses the **Result Pattern** to handle business failures without exceptions.
- **Observability**: Rich logging using **Serilog** with console and file sinks.
- **Full Test Coverage**: Unit tests for all layers using **xUnit**, **Moq**, and **FluentAssertions**.

## 🛠️ Tech Stack

- **Framework**: .NET 10
- **Database**: MongoDB
- **Security**: BCrypt.Net-Next
- **Validation**: FluentValidation
- **Logging**: Serilog
- **Testing**: xUnit, Moq, FluentAssertions
- **Documentation**: Swagger/OpenAPI

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/try/download/community) (Running locally on `localhost:27017` by default)

### Configuration

Update the `src/UserManagement.API/appsettings.json` file with your MongoDB connection string if different from the default:

```json
"MongoDbSettings": {
  "ConnectionString": "mongodb://localhost:27017",
  "DatabaseName": "UserManagementDb"
}
```

### Build & Run

1.  **Build the solution**:
    ```bash
    dotnet build UserManagement.slnx
    ```
2.  **Run the API**:
    ```bash
    dotnet run --project src/UserManagement.API/UserManagement.API.csproj
    ```
3.  **Access Swagger UI**:
    Open `https://localhost:<port>/index.html` (or `http` equivalent) to view and test the API endpoints.

### Running Tests

Execute the comprehensive test suite:
```bash
dotnet test
```

## 📋 Business Rules (User Registration)

The system enforces strict registration rules:

- **Identity**:
  - Email must be unique (case-insensitive).
  - Phone number (Bangladesh format: `+8801XXXXXXXXX`) must be unique if provided.
- **Password Policy**:
  - Minimum 10 characters.
  - Must include: Upper, Lower, Digit, and Special character.
  - Cannot contain email prefix or phone number substrings.
  - Blocks common weak passwords (e.g., "password123").
- **Profile**:
  - First/Last name: 2-50 chars, letters/hyphens/apostrophes only.
  - Age: Minimum 13 years old (if DOB provided).

## 📄 License

This project is specialized for hackathon demonstrations and follows standard collaborative coding practices.
