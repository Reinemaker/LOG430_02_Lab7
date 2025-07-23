# ADR 002: Database Architecture

## Status
Accepted

## Context
The Corner Shop system is designed for distributed, multi-store operation. Each store must be able to function independently (including offline), while the head office requires consolidated reporting and administration. The architecture must support easy onboarding of new stores, reliable data synchronization, and a modern web interface.

## Decision
- Each store uses a local SQLite database for products and sales.
- The head office uses a central MongoDB database for consolidated data and reporting.
- A sync service allows admins to push all unsynced sales from local SQLite to MongoDB from the Reports page.
- The system is built as an ASP.NET Core MVC web application.

## Consequences

### Positive
- Stores can operate offline and independently
- Reliable, consistent data synchronization to head office
- Centralized reporting and administration
- Easy to add new stores (just create a new SQLite file)
- Simple, robust sync process
- Modern, scalable web architecture

### Negative
- Need to ensure all stores regularly sync to central database
- Potential for temporary data divergence until sync
- Slightly more complex architecture

## Implementation Notes
- On store creation, the system creates a new SQLite file and tables for that store
- Store operations use LocalProductService and LocalSaleService
- SyncService pushes unsynced sales to MongoDB
- Admins can trigger sync from the Reports page

## Alternatives Considered

### SQL Server
- Pros:
  - Strong ACID compliance
  - Familiar to many developers
  - Built-in data validation
- Cons:
  - More rigid schema
  - Higher resource requirements
  - More complex deployment

### SQLite (Standalone)
- Pros:
  - Simple deployment
  - No server required
  - Good for small applications
- Cons:
  - Limited scalability
  - Not suitable for concurrent access
  - Limited reporting capabilities

## Implementation Notes (Updated)
- Using MongoDB.Driver 3.4.0 for .NET integration
- Using Entity Framework Core 6.0 with SQLite
- Implementing data validation in the application layer
- Using Docker for consistent deployment
- Implementing proper error handling for database operations
- Automatic synchronization between databases
- Transaction management through EF Core
- Conflict resolution strategy in place 