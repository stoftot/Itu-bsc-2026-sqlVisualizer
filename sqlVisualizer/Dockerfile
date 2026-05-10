FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY ./sqlVisualizer.sln ./
COPY ./visualizer/*.csproj ./visualizer/
COPY ./visualizer.Infrastructure/*.csproj ./visualizer.Infrastructure/
COPY ./visualizer.service/*.csproj ./visualizer.service/
COPY ./common.data/*.csproj ./common.data/
COPY ./sql.executor/*.csproj ./sql.executor/
COPY ./sql.parsing/*.csproj ./sql.parsing/
COPY ./table.generation/*.csproj ./table.generation/
COPY ./animation.generation/*.csproj ./animation.generation/

WORKDIR /app/visualizer

RUN dotnet restore --no-cache

WORKDIR /app

COPY ./visualizer/ ./visualizer/
COPY ./visualizer.Infrastructure/ ./visualizer.Infrastructure/
COPY ./visualizer.service/ ./visualizer.service/
COPY ./common.data/ ./common.data/
COPY ./sql.executor/ ./sql.executor/
COPY ./sql.parsing/ ./sql.parsing/
COPY ./table.generation/ ./table.generation/
COPY ./animation.generation/ ./animation.generation/

WORKDIR /app/visualizer

RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app/visualizer

COPY --from=build /out ./

RUN mkdir -p ./data

EXPOSE 80

ENTRYPOINT ["dotnet", "visualizer.dll"]