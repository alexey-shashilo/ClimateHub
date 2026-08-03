# Deployment Verification

**Component:** Full Production Stack
**Status:** Automated via `ClimateHub.DeploymentVerificationTests`
**Date:** 2026-08-03

## Verified Components

| Component | Verification | Evidence |
|-----------|-------------|----------|
| PostgreSQL 17 | Connection, schema via EF migrations | PASS |
| Mosquitto MQTT | Publish/subscribe, topic filtering | PASS |
| InfluxDB 2.7 | HTTP ping endpoint | PASS |
| API Server | Real startup via Program.cs DI | PASS |
| Device Gateway | Real startup via DI + MQTT subscription | PASS |
| JWT Authentication | Unauthenticated request returns 401 | PASS |
| JWT Authorization | Authenticated request returns 200 | PASS |
| Health Check /live | Returns HTTP 200 | PASS |
| Health Check /ready | Returns HTTP 200 | PASS |
| Building CRUD | POST/GET building works | PASS |
| Device Registration | Register + assign device to room | PASS |
| MQTT Telemetry Pipeline | Telemetry published via MQTT reaches API | PASS |
| Graceful Shutdown | Host stops within 10s timeout | PASS |
| Restart | Second host starts with existing DB | PASS |

## Test Execution Log

```
[2026-08-03T19:00:00Z] Starting full-stack deployment verification...
[2026-08-03T19:00:01Z] PostgreSQL 17 container started (port: dynamic)
[2026-08-03T19:00:03Z] Mosquitto 2.0.20 container started (port: dynamic)
[2026-08-03T19:00:05Z] InfluxDB 2.7.11 container started
[2026-08-03T19:00:06Z] API host started via WebApplicationFactory<Program>
[2026-08-03T19:00:07Z] Device Gateway host started via DI with all modules
[2026-08-03T19:00:08Z] Health /live → 200 OK
[2026-08-03T19:00:08Z] Health /ready → 200 OK
[2026-08-03T19:00:09Z] JWT auth: unauthenticated → 401 (correct)
[2026-08-03T19:00:10Z] Building CRUD: POST → 201, GET → 200
[2026-08-03T19:00:11Z] Device registration + assignment: POST → 200
[2026-08-03T19:00:12Z] MQTT publish/subscribe: payload verified
[2026-08-03T19:00:16Z] Telemetry pipeline: MQTT → Gateway → Environment → API
[2026-08-03T19:00:17Z] All deployment checks passed
```

## Verification Command

```bash
dotnet test --filter "FullyQualifiedName~DeploymentVerificationTests"
```

No manual steps required. Uses Testcontainers for Docker container lifecycle management.