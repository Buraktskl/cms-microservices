# CMS Mikroservisler — Senior Backend Case Study

> İki .NET 8 mikroservisinden oluşan, enterprise düzeyde mimari pattern'lar, dayanıklılık stratejileri ve gözlemlenebilirlik pratiklerini sergileyen production odaklı bir İçerik Yönetim Sistemi.
>
> Bu belge yalnızca "ne uygulandı" değil; "ne düşünüldü, ne ertelendi, production'da nasıl olurdu" sorularına da cevap verir.

---

## 📋 Case Study Gereksinim Karşılaştırması

PDF'de belirtilen tüm zorunlu gereksinimler aşağıda karşılanma durumu ile listelenmiştir. **Hiçbir gereksinim eksik bırakılmamıştır.** Bazı maddeler orijinal gereksinimlerin ötesinde geliştirilmiş; bu tercihler mimari gerekçesiyle açıklanmıştır.

### Content Service Gereksinimleri

| PDF Gereksinimi | Durum | Notlar |
|---|---|---|
| İçeriklerin eklenmesi, güncellenmesi, silinmesi ve listelenmesi | ✅ | `POST/PUT/DELETE/GET /api/v1/contents` — CQRS Command/Query Handler'lar |
| İçerik detaylarının görüntülenmesi | ✅ | `GET /api/v1/contents/{id}` — Redis önbellekli |
| İçerik oluşturulurken ilgili kullanıcı bilgisinin doğrulanması | ✅ | `CreateContentCommandHandler` → `IUserServiceClient.ValidateUserAsync()` HTTP çağrısı |
| Servisler arası iletişim ve hata senaryolarının yönetilmesi | ✅ | Polly v8 Pipeline: Timeout (3s) → Retry (3×) → Circuit Breaker; `CorrelationId` takibi |

### User Service Gereksinimleri

| PDF Gereksinimi | Durum | Notlar |
|---|---|---|
| Kullanıcıların eklenmesi, güncellenmesi, silinmesi ve listelenmesi | ✅ | `POST/PUT/DELETE/GET /api/v1/users` |
| Kullanıcı detay bilgilerinin görüntülenmesi | ✅ | `GET /api/v1/users/{id}` — Redis önbellekli |
| Content Service tarafından yapılan kullanıcı doğrulama taleplerine cevap verebilmesi | ✅ | `GET /api/v1/users/{id}` endpoint'i doğrulama için de kullanılır |

### Teknik Gereksinimler

| PDF Gereksinimi | Durum | Notlar |
|---|---|---|
| .NET ile geliştirme | ✅ | .NET 8 LTS |
| Her mikroservis için model, veri erişim katmanı ve iş mantığı | ✅ | Clean Architecture: Domain / Application / Infrastructure / API katmanları |
| PostgreSQL veritabanı | ✅ | Her servis için bağımsız PostgreSQL instance; EF Core 8 Code-First, migration |
| Veritabanı bağlantı yapılandırması | ✅ | `docker-compose.yml` environment variable'ları + `appsettings.json` |
| RESTful API ile servisler arası iletişim | ✅ | `HttpClient` + `IUserServiceClient`; Content → User senkron doğrulama, Outbox → RabbitMQ asenkron silme olayı |
| Swagger dokümantasyonu ve test edilebilir API | ✅ | Swashbuckle + XML dokümantasyon; `https://localhost:5003/swagger`, `https://localhost:5004/swagger` |
| Docker container'larına paketleme | ✅ | Her servis için optimize multi-stage `Dockerfile` |
| Docker Compose ile aynı ağda çalışma | ✅ | `cms-network` bridge network; tüm servisler dahil |
| Birim testler | ✅ | xUnit + Moq + FluentAssertions; Command/Query Handler coverage |

### API Endpoint Route Tasarım Kararı

> **PDF endpoint'leri:** `GET /users`, `POST /contents` vb.
>
> **Uygulanan endpoint'ler:** `GET /api/v1/users`, `POST /api/v1/contents` vb.

PDF, route'ları `/users` ve `/contents` olarak tanımlamıştır. Bu projeде URL tabanlı API Versioning (`/api/v1/`) tercih edilmiştir; bu endüstri standardı bir yaklaşımdır ve geriye dönük uyumluluğu garanti eder. Konuştuğu yer şudur:

- **Kong API Gateway** `strip_path: true` ile `/users` → servis içi `/api/v1/users` dönüşümünü yapar; **dış istemci için route PDF ile birebir aynıdır** (`http://localhost:8000/users`).
- Servise doğrudan erişimde (`https://localhost:5003/swagger`) versiyonlu route kullanılır.
- Bu mimari, ileride v2 endpoint'leri hiçbir istemciyi kırmadan yayına alınabilmesini sağlar.

---

## Efsane / Gösterim Sistemi

| İşaret | Anlamı |
|---|---|
| ✅ **Uygulandı** | Kodda mevcut, çalışır durumda |
| 📋 **Tasarlandı** | Kod yazılmadı; yaklaşım, gerekçe ve uygulama yöntemi belgelendi |
| 💡 **Enterprise Genişletme** | Mevcut mimariyi doğal olarak genişletir |

---

## Neden .NET 8 LTS?

Bu case study'de Swagger UI dokümantasyonu açıkça istenmektedir. .NET 9, Swashbuckle'ı `Microsoft.AspNetCore.OpenApi` ile değiştirdiği için Swagger UI deneyimini bozar. .NET 8 LTS, en stabil Swashbuckle uyumluluğunu sağlar.

---

## Teknoloji Yığını — Tam Tablo

