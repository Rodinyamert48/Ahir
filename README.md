# Ahır

> **下一代后端平台 | Next-Generation Backend Platform | Nächste-Generation Backend-Plattform**

---

## 🇹🇷 Türkçe

**Ahır — Yeni Nesil Backend Platformu ve Gömülü Veritabanı Motoru**

Ahır, HTTP sunucusu, gömülü NoSQL veritabanı motoru, kimlik doğrulama, yetkilendirme, dosya depolama, WebSocket gerçek zamanlı iletişim, eklenti sistemi, CLI ve izlemeyi tek bir platformda birleştiren profesyonel, üretim odaklı bir backend altyapısıdır.

> Ölçeklenebilir. Üretim için tasarlandı. Sıfır harici veritabanı bağımlılığı.

### Neden Ahır?

Modern backend yığınları birden fazla harici servise ihtiyaç duyar:

- PostgreSQL / MongoDB (veri)
- Redis (önbellek)
- Firebase / Supabase (gerçek zamanlı)
- Ayrı kimlik sağlayıcıları
- Üçüncü taraf dosya depolama

Ahır tüm bu yetenekleri **tek bir ikili dosyada** ve **özel gömülü veritabanı motoruyla** sağlayarak bu karmaşıklığı ortadan kaldırır. PostgreSQL, MongoDB, SQLite veya Redis gerektirmez.

### Kimler Kullanmalı?

- **Backend geliştiriciler** — birden fazla veritabanı yönetmeden ölçeklenebilir API'ler kuranlar
- **Startuplar** — tek bir platformla hızlı MVP çıkaranlar
- **Edge computing** — yerel öncelikli veri depolama ve gerçek zamanlı senkronizasyon gerektiren uygulamalar
- **IoT sistemleri** — gömülü kalıcılık gerektiren cihazlar
- **Kurumsal ekipler** — kendi kendine yeten, denetlenebilir veri katmanı isteyenler
- **Eklenti geliştiriciler** — DLL tabanlı eklentilerle platformu genişletenler

### Temel Özellikler

| Özellik | Açıklama |
|---------|----------|
| **Gömülü Veritabanı** | İkili depolama, WAL, LZ4 sıkıştırma, AES-256-GCM şifreleme ile özel NoSQL motoru |
| **REST API** | ASP.NET Core Kestrel ile tam CRUD, arama (12 operatör), filtreleme, sayfalama, aggregate |
| **Kimlik Doğrulama** | JWT, Argon2id parola hash'leme, API anahtarları, yenileme token'ları, kayıt/giriş |
| **Yetkilendirme** | JWT middleware ile rol tabanlı erişim kontrolü (admin/editor/viewer) |
| **Dosya Depolama** | Parçalı yükleme ve tekilleştirme ile yükleme/indirme/stream |
| **WebSocket** | Gerçek zamanlı olaylar, kanal tabanlı pub/sub, varlık sistemi, token doğrulama |
| **SSE** | Server-Sent Events ile gerçek zamanlı olay akışı |
| **Dashboard** | Web bazlı admin paneli (database gezgini, backup yönetimi, metrikler) |
| **SDK** | C# istemci SDK (NuGet yayınına hazır) |
| **Eklenti Sistemi** | DLL tabanlı, sıcak yüklenebilir, sanal alanlı eklentiler (örneklerle) |
| **CLI** | Tam komut satırı yönetimi + interaktif shell modu |
| **Güvenlik** | AES-256-GCM şifreleme, hız sınırlama, IP filtreleme, JWT auth middleware |
| **İzleme** | OpenTelemetry, gerçek zamanlı CPU/memory/disk metrikleri, performans sayaçları |
| **Yedekleme** | Sıkıştırma ve şifreleme ile tam yedekleme/geri yükleme |
| **Migration** | Schema versioning ile veritabanı migrasyon sistemi |
| **OpenAPI** | Scalar ile interaktif API dökümantasyonu |
| **Docker** | Multi-stage Dockerfile + docker-compose desteği |
| **CI/CD** | GitHub Actions ile build, test, multi-platform publish |
| **Kurulum** | Windows WPF kurulum sihirbazı (Steps) |

### Ahır vs Alternatifler

