FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install additional packages
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        curl \
        wget \
        unzip \
        git \
        make \
        jq \
        ca-certificates \
        gnupg \
        lsb-release \
        sudo \
    && apt-get autoremove -y && apt-get clean -y

# Install Docker CLI
RUN curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update \
    && apt-get -y install --no-install-recommends docker-ce-cli

# Install Terraform
RUN wget -O- https://apt.releases.hashicorp.com/gpg | gpg --dearmor | tee /usr/share/keyrings/hashicorp-archive-keyring.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | tee /etc/apt/sources.list.d/hashicorp.list \
    && apt-get update \
    && apt-get -y install terraform

# Install AWS CLI v2
RUN curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip" \
    && unzip awscliv2.zip \
    && ./aws/install \
    && rm -rf awscliv2.zip aws

# Install Node.js (for additional tooling)
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
    && apt-get install -y nodejs

# Create vscode user
RUN groupadd --gid 1000 vscode \
    && useradd --uid 1000 --gid vscode --shell /bin/bash --create-home vscode \
    && echo vscode ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/vscode \
    && chmod 0440 /etc/sudoers.d/vscode

# Add vscode user to docker group
RUN groupadd docker || true \
    && usermod -aG docker vscode

# Set up .NET tools
USER vscode
RUN dotnet tool install --global dotnet-ef \
    && dotnet tool install --global dotnet-outdated-tool \
    && dotnet tool install --global dotnet-reportgenerator-globaltool

# Add .NET tools to PATH
ENV PATH="${PATH}:/home/vscode/.dotnet/tools"

# Set working directory
WORKDIR /workspace

# Switch back to vscode user
USER vscode
