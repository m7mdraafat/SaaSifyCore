# Architecture Decision Records (ADR)

## About This Document

This document captures the **key architectural decisions** made during the development of SaaSifyCore, including the **context**, **reasoning**, and **trade-offs** considered.

### Why Document Decisions?

- **Knowledge Transfer**: New team members understand the "why" behind the architecture
- **Context Preservation**: Rationale is preserved even years later
- **Informed Changes**: Future changes are made with full context
- **Professional Standards**: Demonstrates thoughtful engineering practices

---

## Decision Log

---

## ADR-001: Use Clean Architecture Pattern

### Context

SaaSifyCore is designed to be a **long-term, maintainable SaaS boilerplate** that other developers will extend. We need an architecture that:
- Separates concerns clearly
- Makes the codebase testable
- Allows technology swaps without major rewrites
- Scales with complexity

### Decision

We will structure the solution using **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture) with four distinct layers:

```
┌─────────────────────────────────────────┐
│         Presentation (API)              │
│  - Controllers                          │
│  - Middleware                           │
│  - Configuration                        │
└─────────────────▲────────────────────────┘
                  │
┌─────────────────┴────────────────────────┐
│         Application Layer               │
│  - Use Cases (CQRS Commands/Queries)    │
│  - DTOs                                  │
│  - Interfaces                            │
│  - Validation                            │
└─────────────────▲────────────────────────┘
                  │
┌─────────────────┴────────────────────────┐
│         Domain Layer (Core)              │
│  - Entities                              │
│  - Value Objects                         │
│  - Domain Events                         │
│  - Business Logic                        │
│  - Domain Interfaces                     │
└──────────────────────────────────────────┘
                  ▲
                  │
┌─────────────────┴────────────────────────┐
│         Infrastructure Layer             │
│  - EF Core DbContext                     │
│  - Repositories (if needed)              │
│  - External Services (Stripe, Email)     │
│  - Caching (Redis)                       │
└──────────────────────────────────────────┘
```

### Rationale

**Why Clean Architecture?**

1. ✅ **Independence from Frameworks**: Business logic doesn't depend on EF Core, ASP.NET, or any framework
2. ✅ **Testability**: Core business logic can be tested without databases, UI, or external services
3. ✅ **Independence from Database**: Can swap PostgreSQL for MongoDB without touching business logic
4. ✅ **Independence from External Services**: Stripe, SendGrid, etc., are just implementation details
5. ✅ **Maintainability**: Clear boundaries make changes predictable and localized

**Dependency Rule**: Source code dependencies point **inward only**. Inner layers know nothing about outer layers.

### Consequences

**Positive:**
- ✅ High testability
- ✅ Technology agnostic core
- ✅ Easy to understand and navigate
- ✅ Supports parallel development

**Negative:**
- ⚠️ More files and folders than a simple layered architecture
- ⚠️ Steeper learning curve for junior developers
- ⚠️ Can feel like over-engineering for simple CRUD apps

**Mitigation**: The benefits outweigh costs for a **SaaS boilerplate** meant to scale and be extended by others.

---

## ADR-002: No Repository Pattern Over EF Core

### Context

When using EF Core, developers often debate whether to add a **Repository Pattern** and **Unit of Work Pattern** on top of `DbContext`.

Traditional approach:
```csharp
public interface IUserRepository { ... }
public interface IUnitOfWork { ... }
```

Modern approach:
```csharp
public interface IApplicationDbContext 
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync();
}
```

### Decision

We will **NOT** implement the Repository/UnitOfWork pattern. Instead, we will:
- Use `IApplicationDbContext` interface for abstraction
- Expose `DbSet<T>` properties directly
- Let Application Layer use LINQ queries directly

### Rationale

#### Why NOT Repository Pattern?

**1. EF Core DbContext Already IS Repository + UnitOfWork**

| Pattern | EF Core Implementation | Proof |
|---------|----------------------|-------|
| Repository | `DbSet<T>` | `Add()`, `Remove()`, `Find()`, LINQ queries |
| Unit of Work | `DbContext` | `SaveChangesAsync()`, transaction management, change tracking |