| Özellik | Ahır | Docker + Stack | MongoDB + Express | Firebase | Supabase |
|---------|------|---------------|-------------------|----------|----------|
| **Tek İkili Dosya** | ✅ Tek çalıştırılabilir | ❌ Docker, Compose, çoklu imaj | ❌ Node.js + Mongo | ❌ SDK + bulut | ❌ Sunucu + Postgres |
| **Gömülü Veritabanı** | ✅ Özel NoSQL motoru | ❌ Harici DB konteynırı gerek | ❌ MongoDB arka plan servisi | ❌ Firestore (bulut) | ❌ PostgreSQL |
| **İlk Çalıştırma Süresi** | ⚡ <100ms | 🐢 Konteynır başına 5-30s | 🐢 3-10s | ⚡ Anlık (bulut) | 🐢 5-15s |
| **Bellek Kullanımı** | ~15 MB boşta | ~200 MB+ servis başına | ~100 MB+ | SDK bağımlı | ~80 MB+ |
| **Disk Alanı** | ~8 MB | 500 MB - 2 GB | ~200 MB | Yok (bulut) | ~200 MB |
| **Kurulum Süresi** | ⚡ 1 tık (Steps.exe) | 🐢 15-30 dk yapılandırma | 🐢 10-20 dk | ⚡ Sadece API anahtarı | ⚡ Bulut, 🐢 Kendi sunucu |
| **Gerçek Zamanlı** | ✅ Yerleşik WebSocket | ❌ Ayrı servis gerek | ❌ Socket.IO gerek | ✅ Firestore realtime | ✅ Realtime |
| **Kimlik Doğrulama** | ✅ JWT + Argon2id + RBAC | ❌ Keycloak/Auth0 gerek | ❌ passport.js gerek | ✅ Firebase Auth | ❌ GoTrue/Auth gerek |
| **Dosya Depolama** | ✅ Yerleşik | ❌ MinIO/S3 gerek | ❌ GridFS gerek | ✅ Firebase Storage | ✅ Storage |
| **Eklenti Sistemi** | ✅ DLL sıcak yükleme | ❌ Sidecar konteynırlar | ❌ npm modülleri | ❌ Cloud functions | ❌ Edge functions |
| **CLI** | ✅ Yerel (14 komut) | ❌ docker + docker-compose | ❌ mongo shell | ❌ firebase CLI | ❌ supabase CLI |
| **Yedekleme** | ✅ Yerleşik (artımlı) | ❌ Volume yedeklemesi gerek | ❌ mongodump | ❌ Manuel dışa aktarım | ❌ pg_dump |
| **Windows Servisi** | ✅ Yerel | ❌ Desteklenmez | ❌ Desteklenmez | ❌ Yok | ❌ Sadece Linux |
| **Güvenlik Duvarı** | ✅ Otomatik (netsh) | ❌ Manuel | ❌ Manuel | ❌ Yok | ❌ Manuel |
| **Çevrimdışı/Edge** | ✅ Tam çevrimdışı | ❌ Registry erişimi gerek | ✅ Evet | ❌ Hayır | ⚠️ Sınırlı |
| **Kaynak Verimliliği** | ≈ %100 daha iyi | Temel | ≈ %60 daha iyi | Yok (bulut) | ≈ %50 daha iyi |

### Hızlı Karşılaştırma

**Ahır vs Docker tabanlı yığınlar:** Docker 3-5 ayrı konteynır (veritabanı, önbellek, kimlik, API, dosya depolama) gerektirir. Her birinin kendi işletim sistemi katmanı, ağı ve orkestrasyonu vardır. Ahır tek ~8 MB ikili dosyadır. Docker kurulumu 15-30 dakika sürer; Ahır kurulumu 1 tıktır.

**Ahır vs MongoDB + Express:** MongoDB ayrı bir arka plan servisi (~200 MB) olarak çalışır, kendi güvenlik yapılandırmasını gerektirir ve yerleşik kimlik, dosya depolama ve gerçek zamanlı özelliklerden yoksundur. Ahır tüm bunları daha kompakt bir gömülü motorla sunar.

**Ahır vs Firebase:** Firebase yalnızca buluttur, internet bağlantısı gerektirir, satıcı bağımlılığı yaratır ve çevrimdışı veya uç cihazlarda çalışmaz. Ahır tamamen kendi kendine yeterlidir, çevrimdışı çalışır ve verileri yerel olarak saklar. Firebase bir hizmettir; Ahır sahip olduğunuz bir yazılımdır.

