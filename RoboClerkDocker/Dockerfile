ARG REPO=mcr.microsoft.com/dotnet/runtime
FROM $REPO:6.0-alpine-amd64

# https://github.com/dotnet/dotnet-docker/tree/e54047fa161d36841ccd5b3290d1765adb861635/src/runtime
# .NET globalization APIs will use invariant mode by default because DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true is set
# by the base runtime-deps image. See https://aka.ms/dotnet-globalization-alpine-containers for more information.

# environment variables
ENV PYTHONUNBUFFERED=1
ENV PANDOC_VERSION 2.16.1
ENV PANDOC_DOWNLOAD_URL https://github.com/jgm/pandoc/releases/download/$PANDOC_VERSION/pandoc-$PANDOC_VERSION-linux-amd64.tar.gz

RUN apk add --update --no-cache python3 && ln -sf python3 /usr/bin/python \
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
	&& apk --update add --no-cache ruby ruby-io-console ruby-irb ruby-json ruby-rake ruby-rdoc git \
    && gem install asciidoctor-kroki 

# Install RoboClerk itself
ADD Publish /home/RoboClerk
WORKDIR /home/RoboClerk
ENV PATH="/home/RoboClerk:${PATH}"
RUN chmod +x /home/RoboClerk/scaffold
RUN chmod +x /home/RoboClerk/generate