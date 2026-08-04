# Task 001 - Prepare Backend Foundation

## Context

This project already contains an existing ASP.NET Core backend.

The project was created as an early prototype and its current architecture no longer matches the target architecture.

Your task is NOT to implement any business logic.

Your task is to prepare the project for a clean restart while preserving the existing solution structure.

---

# Goal

Transform the project into a clean foundation that will be used for a production-ready application.

Do not create unnecessary code.

Do not implement any feature.

Do not generate sample entities.

Only prepare the architecture.

---

# Architecture

The project follows Clean Architecture.

Layers:

- API
- Application
- Domain
- Infrastructure

Do not change this architecture.

---

# Required Actions

## 1.

Remove every prototype implementation that is not part of the project foundation.

Examples include but are not limited to:

- Demo Controllers
- Example Services
- Temporary DTOs
- Example Repositories
- Prototype Entities
- Unused Helpers
- Test Implementations
- Seed Data
- Fake Data

---

## 2.

Preserve:

- Solution
- Project References
- NuGet Packages
- Existing Folder Structure (unless clearly incorrect)

---

## 3.

Inside Domain/Common create the following structure if it does not already exist.

Domain
└── Common
├── Base
├── Constants
├── Enums
├── Exceptions
└── ValueObjects

Do not create any code yet.

Only folders.

---

## 4.

Inside every layer create a DependencyInjection class if it does not already exist.

Example:

Application/DependencyInjection.cs

Infrastructure/DependencyInjection.cs

---

## 5.

Do not create any Entity.

Do not create DbContext.

Do not create Repository.

Do not create Services.

Do not create Controllers.

---

## 6.

Remove every file that belongs to the old prototype and is no longer required.

If you are unsure whether a file should be removed, leave it untouched.

---

# Output

At the end provide a summary.

Example:

Deleted files

Created folders

Files kept

Architecture status

Do not make any additional improvements outside this task.
