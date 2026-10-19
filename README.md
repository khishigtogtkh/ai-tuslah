# AI Support Agent — Tenant бүртгэл

Эхний хэсгийн хүрээ: платформын админ нэвтрэх, программ хөгжүүлэгч компанийг Tenant болгон бүртгэх, жагсаалт болон дэлгэрэнгүй харах. Tenant үүсгэсэн дотоод audit хадгална.

License нь дараагийн хэсэгт `active` / `inactive` төлөвтэй нэмэгдэнэ. Одоогоор License хүснэгт, эрх олгох ажиллагаа, байгууллагын хэрэглэгч, урилга, мэдээлэл засах/устгах, AI Agent, SDK байхгүй. Tenant бүртгэл нь ашиглах эрх олгохгүй.

## Бүтэц

- `backend/src/AiAgent.Api` — HTTP API, нэвтрэлт, CSRF, админ provisioning.
- `backend/src/AiAgent.Application` — Tenant use case, DTO, repository interface.
- `backend/src/AiAgent.Domain` — Tenant, AuditEvent.
- `backend/src/AiAgent.Infrastructure` — Identity, PostgreSQL, EF Core, migration.
- `frontend` — React + TypeScript + Vite, Монгол хэлтэй гурван дэлгэц.
- `scripts/backend.sh` — локал SDK, `.env`, Data Protection тохиргоог ачаалах wrapper.

Автомат тест, тестийн төсөл, Playwright нэмээгүй.

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

Анх ажиллуулахад backend Docker image автоматаар build хийнэ. `db` healthy болсны дараа `be` асна. Backend нь `http://localhost:5000`, PostgreSQL нь `127.0.0.1:5433`. Docker сүлжээ дотор backend нь `db:5432`-оор холбогдоно.

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
bash scripts/backend.sh restore backend/AiAgent.slnx --locked-mode
bash scripts/backend.sh build backend/AiAgent.slnx --no-restore
bash scripts/backend.sh ef database update --project backend/src/AiAgent.Infrastructure --startup-project backend/src/AiAgent.Api --no-build
docker compose up -d --build
```

Migration нь Identity, Tenants, AuditEvents хүснэгтүүдийг үүсгэнэ. Сервер startup дээр migration автоматаар ажиллахгүй. EF командын өмнө шинэ migration-тай assembly-г build хийсэн байна.

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

Login JSON нь `email`, `password`. Tenant үүсгэх JSON нь `name`, `contactEmail`, `phone` (сонголттой, null байж болно). Нэр 200, имэйл 254, утас 32 тэмдэгтээс хэтрэхгүй. Нэр, имэйл заавал; ижил нэр/холбоо барих имэйлийг давхардал гэж хориглохгүй. Нэр, имэйл, утасны захын хоосон зайг цэвэрлэнэ.

Tenant JSON: `id`, `name`, `contactEmail`, `phone`, `createdByAdminId`, `createdAt`. Огноо UTC. Үүсгэхэд `201` болон detail endpoint-ийн `Location` буцаана. Жагсаалт `{ items, totalCount, page, pageSize }` хэлбэртэй, `createdAt DESC, id DESC` эрэмбэтэй. Page 1-ээс, pageSize 1–100; default 25.

Бүх POST-д эхлээд `/api/auth/csrf` дуудаж cookie болон `token` авна. Тухайн cookie-тойгоо `X-CSRF-TOKEN` header дамжуулна. Нэвтрэлтийн дараа шинэ identity-д тохирох token дахин авна; frontend үүнийг хүсэлт бүрд хийдэг.

Алдаа `ProblemDetails`: `400` буруу өгөгдөл/CSRF, `401` нэвтрэлтгүй, `403` админ эрхгүй, `404` олдоогүй, `429` rate limit, `500` ерөнхий серверийн алдаа. `traceId` нь серверийн хүсэлттэй холбох ID. Хэрэглэгчийн нууц үг, cookie, CSRF token-ийг log-д бичихгүй.

## Нэвтрэлт ба өгөгдлийн хамгаалалт

- Identity password hashing, normalized email-ийн unique index, `PlatformAdmin` role.
- Session: HttpOnly, SameSite=Strict, найман цаг, sliding renewal байхгүй; browser local/session storage-д token хадгалахгүй.
- Production cookie Secure; HTTP development орчинд localhost дээр ажиллана.
- Login: нэг IP-д минутанд 10 хүсэлт, таван буруу нууц үгийн дараа 15 минут account lockout. Ерөнхий алдааны мэдээлэл харуулна.
- Tenant endpoint бүр backend дээр админ эрх шаарддаг. UI route guard нь нэмэлт UX хамгаалалт.
- Tenant болон audit нэг SaveChanges transaction-д хадгалагдана. Хэсэгчилсэн бүртгэл үлдэхгүй.
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
bash scripts/backend.sh build backend/AiAgent.slnx --no-restore
cd frontend
npm run typecheck
npm run build
```

