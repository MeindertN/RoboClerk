ARG REPO=mcr.microsoft.com/dotnet/runtime-deps
FROM $REPO:6.0.9-alpine3.16-amd64

# https://github.com/dotnet/dotnet-docker/tree/e54047fa161d36841ccd5b3290d1765adb861635/src/runtime
# .NET globalization APIs will use invariant mode by default because DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true is set
# by the base runtime-deps image. See https://aka.ms/dotnet-globalization-alpine-containers for more information.

# environment variables
ENV DOTNET_VERSION=6.0.9
ENV PYTHONUNBUFFERED=1
ENV PANDOC_VERSION 2.16.1
ENV PANDOC_DOWNLOAD_URL https://github.com/jgm/pandoc/releases/download/$PANDOC_VERSION/pandoc-$PANDOC_VERSION-linux-amd64.tar.gz

# Install .NET Runtime
RUN wget -O dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Runtime/$DOTNET_VERSION/dotnet-runtime-$DOTNET_VERSION-linux-musl-x64.tar.gz \
    && dotnet_sha512='e3eb4804dddbc34be37e3d984c13a668605ef7eea6b2daca95ea3fa7ca51b2fe4845df1ad0d343de9075796bf0d4474d7e14e5b3b4b515005d0e24ec53237ad3' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -oxzf dotnet.tar.gz -C /usr/share/dotnet \
    && rm dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && apk add --update --no-cache python3 && ln -sf python3 /usr/bin/python \
    && python3 -m ensurepip \
    && python3 -m pip install --no-cache --upgrade pip \
    && pip3 install --no-cache --upgrade python-docx \
	&& pip3 install --no-cache --upgrade openpyxl \
    && wget $PANDOC_DOWNLOAD_URL \
    && tar -xzf pandoc-$PANDOC_VERSION-linux-amd64.tar.gz \
    && cp /pandoc-$PANDOC_VERSION/bin/pandoc /usr/bin/ \
    && rm -rf pandoc-$PANDOC_VERSION \
    && rm pandoc-$PANDOC_VERSION-linux-amd64.tar.gz \
	&& apk add --no-cache asciidoctor \
	&& apk --update add --no-cache ruby ruby-io-console ruby-irb ruby-json ruby-rake ruby-rdoc \
    && gem install asciidoctor-kroki 

# Install RoboClerk itself
ADD Publish /home/RoboClerk