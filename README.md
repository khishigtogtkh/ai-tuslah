# AI Support Agent — Tenant бүртгэл

Одоогийн хүрээ: платформын админ нэвтрэх, программ хөгжүүлэгч компанийг Tenant болгон бүртгэх, жагсаалт болон дэлгэрэнгүй харах, License-ийг идэвхжүүлэх/идэвхгүй болгох. Tenant үүсгэсэн болон License өөрчилсөн дотоод audit хадгална.

License зөвхөн `active` / `inactive` төлөвтэй. Tenant бүртгэхэд автоматаар `inactive` License үүснэ; админ тусдаа идэвхжүүлнэ. Байгууллагын хэрэглэгч, урилга, Tenant мэдээлэл засах/устгах, AI Agent, SDK, License хугацаа, төлбөр байхгүй. License-ийн шалгалт нь дараагийн AI runtime-д ашиглах суурь бөгөөд одоогийн админы удирдлагыг хаахгүй.

## Бүтэц

- `backend/src/AiAgent.Api` — HTTP API, нэвтрэлт, CSRF, админ provisioning.
- `backend/src/AiAgent.Application` — Tenant болон License use case, DTO, repository interface.
- `backend/src/AiAgent.Domain` — Tenant, License, AuditEvent.
- `backend/src/AiAgent.Infrastructure` — Identity, PostgreSQL, EF Core, migration.
- `frontend` — React + TypeScript + Vite, Монгол хэлтэй гурван дэлгэц.
- `scripts/backend.sh` — локал SDK, `.env`, Data Protection тохиргоог ачаалах wrapper.

Frontend-ийн API client-ийн unit тест болон GitHub Actions CI нэмсэн. Backend-ийн тестийн төсөл, Playwright байхгүй.

## Орчин

Энэ workspace-д .NET SDK 10.0.402, runtime 10.0.13, EF CLI 10.0.13 нь `.tools` дотор бэлтгэгдсэн. Системийн default .NET 8-ыг өөрчлөхгүй. Node 22 хувилбарыг `.nvmrc` тогтооно. PostgreSQL 17 нь Compose-оор `127.0.0.1:5433` дээр ажиллана; бусад төслийн 5432 портод хүрэхгүй.

Өдөр тутам FE болон Docker backend ажиллуулахад host .NET хэрэггүй. Доорх `.tools` бэлтгэл нь host дээр migration ажиллуулах эсвэл backend-ийг debugger-тай шууд ажиллуулахад хэрэглэгдэнэ.

Шинэ clone дээр SDK, EF CLI-г бэлтгэх:

```bash
cd /home/hishigtogtokh/ai-agent
nvm use
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/ai-agent-dotnet-install.sh
bash /tmp/ai-agent-dotnet-install.sh --version 10.0.402 --install-dir "$PWD/.tools/dotnet" --no-path
export DOTNET_ROOT="$PWD/.tools/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet tool install dotnet-ef --tool-path .tools/ef --version 10.0.13
```

Шинэ орчинд `.env.example`-ийг `.env` болгон хуулж, `POSTGRES_PASSWORD`-д тусдаа санамсаргүй нууц үг тохируулна. `chmod 600 .env` ажиллуулна. Одоо байгаа `.env`-ийг дарж солихгүй. Wrapper нь `.env`-ийг shell configuration байдлаар ачаалдаг тул зөвхөн итгэлтэй локал файл ашиглана.

## Өдөр тутам ажиллуулах

Database болон backend-ийг repository root-оос нэг командаар асаана:

```bash
cd /home/hishigtogtokh/ai-agent
docker compose up -d
```

Анх ажиллуулахад backend Docker image автоматаар build хийнэ. SDK/runtime base image нь өмнө ашигласан хувилбарын digest-ээр тогтоогдсон. `db` healthy болсны дараа `be` асна. Backend нь `http://localhost:5000`, PostgreSQL нь `127.0.0.1:5433`. Docker сүлжээ дотор backend нь `db:5432`-оор холбогдоно.

