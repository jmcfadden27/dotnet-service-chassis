# Changelog

All notable changes to this project are listed here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [semantic versioning](https://semver.org).

## [0.1.0] - 2026-11-11

First public release.

### QLS.Chassis.Core

- Configuration reading from `appsettings.json`, a bootstrap source and environment variables (`ConfigReader`).
- Resilience executor with per-attempt timeouts, retries with exponential backoff and jitter, and a circuit breaker
  per dependency, logging structured events through `ILogger`.
- HTTP and message idempotency coordinators with a pluggable store.
- Structured log event model and factory, and a log sanitizer that redacts secrets from headers, query strings and
  JSON or form bodies.
- Message envelope with header validation, a batch consumer that reports failed messages, and outbound correlation
  and trace header propagation (W3C `traceparent` and AWS X-Ray).

### QLS.Chassis.WebApi

- Global exception handler returning problem details, correlation ID middleware and structured request logging.
- JWT bearer authentication with start-up validation, AWS Cognito user pool ARN support and authentication
  required by default; current-user services.
- Postgres resilience with a readiness health check, and Postgres-backed HTTP idempotency with background cleanup.
- Liveness and readiness health endpoints, and Swagger Bearer security.

[0.1.0]: https://github.com/jmcfadden27/dotnet-service-chassis/releases/tag/v0.1.0