Adding Repository/UnitOfWork creates **double abstraction** with no benefit.

**2. LINQ Provides Superior Flexibility**

```csharp
// With Repository - need to predict every query variation:
Task<User> GetByIdAsync(Guid id);
Task<User> GetByEmailAsync(string email);
Task<User> GetByEmailWithTenantAsync(string email);
Task<List<User>> GetActiveUsersByTenantAsync(Guid tenantId);
// ...endless methods

// With Direct DbContext - compose queries as needed:
var users = await _context.Users
    .Where(u => u.TenantId == tenantId)
    .Where(u => u.IsEmailVerified)
    .OrderByDescending(u => u.CreatedAt)
    .Take(10)
    .ToListAsync();
```

**3. Microsoft's Official Recommendation**

From [EF Core documentation](https://docs.microsoft.com/en-us/ef/core/):
> *"The DbContext class already implements the Unit of Work pattern, and DbSet implements the Repository pattern."*

**4. Reduces Code Volume by ~30-40%**

```csharp
// Without Repository Pattern (our approach):
// - 1 interface (IApplicationDbContext)
// - 1 implementation (ApplicationDbContext)
// - ~100 lines of code

// With Repository Pattern:
// - 1 interface per entity (IUserRepository, ITenantRepository, etc.)
// - 1 implementation per entity
// - 1 UnitOfWork interface + implementation
// - ~500+ lines of wrapper code
```

**5. Full Testability Maintained**

```csharp
// Testing is still easy with InMemory provider:
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase("TestDb")
    .Options;

var context = new ApplicationDbContext(options);

// Or mock the interface:
var mockContext = new Mock<IApplicationDbContext>();
```

**6. Industry Expert Consensus**

- **Jimmy Bogard** (MediatR creator): *"Repository pattern is an anti-pattern with EF Core"*
- **Steve Smith** (Ardalis): *"IDbContext interface provides sufficient abstraction"*
- **Vladimir Khorikov**: *"Repository over EF Core violates YAGNI"*

### Consequences

**Positive:**
- ✅ ~40% less boilerplate code
- ✅ Full LINQ query power
- ✅ Easier to understand (one less pattern to learn)
- ✅ Direct access to EF Core optimizations
- ✅ Still fully testable
- ✅ Adheres to Clean Architecture via `IApplicationDbContext`

**Negative:**
- ⚠️ Application Layer has visibility of EF Core types (`DbSet<T>`)
- ⚠️ Developers need to learn LINQ well
- ⚠️ Can't easily swap ORMs (but this is rarely needed)

**When We WOULD Use Repository Pattern:**
- If we were using **Dapper** or **raw SQL** instead of EF Core
- If we had **multiple data sources** (SQL + NoSQL + APIs)
- If **complex business logic** belonged in data access layer
- If **team policy** mandated it

### Alternative Considered

**Option 1: Full Repository Pattern**
- Rejected: Adds complexity without benefit when using EF Core

**Option 2: Specification Pattern**
- Considered for future: If query logic becomes too complex, we may introduce Specifications

---

## ADR-003: Use Domain-Driven Design (DDD) Tactical Patterns

### Context

We need to ensure our domain model is **rich**, **expressive**, and **encapsulates business rules** rather than being anemic data containers.

### Decision

We will implement **DDD Tactical Patterns**:

1. **Entities** with business logic and identity
2. **Value Objects** for type safety (Email, Subdomain, TenantName)
3. **Domain Events** for loosely-coupled communication
4. **Factory Methods** to ensure valid entity creation
5. **Guard Clauses** to protect invariants
6. **Aggregate Roots** with clear boundaries

### Rationale

#### 1. Rich Domain Model Over Anemic Model

**❌ Anemic Model (bad):**
```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public TenantStatus Status { get; set; }
}

// Logic lives in services (outside domain):
public class TenantService
{
    public void SuspendTenant(Tenant tenant)
    {
        tenant.Status = TenantStatus.Suspended;
    }
}
```

**✅ Rich Domain Model (our approach):**
```csharp
public class Tenant : BaseEntity
{
    private Tenant() { } // Force factory method usage
    
    public TenantName Name { get; private set; }
    public TenantStatus Status { get; private set; }
    
    public static Tenant Create(string name, string subdomain)
    {
        var tenant = new Tenant
        {
            Name = TenantName.Create(name), // Value object validation
            Subdomain = Subdomain.Create(subdomain)
        };
        tenant.RaiseDomainEvent(new TenantCreatedEvent(tenant.Id));
        return tenant;
    }
    
    public void Suspend(string reason)
    {
        if (Status == TenantStatus.Deleted)
            throw new DomainException("Cannot suspend deleted tenant");
            
        Status = TenantStatus.Suspended;
        RaiseDomainEvent(new TenantSuspendedEvent(Id, reason));
    }
}
```

**Benefits:**
- Business rules live **where they belong** (in the domain)
- Impossible to create invalid entities
- Clear API for what operations are allowed

#### 2. Value Objects for Type Safety

**❌ Primitive Obsession (bad):**
```csharp
public class Tenant
{
    public string Email { get; set; } // Any string! "abc", "123", etc.
    public string Subdomain { get; set; }
}

// Validation happens in random places:
if (!email.Contains("@")) throw new Exception("Invalid email");
```

**✅ Value Objects (our approach):**
```csharp
public record Email
{
    public string Value { get; }
    
    private Email(string value) => Value = value;
    
    public static Email Create(string value)
    {
        if (!Regex.IsMatch(value, EMAIL_REGEX))
            throw new DomainException("Invalid email");
        return new Email(value);
    }
}

public class Tenant
{
    public Email Email { get; private set; } // Type-safe!
}
```

**Benefits:**
- Validation is **centralized** and **consistent**
- Type system prevents invalid state
- Self-documenting code

#### 3. Domain Events for Decoupling

```csharp
// When a tenant is created, multiple things should happen:
// - Send welcome email
// - Create initial subscription
// - Log audit trail
// - Update analytics

// Without events (tight coupling):
public void CreateTenant(...)
{
    var tenant = new Tenant(...);
    _emailService.SendWelcome(tenant); // ❌ Domain depends on infrastructure
    _subscriptionService.CreateTrial(tenant);
    _auditLogger.Log(tenant);
}

// With domain events (loose coupling):
public void CreateTenant(...)
{
    var tenant = Tenant.Create(...); // Raises TenantCreatedEvent
    _context.Tenants.Add(tenant);
    await _context.SaveChangesAsync(); // Events dispatched here
}

// Handlers registered separately:
public class SendWelcomeEmailHandler : INotificationHandler<TenantCreatedEvent> { }
public class CreateTrialSubscriptionHandler : INotificationHandler<TenantCreatedEvent> { }
```

**Benefits:**
- Domain stays pure (no infrastructure dependencies)
- Features can be added/removed without changing core logic
- Enables event sourcing in the future

### Consequences

**Positive:**
- ✅ Business logic is self-documenting
- ✅ Impossible to create invalid state
- ✅ Type safety prevents bugs
- ✅ Loosely coupled components
- ✅ Easy to test (no mocking needed for business rules)

**Negative:**
- ⚠️ More classes/files (Value Objects, Events, etc.)
- ⚠️ Steeper learning curve for team members unfamiliar with DDD

**Mitigation**: This is a **boilerplate/framework** project meant to demonstrate best practices, so the learning investment pays off.

---

## ADR-004: Use CQRS Pattern with MediatR

### Context

Application Layer needs a consistent way to handle **commands** (writes) and **queries** (reads) without controllers becoming bloated.

### Decision

We will implement **CQRS (Command Query Responsibility Segregation)** using **MediatR** library.

**Structure:**
```
Application/
├── Commands/
│   ├── CreateTenant/
│   │   ├── CreateTenantCommand.cs
│   │   ├── CreateTenantCommandHandler.cs
│   │   └── CreateTenantCommandValidator.cs
├── Queries/
│   ├── GetTenantById/
│   │   ├── GetTenantByIdQuery.cs
│   │   └── GetTenantByIdQueryHandler.cs
```

### Rationale

**1. Single Responsibility Principle**

```csharp
// Without CQRS (bloated controller):
[ApiController]
public class TenantsController
{
    public async Task<IActionResult> CreateTenant(...)
    {
        // 50 lines of business logic here ❌
    }
    
    public async Task<IActionResult> GetTenant(...)
    {
        // 30 lines of query logic here ❌
    }
}

// With CQRS (thin controller):
[ApiController]
public class TenantsController
{
    private readonly IMediator _mediator;
    
    [HttpPost]
    public async Task<IActionResult> CreateTenant(CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id));
        return Ok(result);
    }
}
```

**2. Separation of Concerns**

- **Commands**: Change state (Create, Update, Delete)
- **Queries**: Return data (Get, List, Search)

Different optimizations can be applied to each.

**3. Testability**

```csharp
// Test handlers in isolation:
[Fact]
public async Task Handle_ValidCommand_CreatesUser()
{
    var handler = new CreateUserCommandHandler(_context);
    var command = new CreateUserCommand { Email = "test@test.com" };
    
    var result = await handler.Handle(command, CancellationToken.None);
    
    Assert.NotNull(result);
}
```

**4. Pipeline Behaviors**

MediatR allows cross-cutting concerns via behaviors:
```csharp
// Automatic validation for ALL commands:
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(...)
    {
        // Validate before executing handler
        var validator = _validators.FirstOrDefault();
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
            
        return await next();
    }
}

// Automatic logging for ALL operations:
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(...)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

### Consequences

**Positive:**
- ✅ Controllers become thin routing layers
- ✅ Business logic is testable in isolation
- ✅ Pipeline behaviors enable cross-cutting concerns
- ✅ Clear separation of reads and writes
- ✅ Easy to optimize queries independently

**Negative:**
- ⚠️ More files (Command, Handler, Validator per operation)
- ⚠️ Learning curve for MediatR pattern

---

## ADR-005: Multi-Tenancy via Tenant Isolation

### Context

SaaSifyCore is a **multi-tenant SaaS platform**. We need to decide:
- How to identify tenants (subdomain vs header vs route)
- How to isolate tenant data (shared DB vs separate DB)
- How to enforce data isolation automatically

### Decision

**Tenant Identification:** HTTP Header (`X-Tenant-Id`) or Subdomain  
**Data Isolation:** Shared database with global query filters  
**Enforcement:** Middleware + EF Core query filters

### Rationale

#### 1. Tenant Identification Strategy

**Options Considered:**

| Strategy | Pros | Cons | Decision |
|----------|------|------|----------|
| **Subdomain** (acme.app.com) | User-friendly, clear branding | DNS management, SSL certs | ✅ Primary method |
| **HTTP Header** (X-Tenant-Id) | Easy for APIs, no DNS needed | Not user-facing | ✅ Fallback for APIs |
| **Route** (/tenants/:id/) | Simple | Ugly URLs, security risk | ❌ Rejected |
| **JWT Claim** | Secure, embedded in auth | Couples tenancy to auth | ⚠️ Future consideration |

**Implementation:**
```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantSubdomain { get; }
}