Frontend-ийг тусдаа terminal-д мөн repository root-оос:

```bash
cd /home/hishigtogtokh/ai-agent
nvm use
npm --prefix frontend ci  # Анх эсвэл frontend/package-lock.json өөрчлөгдсөн үед
npm run dev
```

Root-ийн `npm run dev` нь frontend-ийн dev командыг дуудна; `frontend/` дотроос `npm run dev` ажиллуулах боломж мөн хэвээр. Frontend: `http://localhost:5173`. `/api` нь Vite proxy-оор backend рүү очно. Production frontend build-ийг шалгах `vite preview` нь API proxy агуулахгүй; бүрэн урсгал шалгахдаа dev server эсвэл reverse proxy ашиглана.

Backend код өөрчилсний дараа image-ийг шинэчилнэ:

```bash
docker compose up -d --build be
```

Container нь build хийсэн кодыг ажиллуулна; host дээр код засахад автоматаар reload хийхгүй. Төлөв болон backend log харах:

```bash
docker compose ps
docker compose logs -f be
```

Унтраахдаа frontend terminal-д `Ctrl+C`, repository root-оос:

```bash
docker compose stop
```

`postgres_data` нь database, `backend_data` нь Docker backend-ийн Data Protection keys хадгална. Container restart/recreate хийхэд volume-ууд хадгалагдана. Host дээрх `.data/local-admin.json` нь image-д орохгүй; админы нэвтрэх бүртгэл одоогийн database-д хэвээр байна. Host backend-ээс Docker backend рүү анх шилжихэд шинэ key store ашиглах тул дахин нэвтэрнэ.

Энэ Compose тохиргоо нь зөвхөн localhost-д зориулсан Development орчин. Production deployment-д доорх тусдаа шаардлагууд үйлчилнэ.

## Шинэ database бэлтгэх болон админ үүсгэх

Одоогийн workspace-д migration болон локал админ бэлэн. Шинэ хоосон database эсвэл шинэ migration байгаа үед host-ийн EF CLI-ээр тусдаа хэрэгжүүлнэ:

```bash
docker compose up -d --wait db
docker compose stop be
bash scripts/backend.sh restore backend/AiAgent.slnx --locked-mode
bash scripts/backend.sh build backend/AiAgent.slnx --no-restore
bash scripts/backend.sh ef database update --project backend/src/AiAgent.Infrastructure --startup-project backend/src/AiAgent.Api --no-build
docker compose up -d --build
```

Migration-ууд нь Identity, Tenants, AuditEvents, Licenses хүснэгтүүдийг үүсгэнэ. `20261102125621_AddTenantLicenses` нь хуучин Tenant бүрд нэг `inactive` License үүсгэх бөгөөд үүнийг админы үйлдэл гэж audit-д бүртгэхгүй. Нөхсөн License-ийн `UpdatedAt`, `UpdatedByAdminId` нь null байна. Сервер startup дээр migration автоматаар ажиллахгүй. EF командын өмнө шинэ migration-тай assembly-г build хийсэн байна. Одоо байгаа database-д migration хийхээс өмнө backup авна.

Ажиллаж буй Docker backend дээр админ үүсгэх:

```bash
docker compose exec be dotnet AiAgent.Api.dll provision-admin --email admin@company.mn
```

Нууц үгийг дараагийн prompt-д оруулна: хамгийн багадаа 12 тэмдэгт. Terminal-д харуулахгүй, command argument, environment variable, log-д оруулахгүй. Ижил админыг дахин provision хийхэд бүртгэл давхардахгүй, нууц үг өөрчлөгдөхгүй. Default нууц үг кодод байхгүй. Provisioning нь database transaction ашиглана.

Host дээр backend debugger-тай ажиллуулах шаардлагатай бол эхлээд `docker compose stop be`, дараа нь `bash scripts/backend.sh` ажиллуулна. Ингэснээр 5000 порт давхцахгүй.

