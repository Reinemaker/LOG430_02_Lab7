# ADR 002: Separation of Responsibilities

## Status
Accepted

## Context
The Corner Shop application needs a clear separation of concerns to:
- Maintain code quality
- Enable easy testing
- Support future modifications
- Allow for different implementations
- Ensure maintainability
- Support the 2-tier architecture

## Decision
We will implement a 3-layer architecture with clear separation of responsibilities:

1. **Presentation Layer**
   - Located in `Program.cs`
   - Handles user interface and input/output
   - Manages user interactions
   - Provides clear feedback
   - No business logic

2. **Business Layer**
   - Located in `Services/` directory
   - Implements business rules
   - Handles data validation
   - Manages transactions
   - Coordinates between presentation and data layers
   - Key components:
     - `IProductService` and `ProductService`
     - `ISaleService` and `SaleService`
     - `ISyncService` and `SyncService`

3. **Data Layer**
   - Located in `Services/` directory
   - Handles data persistence
   - Implements database operations
   - Manages database connections
   - Key components:
     - `IDatabaseService` interface
     - `MongoDatabaseService` implementation
     - `EfDatabaseService` implementation

## Consequences

### Positive
- Clear separation of concerns
- Easy to test each layer independently
- Flexible to change implementations
- Maintainable codebase
- Clear dependencies
- Easy to add new features

### Negative
- More initial setup required
- Need to maintain interfaces
- Slightly more complex than monolithic approach
- Need to coordinate between layers

## Implementation Notes
- Using dependency injection
- Interface-based design
- Clear naming conventions
- Consistent error handling
- Proper documentation
- Unit tests for each layer 