# LexVersus Email Marketing

Email marketing web application built with **ASP.NET Core 10** and **React**, powered by **Twilio SendGrid**.

## Features

- **Contact import** — Upload CSV or Excel files (`.csv`, `.xlsx`, `.xls`)
- **Email lists** — Organize contacts into named lists for targeted campaigns
- **Email templates** — Create HTML templates with personalization tags
- **Campaigns** — Create, schedule, or send campaigns to all contacts or a specific list
- **Scheduled sending** — Background service auto-sends campaigns at the scheduled time
- **Authentication** — JWT-based login and registration protects the admin dashboard
- **Unsubscribe (GDPR/CAN-SPAM)** — Per-contact unsubscribe links with public confirmation page
- **SendGrid integration** — Deliver emails via Twilio SendGrid API
- **Multiple senders** — Choose verified From addresses per campaign (Senders page)
- **Dashboard** — Overview of contacts, templates, campaigns, and daily sends

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core 10 Web API |
| Database | SQL Server (Entity Framework Core) |
| Email | Twilio SendGrid |
| File parsing | CsvHelper, ClosedXML |
| Frontend | React 19 + TypeScript + Vite |

## Project Structure

```
LexVersusEmailMarketing/
├── src/
│   ├── LexVersusEmailMarketing.Api/          # Web API controllers
│   ├── LexVersusEmailMarketing.Core/         # Entities, DTOs, interfaces
│   └── LexVersusEmailMarketing.Infrastructure/ # EF Core, SendGrid, import
├── client/                                    # React frontend
└── samples/                                   # Sample import files
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Twilio SendGrid](https://sendgrid.com/) account with verified sender

### 1. Configure settings

Edit `src/LexVersusEmailMarketing.Api/appsettings.Development.json`:

```json
{
  "Jwt": {
    "Key": "your-production-secret-key-at-least-32-characters-long",
    "Issuer": "LexVersusEmailMarketing",
    "Audience": "LexVersusEmailMarketing",
    "ExpireMinutes": 480
  },
  "App": {
    "PublicUrl": "http://localhost:5173"
  },
  "SendGrid": {
    "ApiKey": "SG.your-api-key-here",
    "FromEmail": "marketing@yourdomain.com",
    "FromName": "LexVersus Marketing"
  }
}
```

The `FromEmail` must be a verified sender in your SendGrid account. Set `App:PublicUrl` to your public frontend URL in production (used for unsubscribe links).

### 2. Run the API

```bash
cd src/LexVersusEmailMarketing.Api
dotnet run
```

API runs at `http://localhost:5126`. Database tables are created automatically on first run.

Connection string (in `appsettings.Development.json`):

```
Server=SMION-ZEN\SMION2019;Database=MarketingLexVersusDB;Trusted_Connection=True;TrustServerCertificate=True
```

### 3. Run the React frontend

```bash
cd client
npm install
npm run dev
```

Frontend runs at `http://localhost:5173` and proxies API requests to the backend.

## Usage Workflow

1. **Register / Sign in** — Create an account at `/register` (first-time setup)
2. **Import contacts** — Go to Contacts, upload a CSV/Excel file (see `samples/contacts-sample.csv`)
3. **Create a template** — Go to Templates, design your HTML email with tags like `{{firstName}}`
4. **Create a campaign** — Go to Campaigns, pick a template and audience, then:
   - **Save as draft** — Review before sending
   - **Schedule for later** — Auto-sends at the chosen date/time
   - **Send immediately** — Sends right away via SendGrid

### Personalization Tags

Use these in template subject and HTML body:

| Tag | Replaced with |
|-----|---------------|
| `{{firstName}}` | Contact first name |
| `{{lastName}}` | Contact last name |
| `{{fullName}}` | First + last name |
| `{{email}}` | Contact email address |
| `{{unsubscribeUrl}}` | Unique unsubscribe link for the recipient |

An unsubscribe footer is automatically appended to emails that don't already include one.

### Unsubscribe (GDPR / CAN-SPAM)

Each contact receives a unique unsubscribe link (`/unsubscribe/{token}`). When clicked:
- The contact is marked as unsubscribed (`IsSubscribed = false`)
- Future campaigns skip unsubscribed contacts
- A timestamp is recorded for audit purposes

### Import File Format

CSV and Excel files should have a header row with columns such as:

- **Email** (required) — also accepts `e-mail`, `mail`
- **FirstName** (optional) — also accepts `first_name`, `first name`, `ime`
- **LastName** (optional) — also accepts `last_name`, `last name`, `prezime`

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | No | Create account |
| POST | `/api/auth/login` | No | Sign in |
| GET | `/api/auth/me` | Yes | Current user |
| GET | `/api/unsubscribe/{token}` | No | Unsubscribe info |
| POST | `/api/unsubscribe/{token}` | No | Confirm unsubscribe |
| GET | `/api/dashboard/stats` | Yes | Dashboard statistics |
| GET | `/api/contacts` | Yes | List contacts |
| POST | `/api/contacts` | Yes | Add contact |
| POST | `/api/contacts/import` | Yes | Import CSV/Excel file |
| GET | `/api/contacts/lists` | Yes | List email lists |
| POST | `/api/contacts/lists` | Yes | Create email list |
| GET | `/api/templates` | Yes | List templates |
| POST | `/api/templates` | Yes | Create template |
| PUT | `/api/templates/{id}` | Yes | Update template |
| GET | `/api/campaigns` | Yes | List campaigns |
| POST | `/api/campaigns` | Yes | Create campaign |
| POST | `/api/campaigns/{id}/send` | Yes | Send campaign |

## Production Notes

- Ensure the SQL Server login has read/write access to `MarketingLexVersusDB`
- Store SendGrid API key and JWT secret in environment variables or Azure Key Vault
- Change `Jwt:Key` to a strong random secret in production
- Set `App:PublicUrl` to your production frontend domain
- Consider background job processing (Hangfire/Quartz) for very large campaign sends
- Implement double opt-in for stricter GDPR compliance if required