| Kategori | Teknoloji | Durum | Gerekçe |
|---|---|---|---|
| Çalışma Ortamı | .NET 8 LTS | ✅ | Kararlı LTS, Swagger uyumlu |
| Mimari Desen | Clean Architecture | ✅ | Bağımlılık tersine çevirme, katman izolasyonu |
| Komut/Sorgu Ayrımı | CQRS + MediatR | ✅ | Tek sorumluluk, test edilebilirlik |
| ORM | EF Core 8 Code-First | ✅ | Tip güvenli sorgular, migrasyon geçmişi |
| Veritabanı | PostgreSQL 16 (servis başına) | ✅ | Database-per-Service izolasyonu |
| Asenkron Mesajlaşma | RabbitMQ 3.13 | ✅ | Olay güdümlü SAGA koreografisi |
| Güvenilir Teslim | Transactional Outbox | ✅ | At-least-once garantisi |
| Önbellek | Redis 7 | ✅ | Yanıt önbelleği + Idempotency |
| Dağıtık Kilit | Redlock | 📋 | Tek node Redis yeterli; Redlock çoklu node gerektirir |
| Dayanıklılık | Polly v8 Pipeline | ✅ | Timeout → Retry → Circuit Breaker |
| API Gateway | Kong 3.6 | ✅ | Rate limiting, CORS, yönlendirme |
| Container Yönetimi | Portainer | ✅ | Docker container UI, log ve durum izleme |
| Loglama | Serilog → ELK Stack | ✅ | Yapısal log, merkezi arama |
| Dağıtık İzleme | OpenTelemetry → Jaeger | ✅ | Trace görselleştirme, span bazlı analiz |
| Metrikler | Prometheus + Grafana | 📋 | RED metrik, Circuit Breaker durumu |
| Health Check | ASP.NET Health Checks | ✅ | `/health` uç noktası |
| Health Check UI | AspNetCore.HealthChecks.UI | 📋 | Tüm servislerin tek panelde izlenmesi |
| Uyarı Sistemi | Kibana Alerting / ElastAlert | 📋 | Hata eşiği aşıldığında bildirim |
| API Versiyonlama | Asp.Versioning v8 | ✅ | Geriye dönük uyumlu evrim |
| HTTPS | .NET dev-certs (Kestrel) | ✅ | Şifreli taşıma |
| Kimlik Doğrulama | JWT Bearer / Keycloak | 📋 | Kapsam kararı; yaklaşım belgelendi |
| Gizli Bilgi Yönetimi | HashiCorp Vault | 📋 | Prod'da dinamik DB kimlik bilgisi rotasyonu |
| Paket Deposu | BaGet (Private NuGet) | 📋 | SharedContracts paketinin özel dağıtımı |
| DDD Taktikleri | Aggregate, Domain Event | 📋 | Entity'ler zengin; tam Aggregate root eksik |
| Audit Log | Soft-delete + zaman damgası | ✅ (kısmi) | Kim sildi/değiştirdi logu eksik |
| Feature Flag | Microsoft.FeatureManagement | 📋 | Kademeli özellik açma stratejisi |
| Veritabanı Ölçekleme | Read Replica + CQRS | 📋 | Query handler'lar replica'ya bağlanır |
| Orkestrasyon | Docker Compose → Kubernetes | 📋 | HPA, rolling update, self-healing |
| CI/CD Pipeline | GitHub Actions / Jenkins | 📋 | Build → Test → Push → Deploy |
| Blue/Green Deploy | Ingress traffic shift | 📋 | Sıfır downtime dağıtım |
| Birim Testler | xUnit + Moq + FluentAssertions | ✅ | İzole, okunabilir assertion'lar |
| Entegrasyon Testleri | WebApplicationFactory + TestContainers | ✅ | Gerçek DB/Redis, tam HTTP döngüsü |

---

## Hızlı Başlangıç

```powershell
git clone <repo-url>
cd caseStudy
docker compose up --build
```

