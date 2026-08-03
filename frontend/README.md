# Climate Hub Frontend

Веб-приложение для управления инженерными системами здания — Digital Twin помещений.

## Технологический стек

- **React 19** + TypeScript
- **Vite 8** (сборка)
- **Mantine v9** (UI)
- **TanStack Query v5** (серверное состояние)
- **React Router v7** (навигация)
- **Zod v4** (валидация контрактов)
- **ECharts v6** (графики)
- **MSW v2** (mock API)
- **Vitest v4** (тесты)

## Быстрый старт

```bash
cd frontend
npm install
npm run dev
```

Приложение будет доступно на `http://localhost:5173`.

## Команды

| Команда | Описание |
|---|---|
| `npm run dev` | Запуск dev-сервера |
| `npm run build` | Production-сборка |
| `npm run preview` | Просмотр production-сборки |
| `npm run typecheck` | Проверка типов |
| `npm run test` | Запуск тестов |
| `npm run test:watch` | Тесты в watch-режиме |

## Переменные окружения

См. `.env.example`:

- `VITE_API_BASE_URL` — URL API (по умолчанию `/api/v1` через proxy)
- `VITE_ENABLE_MOCKS` — включение MSW (`true` для разработки)
- `VITE_DEFAULT_BUILDING_ID` — ID здания по умолчанию

## Маршруты

| Маршрут | Описание |
|---|---|
| `/` | Редирект на `/buildings/building-001` |
| `/buildings/:buildingId` | Обзор здания |
| `/rooms/:roomId` | Digital Twin помещения |
| `/rooms/:roomId/history` | История параметров |
| `/rooms/:roomId/devices` | Устройства помещения |
| `/devices` | Реестр устройств |
| `/devices/:deviceId` | Карточка устройства |
| `/devices/register` | Регистрация устройства |
| `/dev/mock-controls` | Панель управления mocks (dev only) |

## Архитектура

```
Page → Feature Hook → Query Hook → API Client → MSW
```

- **Страницы** — композиция feature-компонентов
- **Feature Hooks** — инкапсулируют TanStack Query
- **API Client** — типизированный HTTP-клиент
- **MSW** — mock API в development mode

## Структура

```
src/
├── app/               # App, router, providers
├── pages/             # Страницы
├── features/          # Hooks и логика страниц
├── shared/
│   ├── api/           # HTTP клиент + endpoints
│   ├── contracts/     # Zod схемы и типы
│   ├── components/    # Общие компоненты (Layout)
│   ├── lib/           # Форматтеры, freshness, capability resolver
│   ├── theme/         # Mantine theme
│   └── types/         # TS интерфейсы
├── mocks/
│   ├── handlers/      # MSW handlers
│   ├── database/      # Mock DB и fixtures
│   └── browser.ts     # MSW browser worker
└── tests/             # Unit тесты
```

## Отключение MSW

Установи `VITE_ENABLE_MOCKS=false` — приложение будет пытаться подключиться к реальному API.

## Capability-driven UI

Элементы управления устройствами формируются на основе capabilities:

```typescript
getCapabilityUi('measure.temperature')
// → { code, label: 'Температура', unit: '°C', icon: '🌡', kind: 'measurement' }
```

## Ограничения

- Mock API не поддерживает реальную персистентность (in-memory)
- Нет реального real-time обновления
- Командный контур (управление устройствами) — placeholder
- Полная авторизация не реализована