**Ahır vs Supabase:** Supabase mükemmeldir ancak PostgreSQL (harici bağımlılık) gerektirir, Linux üzerinde çalışır ve bir sunucuya ihtiyaç duyar. Ahır Windows üzerinde yerel olarak çalışır, harici veritabanı yoktur ve milisaniyeler içinde başlar. Supabase bulut önceliklidir; Ahır uç önceliklidir.

---

## 🇬🇧 English

**Ahır — Next-Generation Backend Platform with Embedded Database Engine**

Ahır is a professional, production-grade backend platform that combines an HTTP server, embedded NoSQL database engine, authentication, authorization, file storage, WebSocket real-time communication, plugin system, CLI, and monitoring into a single cohesive platform.

> Built for scale. Designed for production. Zero external database dependencies.

### Why Ahır?

Modern backend stacks rely on multiple external services:

- PostgreSQL / MongoDB for data
- Redis for caching
- Firebase / Supabase for real-time
- Separate auth providers
- Third-party file storage

Ahır eliminates this complexity by providing all these capabilities in a **single binary** with a **custom embedded database engine** that doesn't require PostgreSQL, MongoDB, SQLite, or Redis.

### Who Should Use Ahır?

- **Backend developers** building scalable APIs without managing multiple databases
- **Startups** shipping MVPs rapidly with a unified backend platform
- **Edge computing** applications needing local-first data storage with real-time sync
- **IoT systems** requiring embedded persistence
- **Enterprise teams** wanting a self-contained, auditable data layer
- **Plugin developers** extending platform capabilities via DLL-based plugins

### Core Features

| Feature | Description |
|---------|-------------|
| **Embedded Database** | Custom NoSQL engine with binary storage, WAL, LZ4 compression, AES-256-GCM encryption |
| **REST API** | ASP.NET Core Kestrel with full CRUD, search (12 operators), filter, pagination, aggregate |
| **Authentication** | JWT, Argon2id password hashing, API keys, refresh tokens, register/login |
| **Authorization** | JWT middleware with role-based access control (admin/editor/viewer) |
| **File Storage** | Upload/download/stream with chunk upload and deduplication |
| **WebSocket** | Real-time events, channel-based pub/sub, presence system, token auth |
| **SSE** | Server-Sent Events for real-time event streaming |
| **Dashboard** | Web-based admin panel (DB explorer, backup management, metrics) |
| **SDK** | C# client SDK (NuGet-ready) |
| **Plugin System** | DLL-based, hot-reloadable, sandboxed plugins (with samples) |
| **CLI** | Full command-line management + interactive shell mode |
| **Security** | AES-256-GCM encryption, rate limiting, IP filtering, JWT auth middleware |
| **Monitoring** | OpenTelemetry, real-time CPU/memory/disk metrics, perf counters |
| **Backup** | Full backup/restore with compression and encryption |
| **Migration** | Schema versioning with migration system |
| **OpenAPI** | Scalar interactive API documentation |
| **Docker** | Multi-stage Dockerfile + docker-compose support |
| **CI/CD** | GitHub Actions build, test, multi-platform release |
| **Installer** | Windows WPF installer (Steps) with guided setup wizard |

### Ahır vs Alternatives