## Database-ийг browser дээр харах — Prisma Studio

Repository root-д нэг удаа local tooling dependency суулгана:

```bash
cd /home/hishigtogtokh/ai-agent
nvm use
npm ci
```

Database болон Studio-г нэг командаар асаах:

```bash
npm run db:studio
```

Browser дээр `http://localhost:5555` нээгдэнэ. WSL орчинд browser автоматаар нээгдэхгүй бол энэ хаягийг өөрөө нээнэ. FE, BE ажиллах шаардлагагүй.

PostgreSQL аль хэдийн ажиллаж байвал дассан командаа шууд ашиглаж болно:

```bash
npx prisma studio
```

Studio нь `prisma.config.mjs`-ээр repository root-ийн `.env` доторх `POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` утгыг авна. Нууц үгийг command argument-д оруулах шаардлагагүй. `Tenants`, `AuditEvents`, `AspNetUsers` зэрэг хүснэгтийг browser дээр харна.

Prisma зөвхөн local Studio хэрэгсэл байна. Backend-ийн EF Core model, migration хэвээр; Prisma schema, Prisma Client, Prisma migration нэмээгүй. Studio-оос password hash-ийг энгийн нууц үгээр сольж болохгүй; админы бүртгэлийг Identity-ээр удирдана.

Studio-г ажиллаж буй terminal-д `Ctrl+C` дарж зогсооно. Database-ийг бас зогсоох бол `docker compose stop db` ажиллуулна; өгөгдөл хадгалагдана.
DB болон backend хамт ажиллаж байгаа бол хоёуланг зогсоохдоо `docker compose stop` ашиглана.

Локал баталгаажуулалт: Studio UI болон database bridge хоёулаа HTTP 200 буцааж, `Tenants`, `AuditEvents`, `AspNetUsers` хүснэгтүүдийг database-аас уншсан. Browser-ийн бодит дүрслэлийг шалгаагүй.

## API

| Method / path | Ажиллагаа |
| --- | --- |
| `GET /api/auth/csrf` | Session-д тохирох CSRF token |
| `POST /api/auth/login` | Админ нэвтрэх |
| `GET /api/auth/session` | Одоогийн админы ID, имэйл |
| `GET /api/platform/tenants?page=1&pageSize=25` | Tenant жагсаалт |
| `POST /api/platform/tenants` | Tenant үүсгэх |
| `GET /api/platform/tenants/{id}` | Tenant дэлгэрэнгүй |
| `PATCH /api/platform/tenants/{id}/license` | License төлөв өөрчлөх |

Login JSON нь `email`, `password`. Tenant үүсгэх JSON нь `name`, `contactEmail`, `phone` (сонголттой, null байж болно). Нэр 200, имэйл 254, утас 32 тэмдэгтээс хэтрэхгүй. Нэр, имэйл заавал; ижил нэр/холбоо барих имэйлийг давхардал гэж хориглохгүй. Нэр, имэйл, утасны захын хоосон зайг цэвэрлэнэ.

Tenant JSON: `id`, `name`, `contactEmail`, `phone`, `createdByAdminId`, `createdAt`, `license`. Үүсгэх, жагсаалт, дэлгэрэнгүй хариу бүр `license` объекттой. Түүний талбарууд: `id`, `tenantId`, `status`, `version`, `createdAt`, `updatedAt`, `updatedByAdminId`. Огноо UTC. Үүсгэхэд `201` болон detail endpoint-ийн `Location` буцаана. Жагсаалт `{ items, totalCount, page, pageSize }` хэлбэртэй, `createdAt DESC, id DESC` эрэмбэтэй. Page 1-ээс, pageSize 1–100; default 25.

Бүх POST/PATCH-д эхлээд `/api/auth/csrf` дуудаж cookie болон `token` авна. Тухайн cookie-тойгоо `X-CSRF-TOKEN` header дамжуулна. Нэвтрэлтийн дараа шинэ identity-д тохирох token дахин авна; frontend үүнийг хүсэлт бүрд хийдэг.

