# Result Pattern Implementation

## Overview

The Result Pattern provides a type-safe way to handle operation outcomes without throwing exceptions for expected failures. This implementation includes:

- **Result** - Base result class for operations without return values
- **Result<T>** - Generic result for operations that return values
- **Error** - Structured error representation with code and message
- **DomainErrors** - Pre-defined domain-specific errors
- **ResultExtensions** - Functional programming utilities

---

## Basic Usage

### Simple Success/Failure

```csharp
// Operation that might fail
public Result ValidateAge(int age)
{
    if (age < 18)
        return Result.Failure("User must be 18 or older");
    
    return Result.Success();
}

// Check the result
var result = ValidateAge(15);
if (result.IsFailure)
{
    Console.WriteLine(result.Error.Message); // "User must be 18 or older"
}
```

### Returning Values

```csharp
public Result<User> GetUserById(Guid id)
{
    var user = _context.Users.Find(id);
    
    if (user is null)
        return Result.Failure<User>(DomainErrors.User.NotFound);
    
    return Result.Success(user);
}

// Using the result
var result = GetUserById(userId);
if (result.IsSuccess)
{
    var user = result.Value;
    Console.WriteLine($"Found user: {user.Email}");
}
else
{
    Console.WriteLine($"Error: {result.Error.Message}");
}
```

### Using Domain Errors

```csharp
public Result<User> AuthenticateUser(string email, string password)
{
    var user = _context.Users.FirstOrDefault(u => u.Email == email);
    
    if (user is null)
        return Result.Failure<User>(DomainErrors.User.NotFound);
    
    if (!_passwordHasher.Verify(password, user.PasswordHash))
        return Result.Failure<User>(DomainErrors.User.InvalidCredentials);
    
    if (!user.IsEmailVerified)
        return Result.Failure<User>(DomainErrors.User.EmailNotVerified);
    
    return Result.Success(user);
}
```

---

## Functional Extensions

### Map - Transform Success Values

```csharp
Result<User> userResult = GetUserById(id);

Result<UserDto> dtoResult = userResult.Map(user => new UserDto
{
    Id = user.Id,
    Email = user.Email.Value,
    FullName = $"{user.FirstName} {user.LastName}"
});
```

### Bind - Chain Operations

```csharp
Result<User> result = GetUserById(userId)
    .Bind(user => VerifyUserEmail(user))
    .Bind(user => ActivateUserAccount(user));

if (result.IsSuccess)
{
    Console.WriteLine("User activated successfully");
}
```

### Match - Handle Both Cases

```csharp
var message = GetUserById(userId).Match(
    onSuccess: user => $"Welcome, {user.FirstName}!",
    onFailure: error => $"Error: {error.Message}"
);

Console.WriteLine(message);
```

### OnSuccess / OnFailure - Side Effects

```csharp
GetUserById(userId)
    .OnSuccess(user => _logger.LogInformation("User found: {Email}", user.Email))
    .OnFailure(error => _logger.LogWarning("User not found: {Error}", error.Message));
```

### Ensure - Add Validation

```csharp
Result<User> result = GetUserById(userId)
    .Ensure(
        user => user.IsActive,
        new Error("User.Inactive", "User account is inactive")
    )
    .Ensure(
        user => user.IsEmailVerified,
        DomainErrors.User.EmailNotVerified
    );
```

### Async Operations

```csharp
Result<User> result = await GetUserByIdAsync(userId)
    .MapAsync(user => new UserDto(user.Id, user.Email))
    .BindAsync(dto => SendWelcomeEmailAsync(dto));
```

---

## Controller Integration

### Before (Throwing Exceptions)

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(Guid id)
{
    try
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound("User not found");
        
        return Ok(user);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}
```

### After (Using Result Pattern)

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(Guid id)
{
    var result = await _userService.GetUserByIdAsync(id);
    
    return result.Match(
        onSuccess: user => Ok(user),
        onFailure: error => error.Code switch
        {
            "User.NotFound" => NotFound(error.Message),
            "User.Inactive" => Forbid(),
            _ => BadRequest(error.Message)
        }
    );
}
```

