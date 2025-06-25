FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder

RUN dotnet tool install -g dotnet-script


WORKDIR /work
COPY ./recv-mail.csx  ./

RUN /root/.dotnet/tools/dotnet-script publish \
        --configuration Release \
        --output /work/publish \
        recv-mail.csx


FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /app
COPY --chmod=+x --from=builder  /work/publish  /app

CMD ["/app/recv-mail"]
