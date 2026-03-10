# S13G - Sistema de Processamento de Documentos Fiscais Brasileiros

Um sistema backend robusto e orientado a eventos, construído com ASP.NET Core para processar, armazenar e gerenciar documentos fiscais XML brasileiros (NFe, CTe, NFSe). O sistema implementa os princípios de Clean Architecture com padrões CQRS, garantindo escalabilidade, manutenibilidade e fluxos de trabalho confiáveis baseados em eventos.

## 🌐 Ambiente em Produção

A API está implantada e em execução em:

| Recurso | URL |
|---|---|
| API Base | https://s13g.onrender.com |
| Swagger UI | https://s13g.onrender.com/swagger |
| Health Check | https://s13g.onrender.com/health |

**Infraestrutura:**
- **API**: Render (plano gratuito — cold start após 15 min de inatividade)
- **Banco de dados**: Render PostgreSQL (plano gratuito)
- **Message Broker**: CloudAMQP — Little Lemur (plano gratuito)

> A primeira requisição após um período de inatividade pode levar ~30–60s devido ao cold start do plano gratuito.

---

## 🚀 Funcionalidades

- **Ingestão de Documentos**: Upload e processamento de documentos fiscais XML brasileiros com validação automática
- **Arquitetura Orientada a Eventos**: Processamento assíncrono com mensageria via RabbitMQ
- **Operações Idempotentes**: Prevenção de processamento duplicado de documentos usando chaves de idempotência
- **API RESTful**: API REST completa com documentação OpenAPI/Swagger
- **Persistência de Dados**: PostgreSQL com Entity Framework Core para armazenamento confiável de dados
- **Testes Abrangentes**: Testes unitários e de integração com Testcontainers
- **Clean Architecture**: Separação de responsabilidades com camadas de Domain, Application, Infrastructure e API

## 🏗️ Arquitetura

O sistema segue a Clean/Onion Architecture com fatias verticais:

```
┌─────────────────────┐
│   ASP.NET Core API  │
│   + Swagger UI      │
└─────────┬───────────┘
          │
┌─────────┴───────────┐
│ Application Layer   │
│ - CQRS (MediatR)    │
│ - FluentValidation  │
└─────────┬───────────┘
          │
┌─────────┴───────────┐
│ Domain Layer        │
│ - Entities          │
│ - Business Rules    │
└─────────┬───────────┘
          │
┌─────────┴───────────┐
│ Infrastructure      │
│ - PostgreSQL (EF)   │
│ - RabbitMQ          │
│ - XML Parser        │
└─────────────────────┘
```

### Componentes Principais

- **Camada API**: Controllers REST com validação automática de requisições
- **Camada Application**: Handlers de Command/Query usando MediatR
- **Camada Domain**: Entidades de negócio e eventos de domínio
- **Camada Infrastructure**: Dependências externas (banco de dados, mensageria, processamento de XML)

