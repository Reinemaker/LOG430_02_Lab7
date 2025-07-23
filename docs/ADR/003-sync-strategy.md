# ADR 003: Data Synchronization Strategy

## Status
Accepted

## Context
Stores must operate offline and independently, but the head office requires consolidated, up-to-date data for reporting and logistics.

## Decision
- Each store maintains a local SQLite database for products and sales.
- A sync service is responsible for pushing unsynced sales and stock changes to the central MongoDB.
- Sync can be triggered manually by an admin or scheduled automatically.
- Conflict resolution is handled by last-write-wins for sales; product updates from head office always override local data.

## Consequences
- Reliable, eventual consistency between stores and head office
- Simple, robust sync process
- Temporary data divergence is possible until sync occurs 