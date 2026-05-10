FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY ["sqlVisualizer.sln", "./"]
COPY ["visualizer/visualizer.csproj", "visualizer/"]
COPY ["visualizer.Infrastructure/visualizer.Infrastructure.csproj", "visualizer.Infrastructure/"]
COPY ["visualizer.service/visualizer.service.csproj", "visualizer.service/"]
COPY ["common.data/common.data.csproj", "common.data/"]
COPY ["sql.executor/sql.executor.csproj", "sql.executor/"]
COPY ["sql.parsing/sql.parsing.csproj", "sql.parsing/"]
COPY ["table.generation/table.generation.csproj", "table.generation/"]
COPY ["animation.generation/animation.generation.csproj", "animation.generation/"]

RUN dotnet restore "visualizer/visualizer.csproj" --no-cache

COPY ["visualizer/", "visualizer/"]
COPY ["visualizer.Infrastructure/", "visualizer.Infrastructure/"]
COPY ["visualizer.service/", "visualizer.service/"]
COPY ["common.data/", "common.data/"]
COPY ["sql.executor/", "sql.executor/"]
COPY ["sql.parsing/", "sql.parsing/"]
COPY ["table.generation/", "table.generation/"]
COPY ["animation.generation/", "animation.generation/"]

RUN dotnet publish "visualizer/visualizer.csproj" -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app/visualizer

COPY --from=build /out ./

RUN mkdir -p /app/visualizer/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "visualizer.dll"]
