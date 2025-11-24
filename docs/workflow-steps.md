# AI Workflow Dokumentácia

**Meno:**

**Dátum začiatku:** 23.11.2025

**Dátum dokončenia:**

**Zadanie:** ~~Frontend~~ / **Backend**

---

## 1. Použité AI Nástroje

Vyplň približný čas strávený s každým nástrojom:

- [ ] **Cursor IDE:** __0__ hodín
- [ ] **Claude Code:** _____ hodín
- [ ] **GitHub Copilot:** __0__ hodín
- [ ] **ChatGPT:** __1__ hodín
- [ ] **Claude.ai:** _____ hodín
- [ ] **Iné:**

**Celkový čas vývoja (priližne):** _____ hodín

---

## 2. Zbierka Promptov

> 💡 **Tip:** Kopíruj presný text promptu! Priebežne dopĺňaj po každej feature.

### Prompt #0: _________________________________

**Nástroj:** [ Cursor / Claude Code / Copilot / ChatGPT / Iné ]
**Kontext:** [ Setup projektu / OAuth implementácia / ... ]

**Prompt:**
```
[Sem vlož celý text promptu - presne ako si ho zadal do AI]
```

**Výsledok:**
[ ] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Čo som musel upraviť / opraviť:**
```
[Popíš čo si musel zmeniť. Ak nič, napíš "Nič, fungoval perfektne"]
```

**Poznámky / Learnings:**
```
[Prečo fungoval / nefungoval? Čo by si urobil inak?]
```



### Prompt #1: Uvodný pokec s ChatGPT ohľadom projektu, navrhu, technológií a pod.

**Nástroj:** ChatGPT
**Kontext:** Predstavenie projektu a jeho návrh

**Prompt:**
```
I am planning on starting a new project in dotnet. It will be web api and some event-driven background processing. The web api will consist of login and multiple modules (users, products, orders). Each module will have CRUD operations authenticated with JWT from login. Of course there will also be some validation rules. App has to contain also unit tests and even integration tests. App will run in docker and will use Postgres DB. App will have to store migrations for db. Migrations can also store intial seeding data. EfCore has to be used for db wit code first approach (if recommended). Documentation has to contain manual on how to run the db upgrade and start the service. For the even driven part, some messaging service has to be used. It has to be supported in dotnet and easily implemented. This service has to be created inside of docker. There must also be some event bus for messaging. I need more detailed info on this, with deep analysis of all possibilities I can use for this architecture. Async processing will be then used for processing of orders - not important at the moment. Architecture also has to support chron job - task running every N seconds/minutes. Can you help me with analysis and design of technologies I can use with some recommendations? Can you also help with good, modern and transparent project structure design (maybe separation of web api, workers, database, tests etc.), transparent folder structure. Also help with docker
```

**Výsledok:**
⭐⭐⭐⭐ - architecture.md súbor

**Úpravy:**
```
great, thanks! let me pick what I prefer and then I want you to prepare md file for the project which will describe its architecture, technologies, structure, design, and everything describing the project from technological point of view. pick .net 8 for LTS. go with controllers, good enough for simple CRUD. for Auth I have to use my own Users table so custom login mechanism with JWT. Postgresql with code first. RabbitMQ with Masstransit for async jobs. Background service for cron jobs. xUnit for unit testing with NSubstitute and FluendAssertion. Testcontainers for integration tests. Solution: I like the ideas of folders first - src, tests, docs, docker. In src, I will have MyApp.Api - for all web api operations and login, dtos, validation. It has to have OpenApi/Swagger documentation. MyApp.Dal - everything with database - efcore, dbcontext, migrations (seeding done by Up method), domains. MyApp.Worker - everything for async jobs, coonsumers, MyApp.Common - everything common for api and worker - masstransit, rabbitmq initialization, eventbus, events/commands. I also need md file on how to run the application.
```

