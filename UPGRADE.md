# Upgrade Guide | Yükseltme Rehberi | Upgrade-Anleitung

---

## 🇹🇷 Türkçe

### v1.2.0 — REST API, Kimlik Doğrulama, Dashboard ve SDK

#### Neler Değişti?

**REST API Katmanı**
- `api/v1/databases/{db}/collections/{col}/records` altında tam CRUD API
- Filtreleme (12 operatör), sıralama, sayfalama desteği
- Aggregate endpoint (sum/avg/min/max/count)
- Endpoint: `/api/v1/backup`
- OpenAPI/Scalar dökümantasyonu: `/scalar/v1`

**Kimlik Doğrulama & Yetkilendirme**
- Kullanıcı kaydı: `POST /api/v1/auth/register`
- Giriş: `POST /api/v1/auth/login` — JWT + refresh token
- JWT middleware ile rol bazlı yetkilendirme
- Roller: `admin` (tümü), `editor` (okuma/yazma), `viewer` (salt okunur)

**WebSocket & Gerçek Zamanlı**
- WebSocket: `/ws?channel=X&token=Y`
- SSE: `GET /api/v1/events/stream?channel=default`
- Olay yayınlama: `POST /api/v1/events/publish`

**Dashboard**
- Web tabanlı admin paneli (`http://localhost:3001`)
- Giriş, veritabanı gezgini, kayıt görüntüleyici, backup yönetimi

**SDK**
- `AhirClient` — Tüm API işlemleri için C# SDK

**CLI**
- Interaktif shell: `ahir shell`
- Komutlar: status, databases, use, collections, insert, query, get, delete, count, backup, metrics

**Veritabanı Motoru**
- `QueryAsync` — 12 filtre operatörü ile tam uygulama
- AES-256-GCM beklemede şifreleme
- Aggregate sorgu desteği
- StorageEngine pozisyon hatası düzeltildi

**Yedekleme & İzleme**
- `BackupService` — `.ahirbak` zip dosyalarına tam yedekleme/geri yükleme
- `MonitorService` — gerçek zamanlı CPU/bellek/disk/bağlantı metrikleri
- OpenTelemetry enstrümantasyonu

**Geliştirici Deneyimi**
- 31 birim/entegrasyon testi
- BenchmarkDotNet performans testleri
- Dockerfile + docker-compose.yml
- GitHub Actions CI/CD (build, test, çoklu platform yayın)
- Örnek eklentiler: Logger, Webhook, Status
- `MigrationService` — şema versiyonlama
- `ConfigService` — runtime JSON yapılandırma

#### Breaking Changes
- API route'ları artık `api/v1/` ön eki altında
- Tüm `/api/` endpoint'leri kimlik doğrulama gerektirir (login/register/health hariç)
- `ICollectionEngine` artık `Database` ve `Name` özelliklerini gerektirir
- `IMonitorService` ve `IBackupService` artık somut implementasyonlara sahip

#### Yükseltme Adımları
1. Yapılandırmaya `JwtSecret` ekleyin (`SecurityConfig`)
2. Migration'ları çalıştırın: `MigrationService.RunPendingMigrationsAsync()`
3. API istemcilerini `api/v1/` ön ekine güncelleyin
4. Tüm API isteklerine Bearer token ekleyin

---

### v1.1.0 — İlk Sürüm

- Gömülü NoSQL veritabanı motoru (WAL, LZ4, Bloom filtresi, LRU önbellek)
- ASP.NET Core Kestrel HTTP sunucusu
- JWT + Argon2id kimlik doğrulama
- RBAC yetkilendirme sistemi
- Parçalı yükleme ile dosya depolama
- In-process pub/sub gerçek zamanlı motoru
- DLL tabanlı, sıcak yüklenebilir eklenti sistemi
- CLI (start, stop, status, backup, restore, doctor)
- Windows WPF kurulum sihirbazı (Steps)
- AES-256-GCM şifreleme primitifleri
- Olay veri yolu, yapılandırma modelleri, yardımcı araçlar

