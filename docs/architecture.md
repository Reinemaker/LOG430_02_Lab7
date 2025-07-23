# Corner Shop Management System - Architecture Documentation

## 1. Introduction and Goals

### 1.1 Requirements Overview
- Point-of-sale system for corner shop management
- Support for product and sale management
- Dual database system for data persistence
- Console-based user interface
- Support for concurrent operations

### 1.2 Quality Goals
- High availability and reliability
- Data consistency across databases
- Fast response times for operations
- Easy maintenance and extensibility
- Clear error handling and user feedback

### 1.3 Stakeholders
- Shop owners/managers
- Cashiers
- System administrators
- Developers

## 2. Architecture Constraints

### 2.1 Technical Constraints
- .NET Core 6.0 platform
- Console-based interface
- Docker containerization
- Dual database system (MongoDB and SQLite)

### 2.2 Organizational Constraints
- Development team size
- Time constraints
- Budget limitations
- Training requirements

## 3. System Scope and Context

### 3.1 Business Context
- Corner shop operations
- Point-of-sale transactions
- Inventory management
- Sales tracking

### 3.2 Technical Context
- Development environment
- Production environment
- Database systems
- Container orchestration

## 4. Solution Strategy

### 4.1 Architecture Overview
The system follows a 3-tier architecture:
1. **Presentation Layer**: Console interface
2. **Business Layer**: Service implementations
3. **Data Layer**: Database services

### 4.2 Technology Decisions
- .NET Core for cross-platform support
- MongoDB for document storage
- Entity Framework Core for relational storage
- Docker for containerization

## 5. Building Block View

### 5.1 Whitebox Overall System
See [Logical View](UML/logical-view.puml) for the high-level system structure.

### 5.2 Level 2
#### 5.2.1 Presentation Layer
- Program.cs: Main application entry point
- User interface handling
- Input validation
- Output formatting

#### 5.2.2 Business Layer
- ProductService: Product management
- SaleService: Sale processing
- SyncService: Database synchronization

#### 5.2.3 Data Layer
- MongoDatabaseService: MongoDB implementation
- EfDatabaseService: Entity Framework implementation

### 5.3 Level 3
See [Class Diagram](UML/class-diagram.puml) for detailed component relationships.

## 6. Runtime View

### 6.1 Runtime Scenarios
See [Process View](UML/process-view.puml) for runtime interactions.

### 6.2 Key Processes
1. Product Search
2. Sale Creation
3. Database Synchronization
4. Stock Management

## 7. Deployment View

### 7.1 Infrastructure
See [Physical View](UML/physical-view.puml) for deployment architecture.

### 7.2 Containers
- Application container
- MongoDB container
- SQLite database file

## 8. Cross-cutting Concepts

### 8.1 Security
- Input validation
- Data sanitization
- Error handling

### 8.2 Performance
- Database indexing
- Caching strategies
- Concurrent operations

### 8.3 Scalability
- Container orchestration
- Database synchronization
- Load balancing

## 9. Architecture Decisions

See [Architecture Decision Records](ADR/) for detailed decisions:
- [Database Architecture](ADR/001-database-architecture.md)
- [Separation of Responsibilities](ADR/002-separation-of-responsibilities.md)
- [Platform Choice](ADR/001-platform-choice.md)

## 10. Quality Requirements

### 10.1 Performance
- Response time < 1 second for operations
- Support for 3 concurrent registers
- Efficient database queries

### 10.2 Reliability
- Data consistency across databases
- Automatic error recovery
- Transaction management

### 10.3 Maintainability
- Clear code organization
- Comprehensive documentation
- Automated testing

## 11. Risks and Technical Debt

### 11.1 Identified Risks
- Database synchronization conflicts
- Concurrent operation handling
- Data consistency maintenance

### 11.2 Technical Debt
- Additional test coverage needed
- Performance optimization opportunities
- Documentation updates

## 12. Glossary

- **ORM**: Object-Relational Mapping
- **POS**: Point of Sale
- **EF Core**: Entity Framework Core
- **BSON**: Binary JSON
- **LINQ**: Language Integrated Query 