| Feature | Ahır | Docker + Stack | MongoDB + Express | Firebase | Supabase |
|---------|------|---------------|-------------------|----------|----------|
| **Single Binary** | ✅ One executable | ❌ Requires Docker, Compose, multiple images | ❌ Node.js + Mongo daemon | ❌ SDK + cloud | ❌ Server + Postgres |
| **Embedded DB** | ✅ Custom NoSQL engine | ❌ Needs external DB container | ❌ MongoDB daemon | ❌ Firestore (cloud) | ❌ PostgreSQL |
| **Startup Time** | ⚡ <100ms | 🐢 5-30s per container | 🐢 3-10s | ⚡ Instant (cloud) | 🐢 5-15s |
| **Memory Footprint** | ~15 MB idle | ~200 MB+ per service | ~100 MB+ | SDK-dependent | ~80 MB+ |
| **Disk Footprint** | ~8 MB | 500 MB - 2 GB | ~200 MB | N/A (cloud) | ~200 MB |
| **Setup Time** | ⚡ 1 click (Steps.exe) | 🐢 15-30 min config | 🐢 10-20 min | ⚡ API key only | ⚡ Cloud, 🐢 Self-host |
| **Real-time** | ✅ Built-in WebSocket | ❌ Needs separate service | ❌ Needs Socket.IO | ✅ Firestore realtime | ✅ Realtime |
| **Auth** | ✅ JWT + Argon2id + RBAC | ❌ Needs Keycloak/Auth0 | ❌ Needs passport.js | ✅ Firebase Auth | ❌ Needs GoTrue/Auth |
| **File Storage** | ✅ Built-in | ❌ Needs MinIO/S3 | ❌ Needs GridFS | ✅ Firebase Storage | ✅ Storage |
| **Plugin System** | ✅ DLL hot-reload | ❌ Sidecar containers | ❌ npm modules | ❌ Cloud functions | ❌ Edge functions |
| **CLI** | ✅ Native (14 commands) | ❌ docker + docker-compose | ❌ mongo shell | ❌ firebase CLI | ❌ supabase CLI |
| **Backup** | ✅ Built-in (incremental) | ❌ Needs volume backup | ❌ mongodump | ❌ Manual export | ❌ pg_dump |
| **Windows Service** | ✅ Native | ❌ Not supported | ❌ Not supported | ❌ N/A | ❌ Linux only |
| **Firewall Config** | ✅ Automatic (netsh) | ❌ Manual | ❌ Manual | ❌ N/A | ❌ Manual |
| **Offline/Edge** | ✅ Fully offline | ❌ Needs registry access | ✅ Yes | ❌ No | ⚠️ Limited |
| **Resource Efficiency** | ≈ 100% better | Baseline | ≈ 60% better | N/A (cloud) | ≈ 50% better |

### Quick Comparison

**Ahır vs Docker-based stacks:** Docker requires 3-5 separate containers (database, cache, auth, API, file storage), each with its own OS layer, networking, and orchestration. Ahır is a single ~8 MB binary with zero external dependencies. Docker setup takes 15-30 minutes; Ahır setup is 1 click.

**Ahır vs MongoDB + Express:** MongoDB runs as a separate daemon (~200 MB), requires its own security configuration, and lacks built-in auth, file storage, and real-time. Ahır includes all of these natively with a more compact embedded engine.

**Ahır vs Firebase:** Firebase is cloud-only, requires internet connectivity, vendor lock-in, and doesn't work offline or on edge devices. Ahır is fully self-contained, works offline, and stores data locally with no cloud dependency.

**Ahır vs Supabase:** Supabase requires PostgreSQL (external dependency), runs on Linux, and needs a server. Ahır runs on Windows natively, has no external database, and starts in milliseconds.

---

## 🇩🇪 Deutsch

**Ahır — Die nächste Generation der Backend-Plattform mit integrierter Datenbank-Engine**

Ahır ist eine professionelle, produktionsreife Backend-Plattform, die einen HTTP-Server, eine eingebettete NoSQL-Datenbank-Engine, Authentifizierung, Autorisierung, Dateispeicherung, WebSocket-Echtzeitkommunikation, ein Plugin-System, CLI und Überwachung in einer einzigen Plattform vereint.

> Entwickelt für Skalierbarkeit. Konzipiert für die Produktion. Null externe Datenbankabhängigkeiten.

### Warum Ahır?

Moderne Backend-Stacks sind auf mehrere externe Dienste angewiesen:

- PostgreSQL / MongoDB für Daten
- Redis für Caching
- Firebase / Supabase für Echtzeit
- Separate Authentifizierungsanbieter
- Dateispeicherung von Drittanbietern

Ahır eliminiert diese Komplexität, indem es alle diese Fähigkeiten in einer **einzigen Binärdatei** mit einer **benutzerdefinierten eingebetteten Datenbank-Engine** bereitstellt, die keine externen Datenbanken benötigt.

### Für wen ist Ahır geeignet?

- **Backend-Entwickler**, die skalierbare APIs ohne mehrere Datenbanken erstellen
- **Startups**, die mit einer einheitlichen Plattform schnell MVPs ausliefern
- **Edge-Computing**-Anwendungen mit lokaler Datenspeicherung und Echtzeitsynchronisation
- **IoT-Systeme**, die eingebettete Persistenz benötigen
- **Unternehmensteams**, die eine eigenständige, prüfbare Datenebene wünschen
- **Plugin-Entwickler**, die die Plattform über DLL-basierte Plugins erweitern

### Hauptfunktionen

