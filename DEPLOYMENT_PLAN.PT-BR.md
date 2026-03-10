# Referência de Deploy — S13G

## Arquitetura

```
GitHub (código-fonte + CI/CD)
    │
    ├── GitHub Actions CI ──► build + testes unitários + testes de integração (a cada push/PR)
    │
    └── GitHub Actions CD ──► dispara o deploy no Render (somente main, após CI passar)
                                        │
                              ┌─────────┴──────────┐
                              ▼                    ▼
                    Render Web Service     Render PostgreSQL
                    (.NET 10 API)          (gerenciado, mesma região)
                              │
                              └──────────────────────►  CloudAMQP
                                                        (RabbitMQ gerenciado)
```

## Serviços

| Serviço | Provedor | Plano |
|---|---|---|
| Web API | Render | Gratuito |
| PostgreSQL | Render | Gratuito |
| RabbitMQ | CloudAMQP | Little Lemur (gratuito) |
| CI/CD | GitHub Actions | Gratuito (repositório público) |

### Limitações do plano gratuito

| Limitação | Impacto |
|---|---|
| O serviço web é desligado após 15 min de inatividade | A primeira requisição após ocioso leva ~30–60s (cold start) |
| PostgreSQL excluído após 90 dias | Recriar o banco ou migrar para plano pago caso os dados precisem persistir |
| CloudAMQP limitado a 1M de mensagens/mês | Suficiente para uso em demonstração |

---

## Variáveis de Ambiente (serviço web no Render)

| Variável | Valor | Observações |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | |
| `ASPNETCORE_URLS` | `http://+:10000` | O Render utiliza a porta 10000 por padrão |
| `ConnectionStrings__DefaultConnection` | String de conexão interna do banco no Render | Copiar do painel do Render PostgreSQL — o formato URI é aceito |
| `RabbitMq__HostName` | Hostname do CloudAMQP | Extraído da URL AMQP |
| `RabbitMq__UserName` | Usuário do CloudAMQP | Extraído da URL AMQP |
| `RabbitMq__Password` | Senha do CloudAMQP | Extraído da URL AMQP |
| `RabbitMq__VirtualHost` | Vhost do CloudAMQP | Último segmento de caminho da URL AMQP |
| `RabbitMq__Port` | `5672` | |

> O ASP.NET Core mapeia `__` (sublinhado duplo) para chaves JSON aninhadas, portanto `RabbitMq__HostName` sobrescreve `appsettings.json → RabbitMq → HostName`.

### Segredos do GitHub (Configurações → Secrets → Actions)

| Segredo | Valor |
|---|---|
| `RENDER_DEPLOY_HOOK_URL` | URL do deploy hook nas configurações do serviço web no Render |

---

## Fluxo de Deploy

```
Desenvolvedor faz push para main
        │
        ▼
GitHub Actions: ci.yml
  ├── dotnet build
  ├── dotnet test (unit)
  └── dotnet test (integration) [Testcontainers]
        │
        ▼ (todos verdes)
GitHub Actions: cd.yml
  └── POST para a URL do deploy hook do Render
        │
        ▼
Render constrói a imagem Docker e sobe o novo container
        │
        ▼
EF Core executa as migrations na inicialização
        │
        ▼
/health check aprovado → deploy concluído
```

---

## Rollback

O Render mantém os deploys anteriores disponíveis. Para reverter:

- Acesse o painel do Render → serviço web → **Deploys** → clique em qualquer deploy anterior → **Redeploy**.
