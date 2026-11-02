FROM mcr.microsoft.com/dotnet/sdk:10.0.402@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /src

COPY global.json ./
COPY backend/Directory.Build.props backend/AiAgent.slnx ./backend/
COPY backend/src/AiAgent.Domain/*.csproj backend/src/AiAgent.Domain/packages.lock.json ./backend/src/AiAgent.Domain/
COPY backend/src/AiAgent.Application/*.csproj backend/src/AiAgent.Application/packages.lock.json ./backend/src/AiAgent.Application/
COPY backend/src/AiAgent.Infrastructure/*.csproj backend/src/AiAgent.Infrastructure/packages.lock.json ./backend/src/AiAgent.Infrastructure/
COPY backend/src/AiAgent.Api/*.csproj backend/src/AiAgent.Api/packages.lock.json ./backend/src/AiAgent.Api/
RUN dotnet restore backend/AiAgent.slnx --locked-mode

COPY backend/ ./backend/
RUN dotnet publish backend/src/AiAgent.Api/AiAgent.Api.csproj \
    --configuration Release --no-restore --output /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS runtime
WORKDIR /app
RUN mkdir -p /var/lib/ai-agent/keys \
    && chown -R app:app /var/lib/ai-agent \
    && chmod -R 700 /var/lib/ai-agent
COPY --from=build /out/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
ENV AI_AGENT_DATA_DIR=/var/lib/ai-agent
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "AiAgent.Api.dll"]