| Funktion | Beschreibung |
|----------|-------------|
| **Eingebettete Datenbank** | Benutzerdefinierte NoSQL-Engine mit Binärspeicher, WAL, LZ4-Komprimierung, AES-256-GCM |
| **REST-API** | ASP.NET Core Kestrel mit vollem CRUD, Suche (12 Operatoren), Filter, Paginierung, Aggregat |
| **Authentifizierung** | JWT, Argon2id-Passwort-Hashing, API-Schlüssel, Refresh-Tokens, Registrierung/Login |
| **Autorisierung** | JWT Middleware mit rollenbasierter Zugriffskontrolle (admin/editor/viewer) |
| **Dateispeicherung** | Hochladen/Herunterladen/Streamen mit Chunk-Upload und Deduplizierung |
| **WebSocket** | Echtzeit-Ereignisse, kanalbasierter Pub/Sub, Präsenzsystem, Token-Auth |
| **SSE** | Server-Sent Events für Echtzeit-Event-Streaming |
| **Dashboard** | Webbasiertes Admin-Panel (DB-Explorer, Backup-Verwaltung, Metriken) |
| **SDK** | C#-Client-SDK (NuGet-bereit) |
| **Plugin-System** | DLL-basiert, heiße nachladbar, sandboxed Plugins (mit Beispielen) |
| **CLI** | Vollständige Befehlszeilenverwaltung + interaktiver Shell-Modus |
| **Sicherheit** | AES-256-GCM-Verschlüsselung, Ratenbegrenzung, IP-Filterung, JWT-Auth-Middleware |
| **Überwachung** | OpenTelemetry, Echtzeit-CPU/Arbeitsspeicher/Disk-Metriken, Leistungsindikatoren |
| **Backup** | Vollständige Backups/Wiederherstellung mit Komprimierung und Verschlüsselung |
| **Migration** | Schema-Versionierung mit Migrationssystem |
| **OpenAPI** | Scalar interaktive API-Dokumentation |
| **Docker** | Multi-Stage-Dockerfile + docker-compose-Unterstützung |
| **CI/CD** | GitHub Actions Build, Test, Multi-Plattform-Release |
| **Installation** | Windows WPF-Installationsassistent (Steps) |

### Ahır vs Alternativen

| Funktion | Ahır | Docker + Stack | MongoDB + Express | Firebase | Supabase |
|----------|------|---------------|-------------------|----------|----------|
| **Einzelne Binärdatei** | ✅ Eine ausführbare Datei | ❌ Docker, Compose, mehrere Images | ❌ Node.js + Mongo-Daemon | ❌ SDK + Cloud | ❌ Server + Postgres |
| **Eingebettete DB** | ✅ Benutzerdefinierte NoSQL-Engine | ❌ Externer DB-Container nötig | ❌ MongoDB-Daemon | ❌ Firestore (Cloud) | ❌ PostgreSQL |
| **Startzeit** | ⚡ <100ms | 🐢 5-30s pro Container | 🐢 3-10s | ⚡ Sofort (Cloud) | 🐢 5-15s |
| **Speicherverbrauch** | ~15 MB im Leerlauf | ~200 MB+ pro Dienst | ~100 MB+ | SDK-abhängig | ~80 MB+ |
| **Speicherplatz** | ~8 MB | 500 MB - 2 GB | ~200 MB | N/A (Cloud) | ~200 MB |
| **Einrichtungszeit** | ⚡ 1 Klick (Steps.exe) | 🐢 15-30 Min Konfiguration | 🐢 10-20 Min | ⚡ Nur API-Schlüssel | ⚡ Cloud, 🐢 Selbst gehostet |
| **Echtzeit** | ✅ Integriertes WebSocket | ❌ Separater Dienst nötig | ❌ Socket.IO nötig | ✅ Firestore Echtzeit | ✅ Echtzeit |
| **Authentifizierung** | ✅ JWT + Argon2id + RBAC | ❌ Keycloak/Auth0 nötig | ❌ passport.js nötig | ✅ Firebase Auth | ❌ GoTrue/Auth nötig |
| **Dateispeicherung** | ✅ Integriert | ❌ MinIO/S3 nötig | ❌ GridFS nötig | ✅ Firebase Storage | ✅ Storage |
| **Plugin-System** | ✅ DLL-heißes Nachladen | ❌ Sidecar-Container | ❌ npm-Module | ❌ Cloud-Funktionen | ❌ Edge-Funktionen |
| **CLI** | ✅ Nativ (14 Befehle) | ❌ docker + docker-compose | ❌ mongo shell | ❌ firebase CLI | ❌ supabase CLI |
| **Backup** | ✅ Integriert (inkrementell) | ❌ Volume-Backup nötig | ❌ mongodump | ❌ Manueller Export | ❌ pg_dump |
| **Windows-Dienst** | ✅ Nativ | ❌ Nicht unterstützt | ❌ Nicht unterstützt | ❌ N/V | ❌ Nur Linux |
| **Firewall-Konfiguration** | ✅ Automatisch (netsh) | ❌ Manuell | ❌ Manuell | ❌ N/V | ❌ Manuell |
| **Offline/Edge** | ✅ Vollständig offline | ❌ Registry-Zugriff nötig | ✅ Ja | ❌ Nein | ⚠️ Eingeschränkt |
| **Ressourceneffizienz** | ≈ 100% besser | Basislinie | ≈ 60% besser | N/V (Cloud) | ≈ 50% besser |

