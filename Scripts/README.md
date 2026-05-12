# Construction Assistant - Scripts

В папке `Scripts` лежит минимальный набор для запуска двух сервисов (`AuthService`, `UserService`) и PostgreSQL.

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
- `PostgreSQL`: `localhost:5301` (логин/пароль `postgres/postgres`)