> İlk kez çalıştırıyorsanız aşağıdaki [Kurulum Adımları](#kurulum-adımları) bölümünü okuyun.

---

## Servis Adresleri ve Giriş Bilgileri

### Erişim Adresleri

| Servis | URL | Açıklama |
|---|---|---|
| **User Service Swagger** | http://localhost:5002/swagger | API test arayüzü |
| **Content Service Swagger** | http://localhost:5001/swagger | API test arayüzü |
| **Kong Gateway** | http://localhost:8000 | Tüm API'lerin tek giriş noktası |
| **Jaeger** | http://localhost:16686 | Dağıtık trace görüntüleyici |
| **RabbitMQ Yönetim** | http://localhost:15673 | Mesaj kuyruğu yönetim paneli |
| **Kibana** | http://localhost:5601 | Log arama ve dashboard |
| **Portainer** | http://localhost:9000 | Container yönetim arayüzü |
| Elasticsearch | http://localhost:9200 | Ham log deposu (JSON API) |

### Giriş Bilgileri

| Servis | Kullanıcı Adı | Şifre | Not |
|---|---|---|---|
| **PostgreSQL** (user_db) | `postgres` | `postgres` | Port: 5432 |
| **PostgreSQL** (content_db) | `postgres` | `postgres` | Port: 5433 |
| **RabbitMQ** | `guest` | `guest` | http://localhost:15673 |
| **Portainer** | *(ilk açılışta belirlenir)* | *(ilk açılışta belirlenir)* | http://localhost:9000 |
| **Kibana** | — | — | Kimlik doğrulama kapalı |
| **Elasticsearch** | — | — | Kimlik doğrulama kapalı |
| **Jaeger** | — | — | Kimlik doğrulama yok |
| **Kong Admin API** | — | — | http://localhost:8001 |

### Kong Üzerinden API Kullanımı

Kong gateway, PDF'de tanımlanan route yapısını (`/users`, `/contents`) karşılar:

```
GET    http://localhost:8000/users
POST   http://localhost:8000/users
GET    http://localhost:8000/users/{id}
PUT    http://localhost:8000/users/{id}
DELETE http://localhost:8000/users/{id}

GET    http://localhost:8000/contents
POST   http://localhost:8000/contents
GET    http://localhost:8000/contents/{id}
PUT    http://localhost:8000/contents/{id}
DELETE http://localhost:8000/contents/{id}
```

Opsiyonel header'lar:
```
Idempotency-Key: <uuid>   # POST/PUT/DELETE tekrar güvenliği
X-Correlation-Id: <uuid>  # Dağıtık izleme için
```

---

## Kurulum Adımları

### Ön Gereksinim

| Gereksinim | Versiyon | İndirme |
|---|---|---|
| Docker Desktop | 4.x+ | [docker.com](https://www.docker.com/products/docker-desktop) |

### Başlat

```powershell
git clone <repo-url>
cd caseStudy
docker compose up --build
```

EF Core migrasyonları ve seed data otomatik çalışır — başka hiçbir kurulum gerekmez.

**Diğer komutlar:**

```powershell
docker compose down          # Servisleri durdur
docker compose down -v       # Durdur + volume'ları sil (sıfırdan başlamak için)
```

> **Not:** HTTPS sertifikası `.NET dev-certs` ile oluşturulur. Tarayıcıda `https://localhost:5003` açıldığında "Güvensiz Bağlantı" uyarısı görünebilir — bu normaldir. Swagger ve API tamamen çalışır durumda olur. Kong üzerinden HTTP erişimi (`http://localhost:8000`) sertifika gerektirmez.

> 11 servis otomatik başlar. EF Core migrasyonları ve seed data başlangıçta çalışır. Manuel DB kurulumu gerekmez.

---

## Mimari

```
                    ┌─────────────────────────────────────────┐
                    │             Kong API Gateway             │
                    │   Rate Limiting · CORS · Yönlendirme    │
                    └──────────┬────────────────┬─────────────┘
                               │                │
                     ┌─────────▼────┐    ┌──────▼──────────┐
                     │  UserService │    │  ContentService  │
                     │  :5002 (H)   │◄───│  :5001 (H)       │
                     │  :5003 (S)   │    │  :5004 (S)       │
                     └────┬────┬───┘    └────┬────┬────────┘
                          │    │             │    │
             ┌────────────┘    └──RabbitMQ──►┘    │
             │  Outbox                             │
             ▼  Dispatcher                         ▼
       PostgreSQL                            PostgreSQL
        (user_db)                           (content_db)

(H) = HTTP — Docker iç ağı   (S) = HTTPS — Dış erişim
```

**İletişim akışları:**

- **Senkron (HTTP + Polly):** `POST /contents` → ContentService, `authorId`'yi UserService'e HTTP ile doğrular. Yavaşlarsa → Timeout. Defalarca başarısız olursa → Circuit Breaker açılır, 503 döner.
- **Asenkron (RabbitMQ + SAGA + Outbox):** `DELETE /users/{id}` → `UserDeleted` olayı OutboxMessages tablosuna atomik yazılır → `OutboxDispatcher` RabbitMQ'ya publish eder → `UserDeletedConsumer` tüm içerikleri soft-delete eder.

---

## ✅ Uygulanan Özellikler (Detaylı)

---

### 1. Clean Architecture

Her servis dört katmanda yapılandırılmıştır. Bağımlılık yönü içten dışa doğrudur:

```
UserService.Domain         → Dış bağımlılık yok; iş kuralları burada
UserService.Application    → Yalnızca Domain'e bağlı; use case'ler, handler'lar
UserService.Infrastructure → Application interface'lerini uygular; DB, mesajlaşma, önbellek
UserService.API            → HTTP katmanı; saf IMediator dispatch, middleware
```

Domain entity'leri (`User`, `Content`) tüm iş kurallarını kapsar. `User.MarkAsPendingDeletion()`, `Content.SoftDelete()` gibi metodlar domain davranışını dışarı sızdırmaz. Anemic model yoktur.

**Ne sağlar:** Her katman bağımsız test edilebilir. Infrastructure değişiklikleri domain'i etkilemez. Takım içinde sorumluluk sınırları netleşir.

---

### 2. CQRS + MediatR

Application katmanı Commands (durum değiştirme) ve Queries (okuma) olarak ayrılmıştır. Controller sıfır iş mantığı içerir.

```
Application/
  Commands/
    CreateUser/     CreateUserCommand.cs + CreateUserCommandHandler.cs
    UpdateUser/     UpdateUserCommand.cs + UpdateUserCommandHandler.cs
    DeleteUser/     DeleteUserCommand.cs + DeleteUserCommandHandler.cs  ← Outbox ile
  Queries/
    GetAllUsers/    GetAllUsersQuery.cs  + GetAllUsersQueryHandler.cs   ← Redis ile
    GetUserById/    GetUserByIdQuery.cs  + GetUserByIdQueryHandler.cs   ← Redis ile
```

```csharp
// Controller — sıfır iş mantığı
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
{
    var result = await mediator.Send(new CreateUserCommand(request.Username, request.Email, request.FullName), ct);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

**Ne sağlar:** Her handler tek sorumludur. MediatR pipeline behavior'ları ile validation, performans loglaması, authorization handler'lara dokunmadan eklenebilir. Query handler'lar ilerleyen zamanlarda read replica'ya bağlanabilir.

---

### 3. Transactional Outbox Pattern

**Sorun:** `SaveChanges()` ardından `PublishEvent()` çağrısı, arasında process crash olursa DB güncellenir ama event kaybolur — ContentService hiç bilgilendirilmez.

**Çözüm:** Event verisi, iş durumu güncellemesiyle **aynı DB transaction'ında** `OutboxMessages` tablosuna yazılır.

```csharp
// DeleteUserCommandHandler — iki yazma TEK transaction
user.MarkAsPendingDeletion();
await repository.UpdateAsync(user, ct);
await outbox.AddAsync(OutboxMessage.Create("UserDeleted", payload), ct);
await repository.SaveChangesAsync(ct);  // ← her ikisi atomik
```

`OutboxDispatcher` (BackgroundService) 5 saniyede bir çalışır, işlenmemiş mesajları okur, RabbitMQ'ya publish eder, işlenmiş olarak işaretler. 5 başarısız denemeden sonra dead-letter senaryosu.

**Ne sağlar:** Broker arızasında mesaj kaybolmaz, birikir, kurtarıldığında gönderilir. 2PC (distributed transaction) gerektirmez. At-least-once teslim garantisi.

---

### 4. SAGA Koreografisi (Kullanıcı Silme Akışı)

```
1. DELETE /api/v1/users/{id}
2. User.Status → PendingDeletion (DB güncellendi)
3. OutboxMessage("UserDeleted") atomik yazıldı
4. OutboxDispatcher → RabbitMQ exchange: cms.events
5. UserDeletedConsumer → queue: user.deleted.content-service
6. authorId'ye ait tüm içerikler → SoftDelete
7. User.Status → Deleted
```

Merkezi orkestratör yoktur. Her servis olaylara bağımsız tepki verir. Herhangi bir adım başarısız olursa Outbox yeniden dener.

---

### 5. Polly v8 — Dayanıklılık Pipeline'ı

ContentService, UserService'i üç katmanlı Polly pipeline'ı üzerinden çağırır:

```
İstek → [Timeout: 3sn] → [Retry: 3x, üstel backoff + jitter] → [Circuit Breaker] → UserService
```

| Strateji | Yapılandırma | Davranış |
|---|---|---|
| Timeout | 3 saniye | UserService yanıt vermezse hızlı başarısız ol |
| Retry | 3 deneme, 500ms üstel, jitter | Geçici ağ hatalarını ele alır |
| Circuit Breaker | %50 hata, 5 dk örnekleme, 30sn açık | Kaskad hata önler, sisteme nefes aldırır |

Devre açıkken `503 Service Unavailable` anında döner, çağrı denenmez. **CAP Theorem konumlandırmasıyla tutarlı:** Bölünme altında tutarlılık feda edilmez, availability korunur.

---

### 6. Redis Önbelleği

GET uç noktaları Redis'te 5 dakika TTL ile önbelleğe alınır. Yazma işlemleri ilgili anahtarları invalidate eder:

| İşlem | Önbellek Davranışı |
|---|---|
| `GET /users` | `users:list`'ten oku veya doldur + sakla |
| `GET /users/{id}` | `user:{id}`'den oku veya doldur + sakla |
| `POST /users` | `users:list` invalidate |
| `PUT /users/{id}` | `users:list` + `user:{id}` invalidate |
| `DELETE /users/{id}` | `users:list` + `user:{id}` invalidate |

**Ne sağlar:** Veritabanı yükü azalır. Yanıt süreleri düşer. Yüksek okuma trafiğinde PostgreSQL darboğazı oluşmaz.

---

### 7. Idempotency Key Middleware

Tüm değiştirici uç noktalar `Idempotency-Key` başlığını destekler. İlk istekte yanıt Redis'e (24 saat TTL) kaydedilir. Aynı key ile gelen mükerrer istekler, handler yeniden çalıştırılmadan önbellekten döner.

```http
POST https://localhost:5003/api/v1/users
Idempotency-Key: f47ac10b-58cc-4372-a567-0e02b2c3d479
Content-Type: application/json
```

**Ne sağlar:** Ağ yeniden denemesi veya istemci hatası durumunda çift kayıt oluşmaz. Ödeme gibi kritik işlemlerde zorunludur.

---

### 8. Serilog → ELK Stack

Tüm loglar şu zenginleştirmelerle JSON formatında Elasticsearch'e gönderilir:
- `Service` (UserService / ContentService)
- `Environment` (Production)
- `MachineName` (container hostname)
- `CorrelationId` (`X-Correlation-Id` başlığından)

Kibana'da **http://localhost:5601** adresinden aranabilir. İndeks deseni: `cms-logs-YYYY.MM`

**Ne sağlar:** Yapısal loglar sayesinde "bu correlationId ile ilgili tüm logları getir" veya "son 1 saatte 500 dönen tüm istekler" gibi sorgular saniyeler içinde yapılabilir.

---

### 9. OpenTelemetry → Jaeger

Her iki servis OTLP protokolü üzerinden Jaeger'a span gönderir. **http://localhost:16686** adresinden izleme yapılabilir.

Görünen span'lar:
- ASP.NET Core istek yaşam döngüsü
- ContentService → UserService HTTP çağrıları (Polly retry span'ları dahil)
- Circuit Breaker durumu

W3C `traceparent` başlığı otomatik propagate edilir; manuel context aktarımı gerekmez.

**Ne sağlar:** "Bu istek neden 2 saniye sürdü? Hangi servis yavaşladı?" sorularına görsel olarak cevap verir. ELK (log) + Jaeger (trace) birlikte tam gözlemlenebilirlik üçgeninin 2/3'ünü tamamlar.

---

### 10. Kong API Gateway

Tüm dış trafik Kong üzerinden geçer:
- **Rate limiting:** IP başına dakikada 100 istek
- **CORS:** Tarayıcı kaynaklı istekler için başlık yönetimi
- **Yönlendirme:** `/users` → UserService, `/contents` → ContentService

**Ne sağlar:** Servisler dış dünyadan izole edilir. Cross-cutting concern'ler (rate limit, auth, CORS) tek noktadan yönetilir. Servis eklenmesi veya kaldırılması gateway config değişikliğidir; istemci değişmez.

---

### 11. Portainer ✅

**http://localhost:9000** adresinden Docker container yönetim arayüzü.

**Ne sağlar:** Terminal açmadan container loglarını görme, durdurup başlatma, kaynak kullanımını (CPU/RAM) izleme. DevOps bilgisi olmayan takım üyeleri için kritiktir.

---

### 12. API Versiyonlama

Tüm rotalar `/api/v1/` altında. `Asp.Versioning v8` ile resmi versiyonlama yapısı:

```
GET /api/v1/users    → UsersController v1
GET /api/v2/users    → (gelecekte) UsersController v2, v1 çalışmaya devam eder
```

**Ne sağlar:** Mevcut istemcileri bozmadan API evrimi. B2B entegrasyonlarda zorunludur.

---

### 13. HTTPS

- `HTTP :5001/:5002` — Docker iç ağı (servisler arası)
- `HTTPS :5003/:5004` — Dış erişim (tarayıcı, Swagger)

`dotnet dev-certs` ile oluşturulan sertifika container'a mount edilir. `UseHttpsRedirection()` HTTP'yi HTTPS'e yönlendirir.

---

### 14. Database-per-Service

| Servis | Veritabanı | Container | Port |
|---|---|---|---|
| UserService | `user_db` | `postgres-user` | 5432 |
| ContentService | `content_db` | `postgres-content` | 5433 |

Servisler birbirinin tablolarını paylaşmaz, doğrudan sorgulamaz. Cross-service veri ihtiyacı yalnızca API çağrısı (senkron) veya domain event (asenkron) ile karşılanır.

**Neden iki container, tek container da kullanılabilirdi:**

| Yaklaşım | İzolasyon | Production Uygunluğu |
|---|---|---|
| İki ayrı container *(bu proje)* | Tam — ayrı process, connection pool, WAL | ✅ Üretim topolojisini yansıtır |
| Tek container, iki veritabanı | Güçlü — ayrı şema, credential | ✅ Kabul edilebilir |
| Tek container, ayrı şema | Zayıf — paylaşılan process | ⚠️ Şema kayması riski |

---

### 15. Seed Data

Uygulama ilk başladığında veritabanı boşsa 5 kullanıcı ve 8 içerik otomatik eklenir. `IgnoreQueryFilters().AnyAsync()` kontrolüyle idempotent çalışır; her yeniden başlatmada duplicate olmaz.

| Kullanıcı | E-posta |
|---|---|
| alice | alice@cms.dev |
| bob | bob@cms.dev |
| carol | carol@cms.dev |
| dave | dave@cms.dev |
| eve | eve@cms.dev |

---

### 16. Entegrasyon Testleri (TestContainers)

`WebApplicationFactory` + gerçek PostgreSQL ve Redis container'ları ile tam HTTP döngüsü testi.

```csharp
// Gerçek PostgreSQL container başlatılır
var _postgres = new PostgreSqlBuilder("postgres:16-alpine")
    .WithDatabase("user_test_db").Build();

// Uygulama gerçek DB'ye bağlanır
_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
    host.ConfigureServices(services =>
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(_postgres.GetConnectionString()))));
```

Test senaryoları:
1. Kullanıcı oluşturma → 201
2. ID ile getirme → 200
3. Mükerrer username → 409
4. Olmayan ID → 404
5. Aynı Idempotency-Key ile iki istek → aynı yanıt

---

## 📋 Tasarlanan Özellikler (Uygulanmadı, Gerekçesiyle)

---

### Redlock — Dağıtık Kilitleme

**Nedir:** Birden fazla Redis node'unda güvenli dağıtık kilit sağlar. Tek node Redis'te race condition olmaz; Redlock'un değeri multi-node cluster'da ortaya çıkar.

**Bu case study'de neden yazılmadı:** Tek Redis container kullanılıyor. Redlock, en az 3 bağımsız Redis instance gerektirir. Tek node'da kullanmak gereksiz karmaşıklık ekler, ek güvenlik sağlamaz.

**Ne zaman gerekir:** Stok güncelleme, rezervasyon, tek seferlik kupon kullanımı gibi "aynı anda sadece bir process çalışmalı" senaryolarında.

**Production uygulaması:**
```csharp
// StackExchange.Redis.Extensions.RedisLock
var lockKey = $"lock:user:{userId}";
await using var @lock = await redisLockFactory.CreateLockAsync(lockKey, TimeSpan.FromSeconds(30));
if (!@lock.IsAcquired) throw new ConcurrentModificationException();
// kritik işlem burada
```

---

### HashiCorp Vault — Gizli Bilgi Yönetimi

**Nedir:** Şifreleri, API anahtarlarını, DB kimlik bilgilerini güvenli depolar ve otomatik döndürür.

**Bu case study'de neden yazılmadı:** `.env` dosyası gitignore'da tutulur; gerçek bir sır açığa çıkmaz. Vault kurulumu (Docker + init + unseal + policy) case study odağını infrastructure yönetimine kaydırır.

**Ne sağlar:**
- DB şifresi her 24 saatte otomatik döner — eski şifre otomatik geçersiz olur
- Şifreler env variable değil, dosya olarak mount edilir (`/vault/secrets/db-creds`)
- Tüm erişim denetlenir ve loglanır: "kim hangi secret'a ne zaman eriştı"

**Production uygulaması:**
```csharp
// Vault Agent Sidecar tarafından inject edilen dosyadan oku
builder.Configuration.AddKeyPerFile("/vault/secrets/", optional: false);
// appsettings.json'daki connection string artık kullanılmaz
```

```yaml
# docker-compose (production: K8s annotation ile)
annotations:
  vault.hashicorp.com/agent-inject: "true"
  vault.hashicorp.com/agent-inject-secret-db-creds: "database/creds/user-service"
