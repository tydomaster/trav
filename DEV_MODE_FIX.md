# Исправление ошибки 401: Dev-mode авторизация

## Проблема

При локальной разработке фронтенд отправляет `dev-mode` в заголовке `X-Telegram-Init-Data`, но бэкенд не распознаёт это значение и возвращает 401.

## ✅ Решение

Добавлена обработка `dev-mode` в `TelegramAuthMiddleware.cs`:

- Если `initData == "dev-mode"`, бэкенд автоматически использует мок-пользователя из конфигурации `Dev:MockTelegramId`
- Это работает независимо от режима (Development/Production), что позволяет тестировать локально

## Настройка

### 1. Фронтенд (Vite)

Создайте файл `.env` в папке `Travel Manager MiniApp Design`:

```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_DEV_AUTH_BYPASS=true
```

### 2. Бэкенд

Убедитесь, что в `appsettings.json` есть:

```json
{
  "Dev": {
    "MockTelegramId": "123456789"
  }
}
```

### 3. Перезапустите оба сервиса

```bash
# Бэкенд
cd trav
dotnet run

# Фронтенд (в другом терминале)
cd "Travel Manager MiniApp Design"
npm run dev
```

## Как это работает

1. Фронтенд с `VITE_DEV_AUTH_BYPASS=true` отправляет `dev-mode` в заголовке `X-Telegram-Init-Data`
2. Бэкенд распознаёт `dev-mode` и создаёт/использует мок-пользователя с `TelegramId = 123456789`
3. Все запросы проходят авторизацию с этим пользователем

## Проверка

1. Откройте консоль браузера (F12)
2. Найдите в логах: `[API Client] Initialized: { devAuthBypass: true }`
3. Попробуйте загрузить список поездок
4. В логах бэкенда должно быть: `Dev-mode initData received. Using mock user for development.`

## Для Production

В production (Vercel) установите:
```
VITE_DEV_AUTH_BYPASS=false
```

Тогда фронтенд будет использовать реальный Telegram `initData` из `window.Telegram.WebApp.initData`.