Or even simpler with a helper:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(Guid id)
{
    var result = await _userService.GetUserByIdAsync(id);
    return ToActionResult(result);
}

private IActionResult ToActionResult<T>(Result<T> result)
{
    if (result.IsSuccess)
        return Ok(result.Value);
    
    return result.Error.Code switch
    {
        var code when code.StartsWith("Validation") => BadRequest(result.Error.Message),
        var code when code.Contains("NotFound") => NotFound(result.Error.Message),
        var code when code.Contains("Unauthorized") => Unauthorized(result.Error.Message),
        var code when code.Contains("Forbidden") => Forbid(),
        _ => StatusCode(500, result.Error.Message)
    };
}
```

---

## MediatR Integration

```csharp
// Command
public record CreateUserCommand(string Email, string Password) 
    : IRequest<Result<Guid>>;

// Handler
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // Check if email exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email, ct))
            return Result.Failure<Guid>(DomainErrors.User.EmailAlreadyExists);
        
        // Create user
        var user = User.Create(
            Email.Create(request.Email),
            await _passwordHasher.HashPasswordAsync(request.Password)
        );
        
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
        
        return Result.Success(user.Id);
    }
}

// Controller
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    var command = new CreateUserCommand(request.Email, request.Password);
    var result = await _mediator.Send(command);
    
    return result.Match(
        onSuccess: userId => CreatedAtAction(nameof(GetUser), new { id = userId }, userId),
        onFailure: error => BadRequest(error.Message)
    );
}
```

---

## Best Practices

### ✅ DO

- Use Result for **expected** failures (validation, not found, unauthorized)
- Use specific domain errors from `DomainErrors` class
- Chain operations with `Bind` and `Map`
- Use `Match` for clean branching logic

### ❌ DON'T

- Use Result for **unexpected** failures (null reference, IO errors) - throw exceptions
- Mix throwing exceptions and returning Results in the same layer
- Return `null` when you can return `Result.Failure`
- Ignore the Result - always check `IsSuccess` or use `Match`

---

## Adding New Domain Errors

```csharp
// In DomainErrors.cs
public static class Payment
{
    public static Error InsufficientFunds => new(
        "Payment.InsufficientFunds",
        "Insufficient funds to complete payment");
    
    public static Error PaymentMethodExpired => new(
        "Payment.PaymentMethodExpired",
        "The payment method has expired");
    
    public static Error InvalidAmount(decimal amount) => new(
        "Payment.InvalidAmount",
        $"Payment amount {amount:C} is invalid");
}
```

---

## Railway-Oriented Programming

The Result pattern enables "Railway-Oriented Programming" - operations that stay on the "success track" until a failure occurs:

```csharp
Result<Order> result = ValidateOrder(orderRequest)
    .Bind(order => CheckInventory(order))
    .Bind(order => ProcessPayment(order))
    .Bind(order => ShipOrder(order))
    .Bind(order => SendConfirmationEmail(order));

// If any step fails, the chain short-circuits and returns the error
```

This is much cleaner than:

```csharp
try
{
    var order = ValidateOrder(orderRequest);
    if (order == null) throw new Exception("Invalid order");
    
    if (!CheckInventory(order)) throw new Exception("Out of stock");
    
    if (!ProcessPayment(order)) throw new Exception("Payment failed");
    
    ShipOrder(order);
    SendConfirmationEmail(order);
}
catch (Exception ex)
{
    // Handle error
}
```

---

## Summary

The Result Pattern provides:
- ✅ **Type safety** - compiler ensures you handle failures
- ✅ **Explicitness** - operations that can fail are obvious
- ✅ **Composability** - chain operations elegantly
- ✅ **No exceptions** - use exceptions only for unexpected failures
- ✅ **Better testability** - easy to test success and failure paths

**When to use**: Business logic, validation, domain operations  
**When NOT to use**: Infrastructure failures, framework exceptions, truly exceptional cases