```

---

### BaGet — Private NuGet Sunucusu

**Nedir:** Organizasyon içi NuGet paketlerini barındıran hafif, self-hosted NuGet server.

**Bu case study'de neden yazılmadı:** `SharedContracts` projesi solution içinde proje referansıyla kullanılıyor. BaGet, birden fazla solution veya takımın aynı paketi kullandığı durumda anlam kazanır.

**Ne sağlar:**
- `SharedContracts` (UserDeletedEvent, QueueNames) paket olarak versiyonlanır
- ContentService, `<PackageReference Include="CmsSystem.SharedContracts" Version="1.2.0" />` ile bağlanır
- Her servis farklı versiyonda çalışabilir; breaking change yönetimi kolaylaşır
- CI/CD pipeline'ı yeni event eklendiğinde otomatik paket publish eder

**Production uygulaması:**
```bash
# BaGet Docker ile 5 dakikada kurulur
docker run -d -p 5555:8080 loicsharma/baget

# SharedContracts paketi publish
dotnet pack -c Release
dotnet nuget push *.nupkg --source http://localhost:5555/v3/index.json
```

```xml
<!-- ContentService.Infrastructure.csproj -->
<PackageReference Include="CmsSystem.SharedContracts" Version="1.0.0" />
```

---

### Prometheus + Grafana — Metrik İzleme

**Nedir:** Prometheus, servislerden metrik toplar; Grafana dashboard'larla görselleştirir.

**Bu case study'de neden yazılmadı:** Log (Serilog+ELK) ve trace (OpenTelemetry+Jaeger) uygulandı. Üç gözlemlenebilirlik sinyalinden ikisi mevcut. Prometheus eklemek `docker-compose.yml`'ye 2 servis, her servise `prometheus-net` paketi ve dashboard JSON'ları ekler — bunlar case study kapsamını aşar.

**Ne sağlar:**
- **RED metrikleri:** Request rate (istek/sn), Error rate (hata oranı), Duration (gecikme)
- Circuit Breaker durumu (açık/kapalı/yarı-açık) gerçek zamanlı
- Outbox queue derinliği — "kaç mesaj beklemede?"
- RabbitMQ kuyruk uzunluğu ve consumer lag

**Production uygulaması:**
```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

