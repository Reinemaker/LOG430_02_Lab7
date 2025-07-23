# Domain-Driven Design: Subdomains

## Core Subdomains

### 1. In-Store Sales
- Local sales processing
- Stock management at the store level
- Offline operation and local data persistence

### 2. Logistics Management
- Central stock management (logistics center)
- Restocking and supply chain between stores and logistics
- Restock requests and fulfillment

### 3. Head Office Supervision
- Consolidated sales and stock reporting
- Strategic dashboards and KPIs
- Product catalog management and global updates
- System-wide alerts and notifications

## Context Map

- **In-Store Sales** and **Logistics Management** synchronize data with **Head Office Supervision** via the sync service.
- **Head Office** can push product updates and receive alerts from stores. 