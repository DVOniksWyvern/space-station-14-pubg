FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN apt-get update && apt-get install -y locales

# Locale
RUN sed -i -e \
's/# ru_RU.UTF-8 UTF-8/ru_RU.UTF-8 UTF-8/' /etc/locale.gen \
&& locale-gen

ENV LANG ru_RU.UTF-8
ENV LANGUAGE ru_RU:ru
ENV LC_LANG ru_RU.UTF-8
ENV LC_ALL ru_RU.UTF-8
# +Timezone
ENV TZ Europe/Moscow
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
WORKDIR /app
EXPOSE 1212/tcp
EXPOSE 1212/udp

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
dotnet build Content.Packaging --configuration Release
RUN --mount=type=cache,target=/root/.nuget/packages \
dotnet run --project Content.Packaging server --hybrid-acz --platform linux-x64

FROM base AS final
WORKDIR /app
COPY --from=build /src/release/SS14.Server_linux-x64.zip .
RUN apt-get install -y unzip
RUN unzip SS14.Server_linux-x64.zip -d /app
RUN rm SS14.Server_linux-x64.zip
VOLUME [ "/data" ]
RUN chmod +x /app/Robust.Server
ENTRYPOINT ["/app/Robust.Server", "--data-dir", "/data", "--config-file", "/app/Resources/ConfigPresets/Backmen/mapserver.toml" ]

