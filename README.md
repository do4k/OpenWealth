# OpenWealth

A self-hosted app for tracking the complete picture of your wealth, built for the UK:
net worth, take-home pay, student loans, mortgages, savings and investments — with an
opt-in, passphrase-protected profile you can share with the people you care about.

**Your data is yours.** Profiles only exist for registered users describing their own
finances; there is no way to create a profile for somebody else, and sharing is opt-in,
read-only and passphrase-protected.

## Features

- **Income & tax** — salary, bonus and pension (salary sacrifice / net pay / relief at
  source). Income tax, National Insurance and student loan repayments are calculated
  automatically, including the personal allowance taper above £100k.
- **UK student loans** — Plan 1, Plan 2, Plan 4, Plan 5 and Postgraduate loans.
  Interest rates are configured **globally per plan** (one rate applies to every loan of
  that plan), alongside per-plan repayment thresholds and rates.
- **Mortgages & property** — each mortgage has its own **localised interest rate**, term,
  and fixed/variable rate type. Fixed deals record when the fix ends and an optional
  follow-on rate, and the UI flags mortgages that are about to roll (or have rolled)
  onto a variable rate. Monthly repayments are computed by standard amortisation, and
  linked properties show your equity.
- **Savings & investments** — cash accounts, ISAs, Premium Bonds, pension pots, GIAs
  and more, summed into your net worth.
- **Dashboard** — net worth, assets vs liabilities, full item breakdown, and an annual
  income statement down to monthly take-home.
- **Sharing** — enable a public profile at a random URL, protected by a passphrase you
  choose. Pick what viewers see: net worth only, category totals, or the full breakdown.
  Rotate the link at any time to cut off old viewers.
- **Editable tax config** — all rates and thresholds (tax bands, NI, loan plans) are
  stored per user and seeded with 2025/26 figures, so you can update them each tax year
  without waiting for a code change.

## Stack

- **Backend**: ASP.NET Core 8 (minimal APIs), EF Core + SQLite, JWT auth
- **Frontend**: React 19 + TypeScript, Vite, React Router
- **Tests**: xUnit covering the tax/NI/student-loan/mortgage calculators

## Running with Docker (recommended)

```bash
export JWT__KEY="$(openssl rand -base64 48)"   # persist this somewhere safe
docker compose up --build
```

The app is served at http://localhost:8080 and the SQLite database lives in `./data`.

## Running for development

Backend (http://localhost:5179):

```bash
cd backend/OpenWealth.Api
dotnet run
```

Frontend dev server with hot reload (http://localhost:5173, proxies `/api` to 5179):

```bash
cd frontend
npm install
npm run dev
```

Run the calculation-engine tests:

```bash
dotnet test
```

## Configuration

| Setting | Env var | Notes |
| --- | --- | --- |
| JWT signing key | `JWT__KEY` | Required in production; 32+ chars. A dev key ships in `appsettings.Development.json` only. |
| Database | `ConnectionStrings__Default` | Defaults to `Data Source=openwealth.db` (Docker: `/data/openwealth.db`). |

## Accuracy notes

Calculations are annualised approximations of PAYE for England/Wales/NI rates, intended
for planning rather than payroll: NI is actually assessed per pay period, concurrent
Plan 1 + Plan 2 repayments have special rules, and relief-at-source higher-rate pension
relief (claimed via self-assessment) is not modelled. Seeded 2025/26 figures and all
thresholds are editable under **Tax settings** and **Student loans → Global plan settings**.
