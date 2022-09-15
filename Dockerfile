ARG DOTNET_VERSION=6.0-alpine

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime-deps
ARG APP_PORT=8080
RUN apk add --no-cache icu-libs
WORKDIR /app/

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT false
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8

ENV ASPNETCORE_URLS=http://+:${APP_PORT}
ENV COMPlus_EnableDiagnostics=0
EXPOSE ${APP_PORT}

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
COPY . /app/
WORKDIR /app/
RUN dotnet publish -c Release -o /release/

FROM runtime-deps
ARG USER_ID
ENV DISPLAY=:0
COPY --from=build /release/ .
USER ${USER_ID:-65534}
CMD [ "./NeuroTemnov"]
