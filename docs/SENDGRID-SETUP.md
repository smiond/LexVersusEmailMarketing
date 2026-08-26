# SendGrid postavljanje (LexVersus Email Marketing)

Ova uputa pokriva što treba u SendGridu i kako koristiti više **From** adresa u aplikaciji.

## 1. Što treba u SendGridu

### A) API Key

1. Otvori [SendGrid](https://app.sendgrid.com/) (ili Email dio Twilio konzole).
2. **Settings → API Keys → Create API Key**
3. Tip: **Restricted Access** s dozvolom **Mail Send**
4. Kopiraj ključ (počinje s `SG.`) — vidiš ga samo jednom.

U `src/LexVersusEmailMarketing.Api/appsettings.Development.json`:

```json
"SendGrid": {
  "ApiKey": "SG.xxxxxxxx",
  "FromEmail": "marketing@tvoja-domena.com",
  "FromName": "LexVersus Marketing"
}
```

`FromEmail` / `FromName` su rezervni default ako u aplikaciji još nema pošiljatelja.

Bolje: User Secrets (ne commitaj ključ u git):

```powershell
cd src\LexVersusEmailMarketing.Api
dotnet user-secrets init
dotnet user-secrets set "SendGrid:ApiKey" "SG.xxxxxxxx"
```

### B) Verificirani pošiljatelji (From adrese)

SendGrid **neće** slati s adrese koja nije verificirana.

**Opcija 1 — Single Sender Verification** (brzo, za test):

1. **Settings → Sender Authentication → Verify a Single Sender**
2. Unesi email (npr. `marketing@firma.hr`, `info@firma.hr`)
3. Potvrdi link iz SendGrid maila

**Opcija 2 — Domain Authentication** (preporučeno za produkciju):

1. **Settings → Sender Authentication → Authenticate Your Domain**
2. Dodaj DNS zapise (CNAME) kod registrara domene
3. Nakon toga možeš slati s bilo koje adrese `@tvoja-domena.com`

U aplikaciji smiješ dodati **samo** adrese koje su u SendGridu već verificirane (ili spadaju pod autentificiranu domenu).

## 2. Korištenje u ovoj aplikaciji

### Korak 1 — Dodaj API Key

Postavi `SendGrid:ApiKey` kao gore, pa restartaj API.

### Korak 2 — Dodaj pošiljatelje u UI

1. Pokreni API + frontend
2. Prijavi se → izbornik **Senders**
3. Za svaku SendGrid adresu unesi:
   - **From Email** (npr. `marketing@firma.hr`)
   - **Display Name** (npr. `LexVersus Marketing`)
   - po želji **Default sender**

### Korak 3 — Odaberi pošiljatelja u kampanji

1. **Campaigns → New Campaign**
2. Polje **From Sender** — odaberi adresu za tu kampanju
3. Različite kampanje mogu imati različite From adrese

Ako ne odabereš pošiljatelja, koristi se default iz **Senders**, pa tek onda `SendGrid:FromEmail` iz konfiguracije.

## 3. Provjera

1. Pošalji test kampanju na **svoj** inbox
2. Provjeri From: ime + email
3. Ako SendGrid vrati grešku (403 / Forbidden):
   - API Key nema Mail Send
   - From adresa nije verificirana
   - krivi / istekli API Key

## 4. Twilio vs SendGrid

| Twilio Account SID / Auth Token | **Ne koristi se** za email u ovoj aplikaciji |
| SendGrid API Key (`SG....`)     | **Da** — obavezno |
| Verificirani From email         | **Da** — po jednoj adresi ili cijela domena |

## 5. Sigurnost

- Ne dijeliti API Key u chatu / gitu
- Rotirati ključ ako je procurio
- U produkciji držati tajne u User Secrets / env / Key Vault