Алдаа `ProblemDetails`: `400` буруу өгөгдөл/CSRF, `401` нэвтрэлтгүй, `403` админ эрхгүй, `404` олдоогүй, `409` License version зөрчил, `429` rate limit, `500` ерөнхий серверийн алдаа. `traceId` нь серверийн хүсэлттэй холбох ID. Хэрэглэгчийн нууц үг, cookie, CSRF token-ийг log-д бичихгүй.

## License удирдах

1. Админаар нэвтрээд Tenant жагсаалтын License баганаас төлөв харна.
2. Tenant-ийн дэлгэрэнгүйд орно.
3. `inactive` үед «License идэвхжүүлэх», `active` үед «License идэвхгүй болгох» товч дарна.
4. Төлөв болон сүүлд өөрчилсөн огноо серверийн хариугаар шинэчлэгдэнэ. Жагсаалт руу буцахад төлөвийг дахин ачаална.

Тусдаа License дэлгэц болон GET endpoint байхгүй. Tenant үүсгэх форм License сонголт агуулахгүй.

Хоёр үйлдэл нэг endpoint ашиглана:

```http
PATCH /api/platform/tenants/{id}/license
Content-Type: application/json
X-CSRF-TOKEN: <одоогийн session-ийн CSRF token>
```

```json
{
  "status": "active",
  "version": "<Tenant хариуны license.version UUID>"
}
```

`status` нь зөвхөн жижиг үсгийн `active` эсвэл `inactive`; `version` нь заавал хүчинтэй, хоосон биш UUID байна. Амжилттай үед `200` болон License объект буцаана. Үйлдэгч админыг backend өөрөө session-оос авна.

Ижил төлөв хүссэн давтан хүсэлтэд одоогийн License-ийг `200`-гаар буцааж, version, өөрчилсөн огноо, audit-ийг өөрчлөхгүй. Өөр төлөв хүссэн боловч version хуучирсан эсвэл зэрэгцээ өөрчлөлт түрүүлж хадгалагдсан бол `409` буцаана. Frontend энэ үед мэдээллээ дахин ачаалж тайлбар харуулна; PATCH-ийг автоматаар давтахгүй.

License өөрчлөлт бүр шинэ version-той болж, `LicenseActivated` / `LicenseDeactivated` audit-тай нэг transaction-д хадгалагдана. Зөрчил, database алдаа гарвал update болон audit хоёулаа буцна. License-ийн timestamp-ууд PostgreSQL-ийн microsecond нарийвчлалтай байна.

`LicenseService.HasActiveLicenseAsync(tenantId)` нь License байгаа бөгөөд `active` үед true буцаана. License байхгүй болон `inactive` үед false. Энэ service нь ирээдүйн AI runtime-д зориулагдсан; одоогоор AI endpoint байхгүй. Идэвхгүй Tenant-ийг админ харах, дахин идэвхжүүлэх боломж хэвээр.

## Нэвтрэлт ба өгөгдлийн хамгаалалт

- Identity password hashing, normalized email-ийн unique index, `PlatformAdmin` role.
- Session: HttpOnly, SameSite=Strict, найман цаг, sliding renewal байхгүй; browser local/session storage-д token хадгалахгүй.
- Production cookie Secure; HTTP development орчинд localhost дээр ажиллана.
- Login: нэг IP-д минутанд 10 хүсэлт, таван буруу нууц үгийн дараа 15 минут account lockout. Ерөнхий алдааны мэдээлэл харуулна.
- Tenant endpoint бүр backend дээр админ эрх шаарддаг. UI route guard нь нэмэлт UX хамгаалалт.
- Tenant, анхны License болон audit нэг SaveChanges transaction-д хадгалагдана. License өөрчлөлт, audit мөн нэг transaction-д хадгалагдана. Хэсэгчилсэн бүртгэл үлдэхгүй.
- Data Protection keys нь `.data/keys` эсвэл `AI_AGENT_DATA_DIR/keys` дотор хадгалагдана. Wrapper 077 umask ашиглана; энэ хавтас Git-д орохгүй.
- Production-д key storage-г тусдаа хамгаалсан persistent volume дээр хадгалж, backup болон дискний шифрлэлтийг deployment орчинд тохируулна. Олон instance ижил key store, application name ашиглана.

