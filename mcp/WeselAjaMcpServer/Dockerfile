FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY WeselAjaMcpServer.csproj ./

RUN dotnet restore WeselAjaMcpServer.csproj

COPY . .

RUN dotnet publish WeselAjaMcpServer.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV MCP_TRANSPORT=http
ENV MCP_HTTP_URL=http://0.0.0.0:3001

COPY --from=build /app/publish .

RUN mkdir -p /app/generated \
    && chown -R $APP_UID:$APP_UID /app/generated

USER $APP_UID

EXPOSE 3001

ENTRYPOINT ["dotnet", "WeselAjaMcpServer.dll"]