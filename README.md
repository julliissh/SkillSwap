# SkillSwap

Вебплатформа для взаємного навчання та обміну навичками.

## 1. Запуск бази даних (PostgreSQL)

База даних розгортається локально за допомогою Docker Compose:

```bash
cd Docker
cp .env.example .env   # після копіювання за потреби змініть POSTGRES_PASSWORD
docker compose up -d
```

> **Важливо:** пароль у налаштуваннях бекенду повинен збігатися зі значенням `POSTGRES_PASSWORD` у файлі `.env`.

## 2. Запуск бекенду (ASP.NET Core Web API)

Налаштуйте локальний рядок підключення до PostgreSQL через .NET User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=skillswap_db;Username=postgres;Password=change_me" --project Api
```

Запуск API:
```bash
dotnet run --project Api
```

- Документація **Swagger UI**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- Перевірка стану сервісу **Health Check**: [http://localhost:5000/api/health](http://localhost:5000/api/health)

## 3. Запуск фронтенду (Next.js)

```bash
cd Frontend
npm install
npm run dev
```

Фронтенд буде доступний за адресою [http://localhost:3000](http://localhost:3000).