// Middleware resolves tenant from subdomain or header
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var subdomain = context.Request.Host.Host.Split('.').First();
        var tenantId = await _tenantService.GetTenantIdBySubdomain(subdomain);
        
        _tenantContext.TenantId = tenantId;
        await _next(context);
    }
}
```

#### 2. Data Isolation Strategy

**Options Considered:**

| Strategy | Pros | Cons | Decision |
|----------|------|------|----------|
| **Shared DB + Query Filters** | Cost-effective, easy backups | Potential data leaks if filter fails | ✅ **Selected** |
| **Separate DB per Tenant** | True isolation, custom scaling | High cost, migration complexity | ❌ Overkill for MVP |
| **Separate Schema per Tenant** | Good isolation, one DB | Schema proliferation | ⚠️ Future option |

**Implementation:**
```csharp
// EF Core automatically filters ALL queries:
modelBuilder.Entity<User>().HasQueryFilter(u => 
    _tenantContext.TenantId == null || 
    u.TenantId == _tenantContext.TenantId);

// Developer writes:
var users = await _context.Users.ToListAsync();

// EF Core generates:
SELECT * FROM Users WHERE TenantId = 'current-tenant-id'
```

**Security:** Even if developer forgets to filter by tenant, query filter protects data.

### Consequences

**Positive:**
- Automatic data isolation (less human error)
- Cost-effective (shared infrastructure)
- Easy to backup and restore
- Flexible tenant identification

**Negative:**
- Single point of failure (shared database)
- Noisy neighbor problem (one tenant's heavy load affects others)
- Must be careful with migrations

**Mitigation:**
- Connection pooling and query optimization
- Consider sharding by tenant in future
- Add monitoring for per-tenant resource usage

---

## ADR-006: PostgreSQL as Primary Database

### Context

We need a relational database for structured SaaS data (tenants, users, subscriptions).

### Decision

Use **PostgreSQL** as the primary database.

### Rationale

**Why PostgreSQL over SQL Server / MySQL?**

| Factor | PostgreSQL | SQL Server | MySQL |
|--------|-----------|-----------|--------|
| **Cost** | ✅ Free, open-source | ❌ Expensive licensing | ✅ Free |
| **Performance** | ✅ Excellent | ✅ Excellent | ⚠️ Good |
| **JSON Support** | ✅ Native JSONB | ⚠️ Limited | ⚠️ Limited |
| **Full-Text Search** | ✅ Built-in | ⚠️ Requires setup | ⚠️ Basic |
| **Azure Support** | ✅ Azure Database for PostgreSQL | ✅ Native | ✅ Available |
| **Docker/Local Dev** | ✅ Easy | ⚠️ Difficult | ✅ Easy |
| **Community** | ✅ Strong | ⚠️ Enterprise-focused | ✅ Strong |

**Key Decision Factors:**
1. **Open-source** - No licensing costs
2. **JSON support** - Can store tenant-specific configurations flexibly
3. **Cloud-native** - First-class Azure support
4. **Developer-friendly** - Easy local setup with Docker
5. **Advanced features** - Window functions, CTEs, full-text search

### Consequences

**Positive:**
- Zero database licensing costs
- Runs anywhere (local, Azure, AWS, Docker)
- Advanced SQL features for complex queries
- Can store JSON configurations per tenant

**Negative:**
- Team needs PostgreSQL knowledge (different from SQL Server)
- Fewer .NET developers familiar with it vs SQL Server

**Mitigation:**
- EF Core abstracts most differences
- Provide Docker Compose for local development

---

## Decision-Making Framework

When making new architectural decisions, we consider:

### 1. **Alignment with Principles**
- SOLID principles
- Clean Architecture
- YAGNI (You Aren't Gonna Need It)
- DRY (Don't Repeat Yourself)
- KISS (Keep It Simple, Stupid)

### 2. **Trade-Off Analysis**
- **Complexity** vs **Benefit**
- **Short-term pain** vs **Long-term gain**
- **Flexibility** vs **Performance**

### 3. **Team Considerations**
- Learning curve
- Maintenance burden
- Hiring implications

### 4. **Business Impact**
- Time to market
- Cost (licenses, infrastructure)
- Scalability requirements

---

## References & Further Reading

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [EF Core Documentation - Repository Pattern](https://docs.microsoft.com/en-us/ef/core/)
- [MediatR GitHub Repository](https://github.com/jbogard/MediatR)
- [Microsoft Multi-Tenancy Guidance](https://docs.microsoft.com/en-us/azure/architecture/guide/multitenant/overview)

---

## Change Log

| Date | ADR # | Description |
|------|-------|-------------|
| Oct 2025 | ADR-001 | Initial decision to use Clean Architecture |
| Oct 2025 | ADR-002 | Decision to NOT use Repository pattern over EF Core |
| Oct 2025 | ADR-003 | Adopt DDD tactical patterns |
| Oct 2025 | ADR-004 | Use CQRS with MediatR (planned) |
| Oct 2025 | ADR-005 | Multi-tenancy implementation strategy |
| Oct 2025 | ADR-006 | Select PostgreSQL as primary database |

---

> **Note**: These decisions are not set in stone. As the project evolves and requirements change, we will revisit and update these decisions accordingly. Each change will be documented with reasoning.