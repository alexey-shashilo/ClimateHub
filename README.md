# Climate Hub

**Status:** Architecture Prototype / Active Development  
**Not approved for safety-critical production control**

Локальная модульная платформа управления микроклиматом и инженерными системами частного дома.

## Быстрый старт

### Предварительные требования

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Запуск всей системы (Docker Compose)

```bash
cd deploy
docker compose up -d
# Без сервисов приложений (только инфраструктура):
docker compose up -d postgres mosquitto influxdb redis

# С API и Device Gateway (сборка из исходников):
docker compose --profile full up -d --build
```

Компоненты:
| Сервис | Порт | Назначение |
|---|---|---|
| PostgreSQL 17 | 5432 | Основное хранилище |
| Mosquitto 2.0 | 1883 | MQTT брокер |
| InfluxDB 2.7 | 8086 | Time-series |
| Redis 7.4 | 6379 | Кэш/координация |
| ClimateHub API | 5000 | REST API |
| Device Gateway | - | MQTT consumer |

### Запуск для разработки (с хоста)

```bash
# Инфраструктура
cd deploy
docker compose up -d postgres mosquitto influxdb redis

# API (терминал 1)
cd src/ClimateHub.Api
dotnet run --launch-profile http

# Device Gateway (терминал 2)
cd src/ClimateHub.DeviceGateway
dotnet run
```

### Проверка

```bash
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
```

### Тестирование

```bash
dotnet test
```

### Device Simulator

```bash
cd tools/ClimateHub.DeviceSimulator
dotnet run -- --building-id <guid> --device-id <guid> --interval 5000
```

## Структура проекта

```
climate-hub/
├── src/
│   ├── ClimateHub.Api/                   # REST API (ASP.NET Core)
│   ├── ClimateHub.DeviceGateway/         # MQTT consumer (Worker Service)
│   ├── ClimateHub.SharedKernel/          # Общие типы, абстракции, value objects
│   ├── ClimateHub.Infrastructure/        # OpenTelemetry, health checks, DI
│   └── Modules/
│       ├── Building/                     # Пространственная структура здания
│       ├── Devices/                      # Регистрация и управление устройствами
│       └── Environment/                  # Телеметрия и состояние микроклимата
├── tests/
│   ├── ClimateHub.SmokeTests/            # Smoke-тесты SharedKernel
│   ├── ClimateHub.Modules.Building.UnitTests/
│   ├── ClimateHub.Modules.Devices.UnitTests/
│   ├── ClimateHub.Modules.Environment.UnitTests/
│   └── ClimateHub.EndToEndTests/         # E2E (требует Docker Engine)
├── tools/
│   └── ClimateHub.DeviceSimulator/       # Симулятор для проверки MQTT
├── deploy/
│   ├── docker-compose.yml                # Локальная инфраструктура
│   ├── docker/                           # Dockerfile'ы для API и Gateway
│   ├── postgres/                         # SQL-инициализация схем
│   └── mqtt/                             # Конфигурация Mosquitto
├── docs/
├── Directory.Build.props
├── Directory.Packages.props
└── global.json
```

## MQTT API

Публикация телеметрии:

```
Topic: climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment
QoS: 1
```

Payload:
```json
{
  "messageId": "guid",
  "messageType": "environment.telemetry",
  "protocolVersion": "1.0",
  "buildingId": "guid",
  "deviceId": "guid",
  "bootId": "guid",
  "sequenceNumber": 1,
  "measuredAt": "2026-07-31T08:00:00Z",
  "payload": {
    "temperatureC": 22.4,
    "relativeHumidityPct": 41.7,
    "co2Ppm": 735
  }
}
```

## REST API

| Метод | Путь | Описание |
|---|---|---|
| POST | /api/v1/buildings | Создать здание |
| POST | /api/v1/buildings/{id}/floors | Создать этаж |
| POST | /api/v1/floors/{id}/rooms | Создать комнату |
| POST | /api/v1/devices | Зарегистрировать устройство |
| POST | /api/v1/devices/{id}/assignments | Назначить устройство в комнату |
| GET | /api/v1/rooms/{id}/environment | Текущее состояние микроклимата |
| GET | /api/v1/rooms/{id}/environment/history | История измерений |
| GET | /health/live | Liveness probe |
| GET | /health/ready | Readiness probe |