## Production бэлтгэл

Production deployment энэ ажлын хүрээнд хийгдээгүй. Deploy хийхдээ:

1. Backend-ийг Release publish; frontend-ийг `npm ci` → `npm run build` хийнэ.
2. Нэг HTTPS origin дээр static frontend болон `/api` reverse proxy байрлуулна. SPA fallback нь `/api`-г HTML рүү шилжүүлэхгүй.
3. `ASPNETCORE_ENVIRONMENT=Production`, `AllowedHosts`, `ConnectionStrings__Database`, хамгаалсан `AI_AGENT_DATA_DIR` тохируулна. Connection string-ийг secret store/environment-ээр өгнө. Локал wrapper-ийг production entrypoint болгохгүй.
4. Forwarded headers нь default-аар loopback proxy-д итгэнэ. Өөр proxy ашиглавал түүний IP-г тодорхой trusted proxy болгон тохируулна; бүх эх үүсвэрт итгэхгүй. TLS termination-ийн `X-Forwarded-Proto=https` зөв дамжина.
5. Migration-ийг тусдаа deployment алхмаар backup-тайгаар хэрэгжүүлнэ. Runtime хэрэглэгчид schema өөрчлөх эрх өгөхгүй.
6. Login rate limiter одоогоор process-local. Олон instance-д гаргах үед reverse proxy дээр нийтлэг rate limit тохируулна.

## Локал шалгалт

```bash
bash scripts/backend.sh restore backend/AiAgent.slnx --locked-mode
bash scripts/backend.sh build backend/AiAgent.slnx --configuration Release --no-restore
cd frontend
npm ci
npm test
npm run typecheck
npm run build
```

`npm test` нь Vitest-ээр `frontend/src/api.test.ts`-ийг нэг удаа ажиллуулаад дуусна. GET/POST, CSRF, validation болон HTTP алдаа, session-expired event, сүлжээний алдаа, AbortError-ийг шалгана. Тестүүд Node орчинд mock fetch ашигладаг тул backend, database, админы бүртгэл шаардахгүй.

Browser-оор нэвтрэх, Tenant үүсгэх, жагсаалт, дэлгэрэнгүй, refresh ажиллагааг гараар шалгана. API-аар нэвтрэлтгүй хандалт, буруу login, CSRF-гүй POST, буруу Tenant өгөгдлийг шалгаж болно. Database-аас Tenant болон audit хамт үүссэнийг, restart-аас хойш өгөгдөл болон session хадгалагдсаныг батална.

PostgreSQL өгөгдөл `ai-agent_postgres_data` volume-д хадгалагдана. Энгийн зогсоолтод `docker compose stop` ашиглана. `docker compose down -v` нь өгөгдөл устгадаг тул ашиглахгүй.

## Хэрэгжилтийн баталгаажуулалт — 2026-10-20

- Backend Release build: 0 warning, 0 error. Frontend type check болон production build амжилттай.
- Анхны migration локал PostgreSQL-д хэрэгжсэн. License хүснэгт үүсээгүй.
- Админ provisioning-ийг давтан ажиллуулж, бүртгэл давхардахгүй байгааг шалгасан.
- Локал frontend proxy-оор login, session, Tenant үүсгэх/жагсаах/дэлгэрэнгүй хүсэлт амжилттай. Нэвтрэлтгүй хүсэлт 401, CSRF-гүй POST болон буруу өгөгдөл 400, олдоогүй Tenant 404 буцаасан.
- Нэг локал шалгалтын Tenant, түүнтэй зөв холбоостой нэг audit үүссэн. Backend restart-ийн дараа session болон Tenant хоёулаа хадгалагдсан.
- Локал админы имэйл, санамсаргүй нууц үг `.data/local-admin.json` дотор 0600 эрхтэй хадгалагдсан; Git-д орохгүй. Энэ нь зөвхөн энэ workspace-ийн локал бүртгэл, application-ийн default account биш.
- Browser-ийн бодит дүрслэл, production deployment болон backup/restore шалгаагүй. Энэ анхны хэрэгжилтийн шалгалтад автомат тест байгаагүй.

