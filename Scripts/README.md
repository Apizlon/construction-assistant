# Construction Assistant - Scripts

В папке `Scripts` лежит минимальный набор для запуска микросервисов и PostgreSQL через Docker Compose.

## Быстрый старт

Из папки `Scripts`:

```bash
./build.sh
./up.sh
```

Остановить:

```bash
./down.sh
```

Полный сброс (остановка + удаление volume с данными Postgres):

```bash
./hard_reset.sh
```

## Порты

- `AuthService`: `http://localhost:5001`
- `UserService`: `http://localhost:5002`
- `ProjectCalculationService`: `http://localhost:5003`
- `PostgreSQL`: `localhost:5301` (логин/пароль `postgres/postgres`)

## Frontend (личный кабинет)

Frontend лежит в `E:\Projects\construction-assistant\Frontend` и запускается локально через Node.js.

Из папки `Frontend`:

```bash
npm install
npm run dev
```

По умолчанию фронт ожидает сервисы на:
- `http://localhost:5001` (AuthService)
- `http://localhost:5002` (UserService)
- `http://localhost:5003` (ProjectCalculationService)