### Kurzer Vergleich

**Ahır vs Docker-basierte Stacks:** Docker benötigt 3-5 separate Container (Datenbank, Cache, Authentifizierung, API, Dateispeicherung), jeder mit eigener Betriebssystemebene, Netzwerk und Orchestrierung. Ahır ist eine einzelne ~8 MB Binärdatei ohne externe Abhängigkeiten. Die Docker-Einrichtung dauert 15-30 Minuten; die Ahır-Einrichtung ist 1 Klick.

**Ahır vs MongoDB + Express:** MongoDB läuft als separater Daemon (~200 MB), erfordert eine eigene Sicherheitskonfiguration und hat keine integrierte Authentifizierung, Dateispeicherung und Echtzeitfunktionen. Ahır bietet all dies nativ mit einer kompakteren eingebetteten Engine.

**Ahır vs Firebase:** Firebase ist reine Cloud, erfordert Internetverbindung, Vendor-Lock-In und funktioniert nicht offline oder auf Edge-Geräten. Ahır ist vollständig eigenständig, arbeitet offline und speichert Daten lokal ohne Cloud-Abhängigkeit.

**Ahır vs Supabase:** Supabase erfordert PostgreSQL (externe Abhängigkeit), läuft auf Linux und benötigt einen Server. Ahır läuft nativ unter Windows, hat keine externe Datenbank und startet in Millisekunden.

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────┐
│                      Ahir Server                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐  │
│  │ REST API │  │ WebSocket│  │   CLI    │  │Dashboard│  │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬───┘  │
│       │              │              │              │      │
│  ┌────▼──────────────▼──────────────▼──────────────▼───┐ │
│  │                  Core Layer                         │ │
│  │  Auth · RBAC · Rate Limiter · Event Bus · Cache    │ │
│  └────────────────────────┬───────────────────────────┘ │
│                           │                              │
│  ┌────────────────────────▼───────────────────────────┐ │
│  │              Embedded Database Engine               │ │
│  │  Storage · Index · WAL · Bloom Filter · Compaction │ │
│  └────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Language | C# 12 (.NET 9) |
| HTTP Server | ASP.NET Core Kestrel |
| Serialization | System.Text.Json |
| Compression | LZ4 |
| Encryption | AES-256-GCM |
| Password Hashing | Argon2id |
| JWT | System.IdentityModel.Tokens.Jwt |
| Hashing | SHA-512, CRC32 |
| Logging | Serilog |
| Dependency Injection | Microsoft DI |
| Testing | xUnit, BenchmarkDotNet |
| CLI | Native argument parsing |
| Installer | WPF (.NET 9) |

## Project Structure

