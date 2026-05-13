export function isValidEmail(email: string) {
  const v = email.trim();
  if (!v) return false;
  // Pragmatic email validation for UI
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
}

export function isValidPassword(password: string) {
  return password.length >= 6;
}

