export function getRuErrorMessage(err: any, fallback: string) {
  const message = err?.response?.data?.message ?? err?.message ?? "";

  // Backend messages mapping (keep UI fully Russian)
  const map: Record<string, string> = {
    "Invalid credentials": "Неверный логин или пароль",
    EMAIL_ALREADY_USED: "Этот email уже используется",
    ADMIN_REGISTRATION_ATTEMPT: "Регистрация администратора запрещена",
    "Project not found": "Проект не найден",
    "Share code not found or inactive": "Код доступа не найден или отключён",
    "Code is required": "Введите код доступа",
    "Project name is required": "Название проекта обязательно"
  };

  return map[message] ?? fallback;
}