```
Ahir.sln
├── src/
│   ├── Ahir.Core/              # Core models, interfaces, utilities, constants
│   │   └── Services/           # ConfigService, MigrationService
│   ├── Ahir.Database/          # Embedded NoSQL DB engine
│   │   ├── Storage/            # Binary file storage (LZ4, AES-256-GCM, WAL)
│   │   ├── Index/              # In-memory hash-based indexing
│   │   └── Cache/              # LRU cache + Bloom filter
│   ├── Ahir.Security/          # AES-256-GCM, Argon2id, JWT, RBAC
│   ├── Ahir.Server/            # Kestrel HTTP server, middleware pipeline
│   │   ├── Controllers/        # REST API controllers
│   │   ├── Middleware/         # Auth, rate limit, IP filter, WebSocket
│   │   └── Services/           # BackupService, MonitorService
│   ├── Ahir.Storage/           # File storage, chunk upload, buckets
│   ├── Ahir.Realtime/          # WebSocket, event bus, channel/presence
│   ├── Ahir.Plugin/            # DLL plugin system with hot reload
│   ├── Ahir.Plugin.Samples/    # Sample plugins (Logger, Webhook, Status)
│   ├── Ahir.CLI/               # CLI + interactive shell
│   ├── Ahir.Dashboard/         # Web admin panel (HTML/JS dashboard)
│   ├── Ahir.SDK/               # C# Client SDK
├── benchmarks/
│   └── Ahir.Benchmarks/        # BenchmarkDotNet performance tests
├── tests/
│   └── Ahir.Tests/             # 31 xUnit tests
├── tools/
│   ├── Steps/                  # WPF installer (real setup)
│   └── Demo/Demo_Steps/        # WPF demo (same UI, no install)
├── .github/workflows/          # GitHub Actions CI/CD
├── Dockerfile                  # Multi-stage Docker build
├── docker-compose.yml          # Docker Compose setup
├── PHASES.md                   # Implementation phases
└── UPGRADE.md                  # Version upgrade guide

## 🇹🇷 Hızlı Başlangıç

```bash
# Derleme
dotnet build

# Sunucuyu başlatma
dotnet run --project src/Ahir.CLI -- start

# Interaktif shell
dotnet run --project src/Ahir.CLI -- shell

# API dökümantasyonu (sunucu çalışırken)
# http://localhost:8080/scalar/v1

# Admin paneli
dotnet run --project src/Ahir.Dashboard

# Testleri çalıştırma
dotnet test

# Benchmark çalıştırma
dotnet run -c Release --project benchmarks/Ahir.Benchmarks

# Windows kurulum sihirbazı
tools/Steps/bin/Debug/net9.0-windows/Steps.exe

# Demo (kurulum yapmadan önizleme)
tools/Demo/Demo_Steps/bin/Debug/net9.0-windows/Demo_Steps.exe

# Docker ile çalıştırma
docker compose up --build
```

### API Uç Noktaları

| Metot | Yol | Açıklama |
|--------|------|----------|
| POST | `/api/v1/auth/register` | Kullanıcı kaydı |
| POST | `/api/v1/auth/login` | Giriş (JWT döner) |
| GET | `/api/v1/databases` | Veritabanlarını listele |
| POST | `/api/v1/databases` | Veritabanı oluştur |
| GET | `/api/v1/databases/{name}` | Veritabanı bilgisi |
| DELETE | `/api/v1/databases/{name}` | Veritabanını sil |
| POST | `/api/v1/databases/{db}/collections` | Koleksiyon oluştur |
| GET | `/api/v1/databases/{db}/collections` | Koleksiyonları listele |
| POST | `/api/v1/databases/{db}/collections/{col}/records` | Kayıt ekle |
| GET | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Kayıt getir |
| PUT | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Kayıt güncelle |
| DELETE | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Kayıt sil |
| POST | `/api/v1/databases/{db}/collections/{col}/records/query` | Kayıt sorgula |
| POST | `/api/v1/databases/{db}/collections/{col}/records/aggregate` | Aggregate sorgu |
| GET | `/api/v1/databases/{db}/collections/{col}/count` | Kayıt sayısı |
| POST | `/api/v1/databases/{db}/collections/{col}/indexes` | İndeks oluştur |
| GET | `/ws?channel=X` | WebSocket bağlantısı |
| GET | `/api/v1/events/stream?channel=X` | SSE akışı |
| GET | `/api/v1/backup` | Yedekleri listele |
| POST | `/api/v1/backup` | Yedek oluştur |
| GET | `/api/v1/metrics` | Sistem metrikleri |
| GET | `/scalar/v1` | API dökümantasyonu |

---

## 🇬🇧 Quick Start

```bash
# Build
dotnet build

# Start server via CLI
dotnet run --project src/Ahir.CLI -- start

# Interactive shell
dotnet run --project src/Ahir.CLI -- shell

# API docs (while server is running)
# Open http://localhost:8080/scalar/v1

# Admin dashboard
dotnet run --project src/Ahir.Dashboard

# Run tests
dotnet test

