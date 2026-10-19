# ai-agent — хөгжүүлэлтийн суурь орчин

Одоогоор application код, React scaffold, API төсөл, database schema болон migration үүсгээгүй.

## Орчны сонголт

- Windows 11, WSL2, Ubuntu 24.04.3 LTS.
- Одоо байгаа Node.js 22.23.2, npm 10.9.8; `.nvmrc` хувилбарыг тогтооно.
- .NET SDK 10.0.402, ASP.NET Core runtime 10.0.13: зөвхөн энэ төслийн `.tools/dotnet` дотор. Системийн .NET 8-ыг хадгална.
- Docker Engine 28.5.1 болон Compose 2.40.2: одоо ажиллаж байгаа Ubuntu daemon ашиглана.
- PostgreSQL 17.11: энэ төслийн Compose service; зөвхөн `127.0.0.1:5433` дээр нээлттэй. Image digest-ийг Compose файлд тогтоосон.
- Git: тусдаа local repository, `main` салбар. GitHub SSH нэвтрэлт шалгагдсан; remote repository хараахан холбогоогүй.
- EF Core CLI 10.0.13: `.tools/ef` дотор суусан. EF Core, Npgsql provider: backend төсөл үүсгэх үед хоёуланг нь 10.x хувилбараар нэмнэ.

## Terminal бэлтгэх

```bash
cd /home/hishigtogtokh/ai-agent
nvm use
export DOTNET_ROOT="$PWD/.tools/dotnet"
export DOTNET_CLI_HOME="$PWD/.tools/cli-home"
export NUGET_PACKAGES="$PWD/.tools/nuget"
export PATH="$DOTNET_ROOT:$PWD/.tools/ef:$PATH"
dotnet --version
dotnet-ef --version
```

Эдгээр export нь тухайн terminal-д үйлчилнэ. Системийн default `dotnet` нь .NET 8 хэвээр байна.

Шинэ clone дээр SDK болон EF CLI-г дахин бэлтгэх шаардлагатай (`.tools` нь Git-д орохгүй):

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/ai-agent-dotnet-install.sh
bash /tmp/ai-agent-dotnet-install.sh --version 10.0.402 --install-dir "$PWD/.tools/dotnet" --no-path
# Дээрх terminal export-уудыг ажиллуулсны дараа:
dotnet tool install dotnet-ef --tool-path .tools/ef --version 10.0.13
```

## Database удирдах

Анхны локал `.env` нь үүссэн, нууц үг санамсаргүй утгатай, файл 0600 эрхтэй. Утгыг Git болон чатад нийтлэхгүй.
Шинэ clone дээр `.env.example`-ийг `.env` болгон хуулж, тусдаа нууц үг тохируулна.

```bash
docker compose up -d --wait
docker compose ps
docker compose exec -T db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "SELECT version();"'
docker compose stop
```

Холболтын host: `127.0.0.1`, port: `5433`, database/user: `ai_agent`. Password нь локал `.env` дотор байна.
Өгөгдөл `ai-agent_postgres_data` named volume-д хадгалагдана. `docker compose down -v` нь өгөгдлийг устгадаг тул энгийн зогсоолтод ашиглахгүй.
Бусад төслийн PostgreSQL 15 нь 5432 дээр хэвээр ажиллана.

## Дараагийн кодын шат

1. `frontend/` дотор React + TypeScript + Vite төсөл үүсгэх.
2. `backend/` дотор ASP.NET Core 10 API төсөл үүсгэх.
3. EF Core 10.x, Npgsql EF provider 10.x нэмэх; connection string-ийг локал secret-ээр тохируулах.
4. Frontend 5173, API 5000 портыг тогтоож, локал CORS болон анхны migration-ийг шалгах.
5. GitHub remote repository-г холбох.

Эдгээр application ажлууд одоогийн орчин бүрдүүлэх хүрээнд хийгдээгүй.

## Шалгалтын дүн — 2026-10-19

- WSL2 kernel, Ubuntu 24.04, Windows build 26200, Node/npm, Git болон Docker/Compose хувилбаруудыг бодитоор шалгасан.
- .NET SDK болон ASP.NET Core runtime ажиллаж байгааг CLI-ээр шалгасан; application build хараахан хийгээгүй.
- PostgreSQL healthcheck болон TCP/password холболтоор `SELECT` амжилттай. Контейнерийг дахин үүсгэсний дараа холболтыг дахин баталгаажуулсан.
- Одоо байгаа MangaWeb PostgreSQL 15, Redis контейнерүүд ажиллаж хэвээр.
- GitHub SSH authentication амжилттай. Энэ төслийн remote, push хийгдээгүй.
- Windows VS Code-д WSL, C# Dev Kit, C#, ESLint суусан; WSL editor session болон debugger-ийг UI дээр шалгаагүй.
- `.env` файл 0600 эрхтэй; `.env`, `.tools` нь Git-д ignored.
- EF/Npgsql-ийн application integration, migration, frontend/browser шалгалт нь дараагийн кодын шатанд хамаарна.