---

## 🇬🇧 English

### v1.2.0 — REST API, Authentication, Dashboard & SDK

#### What's New

**REST API Layer**
- Full CRUD API at `api/v1/databases/{db}/collections/{col}/records`
- Query with filtering (12 operators), sorting, pagination
- Aggregate endpoint (sum/avg/min/max/count)
- File upload/download via `api/v1/storage/{bucket}`
- Backup management via `api/v1/backup`
- OpenAPI/Scalar docs at `/scalar/v1`

**Authentication & Authorization**
- `POST /api/v1/auth/register` — user registration with Argon2id hashing
- `POST /api/v1/auth/login` — JWT + refresh token
- `POST /api/v1/auth/validate` / `POST /api/v1/auth/refresh`
- JWT Bearer auth middleware with role-based permission checks
- Roles: `admin` (all), `editor` (read/write), `viewer` (read-only)

**WebSocket & Real-time**
- WebSocket transport at `/ws?channel=X&token=Y`
- Server-Sent Events at `GET /api/v1/events/stream?channel=default`
- Pub/sub event publishing at `POST /api/v1/events/publish`

**Dashboard**
- Admin web UI at `http://localhost:3001` (static HTML/JS dashboard)
- Login, database explorer, record browser, backup management
- Real-time metrics display

**Client SDK**
- `AhirClient` C# SDK with typed methods for all API operations
- NuGet-ready with authentication, CRUD, query, backup, metrics

**CLI**
- Interactive shell mode: `ahir shell`
- Commands: status, databases, use, collections, insert, query, get, delete, count, backup, backups, metrics

**Database Engine**
- `QueryAsync` fully implemented with 12 filter operators
- StorageEngine position tracking fixed
- AES-256-GCM encryption at rest (via `DatabaseConfig.EnableEncryption`)
- Aggregate query support

**Backup & Monitoring**
- `BackupService` — full/incremental backup to `.ahirbak` zip files
- `MonitorService` — real-time CPU/memory/disk/connection metrics
- OpenTelemetry instrumentation (AspNetCore + Runtime)

**Developer Experience**
- 31 unit/integration tests
- BenchmarkDotNet benchmarks
- Dockerfile + docker-compose.yml
- GitHub Actions CI/CD (build, test, publish multi-platform)
- Sample plugins: Logger, Webhook Forwarder, Status Reporter
- `MigrationService` for schema versioning
- `ConfigService` for runtime JSON configuration

#### Breaking Changes
- API routes are now under `api/v1/` prefix
- Authentication is required for all `/api/` endpoints (except login/register/health)
- `ICollectionEngine` now requires `Database` and `Name` properties
- `IMonitorService` and `IBackupService` are now concrete implementations

#### Upgrade Steps
1. Update config: add `JwtSecret` to `SecurityConfig`
2. Run migrations: `MigrationService.RunPendingMigrationsAsync()`
3. Update API clients to use `api/v1/` prefix
4. Add Bearer token to all API requests

---

### v1.1.0 — Initial Release

- Embedded NoSQL database engine (WAL, LZ4, Bloom filter, LRU cache)
- ASP.NET Core Kestrel HTTP server
- JWT + Argon2id authentication
- RBAC permission system
- File storage with chunked upload
- In-process pub/sub realtime engine
- DLL-based plugin system with hot reload
- CLI (start, stop, status, backup, restore, doctor)
- Windows WPF installer (Steps)
- AES-256-GCM encryption primitives
- Event bus, configuration models, utilities

---

## 🇩🇪 Deutsch

### v1.2.0 — REST-API, Authentifizierung, Dashboard & SDK

#### Was ist neu?