app.MapPrometheusScrapingEndpoint(); // → /metrics
```

```yaml
# docker-compose eklemeleri
prometheus:
  image: prom/prometheus:v2.51.0
  volumes: [./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml]
  ports: ["9090:9090"]

grafana:
  image: grafana/grafana:10.4.0
  ports: ["3000:3000"]
```

---

### Health Check UI

**Nedir:** `AspNetCore.HealthChecks.UI` paketi, tüm servislerin sağlık durumunu tek bir web arayüzünde gösterir.

**Bu case study'de neden yazılmadı:** Her servisin `/health` uç noktası mevcuttur. UI paketi yalnızca görselleştirme ekler; production değeri gerçek ortamda belirginleşir.

**Ne sağlar:** Tüm servisler, bağımlılıklar (DB, Redis, RabbitMQ) tek ekranda yeşil/kırmızı. Üst yönetime gösterilecek operasyonel dashboard için hızlıca kurulabilir.

**Production uygulaması:**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgsql(connStr, name: "postgresql")
    .AddRedis(redisConn, name: "redis")
    .AddRabbitMQ(rabbitConn, name: "rabbitmq");

builder.Services.AddHealthChecksUI()
    .AddInMemoryStorage();

app.MapHealthChecksUI(options => options.UIPath = "/health-ui");
```

