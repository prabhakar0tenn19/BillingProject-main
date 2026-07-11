# Billing System — Progress Log
> For any AI picking this up next: read this file first. It documents everything done, all decisions made, and what's next.

---

## Project Overview
Sanitaryware manufacturing company billing software.
- **Business**: Tap/Shower/Drain Cover/Washbasin manufacturer
- **Users**: Non-technical company staff (company-side only, no customer portal)
- **Key Feature**: Party-specific pricing — each customer/party gets different prices per product

## Tech Stack (Final Decisions)
| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 Web API |
| Database | MongoDB Atlas (`billing_db`) |
| MongoDB Driver | MongoDB.Driver 2.28.0 |
| PDF | QuestPDF 2024.10.4 |
| Images (signature) | Cloudinary |
| Frontend | React + Vite + Ant Design (not built yet) |

## Credentials (Stored in appsettings.json)
- **MongoDB**: `mongodb+srv://prabhakar0tenn_db_user:peter0lily07@cluster0.pd6jgqi.mongodb.net/?appName=Cluster0`
- **DB Name**: `billing_db`
- **Cloudinary Cloud**: `dvsukstvb`
- **Cloudinary Key**: `488346396724369`
- **Cloudinary Secret**: `ccuzehWLCBvJAivBj93Xtz0TcNk`

## Project Structure
```
BillingProject-main/          ← workspace root
  backend/                    ← NEW clean ASP.NET Core 8 API (this is what we're building)
    BillingSystem.csproj
    appsettings.json
    Program.cs
    Infrastructure/
      MongoDbContext.cs        ← typed MongoDB collection access
      DataSeeder.cs           ← seeds 4 categories + default settings on first run
    Models/                   ← MongoDB document models (BsonId, BsonElement)
    DTOs/                     ← clean request/response contracts (basePrice NEVER in responses)
    Services/
      Interfaces/
    Helpers/
    Controllers/
  frontend/                   ← NOT BUILT YET (React + Vite + Ant Design)
  BillingProject-main/        ← OLD broken SQL Server project (IGNORE THIS)
```

## MongoDB Collections Design
| Collection | Purpose |
|---|---|
| `settings` | Single document — company info, GST rate, invoice counter, signature URL |
| `categories` | 4 main: Taps(HSN:8481), Showers(8481), Drain Covers(7325), Washbasins(6910) |
| `products` | Catalog with `basePrice` field (INTERNAL ONLY — never sent to frontend) |
| `customers` | Parties/clients |
| `customer_pricing` | Per-party per-product custom price overrides |
| `invoices` | Embedded items + customer/company snapshots |

## CRITICAL Business Logic
1. **basePrice**: Stored in product but NEVER returned in API responses. Hidden via DTO design.
2. **Party Pricing**: `customer_pricing` collection stores negotiated prices per party. Falls back to basePrice if no override.
3. **Snapshot Pattern**: Invoice stores frozen copy of customer+company details at time of billing. Historical accuracy guaranteed.
4. **Atomic Invoice Numbers**: MongoDB `findOneAndUpdate` with `$inc` on `settings.nextInvoiceNumber`. Thread-safe.
5. **GST 5%**: Split as CGST 2.5% + SGST 2.5% on PDF. Stored as total GST in DB.
6. **Soft Delete**: All entities have `isActive` flag. Nothing hard-deleted.
7. **Digital Signature**: Uploaded to Cloudinary once via Settings page. URL stored in settings. Fetched and base64-embedded in every PDF.

## HSN Codes
- Taps → 8481
- Showers → 8481
- Drain Covers → 7325
- Washbasins → 6910

## API Base URL
`http://localhost:5000/api/v1/`
Swagger UI: `http://localhost:5000/swagger`

## Build Status
| File | Status |
|---|---|
| BillingSystem.csproj | ✅ Done |
| appsettings.json | ✅ Done |
| Infrastructure/MongoDbContext.cs | ✅ Done |
| Models/* | ✅ Done (Enhanced with Product Images) |
| DTOs/* | ✅ Done (Enhanced with Product Images) |
| Services/Interfaces/* | ✅ Done |
| Services/* (implementations) | ✅ Done |
| Helpers/NumberToWords.cs | ✅ Done |
| Controllers/* | ✅ Done (Enhanced with Product Images) |
| Program.cs | ✅ Done |
| Infrastructure/DataSeeder.cs | ✅ Done |

## What's Next (Frontend)
1. Configure Vite React frontend options
2. Build pages: Dashboard, Parties, Products, Categories, New Bill (3-step wizard), Invoices, Reports, Settings
3. Integrate Ant Design, Axios, React Router

## Known Issues / Watch Out For
- QuestPDF requires `QuestPDF.Settings.License = LicenseType.Community;` before first PDF generation (set in Program.cs)
- Cloudinary upload must use signed upload (using ApiKey + ApiSecret)
- MongoDB compound index on (customerId, productId) in customer_pricing — enforces 1 price override per product per party
- CORS is configured to allow localhost:5173 (Vite default) and localhost:3000

## Last Updated
2026-07-11 — Backend completed, verified compiled and seeded. Ready for frontend.