**REST-API-Ebene**
- Vollständige CRUD-API unter `api/v1/databases/{db}/collections/{col}/records`
- Filterung (12 Operatoren), Sortierung, Paginierung
- Aggregat-Endpunkt (Summe/Durchschnitt/Min/Max/Anzahl)
- Datei-Upload/Download über `api/v1/storage/{bucket}`
- Backup-Verwaltung über `api/v1/backup`
- OpenAPI/Scalar-Dokumentation unter `/scalar/v1`

**Authentifizierung & Autorisierung**
- Benutzerregistrierung: `POST /api/v1/auth/register`
- Anmeldung: `POST /api/v1/auth/login` — JWT + Refresh-Token
- JWT-Auth-Middleware mit rollenbasierter Berechtigungsprüfung
- Rollen: `admin` (alle), `editor` (lesen/schreiben), `viewer` (schreibgeschützt)

**WebSocket & Echtzeit**
- WebSocket-Transport unter `/ws?channel=X&token=Y`
- SSE: `GET /api/v1/events/stream?channel=default`
- Ereignisveröffentlichung: `POST /api/v1/events/publish`

**Dashboard**
- Webbasiertes Admin-Panel (`http://localhost:3001`)
- Anmeldung, Datenbank-Explorer, Datensatz-Viewer, Backup-Verwaltung

**SDK**
- `AhirClient` C# SDK mit typisierten Methoden für alle API-Operationen

**CLI**
- Interaktiver Shell-Modus: `ahir shell`
- Befehle: status, databases, use, collections, insert, query, get, delete, count, backup, metrics

**Datenbank-Engine**
- `QueryAsync` vollständig implementiert mit 12 Filteroperatoren
- AES-256-GCM-Verschlüsselung im Ruhezustand
- Aggregat-Abfrageunterstützung
- StorageEngine-Positionsverfolgung korrigiert

**Backup & Überwachung**
- `BackupService` — vollständige Sicherung/Wiederherstellung als `.ahirbak`-ZIP
- `MonitorService` — Echtzeit-CPU/Arbeitsspeicher/Disk/Verbindungsmetriken
- OpenTelemetry-Instrumentierung

**Entwicklererfahrung**
- 31 Unit-/Integrationstests
- BenchmarkDotNet-Leistungstests
- Dockerfile + docker-compose.yml
- GitHub Actions CI/CD (Build, Test, Multi-Plattform-Veröffentlichung)
- Beispiel-Plugins: Logger, Webhook, Status
- `MigrationService` für Schema-Versionierung
- `ConfigService` für Runtime-JSON-Konfiguration

#### Breaking Changes
- API-Routen sind jetzt unter `api/v1/`-Präfix
- Alle `/api/`-Endpunkte erfordern Authentifizierung (außer login/register/health)
- `ICollectionEngine` erfordert jetzt `Database`- und `Name`-Eigenschaften
- `IMonitorService` und `IBackupService` haben jetzt konkrete Implementierungen

#### Upgrade-Schritte
1. `JwtSecret` zur Konfiguration hinzufügen (`SecurityConfig`)
2. Migrationen ausführen: `MigrationService.RunPendingMigrationsAsync()`
3. API-Clients auf `api/v1/`-Präfix aktualisieren
4. Bearer-Token zu allen API-Anfragen hinzufügen

---

### v1.1.0 — Erste Veröffentlichung

- Eingebettete NoSQL-Datenbank-Engine (WAL, LZ4, Bloom-Filter, LRU-Cache)
- ASP.NET Core Kestrel HTTP-Server
- JWT + Argon2id-Authentifizierung
- RBAC-Berechtigungssystem
- Dateispeicherung mit Chunk-Upload
- In-Prozess-Pub/Sub-Echtzeit-Engine
- DLL-basiertes Plugin-System mit heißem Nachladen
- CLI (Start, Stopp, Status, Backup, Wiederherstellung, Diagnose)
- Windows-WPF-Installationsassistent (Steps)
- AES-256-GCM-Verschlüsselungsprimitiven
- Ereignisbus, Konfigurationsmodelle, Hilfsprogramme
