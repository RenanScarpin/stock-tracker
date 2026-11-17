# StockTracker

Aplicação de console em **C# / .NET 10** que monitora a cotação de um ativo da B3 e envia alertas por e-mail quando o preço ultrapassa limites definidos pelo usuário.

O projeto também oferece um **modo de simulação**, com preços fictícios e e-mails exibidos no terminal, para que seja possível avaliar o funcionamento sem configurar serviços externos.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Acesso à internet para o monitoramento real
- Conta SMTP para enviar e-mails (somente no modo real)
- Chave de API da [brapi](https://brapi.dev/) para consultar ativos que exigem autenticação

## Execução rápida: simulação

No terminal, entre na pasta `StockTracker` (onde está o arquivo `StockTracker.csproj`) e execute:

```bash
dotnet run -- --simulate PETR4 22.67 22.59
```

A simulação utiliza oito cotações fictícias, gera três alertas (venda, compra e venda) e mostra os e-mails no console. **Não é necessário criar um `.env`, ter uma chave da brapi ou configurar SMTP.**

## Monitoramento real

O programa recebe três argumentos, nesta ordem:

```bash
dotnet run -- <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>
```

Por exemplo:

```bash
dotnet run -- PETR4 22.67 22.59
```

- Se a cotação for **maior que 22.67**, envia um alerta de **venda**.
- Se a cotação for **menor que 22.59**, envia um alerta de **compra**.
- Entre os limites, inclusive nos valores iguais a eles, não envia alertas.

O programa continua consultando o preço no intervalo configurado, mas não repete um alerta enquanto a cotação permanecer na mesma faixa. Um novo alerta pode ser enviado após a mudança de faixa. Para encerrar, pressione **Ctrl+C**.

### Configuração

**1.** Dentro da pasta `StockTracker`, copie `.env.example` para `.env`. No PowerShell:

```powershell
Copy-Item .env.example .env
```

Preencha o `.env`:

```dotenv
SMTP_PASSWORD=sua_senha_smtp
POLL_INTERVAL_SECONDS=300
BRAPI_API_KEY=
```

- `SMTP_PASSWORD`: senha de autenticação SMTP.
- `POLL_INTERVAL_SECONDS`: intervalo entre consultas, em segundos (por exemplo, `300` = 5 minutos).
- `BRAPI_API_KEY`: chave opcional da brapi. Os ativos PETR4, MGLU3, VALE3 e ITUB4 podem ser consultados sem token.

**2.** Ajuste o arquivo `appsettings.json` com as informações do servidor SMTP:

```json
{
  "To": "destinatario@exemplo.com",
  "From": "remetente@gmail.com",
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "remetente@gmail.com"
}
```

O exemplo usa Gmail com STARTTLS na porta 587. Para outro provedor, utilize os dados SMTP correspondentes. O campo `From` deve ser um endereço autorizado pelo servidor SMTP.

**3.** Verifique o envio de e-mail antes de iniciar o monitoramento:

```bash
dotnet run -- --test-email
```

Depois, execute o comando de monitoramento real mostrado acima.

## Outros comandos

| Comando | Função |
| --- | --- |
| `dotnet run -- --simulate PETR4 22.67 22.59` | Simula preços e e-mails sem serviços externos |
| `dotnet run -- --test-alerts` | Testa as transições de compra/venda com preços fixos |
| `dotnet run -- --test-email` | Envia uma mensagem de teste usando o SMTP configurado |
| `dotnet build` | Compila o projeto |

## Organização do código

| Arquivo | Responsabilidade |
| --- | --- |
| `Program.cs` | Valida argumentos, carrega configurações e inicializa os modos de execução |
| `QuoteClient.cs` | Consulta a brapi por HTTP e extrai a cotação do JSON |
| `PriceAlert.cs` | Decide quando há alerta de compra ou venda e evita repetições |
| `PriceMonitor.cs` | Coordena as consultas periódicas, os alertas e o envio de e-mails |
| `EmailSender.cs` | Envia mensagens via SMTP com MailKit |
| `Logger.cs` | Padroniza mensagens de informação, alerta e erro no console |

O monitoramento é **assíncrono** (`async`/`await`). O `PriceMonitor` recebe funções para obter preços e enviar mensagens; no modo real, elas chamam a brapi e o SMTP, e no modo de simulação usam dados fictícios e imprimem os e-mails no terminal. Assim, a mesma lógica de alertas é utilizada nos dois modos.

## Observações

- As cotações são fornecidas pela **brapi** e podem ter atraso.
- Em caso de falhas temporárias, o monitoramento registra o erro e pode tentar novamente na próxima consulta. Problemas permanentes, como credenciais inválidas, encerram a execução.

## Uso de ferramentas de IA

- ChatGPT (GPT-6) foi utilizado apenas como apoio consultivo.
