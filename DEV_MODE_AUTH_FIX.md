# Исправление ошибки 401 для dev-mode

## Проблема
При открытии приложения в обычном браузере (не через Telegram MiniApp) возникает ошибка 401 Unauthorized, так как `window.Telegram.WebApp` отсутствует.

## Решение

### 1. Фронтенд (`Travel Manager MiniApp Design/src/lib/api/client.ts`)

Фронтенд теперь автоматически использует `dev-mode` в следующих случаях:
- Если установлена переменная `VITE_DEV_AUTH_BYPASS=true`
- Если `window.Telegram.WebApp` отсутствует И приложение запущено в dev режиме (не production)

```typescript
function getInitData(): string | null {
  // Явный dev-mode через переменную окружения
  if (DEV_AUTH_BYPASS) {
    return 'dev-mode';
  }

  const hasTelegramWebApp = typeof window !== 'undefined' && (window as any).Telegram?.WebApp;
  
  if (hasTelegramWebApp) {
    return (window as any).Telegram.WebApp.initData || null;
  } else {
    // Автоматически используем dev-mode в dev окружении
    if (!import.meta.env.PROD) {
      return 'dev-mode';
    }
  }

  return null;
}
```

### 2. Бэкенд (`trav/Middleware/TelegramAuthMiddleware.cs`)

Бэкенд обрабатывает `initData == "dev-mode"` и создает/использует мок-пользователя:

```csharp
if (initData == "dev-mode")
{
    var mockTelegramId = long.Parse(_configuration["Dev:MockTelegramId"] ?? "123456789");
    // Создает или находит пользователя с этим TelegramId
}
```

### 3. Конфигурация

В `appsettings.json` или `appsettings.Development.json` должно быть:
```json
{
  "Dev": {
    "MockTelegramId": "123456789"
  }
}
```

## Проверка

1. **Локальная разработка:**
   - Откройте приложение в браузере (не через Telegram)
   - В консоли должно появиться: `[API Client] ⚠️ Telegram WebApp not found. Using dev-mode automatically.`
   - Запросы должны проходить с заголовком `X-Telegram-Init-Data: dev-mode`
   - Бэкенд должен аутентифицировать мок-пользователя

2. **В Telegram MiniApp:**
   - Откройте приложение через Telegram
   - В консоли должно появиться: `[API Client] Using Telegram initData`
   - Используется реальный `initData` от Telegram

3. **Production:**
   - Если `VITE_DEV_AUTH_BYPASS=false` и нет Telegram WebApp - будет ошибка 401 (это правильно)
   - Если `VITE_DEV_AUTH_BYPASS=true` - будет использоваться dev-mode

## Логи бэкенда

При успешной аутентификации в dev-mode вы увидите:
```
Dev-mode initData received. Using mock user for development.
Using existing dev-mode user with TelegramId: 123456789, UserId: 1
User authenticated - UserId: 1, TelegramId: 123456789, Name: Dev User
```

## Переменные окружения

### Фронтенд (`.env`):
```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_DEV_AUTH_BYPASS=true  # Опционально, автоматически определяется в dev
```

### Бэкенд (`appsettings.json`):
```json
{
  "Dev": {
    "MockTelegramId": "123456789"
  }
}
```