## Docker локал ажиллагааны баталгаажуулалт — 2026-10-20

- Multi-stage backend image build амжилттай. Runtime нь `app` (UID 1654) хэрэглэгчээр ажиллана; `.env`, `.data`, frontend болон host build artifact-ууд image-д орохгүй.
- `docker compose up -d` нь DB болон backend-ийг асаасан. Backend нь DB healthy болсны дараа эхэлж, зөвхөн `127.0.0.1:5000` портод нээлттэй.
- Root-ийн `npm run dev` frontend-ийг асаасан. Frontend proxy-оор CSRF, админ login, session, Tenant жагсаалт HTTP 200; нэвтрэлтгүй Tenant хүсэлт HTTP 401 буцаасан.
- Backend container restart-ийн дараа өмнөх session болон Tenant өгөгдөл хадгалагдсан. Шалгалтаар шинэ Tenant үүсгээгүй.
- Browser-ийн дүрслэл болон production deployment шалгаагүй; энэ Docker шалгалтад автомат тест ашиглаагүй.

## GitHub Actions CI — Лаборатори 7

Жишиг жижиг төслийн оронд одоогийн React + .NET төслийг ашиглана. `.github/workflows/ci.yml` нь бүх салбарын `push`, мөн Pull Request нээх, дахин нээх, commit нэмэх үед ажиллана. Нээлттэй PR-ийн салбар руу push хийхэд push болон PR тус бүрийн run харагдах нь хэвийн.

Хоёр job `ubuntu-latest` дээр тусдаа, 15 минутын хязгаартай ажиллана:

| Job | Алхмууд |
| --- | --- |
| Frontend test and build | Checkout → `.nvmrc`-ийн Node → `npm ci` → `npm test` → `npm run typecheck` → `npm run build` |
| Backend build | Checkout → `global.json`-ийн .NET SDK → locked restore → Release build |

Frontend командууд `frontend/` дотор, backend командууд root-оос ажиллана. npm cache нь frontend lockfile-аас түлхүүрээ авна. Workflow нь repository contents унших эрхтэй; database, `.env`, repository secret шаардахгүй. Deploy, migration, Docker build болон backend тест ажиллуулахгүй.

Локал баталгаажуулалт — 2026-10-20: цэвэр `npm ci`, API-ийн 13/13 unit тест, frontend typecheck болон production build амжилттай. Backend locked restore болон Release build амжилттай, 0 warning / 0 error. Workflow YAML болон `push`, `pull_request` trigger-ийг шалгасан.

Эхний GET тестийн expected URL-ийг түр `/api/wrong-tenants` болгоход 1 тест унаж, 12 тест амжилттай болсон; exit code 1, файл/мөр болон expected/received ялгаа гарсан. Тестийг сэргээсний дараа 13/13 дахин амжилттай болсон. Энэ нь локал баталгаажуулалт бөгөөд GitHub run-ийн нотолгоог орлохгүй.

### GitHub дээр ногоон → улаан → дахин ногоон шалгах

