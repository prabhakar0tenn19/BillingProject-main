# Billing System API — Endpoints Reference
**Base URL**: `https://billingproject-main.onrender.com/api/v1`
**Swagger UI**: `https://billingproject-main.onrender.com/swagger`

Every API response follows this wrapper envelope:
```json
{
  "success": true,
  "data": { ... },
  "message": "Optional response message"
}
```

---

## 1. Categories
Manages product categories. Default categories are Taps (8481), Showers (8481), Drain Covers (7325), and Washbasins (6910).
- `GET /categories` - Get all active categories.
- `GET /categories/{id}` - Get category by ID.
- `POST /categories` - Create a new category.
  - Body: `{ "name": "...", "description": "...", "hsnCode": "..." }`
- `PUT /categories/{id}` - Update a category.
- `DELETE /categories/{id}` - Soft delete a category.

---

## 2. Products
Manages the master product catalog. Note: `basePrice` is hidden from general catalog listings to maintain business confidentiality, but can be set via Create/Update.
- `GET /products` - Get all active products. Supports query parameters `categoryId` and `search`.
- `GET /products/{id}` - Get product by ID.
- `POST /products` - Create product.
  - Body: `{ "name": "...", "modelNumber": "...", "categoryId": "...", "description": "...", "basePrice": 450.0, "stock": 10 }`
- `PUT /products/{id}` - Update product.
- `DELETE /products/{id}` - Soft delete product.
- `PATCH /products/{id}/stock` - Update stock quantity.
  - Body: `{ "quantity": 5, "operation": "add" }` (or `"set"`)
- `POST /products/{id}/image` - Upload product image to Cloudinary (Form Data with `file`).
- `DELETE /products/{id}/image` - Remove product image.

---

## 3. Customers (Parties)
Manages customer billing profiles.
- `GET /customers` - Get list of active customers.
- `GET /customers/lookup` - Lightweight lookup list of customers for select-boxes.
- `GET /customers/{id}` - Get customer by ID.
- `POST /customers` - Create customer profile.
  - Body: `{ "partyName": "...", "contactPerson": "...", "phone": "...", "email": "...", "billingAddress": "...", "shippingAddress": "...", "gstin": "...", "panNumber": "..." }`
- `PUT /customers/{id}` - Update customer.
- `DELETE /customers/{id}` - Soft delete customer.

---

## 4. Customer Pricing (Party Catalogs)
Sets and overrides prices for specific parties.
- `GET /customers/{customerId}/pricing` - List all active price overrides for a customer.
- `GET /customers/{customerId}/pricing/bill-ready` - **[CRITICAL FOR BILLING WIZARD]** Returns all catalog products with their effective prices for this customer. If a negotiated price exists, it uses it; otherwise, it falls back to the product's base price.
- `POST /customers/{customerId}/pricing` - Set/Upsert price override.
  - Body: `{ "productId": "...", "negotiatedPrice": 380.00 }`
- `PUT /customers/{customerId}/pricing/{productId}` - Update override price.
- `DELETE /customers/{customerId}/pricing/{productId}` - Delete override (revert to base price).

---

## 5. Invoices
Handles B2B GST invoices generation, history, and status updates.
- `GET /invoices` - Get paged list of invoices. (Supports `page`, `pageSize`, `search`, `status`).
- `GET /invoices/{id}` - Get detailed invoice by ID (includes snapshotted customer & company data).
- `POST /invoices` - Create a new invoice. This automatically decrements product stock and locks snapshot data.
  - Body:
    ```json
    {
      "customerId": "...",
      "invoiceDate": "2026-07-11T12:00:00Z",
      "dueDate": "2026-08-11T12:00:00Z",
      "remarks": "...",
      "items": [
        { "productId": "...", "quantity": 10, "overrideRate": 390.00 }
      ]
    }
    ```
- `DELETE /invoices/{id}` - Cancel an invoice (soft delete/status update).
- `GET /invoices/{id}/pdf` - Download invoice PDF.

---

## 6. Payments
Handles payment status tracking for invoices.
- `GET /payments/pending` - List all pending invoices.
- `POST /payments/{id}/pay` - Mark invoice as paid.
  - Body: `{ "paymentMode": "upi", "paidAt": "2026-07-11T12:00:00Z" }` (modes: `cash`, `cheque`, `neft`, `upi`)
- `POST /payments/{id}/unpay` - Revert invoice status to pending.

---

## 7. Reports & Dashboard
Retrieves business insights and data metrics.
- `GET /dashboard/summary` - Key statistics (total sales, pending amount, active customers, stock alert counts).
- `GET /dashboard/monthly-sales` - Monthly sales data graph metrics.
- `GET /dashboard/recent-invoices` - List of latest invoices.
- `GET /dashboard/top-customers` - Top customer sales data.
- `GET /reports/sales` - Detailed sales reports filtered by date range.
- `GET /reports/customer-sales` - Sales breakdown by customer.
- `GET /reports/product-sales` - Sales breakdown by product.
- `GET /reports/gst` - Summary of CGST and SGST collected for date range.

---

## 8. Settings
Configures global company details (printed on invoices) and digital signatures.
- `GET /settings` - Retrieve current company settings.
- `PUT /settings` - Update company details.
- `POST /settings/signature` - Upload official stamp/signature image to Cloudinary (Form Data with `file`).
- `DELETE /settings/signature` - Delete stamp/signature image.