## 📋 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [RabbitMQ 3.12+](https://www.rabbitmq.com/download.html)
- [Docker](https://www.docker.com/) (para executar os testes com Testcontainers)

## 🛠️ Instalação e Configuração

### Escolha o Método de Configuração

Escolha uma das três opções a seguir:

---

## **Opção A: Docker Compose (Recomendado — Sem Dependências para Instalar)**

Ideal se você já tem o Docker instalado. Os serviços são executados em containers isolados.

```bash
bash scripts/setup.sh
```

O script irá:
- ✅ Verificar Docker e Docker Compose
- ✅ Iniciar PostgreSQL e RabbitMQ em containers
- ✅ Aguardar os serviços ficarem saudáveis
- ✅ Restaurar os pacotes NuGet
- ✅ Compilar a solução
- ✅ Aplicar as migrations do banco de dados

Em seguida, inicie a API:
```bash
cd src/Api && dotnet run
```

**Requisitos:** Docker & Docker Compose

> Um `Dockerfile` na raiz do repositório também é utilizado para implantações em produção
> (ex.: Render). Ele expõe a porta `10000` (`ASPNETCORE_URLS=http://+:10000`), que é
> a porta padrão esperada pela plataforma Render. O `dotnet run` local ainda utiliza
> a porta `5000` via `launchSettings.json`.

---

## **Opção B: Instalação Local**

Instale o PostgreSQL e o RabbitMQ diretamente na sua máquina. Escolha seu sistema operacional:

### **macOS (com Homebrew)**

```bash
bash scripts/setup-local-macos.sh
```

Em seguida, inicie a API:
```bash
cd src/Api
dotnet run
```

**O que o script faz:**
- ✅ Instala o PostgreSQL 15 via Homebrew
- ✅ Instala o RabbitMQ via Homebrew
- ✅ Inicia ambos os serviços
- ✅ Cria o banco de dados
- ✅ Aplica as migrations

**Requisitos:** Homebrew

**Gerenciar serviços:**
```bash
brew services start postgresql@15      # Iniciar banco de dados
brew services start rabbitmq           # Iniciar RabbitMQ
brew services stop postgresql@15       # Parar banco de dados
brew services stop rabbitmq            # Parar RabbitMQ
```

---

### **Linux (Ubuntu/Debian)**

```bash
sudo bash scripts/setup-local-linux.sh
```

Em seguida, inicie a API:
```bash
cd src/Api
dotnet run
```

**O que o script faz:**
- ✅ Instala o PostgreSQL 15 via apt
- ✅ Instala o RabbitMQ via apt
- ✅ Inicia ambos os serviços
- ✅ Cria o banco de dados
- ✅ Aplica as migrations

**Requisitos:** acesso sudo, apt-get

**Gerenciar serviços:**
```bash
sudo systemctl start postgresql         # Iniciar banco de dados
sudo systemctl start rabbitmq-server    # Iniciar RabbitMQ
sudo systemctl stop postgresql          # Parar banco de dados
sudo systemctl stop rabbitmq-server     # Parar RabbitMQ
```

---

### **Windows (com Chocolatey)**

Execute o PowerShell como Administrador:

```powershell
.\scripts\setup-local-windows.ps1
```

Em seguida, inicie a API:
```bash
cd src/Api
dotnet run
```

**O que o script faz:**
- ✅ Instala o Chocolatey (se necessário)
- ✅ Instala o PostgreSQL 15
- ✅ Instala o RabbitMQ
- ✅ Inicia ambos os serviços
- ✅ Cria o banco de dados
- ✅ Aplica as migrations

**Requisitos:** acesso de Administrador

**Gerenciar serviços:**
1. Abra o Gerenciador de Serviços (services.msc)
2. Localize os serviços PostgreSQL e RabbitMQ
3. Clique com o botão direito → Iniciar/Parar

---

## **Opção C: Configuração Manual (Avançado)**

Se os scripts não funcionarem no seu ambiente, você pode instalar tudo manualmente:

#### 1. Clonar o Repositório

```bash
git clone https://github.com/your-username/S13G.git
cd S13G
```

#### 2. Instalar os Serviços

**PostgreSQL:**
- Baixe em https://www.postgresql.org/download/
- Instale o PostgreSQL 15 ou superior
- Certifique-se de que está em execução na porta 5432

**RabbitMQ:**
- Baixe em https://www.rabbitmq.com/download.html
- Instale o RabbitMQ 3.12 ou superior
- Certifique-se de que está em execução na porta 5672

#### 3. Criar o Banco de Dados

```bash
# No macOS/Linux
psql -U postgres -c "CREATE DATABASE s13g;"

# No Windows (no PostgreSQL Command Line)
CREATE DATABASE s13g;
```

#### 4. Atualizar as Strings de Conexão (se necessário)

Edite `src/Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=s13g;Username=postgres;Password=YOUR_PASSWORD"
  },
}
```

> **Dica:** a API também respeita as variáveis de ambiente `DB_USER` e `DB_PASS`
> para sobrescrever o usuário e a senha sem precisar editar o arquivo. Em instalações
> via Homebrew no macOS, o banco de dados usa por padrão o usuário do sistema operacional
> sem senha — defina `DB_USER=$(whoami)` antes de iniciar a API.
>
> Para desenvolvimento local, você também pode criar `src/Api/appsettings.Development.json`
> (ignorado pelo .gitignore) com sua string de conexão local. O `launchSettings.json` garante
> que `ASPNETCORE_ENVIRONMENT=Development` seja definido automaticamente ao usar `dotnet run`,
> de modo que as configurações de desenvolvimento são carregadas sem nenhuma configuração
> extra de variável de ambiente.
>
> A API também aceita URIs PostgreSQL no estilo Render (`postgresql://user:pass@host/db`)
> em `ConnectionStrings__DefaultConnection` — elas são convertidas para o formato
> key=value do Npgsql na inicialização.

#### 5. Aplicar as Migrations do Banco de Dados

As migrations podem ser aplicadas de duas formas:

- **Recomendado:** execute a partir do projeto `Infrastructure`, que inclui a design-time factory. Isso evita carregar o host ASP.NET completo e contorna erros de DI/startup.

```bash
cd src/Infrastructure
# mantém artefatos de build existentes, mais rápido
dotnet ef database update --no-build --project Infrastructure.csproj
```

- **Alternativa:** execute a partir do projeto API. Nesse caso, defina a variável de ambiente com a string de conexão (ex.: use `${{USER}}` como usuário do banco em ambientes macOS/Homebrew) ou certifique-se de que o role `postgres` existe no servidor.

```bash
cd src/Api
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=s13g;Username=${USER};"
# opcional: inclua a senha se você configurou uma

# em seguida, execute

```

Se você receber um erro sobre as ferramentas do Entity Framework, instale-as:
```bash
dotnet tool install --global dotnet-ef --version 9.0.3
```

#### 6. Compilar e Executar

```bash
dotnet restore
dotnet build
cd src/Api
dotnet run
```

---

## Executando a API

Após concluir a configuração com qualquer uma das opções acima, inicie a API:

```bash
cd src/Api
dotnet run
```

A API estará disponível em:
- **HTTP**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

Se você tiver um certificado de desenvolvimento confiável instalado, o servidor também
vinculará HTTPS na porta 5001. Para configurar isso, execute:

```bash
# instalar e confiar no certificado de desenvolvimento (no macOS será solicitada aprovação)
dotnet dev-certs https --trust
```

Em seguida, você também pode acessar `https://localhost:5001/swagger`.

## Configuração

Todas as configurações estão em `src/Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=s13g;Username=postgres;Password=postgres"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "documents",
    "QueueName": "documents.processed",
    "DeadLetterExchange": "documents.dlx",
    "RetryCount": 5,
    "RetryInitialDelayMs": 500
  }
}
```

Atualize esses valores caso seus serviços estejam em hosts ou portas diferentes.

## 📖 Uso da API

### Endpoints

#### Upload de Documento
```http
POST /documents/upload
Content-Type: multipart/form-data

Form Data:
- file: [arquivo XML]
- Idempotency-Key: [header] chave-unica-para-requisicao
```

#### Listar Documentos
```http
GET /documents?page=1&pageSize=20&cnpj=12345678901234&uf=SP&fromDate=2024-01-01&toDate=2024-12-31
```

#### Obter Documento por ID
```http
GET /documents/{id}
```

#### Atualizar Documento
```http
PUT /documents/{id}
Content-Type: application/json

{
  "state": "SP",
  "status": "Processed"
}
```

#### Excluir Documento
```http
DELETE /documents/{id}
```

#### Health Check
```http
GET /health
```

Retorna um relatório JSON com o status dos serviços `postgresql` e `rabbitmq`:
```json
{
  "status": "Healthy",
  "services": {
    "postgresql": { "status": "Healthy", "error": null },
    "rabbitmq":   { "status": "Healthy", "error": null }
  }
}
```

### Tipos de Documentos Suportados

- **NFe**: Nota Fiscal Eletrônica
- **CTe**: Conhecimento de Transporte Eletrônico
- **NFSe**: Nota Fiscal de Serviços Eletrônica

## 🧪 Testes

### Testes Unitários

```bash
cd tests/Unit
dotnet test
```

### Testes de Integração

Os testes de integração utilizam Testcontainers para PostgreSQL e RabbitMQ:

```bash
cd tests/Integration
dotnet test
```

Observação: os testes de integração requerem que o Docker esteja em execução.

## 🔧 Desenvolvimento

### Estrutura do Projeto

```
S13G/
├── src/
│   ├── Api/                 # ASP.NET Core Web API
│   ├── Application/         # Handlers CQRS e serviços
│   ├── Domain/              # Entidades, eventos e interfaces
│   └── Infrastructure/      # Dependências externas
├── tests/
│   ├── Unit/                # Testes unitários
│   └── Integration/         # Testes de integração
├── ARCHITECTURE.md          # Documentação detalhada da arquitetura
└── S13G.sln                # Arquivo de solução
```

### Tecnologias Utilizadas

- **ASP.NET Core (.NET 10)**: Framework web
- **Entity Framework Core**: ORM para PostgreSQL
- **MediatR**: Implementação de CQRS
- **FluentValidation**: Validação de requisições
- **RabbitMQ.Client**: Filas de mensagens
- **Polly**: Políticas de resiliência
- **Swashbuckle**: Documentação OpenAPI
- **NUnit**: Framework de testes
- **Testcontainers**: Containers para testes de integração

### Saúde das Dependências

O projeto tem como alvo o .NET 10 com diversas bibliotecas de terceiros. Eventualmente, pacotes podem receber avisos de segurança; você pode ver alertas durante o `dotnet build` como:

```
warning NU1903: Package 'Npgsql' 8.0.0 has a known high severity vulnerability
```

Para manter as dependências atualizadas:

1. Execute `dotnet list package --vulnerable` para ver os pacotes com avisos.
2. Edite o(s) arquivo(s) `.csproj` correspondente(s) e atualize o atributo `Version` para uma versão corrigida.
3. Execute `dotnet restore` e recompile.

O repositório atual já foi atualizado para usar versões corrigidas e inclui referências explícitas onde necessário; você pode ajustá-las conforme novas correções forem lançadas.

## 🔒 Tratamento de Dados Sensíveis

| Preocupação | Abordagem |
|---|---|
| **Credenciais do banco de dados** | Nunca definidas no código — lidas das variáveis de ambiente `DB_USER` / `DB_PASS` na inicialização; `appsettings.json` contém apenas um valor de placeholder para localhost. |
| **Credenciais do RabbitMQ** | Lidas de `appsettings.json` / variáveis de ambiente; `guest/guest` é o padrão para uso local apenas, podendo ser substituído via variáveis de ambiente antes do uso em produção. |
| **CNPJ** | Armazenado como texto simples internamente (necessário para filtros e busca), mas a API não expõe o CNPJ em mensagens de erro ou logs. |
| **XML bruto** | O XML fiscal completo é armazenado na tabela `FiscalDocuments` pois é legalmente exigido para fins de auditoria. O acesso é restrito a requisições autenticadas (camada de autenticação a ser adicionada). |
| **Chaves de idempotência** | Armazenadas como hash SHA-256 (`DocumentKeys.KeyHash`), não o valor original. |
| **Strings de conexão** | Nunca registradas em logs; `appsettings.json` é excluído do controle de versão via `.gitignore` em ambientes de produção. |

> Em ambiente de produção: utilize um gerenciador de segredos (Azure Key Vault, AWS Secrets Manager ou HashiCorp Vault), habilite TLS nas conexões com PostgreSQL e RabbitMQ, e adicione autenticação via JWT ou API-key na camada de API.

---

## Melhorias Possíveis

Com mais tempo disponível, as seguintes melhorias seriam priorizadas:

| Área | Melhoria |
|---|---|
| **Autenticação e autorização** | Adicionar middleware JWT bearer ou API-key; restringir endpoints por papel (role). |
| **Armazenamento do XML bruto** | Mover o XML bruto para fora da tabela principal e armazená-lo em object storage (S3/Azure Blob) — manter apenas uma URL de referência para reduzir o tamanho das linhas no banco de dados em escala. |
| **Validação XSD da SEFAZ** | Carregar os arquivos XSD oficiais da SEFAZ no `XmlSchemaValidator` para validação completa de schema, em vez de apenas verificações estruturais. |
| **Padrão Outbox** | Substituir a publicação direta no RabbitMQ por um outbox transacional (gravar o evento no banco de dados na mesma transação, retransmitindo de forma assíncrona) para eliminar a janela de inconsistência entre publicação e persistência. |
| **Escalonamento do Consumer** | Extrair o `RabbitMqConsumer` para um worker service separado, permitindo que ele seja escalado independentemente da API. |
| **Testes de carga** | Adicionar scripts NBomber ou k6 para medir a taxa de ingestão e a latência de consultas sob carga concorrente. |
| **Testes de arquitetura** | Adicionar regras NetArchTest para garantir as restrições de dependência entre camadas (ex.: Domain não deve referenciar Infrastructure). |
| **Observabilidade** | Adicionar logging estruturado (Serilog), rastreamento distribuído (OpenTelemetry) e métricas (Prometheus/Grafana). |
| **Soft delete** | Substituir o `DELETE` físico por um timestamp `DeletedAt` para preservar o histórico de auditoria. |

---

## 🤝 Contribuindo

1. Faça um fork do repositório
2. Crie uma branch para sua funcionalidade (`git checkout -b feature/minha-funcionalidade`)
3. Faça o commit das suas alterações (`git commit -m 'Add amazing feature'`)
4. Envie para a branch (`git push origin feature/minha-funcionalidade`)
5. Abra um Pull Request

### Padrões de Código

- Siga as convenções de codificação do C#
- Use nomes significativos para variáveis e métodos
- Adicione comentários de documentação XML
- Escreva testes para novas funcionalidades
- Certifique-se de que todos os testes passam antes de submeter o PR

## 📄 Licença

Este projeto está licenciado sob a Licença MIT — consulte o arquivo [LICENSE](LICENSE) para mais detalhes.

## 📚 Recursos Adicionais

- [Especificação dos Documentos Fiscais Brasileiros](https://www.nfe.fazenda.gov.br/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Padrão CQRS](https://martinfowler.com/bliki/CQRS.html)
- [Documentação do MediatR](https://github.com/jbogard/MediatR)

## 🆘 Suporte

Para dúvidas ou problemas, abra uma issue no GitHub ou entre em contato com a equipe de desenvolvimento.
