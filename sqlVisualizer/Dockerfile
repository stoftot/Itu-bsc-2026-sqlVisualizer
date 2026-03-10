FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY ./visualizer/*.csproj ./visualizer/

WORKDIR /app/visualizer

RUN dotnet restore --no-cache

COPY ./visualizer/ ./

RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 

WORKDIR /app/visualizer

COPY --from=build /out ./

RUN mkdir -p ./data

EXPOSE 80

ENTRYPOINT ["dotnet", "visualizer.dll"]
