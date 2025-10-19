# Architecture Decision Records (ADR)

## About This Document

This document captures the key architectural decisions made during the development of SaaSifyCore, including the context, reasoning, and trade-offs considered.

### Why Document Decisions?

- Knowledge Transfer
- Context Preservation
- Informed Changes
- Professional Standards

---

## Decision Log

---

## ADR-001: Use Clean Architecture Pattern

Context: Need long-term maintainable structure.

Decision: Adopt Clean Architecture with separated layers (API, Application, Domain, Infrastructure).

Rationale (summary): Framework independence, testability, technology agnostic, maintainability.

Consequences: More files and learning curve; benefits outweigh costs.

---

## ADR-002: No Repository Pattern Over EF Core

Context: Whether to wrap DbContext with repositories.

Decision: Use `IApplicationDbContext` only; no extra repositories.

Rationale: DbContext already provides needed patterns; reduces boilerplate; preserves LINQ flexibility.

Consequences: Direct EF Core exposure; acceptable trade-off.

---

## ADR-003: Use DDD Tactical Patterns

Context: Avoid anemic domain model.

Decision: Employ entities with behavior, value objects, domain events, factory methods.

Rationale: Centralized invariants, type safety, expressive model.

Consequences: More classes; improved clarity and correctness.

---

## ADR-004: Use CQRS with MediatR

Context: Prevent bloated controllers.

Decision: Separate commands and queries via MediatR.

Rationale: Single responsibility, testability, pipeline behaviors for cross-cutting concerns.

Consequences: More files; clearer separation.

---

## ADR-005: Multi-Tenancy via Shared DB + Query Filters

Context: Tenant data isolation strategy.

Decision: Shared database, global query filters, tenant resolution middleware.

Rationale: Cost-effective, automatic isolation, simpler operations.

Consequences: Needs careful performance management; acceptable for MVP.

---

## ADR-006: PostgreSQL as Primary Database

Context: Choose relational database.

Decision: Use PostgreSQL.

Rationale: Open-source, advanced features, strong JSON support, broad tooling.

Consequences: Team learning curve; mitigated by EF Core.

---

## ADR-007: JWT Authentication with Refresh Tokens

Context: Need secure, multi-tenant friendly auth.

Decision: Short-lived access tokens, long-lived refresh tokens, tenant claim, BCrypt hashing, rotation strategy.

Rationale: Stateless, mobile-ready, scalable, supports tenant isolation.

Consequences: Requires refresh token storage; mitigated by indexing and rotation.

---

## ADR-008: Custom Rate Limiting for Auth Endpoints

Context: Protect authentication surfaces from abuse.

Decision: Hybrid approach: library IP rate limiting + custom per-tenant/per-endpoint middleware.

Rationale: Fine-grained control, tenant fairness, brute force defense.

Consequences: In-memory limits not distributed; future option to move to Redis.

---

## ADR-009: Database Index Optimization

Context: Improve performance of auth and tenant-related queries.

Decision: Add covering, composite, and partial indexes (Users and RefreshTokens), optimize SaveChanges timestamp handling.

Rationale: Faster logins, token validation, reduced IO.

Consequences: Slight write overhead; justified by read performance gains.

---

## ADR-010: Explicit Tenant Validation for Security-Critical Operations

Context: Need visibility into cross-tenant access attempts.

Decision: Use `.IgnoreQueryFilters()` plus explicit tenant checks for auth/token flows.

Rationale: Enables auditing, attack detection, compliance support.

Consequences: Slightly more verbose; improves security observability.

---

## Decision-Making Framework

Factors: Principles (SOLID, YAGNI, KISS), trade-offs (complexity vs value), team impact, business impact, security and compliance.

---

## Change Log

| Date      | ADR #  | Description                                        |
|-----------|--------|----------------------------------------------------|
| Oct 2025  | ADR-001| Clean Architecture adopted                         |
| Oct 2025  | ADR-002| No repository pattern over EF Core                 |
| Oct 2025  | ADR-003| DDD tactical patterns adopted                      |
| Oct 2025  | ADR-004| CQRS with MediatR planned                          |
| Oct 2025  | ADR-005| Multi-tenancy via shared DB and query filters      |
| Oct 2025  | ADR-006| PostgreSQL selected                                |
| Oct 2025  | ADR-007| JWT auth with refresh tokens                       |
| Oct 2025  | ADR-008| Custom rate limiting for auth endpoints            |
| Oct 2025  | ADR-009| Database index optimization strategy               |
| Oct 2025  | ADR-010| Explicit tenant validation for security operations |

---

Note: Decisions may evolve; updates will include rationale.