---

### Kibana Alerting / ElastAlert

**Nedir:** Elasticsearch üzerindeki log verilerine göre koşul tabanlı uyarı sistemi.

**Bu case study'de neden yazılmadı:** Alerting kural tanımları ve bildirim entegrasyonları (Slack, e-posta, PagerDuty) çalışan bir prod ortamına ihtiyaç duyar.

**Ne sağlar:**
- "Son 5 dakikada 10'dan fazla 500 hatası → Slack kanalına bildirim"
- "Outbox'ta 50'den fazla işlenmemiş mesaj → alarm"
- "Circuit Breaker 2 dakikadır açık → on-call tetikle"

**Production uygulaması (Kibana Stack Management → Rules):**
```json
{
  "rule_type_id": ".es-query",
  "params": {
    "index": ["cms-logs-*"],
    "esQuery": { "query": { "match": { "level": "Error" } } },
    "threshold": [{ "comparator": ">", "value": 10 }],
    "timeWindowSize": 5,
    "timeWindowUnit": "m"
  }
}
```

---

### Read Replica + CQRS Entegrasyonu

**Nedir:** PostgreSQL streaming replication ile primary DB yazma için, read replica okuma için kullanılır.

**Bu case study'de neden yazılmadı:** Tek PostgreSQL instance yeterli; replica kurulumu Docker Compose'u karmaşıklaştırır.

**Ne sağlar:** Ağır okuma trafiği primary'yi etkilemez. Raporlama sorguları replica'ya gider, yazma operasyonları yavaşlamaz.

**Production uygulaması:** CQRS mimarisi bu geçişi kolaylaştırır. Query handler'larda connection string değişir:

```csharp
// GetAllUsersQueryHandler
public class GetAllUsersQueryHandler(
    IUserRepository repository,      // ← primary DB
    IReadOnlyUserRepository readRepo, // ← read replica
    ICacheService cache) ...
```

```csharp
// DI kaydı
services.AddDbContext<ReadOnlyAppDbContext>(o =>
    o.UseNpgsql(config["ConnectionStrings:UserDbReplica"])
     .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
```

---

### Kubernetes + Helm — Container Orkestrasyonu

**Bu case study'de neden yazılmadı:** K8s kurulumu, YAML manifests ve Helm chart'ları uygulama mimarisini değil altyapıyı gösterir. Docker Compose ile tüm servisler yerel olarak çalışır ve mimari doğrulanabilir.

**Ne sağlar:**
- **HPA:** CPU %70 üstünde pod sayısı otomatik artar
- **Rolling Update:** Yeni sürüm eski sürüm kapanmadan devreye girer
- **Self-healing:** Crash eden pod otomatik yeniden başlar
- **Resource limit:** Her pod CPU/RAM sınırı ile izole

**Production yapısı:**
```
helm/
├── user-service/
│   ├── Chart.yaml
│   ├── values.yaml          # replicas: 3, cpu: 500m, memory: 256Mi
│   └── templates/
│       ├── deployment.yaml
│       ├── service.yaml
│       └── hpa.yaml         # minReplicas: 2, maxReplicas: 10
├── content-service/
└── infrastructure/          # PostgreSQL, Redis, RabbitMQ — Bitnami chart'ları
```

---

### CI/CD Pipeline — GitHub Actions / Jenkins

**Bu case study'de neden yazılmadı:** Pipeline, çalışan bir repository ve deployment hedefi gerektirir.

**Ne sağlar:** Her PR'da otomatik build + test. Her merge'de otomatik deploy. "Çalışıyor mu?" sorusu ortadan kalkar.

**Production pipeline:**
```
PR açıldı
  → dotnet build
  → dotnet test (unit)
  → dotnet test --filter Integration (TestContainers)
  → docker build + push → Container Registry

main'e merge
  → helm upgrade --install (staging)
  → smoke test: GET /health → 200?
  → helm upgrade --install (production)
  → Blue/Green traffic shift
```

**Jenkins alternatifi** (kurumsal tercih):
```groovy
pipeline {
    stages {
        stage('Build')  { steps { sh 'dotnet build' } }
        stage('Test')   { steps { sh 'dotnet test' } }
        stage('Docker') { steps { sh 'docker build && docker push' } }
        stage('Deploy') { steps { sh 'helm upgrade --install' } }
    }
}
```

---

### Blue/Green Deployment — Sıfır Downtime

**Bu case study'de neden yazılmadı:** K8s gerektirir.

**Ne sağlar:**
- Yeni sürüm `green` ortamına deploy edilir, smoke test yapılır
- Trafik `blue`'dan `green`'e Ingress annotation ile kaydırılır (0sn downtime)
- Sorun çıkarsa `blue`'ya anlık geri dönüş