# Run benchmarks
dotnet run -c Release --project benchmarks/Ahir.Benchmarks

# Run installer (Windows)
tools/Steps/bin/Debug/net9.0-windows/Steps.exe

# Run demo (preview installer without installing)
tools/Demo/Demo_Steps/bin/Debug/net9.0-windows/Demo_Steps.exe

# Docker
docker compose up --build
```

### API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/auth/register` | Register a new user |
| POST | `/api/v1/auth/login` | Login (returns JWT) |
| GET | `/api/v1/databases` | List databases |
| POST | `/api/v1/databases` | Create database |
| GET | `/api/v1/databases/{name}` | Get database info |
| DELETE | `/api/v1/databases/{name}` | Drop database |
| POST | `/api/v1/databases/{db}/collections` | Create collection |
| GET | `/api/v1/databases/{db}/collections` | List collections |
| POST | `/api/v1/databases/{db}/collections/{col}/records` | Insert record |
| GET | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Get record |
| PUT | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Update record |
| DELETE | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Delete record |
| POST | `/api/v1/databases/{db}/collections/{col}/records/query` | Query records |
| POST | `/api/v1/databases/{db}/collections/{col}/records/aggregate` | Aggregate records |
| GET | `/api/v1/databases/{db}/collections/{col}/count` | Count records |
| POST | `/api/v1/databases/{db}/collections/{col}/indexes` | Create index |
| GET | `/ws?channel=X` | WebSocket connection |
| GET | `/api/v1/events/stream?channel=X` | SSE stream |
| GET | `/api/v1/backup` | List backups |
| POST | `/api/v1/backup` | Create backup |
| GET | `/api/v1/metrics` | System metrics |
| GET | `/scalar/v1` | API documentation |

---

## 🇩🇪 Kurzanleitung

```bash
# Build
dotnet build

# Server starten
dotnet run --project src/Ahir.CLI -- start

# Interaktive Shell
dotnet run --project src/Ahir.CLI -- shell

# API-Dokumentation (während Server läuft)
# http://localhost:8080/scalar/v1

# Admin-Dashboard
dotnet run --project src/Ahir.Dashboard

# Tests ausführen
dotnet test

# Benchmarks ausführen
dotnet run -c Release --project benchmarks/Ahir.Benchmarks

# Installationsassistent (Windows)
tools/Steps/bin/Debug/net9.0-windows/Steps.exe

# Demo (Vorschau ohne Installation)
tools/Demo/Demo_Steps/bin/Debug/net9.0-windows/Demo_Steps.exe

# Docker
docker compose up --build
```

### API-Endpunkte

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| POST | `/api/v1/auth/register` | Benutzer registrieren |
| POST | `/api/v1/auth/login` | Anmelden (JWT) |
| GET | `/api/v1/databases` | Datenbanken auflisten |
| POST | `/api/v1/databases` | Datenbank erstellen |
| GET | `/api/v1/databases/{name}` | Datenbankinfo abrufen |
| DELETE | `/api/v1/databases/{name}` | Datenbank löschen |
| POST | `/api/v1/databases/{db}/collections` | Sammlung erstellen |
| GET | `/api/v1/databases/{db}/collections` | Sammlungen auflisten |
| POST | `/api/v1/databases/{db}/collections/{col}/records` | Datensatz einfügen |
| GET | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Datensatz abrufen |
| PUT | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Datensatz aktualisieren |
| DELETE | `/api/v1/databases/{db}/collections/{col}/records/{id}` | Datensatz löschen |
| POST | `/api/v1/databases/{db}/collections/{col}/records/query` | Datensätze abfragen |
| POST | `/api/v1/databases/{db}/collections/{col}/records/aggregate` | Aggregatabfrage |
| GET | `/api/v1/databases/{db}/collections/{col}/count` | Datensätze zählen |
| POST | `/api/v1/databases/{db}/collections/{col}/indexes` | Index erstellen |
| GET | `/ws?channel=X` | WebSocket-Verbindung |
| GET | `/api/v1/events/stream?channel=X` | SSE-Stream |
| GET | `/api/v1/backup` | Backups auflisten |
| POST | `/api/v1/backup` | Backup erstellen |
| GET | `/api/v1/metrics` | Systemmetriken |
| GET | `/scalar/v1` | API-Dokumentation |

## License | Lisans | Lizenz

MIT License — see [LICENSE](LICENSE) for details.