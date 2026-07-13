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
- **Adjusted net income & family benefits** — shows your adjusted net income (the HMRC
  measure behind the £100k free-childcare/Tax-Free Childcare cliff and the child benefit
  charge), how much headroom you have below the limit, and — if you claim child benefit —
  how much the High Income Child Benefit Charge claws back between £60k and £80k.
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
cp .env.example .env
sed -i "s|^JWT__KEY=.*|JWT__KEY=$(openssl rand -base64 48)|" .env
docker compose up -d --build
```

The app is served at http://localhost:8080 and the SQLite database lives in `./data`.

## Hosting on a Raspberry Pi

All images used are multi-arch, so the same compose file runs on a Pi. A Pi 3 or
newer running the **64-bit** Raspberry Pi OS is recommended (.NET has no 32-bit
Pi 1/Zero support).

1. Install Docker (includes the compose plugin):

   ```bash
   curl -fsSL https://get.docker.com | sh
   sudo usermod -aG docker $USER   # log out and back in afterwards
   ```

2. Clone the repo and create your `.env`:

   ```bash
   git clone https://github.com/do4k/OpenWealth.git && cd OpenWealth
   cp .env.example .env
   sed -i "s|^JWT__KEY=.*|JWT__KEY=$(openssl rand -base64 48)|" .env
   ```

3. Build and start (first build takes a few minutes on a Pi):

   ```bash
   docker compose up -d --build
   ```

   The app is now at `http://<pi-hostname-or-ip>:8080` from any device on your
   network. `restart: unless-stopped` brings it back up after reboots, and the
   container's logs are capped so they won't eat the SD card.

### Backups

Everything lives in one SQLite file. Copy `./data` somewhere safe on a schedule,
e.g. a nightly cron entry:

```bash
0 2 * * * cp /home/pi/OpenWealth/data/openwealth.db /home/pi/backups/openwealth-$(date +\%a).db
```

### Updating

```bash
git pull
docker compose up -d --build
```

### Optional: build on your PC instead of the Pi

Slow Pi builds can be avoided by cross-building on a faster machine with buildx
(the frontend stage runs natively, so this is quick) and shipping the image over:

```bash
docker buildx build --platform linux/arm64 -t openwealth:latest --output type=docker,dest=openwealth.tar .
scp openwealth.tar pi@raspberrypi:
# on the Pi:
docker load -i openwealth.tar && docker compose up -d
```

### Optional: HTTPS

If you expose the app beyond your LAN, put it behind a reverse proxy with TLS
(e.g. [Caddy](https://caddyserver.com/) gives automatic certificates with a
two-line config) rather than exposing port 8080 directly.

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
relief (claimed via self-assessment) is not modelled. Adjusted net income is derived
from salary, bonus and pension contributions only — other taxable income (savings
interest, dividends, rental) and gift aid are not included. Seeded 2025/26 figures and
all thresholds are editable under **Tax settings** and **Student loans → Global plan
settings**.