```yaml
# Kong/Nginx Ingress traffic split
annotations:
  nginx.ingress.kubernetes.io/canary: "true"
  nginx.ingress.kubernetes.io/canary-weight: "100"  # %100 green'e
```

**EF Core migrations için:** Startup'taki `MigrateAsync()` yerine ayrı bir K8s Job/initContainer çalışır; migration tamamlanmadan pod başlamaz.

---

### Feature Flag — Microsoft.FeatureManagement

**Nedir:** Yeni özellikleri belirli kullanıcılara veya yüzdelik dilimlere açan geçiş mekanizması.

**Bu case study'de neden yazılmadı:** Tek ortam; kademeli açma ihtiyacı yok.

**Ne sağlar:** "Yeni içerik editörünü önce iç ekibe aç, hatasız çalışınca herkese aç." Kod deployment'tan bağımsız özellik açma.

**Production uygulaması:**
```csharp
builder.Services.AddFeatureManagement()
    .AddFeatureFilter<PercentageFilter>();  // %10 kullanıcıya aç

// Controller'da
if (await featureManager.IsEnabledAsync("NewContentEditor"))
    return Ok(await mediator.Send(new GetContentsV2Query()));
```

```json
// appsettings.json veya Azure App Configuration
"FeatureManagement": {
  "NewContentEditor": {
    "EnabledFor": [{ "Name": "Percentage", "Parameters": { "Value": 10 } }]
  }
}
```

---

### DDD — Domain-Driven Design Taktikleri

**Mevcut durum:** Entity'ler zengin davranışa sahip (`User.MarkAsPendingDeletion()`, `Content.SoftDelete()`). Bu DDD'nin temel taktikleri olan Rich Domain Model'ı karşılar.

**Eksik olan:**
- **Aggregate Root:** `User`, ilişkili `Address` veya `Profile` entity'lerini koruyan aggregate root değil
- **Domain Event:** Entity içinde raise edilen event'ler; `User.MarkAsPendingDeletion()` domain event fırlatmalı
- **Value Object:** `Email` string yerine `Email` value object olmalı (doğrulama, eşitlik semantiği)

**Production uygulaması:**
```csharp
// Domain Event
public class UserMarkedForDeletionEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Username { get; }
}

// User entity'de
public void MarkAsPendingDeletion()
{
    Status = UserStatus.PendingDeletion;
    AddDomainEvent(new UserMarkedForDeletionEvent(Id, Username));
}

// MediatR handler domain event'i yakalar, Outbox'a yazar
```

---

### Audit Log

**Mevcut durum:** `CreatedAt`, `UpdatedAt` alanları her entity'de mevcut. Soft-delete (`Status = Deleted`) tutulur.

**Eksik olan:** "Kim, ne zaman, ne değiştirdi?" izlenemiyor. Eski değer saklanmıyor.

**Ne sağlar:** KVKK/GDPR uyumu, güvenlik denetimi, destek süreçlerinde "kullanıcı ne zaman ne yaptı?" sorusu cevaplanabilir.

**Production uygulaması (EF Core Interceptor):**
```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted);

        foreach (var entry in entries)
        {
            auditRepo.Add(new AuditLog
            {
                EntityName  = entry.Entity.GetType().Name,
                EntityId    = (Guid)entry.Property("Id").CurrentValue!,
                Action      = entry.State.ToString(),
                OldValues   = JsonSerializer.Serialize(entry.OriginalValues.ToObject()),
                NewValues   = JsonSerializer.Serialize(entry.CurrentValues.ToObject()),
                ChangedBy   = httpContextAccessor.HttpContext?.User?.Identity?.Name,
                ChangedAt   = DateTime.UtcNow
            });
        }
        return await base.SavingChangesAsync(result, context, cancellationToken);
    }
}
```

---

### CAP Teoremi Konumlandırması

**Bu case study'nin pozisyonu: PA/EL (Partition-tolerant + Available / Eventually consistent)**

CAP teoremi: Dağıtık bir sistem, bölünme (P) altında **ya tutarlılık (C) ya erişilebilirlik (A)** seçebilir.

Bu sistemde bilinçli seçim: **Erişilebilirlik** tercih edilir.

**Kanıtlar:**
1. RabbitMQ broker geçici olarak kapansa bile kullanıcı silme isteği başarıyla döner (Outbox bu durumda biriktirir). ContentService anlık olarak eski veriyi görebilir — bu **nihai tutarlılık**tır.
2. Circuit Breaker açıkken ContentService, UserService'e ulaşamasa da mevcut içerikleri okumaya devam eder.
3. Redis önbelleği TTL dolmadan stale data döndürebilir.

**PACELC genişletmesi:** Bölünme olmadığında da bu sistem **Gecikme (L)** yerine **Tutarlılık (C)** tercih etmez — yani EL'dir: düşük gecikme, nihai tutarlılık.

---

## API Referansı

### User Service — `https://localhost:5003/api/v1/users`

| Metot | Uç Nokta | Açıklama | Başarı Kodları |
|---|---|---|---|
| GET | `/api/v1/users` | Tüm kullanıcıları listele (önbellekli) | 200 |
| GET | `/api/v1/users/{id}` | ID ile kullanıcı getir (önbellekli) | 200 / 404 |
| POST | `/api/v1/users` | Kullanıcı oluştur | 201 / 409 / 422 |
| PUT | `/api/v1/users/{id}` | Kullanıcı güncelle | 200 / 404 / 409 |
| DELETE | `/api/v1/users/{id}` | Kullanıcı sil + SAGA tetikle | 204 / 404 |

### Content Service — `https://localhost:5004/api/v1/contents`

| Metot | Uç Nokta | Açıklama | Başarı Kodları |
|---|---|---|---|
| GET | `/api/v1/contents` | Tüm içerikleri listele (önbellekli) | 200 |
| GET | `/api/v1/contents/{id}` | ID ile içerik getir (önbellekli) | 200 / 404 |
| POST | `/api/v1/contents` | İçerik oluştur (yazar doğrulaması) | 201 / 422 / 503 |
| PUT | `/api/v1/contents/{id}` | İçerik güncelle | 200 / 404 |
| DELETE | `/api/v1/contents/{id}` | İçeriği soft-delete et | 204 / 404 |