**Poznámky:**
```
Nejake detaily som si už sám potom upravil.
```

### Prompt #2: _________________________________

**Nástroj:** [ Cursor / Claude Code / Copilot / ChatGPT / Iné ]
**Kontext:** [ Setup projektu / OAuth implementácia / ... ]

**Prompt:**
```
[Sem vlož celý text promptu - presne ako si ho zadal do AI]
```

**Výsledok:**
[ ] ✅ Fungoval perfektne (first try)
[ ] ⭐⭐⭐⭐ Dobré, potreboval malé úpravy
[ ] ⭐⭐⭐ OK, potreboval viac úprav
[ ] ⭐⭐ Slabé, musel som veľa prepísať
[ ] ❌ Nefungoval, musel som celé prepísať

**Čo som musel upraviť / opraviť:**
```
[Popíš čo si musel zmeniť. Ak nič, napíš "Nič, fungoval perfektne"]
```

**Poznámky / Learnings:**
```
[Prečo fungoval / nefungoval? Čo by si urobil inak?]
```

---

## 3. Problémy a Riešenia

> 💡 **Tip:** Problémy sú cenné! Ukazujú ako riešiš problémy s AI.

### Problém #1: _________________________________

**Čo sa stalo:**
```
[Detailný popis problému - čo nefungovalo? Aká bola chyba?]
```

**Prečo to vzniklo:**
```
[Tvoja analýza - prečo AI toto vygeneroval? Čo bolo v prompte zlé?]
```

**Ako som to vyriešil:**
```
[Krok za krokom - čo si urobil? Upravil prompt? Prepísal kód? Použil iný nástroj?]
```

**Čo som sa naučil:**
```
[Konkrétny learning pre budúcnosť - čo budeš robiť inak?]
```

**Screenshot / Kód:** [ ] Priložený

---

### Problém #2: _________________________________

**Čo sa stalo:**
```
```

**Prečo:**
```
```

**Riešenie:**
```
```

**Learning:**
```
```

## 4. Kľúčové Poznatky

### 4.1 Čo fungovalo výborne

**1.**
```
[Príklad: Claude Code pre OAuth - fungoval first try, zero problémov]
```

**2.**
```
```

**3.**
```
```

**[ Pridaj viac ak chceš ]**

---

### 4.2 Čo bolo náročné

**1.**
```
[Príklad: Figma MCP spacing - často o 4-8px vedľa, musel som manuálne opravovať]
```

**2.**
```
```

**3.**
```
```

---

### 4.3 Best Practices ktoré som objavil

**1.**
```
[Príklad: Vždy špecifikuj verziu knižnice v prompte - "NextAuth.js v5"]
```

**2.**
```
```

**3.**
```
```

**4.**
```
```

**5.**
```
```

---

### 4.4 Moje Top 3 Tipy Pre Ostatných

**Tip #1:**
```
[Konkrétny, actionable tip]
```

**Tip #2:**
```
```

**Tip #3:**
```
```

---

## 6. Reflexia a Závery

### 6.1 Efektivita AI nástrojov

**Ktorý nástroj bol najužitočnejší?** _________________________________

**Prečo?**
```
```

**Ktorý nástroj bol najmenej užitočný?** _________________________________

**Prečo?**
```
```

---

### 6.2 Najväčšie prekvapenie
```
[Čo ťa najviac prekvapilo pri práci s AI?]
```

---

### 6.3 Najväčšia frustrácia
```
[Čo bolo najfrustrujúcejšie?]
```

---

### 6.4 Najväčší "AHA!" moment
```
[Kedy ti došlo niečo dôležité o AI alebo o developmente?]
```

---

### 6.5 Čo by som urobil inak
```
[Keby si začínal znova, čo by si zmenil?]
```

### 6.6 Hlavný odkaz pre ostatných
```
[Keby si mal povedať jednu vec kolegom o AI development, čo by to bylo?]
```
