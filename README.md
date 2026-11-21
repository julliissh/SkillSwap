# SkillSwap

Вебплатформа для взаємного навчання та обміну навичками.

## 1. Запуск інфраструктури

Для запуску PostgreSQL і Mailpit використовується Docker Compose.

Відкрийте PowerShell у кореневій папці SkillSwap.

Створіть файл `Docker/.env` на основі шаблону:

```powershell
Copy-Item Docker/.env.example Docker/.env
```

За потреби змініть `POSTGRES_PASSWORD` у `Docker/.env`.

Запустіть контейнери:

```powershell
docker compose --env-file Docker/.env -f Docker/docker-compose.yml up -d
```

Доступні сервіси:

- **PostgreSQL:** `localhost:5434`, база даних `skillswap_db`.
- **Mailpit Web UI:** [http://localhost:8026](http://localhost:8026)
- **Mailpit SMTP:** `localhost:1026`.

## 2. Налаштування та запуск backend

### 2.1. Налаштування `.env`

Створіть файл `Api/.env`:

```powershell
Copy-Item Api/.env.example Api/.env
```

У файлі потрібно налаштувати:

- `Jwt__Key` — ключ для підпису JWT.
- `EmailCode__HmacKey` — ключ для захисту кодів підтвердження email.
- `ConnectionStrings__DefaultConnection` — підключення до PostgreSQL. Пароль має збігатися з `POSTGRES_PASSWORD` у `Docker/.env`.

Для JWT і HMAC згенеруйте **два різні випадкові ключі**. Кожен повинен містити щонайменше 32 байти та бути закодованим у Base64.

Команда PowerShell:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Виконайте її двічі й запишіть результати у відповідні змінні файлу `Api/.env`.

Файли `.env` містять секретні дані та не повинні потрапляти до Git.

### 2.2. Застосування міграцій

```powershell
dotnet ef database update --project Persistence --startup-project Api
```

### 2.3. Запуск API

Із кореневої папки SkillSwap:

```powershell
dotnet run --project Api --urls "http://localhost:5001"
```

Або з папки `Api`:

```powershell
dotnet run --urls "http://localhost:5001"
```

Після запуску:

- **Swagger UI:** [http://localhost:5001/swagger](http://localhost:5001/swagger)
- **Health Check:** [http://localhost:5001/api/health](http://localhost:5001/api/health)

Swagger доступний у режимі `Development`.

## 3. Запуск frontend

Із кореневої папки SkillSwap:

```powershell
cd Frontend
npm install
npm run dev
```

Frontend буде доступний за адресою [http://localhost:3000](http://localhost:3000).

Наразі frontend перебуває на початковому етапі розробки, а основна реалізована функціональність авторизації знаходиться в backend.
