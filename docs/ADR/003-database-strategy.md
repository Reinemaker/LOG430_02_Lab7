# ADR 003: Database Strategy

## Status
Accepted

## Context
We need to implement a database strategy that supports both SQL and NoSQL databases for a corner shop management system.

## Decision
We implemented a dual-database strategy using:

1. **MongoDB (NoSQL)**
   - Document-based storage
   - Flexible schema
   - Good for rapid development
   - Easy to scale horizontally

2. **Entity Framework Core with SQLite (SQL)**
   - Relational database
   - ACID compliance
   - Strong data consistency
   - Good for complex queries

3. **Abstraction Layer**
   - Common interface (IDatabaseService)
   - Database-agnostic business logic
   - Easy to switch between databases
   - Consistent API for both implementations

## Implementation Details

1. **Interface Design**
```csharp
public interface IDatabaseService
{
    Task InitializeDatabase();
    Task<List<Product>> SearchProducts(string searchTerm);
    // ... other methods
}
```

2. **Database Implementations**
```csharp
public class MongoDatabaseService : IMongoDatabaseService
{
    // MongoDB implementation
}

public class EfDatabaseService : IEfDatabaseService
{
    // EF Core implementation
}
```

3. **Transaction Support**
   - MongoDB: Atomic operations
   - EF Core: ACID transactions
   - Both: Stock consistency checks

## Consequences
### Positive
- Flexibility in database choice
- Consistent business logic
- Easy to test and maintain
- Good separation of concerns

### Negative
- More complex architecture
- Need to maintain two implementations
- Potential data synchronization issues
- Higher development overhead 