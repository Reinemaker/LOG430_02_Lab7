# ADR 002: Platform Choice

## Status
Accepted

## Context
We need to choose a platform for implementing a corner shop management system that supports both SQL and NoSQL databases.

## Decision
We chose .NET 8.0 as our platform for the following reasons:

1. **Cross-Platform Support**
   - Runs on Windows, Linux, and macOS
   - Consistent behavior across platforms
   - Easy deployment in various environments

2. **Database Support**
   - Native support for both SQL and NoSQL databases
   - Entity Framework Core for SQL databases
   - MongoDB.Driver for NoSQL databases
   - Easy to implement database abstraction

3. **Performance**
   - High-performance runtime
   - Efficient memory management
   - Good support for async/await operations

4. **Development Experience**
   - Strong typing and compile-time checks
   - Rich IDE support
   - Comprehensive documentation
   - Large ecosystem of libraries

5. **Containerization**
   - Easy to containerize with Docker
   - Small container image size
   - Good support for microservices

## Consequences
### Positive
- Consistent development experience
- Good performance
- Strong database support
- Easy deployment

### Negative
- Learning curve for developers not familiar with .NET
- Larger runtime compared to some alternatives
- More complex setup for some features 