FROM microsoft/dotnet:3.0-sdk AS build
WORKDIR /app
RUN apt-get update && apt-get install -y libkrb5-dev zlib1g-dev clang-3.9
COPY . .
RUN dotnet restore --runtime linux-x64
RUN dotnet publish -c Release -r linux-x64 -o out

FROM scratch AS runtime
WORKDIR /app
COPY --from=build /app/out/* /app/
ENTRYPOINT ["/app/HelloWorld"] 