Автомат тест ажиллуулахгүй. Browser-оор нэвтрэх, Tenant үүсгэх, жагсаалт, дэлгэрэнгүй, refresh ажиллагааг гараар шалгана. API-аар нэвтрэлтгүй хандалт, буруу login, CSRF-гүй POST, буруу Tenant өгөгдлийг шалгаж болно. Database-аас Tenant болон audit хамт үүссэнийг, restart-аас хойш өгөгдөл болон session хадгалагдсаныг батална.

PostgreSQL өгөгдөл `ai-agent_postgres_data` volume-д хадгалагдана. Энгийн зогсоолтод `docker compose stop` ашиглана. `docker compose down -v` нь өгөгдөл устгадаг тул ашиглахгүй.

## Хэрэгжилтийн баталгаажуулалт — 2026-10-20

- Backend Release build: 0 warning, 0 error. Frontend type check болон production build амжилттай.
- Анхны migration локал PostgreSQL-д хэрэгжсэн. License хүснэгт үүсээгүй.
- Админ provisioning-ийг давтан ажиллуулж, бүртгэл давхардахгүй байгааг шалгасан.
- Локал frontend proxy-оор login, session, Tenant үүсгэх/жагсаах/дэлгэрэнгүй хүсэлт амжилттай. Нэвтрэлтгүй хүсэлт 401, CSRF-гүй POST болон буруу өгөгдөл 400, олдоогүй Tenant 404 буцаасан.
- Нэг локал шалгалтын Tenant, түүнтэй зөв холбоостой нэг audit үүссэн. Backend restart-ийн дараа session болон Tenant хоёулаа хадгалагдсан.
- Локал админы имэйл, санамсаргүй нууц үг `.data/local-admin.json` дотор 0600 эрхтэй хадгалагдсан; Git-д орохгүй. Энэ нь зөвхөн энэ workspace-ийн локал бүртгэл, application-ийн default account биш.
- Browser-ийн бодит дүрслэл, production deployment болон backup/restore шалгаагүй. Автомат тест бичээгүй, ажиллуулаагүй.

## Docker локал ажиллагааны баталгаажуулалт — 2026-10-20

- Multi-stage backend image build амжилттай. Runtime нь `app` (UID 1654) хэрэглэгчээр ажиллана; `.env`, `.data`, frontend болон host build artifact-ууд image-д орохгүй.
- `docker compose up -d` нь DB болон backend-ийг асаасан. Backend нь DB healthy болсны дараа эхэлж, зөвхөн `127.0.0.1:5000` портод нээлттэй.
- Root-ийн `npm run dev` frontend-ийг асаасан. Frontend proxy-оор CSRF, админ login, session, Tenant жагсаалт HTTP 200; нэвтрэлтгүй Tenant хүсэлт HTTP 401 буцаасан.
- Backend container restart-ийн дараа өмнөх session болон Tenant өгөгдөл хадгалагдсан. Шалгалтаар шинэ Tenant үүсгээгүй.
- Browser-ийн дүрслэл болон production deployment шалгаагүй; автомат тест нэмээгүй.