1. CI өөрчлөлтийг `main` руу commit/push хийнэ. Branch protection шаардвал PR-аар оруулна. Actions табаас `main`-ийн `CI` push run-д хоёр job ногоон болсныг шалгана.
2. `main`-ээс `ci/failure-demo` салбар үүсгэнэ. `frontend/src/api.test.ts` дэх эхний GET тестийн хүлээгдэж буй `'/api/platform/tenants'` утгыг түр `'/api/wrong-tenants'` болгож, тусдаа commit/push хийнэ. Application-ийн кодыг өөрчлөхгүй.
3. `ci/failure-demo` салбараас `main` руу PR нээж, `push` болон `pull_request` trigger хоёул ажилласныг шалгана.
4. Улаан run-ийн `Frontend test and build` → `Test` алхмыг нээнэ. Тестийн нэр, файл/мөр, expected болон received утга, exit code 1-ийг тэмдэглэнэ. Дараагийн typecheck/build алхмууд skipped болох бөгөөд backend build тусдаа ажиллана.
5. Тестийн утгыг буцааж засаж, тусдаа commit/push хийнэ. Push болон PR run-ууд дахин ногоон болсныг шалгана. Эвдэрсэн тесттэй commit-ийг `main` руу merge хийхгүй.
6. Туршилтын PR-ийг merge хийхгүйгээр хаана. Run бүрийн холбоос, commit ID болон алдааны логийг лабораторийн нотолгоонд хадгалж, доорх хүснэгтэд GitHub дээр бодитоор баталгаажуулсан үр дүнг бүртгэнэ.

Локал тест/build амжилттай болох нь GitHub runner дээр workflow ажилласныг батлахгүй. GitHub run-ийн үр дүн болон нотолгоо бүрдээгүй бол лабораторийн external баталгаажуулалт дуусаагүй байна.

| Үе шат | Commit | Run холбоос | Үр дүн / нотолгоо |
| --- | --- | --- | --- |
| Эхний push | — | — | Хүлээгдэж байна |
| Зориуд унагасан тест / push | — | — | Хүлээгдэж байна |
| Зориуд унагасан тест / PR | — | — | Хүлээгдэж байна |
| Засварын push / PR | — | — | Хүлээгдэж байна |

## License хэрэгжилтийн баталгаажуулалт — 2026-11-03

- Backend Release build, Docker image build, frontend typecheck болон production build амжилттай. Одоо байгаа frontend API client-ийн 13/13 тест амжилттай; шинэ автомат тест нэмээгүй.
- `20261102125621_AddTenantLicenses` локал database-д хэрэгжсэн. Өмнөх хоёр Tenant-д тус бүр нэг `inactive` License үүсэж, хуучин audit болон админы бүртгэл өөрчлөгдөөгүй. EF model болон migration snapshot зөрөөгүй.
- Локал frontend proxy-оор админ login, Tenant үүсгэлт/жагсаалт/дэлгэрэнгүй, License идэвхжүүлэх/идэвхгүй болгох ажиллагааг шалгасан. Нэвтрэлтгүй PATCH 401, CSRF-гүй болон буруу status/version-той PATCH 400, олдоогүй Tenant 404, хуучирсан зөрчилтэй version 409 буцаасан.
- Нэг License мөрийг түр түгжиж хоёр зэрэгцээ update хүлээлгэсний дараа нэг хүсэлт 200, нөгөө нь 409 буцаасан. Зөвхөн нэг audit хадгалагдсан; унасан хүсэлтийн audit буцсан.
- Ижил төлөв рүү давтан хүсэлт хийхэд хариуны metadata өөрчлөгдөөгүй, audit нэмэгдээгүй. PATCH хариу болон database-аас дахин уншсан License ижил байсан.
- Docker backend restart-ийн дараа session, License төлөв, version хадгалагдсан. Database болон backend нь хэвийн Compose service-ээр ажиллана.
- Шалгалтад зориулан `License локал шалгалт 2026-11-02` нэртэй нэг Tenant нэмсэн; эцэст нь `inactive` болгосон. Migration-ээс өмнөх backup нь Git-д орохгүй `.data/backups/pre-license-20261102T125618Z.dump` дотор бий.
- Browser-ийн бодит дүрслэл, GitHub CI run болон production deployment энэ ажлаар баталгаажаагүй.
