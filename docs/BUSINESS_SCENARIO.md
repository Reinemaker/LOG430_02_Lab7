# CornerShop E-Commerce Business Scenario

## **Business Process: E-Commerce Order Processing**

### **Process Overview**
Complete order lifecycle from cart creation to delivery confirmation, including inventory management, payment processing, and customer notifications.

### **Key Business Events**

#### **Cart Management Events**
- `CartCreated` - New shopping cart created for customer
- `ItemAddedToCart` - Product added to cart with quantity
- `ItemRemovedFromCart` - Product removed from cart
- `CartUpdated` - Cart contents modified
- `CartExpired` - Cart automatically expired after inactivity
- `CartCheckedOut` - Customer initiated checkout process

#### **Order Processing Events**
- `OrderCreated` - New order created from cart
- `OrderValidated` - Order validated (inventory, customer, payment method)
- `OrderConfirmed` - Order confirmed and ready for processing
- `OrderCancelled` - Order cancelled (customer request, validation failure)
- `OrderShipped` - Order shipped with tracking information
- `OrderDelivered` - Order delivered and confirmed
- `OrderReturned` - Order returned by customer

#### **Inventory Management Events**
- `StockReserved` - Stock reserved for order
- `StockReleased` - Stock released (order cancelled)
- `StockUpdated` - Inventory levels updated
- `LowStockAlert` - Low stock threshold reached
- `OutOfStockAlert` - Product out of stock

#### **Payment Processing Events**
- `PaymentInitiated` - Payment process started
- `PaymentAuthorized` - Payment authorized by payment provider
- `PaymentCompleted` - Payment successfully processed
- `PaymentFailed` - Payment failed
- `PaymentRefunded` - Payment refunded

#### **Customer Management Events**
- `CustomerRegistered` - New customer account created
- `CustomerProfileUpdated` - Customer profile information updated
- `CustomerLogin` - Customer logged in
- `CustomerLogout` - Customer logged out

#### **Notification Events**
- `EmailSent` - Email notification sent
- `SMSsent` - SMS notification sent
- `PushNotificationSent` - Push notification sent

### **Event Flow Example: Complete Order Process**

1. **Customer adds items to cart**
   - `CartCreated` → `ItemAddedToCart` → `CartUpdated`

2. **Customer checks out**
   - `CartCheckedOut` → `OrderCreated` → `OrderValidated`

3. **Inventory and payment processing**
   - `StockReserved` → `PaymentInitiated` → `PaymentAuthorized` → `PaymentCompleted`

4. **Order confirmation and shipping**
   - `OrderConfirmed` → `OrderShipped` → `OrderDelivered`

5. **Notifications throughout the process**
   - `EmailSent` (order confirmation)
   - `EmailSent` (shipping notification)
   - `EmailSent` (delivery confirmation)

### **Event Schema Structure**
```json
{
  "eventId": "uuid-v4",
  "eventType": "OrderCreated",
  "aggregateId": "order-123",
  "aggregateType": "Order",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": 1,
  "data": {
    // Event-specific data
  },
  "metadata": {
    "correlationId": "correlation-uuid",
    "causationId": "previous-event-uuid",
    "userId": "user-123",
    "source": "OrderService"
  }
}
```

### **Business Rules**
- Orders cannot be created without valid customer and items
- Stock must be available before order confirmation
- Payment must be authorized before order processing
- Cart expires after 30 minutes of inactivity
- Low stock alerts trigger when inventory < 10 units
- Failed payments automatically cancel orders
- All events must be idempotent and replayable 