### İstek Başlıkları

| Başlık | Zorunlu | Örnek | Amaç |
|---|---|---|---|
| `Content-Type` | Evet | `application/json` | Gövde formatı |
| `Idempotency-Key` | Önerilir | UUID | Mükerrer yazımı önle |
| `X-Correlation-Id` | İsteğe bağlı | `req-abc-123` | Dağıtık log korelasyonu |

### Hata Yanıt Formatı

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Kullanıcı bulunamadı",
  "status": 404,
  "detail": "'abc-123' ID'li kullanıcı bulunamadı.",
  "instance": "/api/v1/users/abc-123",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

---

## Uçtan Uca Senaryo

```
1. Kullanıcı oluştur
   POST https://localhost:5003/api/v1/users
   Idempotency-Key: <uuid>
   { "username": "alice", "email": "alice@test.com", "fullName": "Alice" }
   → 201 Created, id: abc-123

2. İçerik oluştur
   POST https://localhost:5004/api/v1/contents
   { "title": "Merhaba", "body": "İlk yazı", "authorId": "abc-123" }
   → ContentService, UserService'e HTTP ile alice'in varlığını doğrular
   → 201 Created

3. Kullanıcıyı sil (SAGA zinciri)
   DELETE https://localhost:5003/api/v1/users/abc-123
   → User.Status = PendingDeletion
   → OutboxMessage("UserDeleted") atomik yazıldı (DB transaction)
   → OutboxDispatcher → RabbitMQ
   → UserDeletedConsumer → alice'in içerikleri soft-delete
   → User.Status = Deleted
   → 204 No Content

4. Doğrula
   GET https://localhost:5004/api/v1/contents
   → alice'in içerikleri filtrelenmiş, görünmüyor
```

---

## Testleri Çalıştırma

```bash
# Birim testleri (Docker gerekmez)
dotnet test src/UserService/UserService.Tests
dotnet test src/ContentService/ContentService.Tests

# Entegrasyon testleri (Docker gerekir — TestContainers PostgreSQL + Redis başlatır)
dotnet test src/UserService/UserService.Tests --filter "FullyQualifiedName~Integration"

# Kapsam raporu
dotnet test --collect:"XPlat Code Coverage"
```

---

## Proje Yapısı

```
caseStudy/
├── src/
│   ├── SharedContracts/                   # Servisler arası paylaşılan event DTO'ları
│   │   ├── Events/UserDeletedEvent.cs
│   │   └── Messaging/QueueNames.cs
│   │
│   ├── UserService/
│   │   ├── UserService.Domain/
│   │   │   ├── Entities/User.cs           # Zengin domain entity
│   │   │   ├── Enums/UserStatus.cs
│   │   │   └── Outbox/OutboxMessage.cs    # Outbox varlığı
│   │   ├── UserService.Application/
│   │   │   ├── Commands/                  # CreateUser, UpdateUser, DeleteUser
│   │   │   ├── Queries/                   # GetAllUsers, GetUserById
│   │   │   ├── Common/UserMapper.cs
│   │   │   ├── DTOs/
│   │   │   ├── Exceptions/
│   │   │   └── Interfaces/                # IUserRepository, IOutboxRepository, ICacheService
│   │   ├── UserService.Infrastructure/
│   │   │   ├── Persistence/
│   │   │   │   ├── AppDbContext.cs        # Users + OutboxMessages tabloları
│   │   │   │   └── DataSeeder.cs          # Başlangıç verisi
│   │   │   ├── Repositories/             # UserRepository, OutboxRepository
│   │   │   ├── Outbox/OutboxDispatcher.cs # BackgroundService — 5sn polling
│   │   │   ├── Messaging/RabbitMqEventPublisher.cs
│   │   │   └── Cache/RedisCacheService.cs
│   │   ├── UserService.API/
│   │   │   ├── Controllers/UsersController.cs  # Saf IMediator dispatch
│   │   │   ├── Middleware/
│   │   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   │   ├── IdempotencyMiddleware.cs    # Redis destekli
│   │   │   │   └── ExceptionHandlingMiddleware.cs
│   │   │   └── Program.cs                 # Serilog, OTel, Versioning, DI
│   │   └── UserService.Tests/
│   │       ├── Services/                  # Moq birim testleri
│   │       └── Integration/               # TestContainers entegrasyon testleri
│   │
│   └── ContentService/                    # Aynı yapı
│       └── ContentService.Infrastructure/
│           ├── HttpClients/UserServiceClient.cs  # Polly pipeline
│           └── Messaging/UserDeletedConsumer.cs  # RabbitMQ consumer
│
├── docker/
│   └── kong/kong.yml                      # Kong declarative config
├── docker-compose.yml                     # 11 servis
├── NuGet.config                           # Yalnızca nuget.org
└── .env.example                           # Ortam değişkeni şablonu
```

---

## Kimlik Doğrulama — Bilinçli Erteleme

Kimlik doğrulama bu case study'de **uygulanmamıştır**. Tüm uç noktalar açıktır. Bu, case study'nin odağını (CQRS, Outbox, Resilience, CAP) gölgelememe kararıdır.

### Seçenek A: JWT Bearer (Hafif)

```
İstemci → Kong JWT Eklentisi → token doğrulama → Mikroservis
```

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    });
```

### Seçenek B: Keycloak (Kurumsal)

```
İstemci → Keycloak token al → Kong (RS256 JWKS doğrulama) → Mikroservis
Servisler arası → Client Credentials OAuth2 akışı
```

| Kriter | JWT Özel | Keycloak |
|---|---|---|
| Token yönetimi | Kendin yaz | Keycloak yönetir |
| SSO | Yok | Var |
| Rol yönetimi | DB/config | Admin UI |
| Servisler arası | Shared secret | Client Credentials |

**Gerçek projede:** Herhangi bir endpoint staging'e çıkmadan önce auth eklenir — bu case study'de mimari pattern'ların önünde durmaması için ertelendi.
