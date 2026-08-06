# Implementation Workflow

Priority: Highest

Every implementation in this project must follow this workflow.

Do not skip phases.

Complete each phase before moving to the next one.

---

# Project Development Flow

Phase 1

Project Foundation

↓

Phase 2

Domain Layer

↓

Phase 3

Infrastructure Layer

↓

Phase 4

Database

↓

Phase 5

Authentication

↓

Phase 6

Application Layer (CQRS)

↓

Phase 7

API Layer

↓

Phase 8

Real Time Features

↓

Phase 9

Background Jobs

↓

Phase 10

Testing

---

# Phase 1

Project Foundation

Goal

Prepare the project infrastructure.

Tasks

- Configure solution structure
- Configure Dependency Injection
- Configure Serilog
- Configure Mapster
- Configure FluentValidation
- Configure MediatR
- Configure PostgreSQL
- Configure Supabase
- Configure JWT
- Configure Options pattern

Deliverable

A compilable project with all infrastructure packages installed.

---

# Phase 2

Domain Layer

Goal

Build the business model.

Order

1.

Common

- BaseEntity
- AuditableEntity
- AggregateRoot

2.

Enums

3.

Constants

4.

Exceptions

5.

Value Objects

6.

Entities

Order

Users

↓

Sports

↓

Events

↓

Messaging

↓

Reviews

↓

Social

↓

Gamification

↓

Moderation

Deliverable

Complete Domain Layer

No Infrastructure dependency allowed.

---

# Phase 3

Infrastructure Layer

Goal

Implement persistence.

Order

DbContext

↓

DbSets and convention model review

↓

Repositories

↓

Services

↓

Supabase Storage

↓

JWT

↓

OTP

Deliverable

Infrastructure ready.

---

# Phase 4

Database

Goal

Generate database schema.

Order

DbSets and convention model review

↓

Migration

↓

Database Update

↓

Seed Data

Seed Data

- Sports
- Report Reasons
- Badges

Deliverable

Working PostgreSQL database.

---

# Phase 5

Authentication

Goal

Implement authentication.

Order

Phone Login

↓

OTP Verification

↓

JWT

↓

Refresh Token

↓

Device Registration

↓

User Sessions

↓

Authorization

Deliverable

Complete authentication system.

---

# Phase 6

Application Layer

Architecture

CQRS

Every feature follows

Feature

↓

Commands

↓

Queries

↓

Validators

↓

Handlers

↓

DTOs

Implementation Order

Identity

↓

Events

↓

Messaging

↓

Reviews

↓

Social

↓

Gamification

↓

Moderation

Deliverable

Complete business layer.

---

# Phase 7

API Layer

Controllers remain thin.

Controllers

↓

MediatR

↓

Application

Never call repositories directly.

Order

Identity

↓

Events

↓

Messaging

↓

Reviews

↓

Social

↓

Gamification

↓

Moderation

Deliverable

REST API completed.

---

# Phase 8

Real-Time Features

Order

SignalR

↓

Event Chat

↓

Typing Indicator

↓

Online Presence

↓

Push Notifications

Deliverable

Complete real-time infrastructure.

---

# Phase 9

Background Jobs

Goal

Move heavy operations out of request pipeline.

Examples

- Notification delivery
- Badge calculations
- Statistics updates
- Attendance calculations
- Cleanup expired sessions
- Cleanup expired OTP codes
- Scheduled recurring events

Deliverable

Background processing infrastructure.

---

# Phase 10

Testing

Unit Tests

↓

Integration Tests

↓

API Tests

↓

Performance Tests

↓

Security Tests

Deliverable

Production-ready application.

---

# General Development Rules

Always implement in this order

Entity

↓

DbSet

↓

Convention model review

↓

Migration

↓

CQRS

↓

Controller

↓

Tests

Never implement controllers before business logic.

Never create migrations before the convention model has been reviewed.

Use `IApplicationDbContext` by default. Add a domain-specific repository only
when a real query abstraction is justified; never add generic repositories or
a separate Unit of Work layer.

---

# Cursor Rules

When implementing a task:

- Follow BACKEND_STANDARDS.md.
- Follow database-reference.md.
- Follow database-erd.md.
- Follow the corresponding database documentation.
- Do not modify unrelated files.
- Do not introduce new architecture.
- Do not generate placeholder code.
- Do not ignore project standards.
- Stop immediately if required information is missing.

---

# Definition of Done

A task is considered complete only if:

- Project builds successfully.
- No compiler warnings related to the implementation.
- Follows project architecture.
- Follows naming conventions.
- Uses EF Core conventions; Fluent API only for documented exceptions.
- Uses MediatR.
- Uses FluentValidation.
- Uses Result Pattern.
- Uses Mapster.
- Uses dependency injection.
- Includes logging where appropriate.
- Includes authorization where required.
- No duplicated business logic.
- Code is production-ready.

---

# Development Philosophy

Build the application as if it will be maintained by a team of senior engineers for many years.

Prioritize:

- Readability
- Maintainability
- Scalability
- Performance
- Security

Avoid shortcuts.

Prefer explicit and understandable code over clever implementations.

Every implementation should be suitable for a production environment.
