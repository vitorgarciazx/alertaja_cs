# AlertaJá --- Backend e Simulador IoT

## 1. Contexto do projeto

O **AlertaJá** é um sistema de monitoramento de condições ambientais e
geração de alertas. O backend recebe medições enviadas por sensores,
registra os dados no banco Oracle, avalia os valores recebidos em
relação aos limites configurados para cada sensor e mantém os alertas e
o nível de risco das regiões atualizados.

Este documento descreve exclusivamente o **backend ASP.NET Core** e o
**simulador IoT em Python**. O frontend não faz parte do escopo desta
documentação.

### Principais responsabilidades

-   Manter regiões e sensores cadastrados.
-   Receber e consultar medições.
-   Classificar medições de acordo com os limites de atenção, alerta e
    perigo de cada sensor.
-   Criar, atualizar e resolver alertas automaticamente.
-   Atualizar o nível de risco consolidado de uma região.
-   Autenticar usuários e controlar o acesso a operações
    administrativas.
-   Simular o envio de medições por meio de um programa Python.

## 2. Tecnologias e requisitos

### Requisitos de ambiente

  -----------------------------------------------------------------------
  Componente                          Requisito
  ----------------------------------- -----------------------------------
  Sistema operacional                 Windows 10 ou 11 recomendado para
                                      seguir os comandos deste documento

  .NET                                SDK .NET 10, pois o projeto é
                                      compilado para `net10.0`

  Python                              Python 3.10 ou superior recomendado

  Banco de dados                      Instância Oracle acessível pela
                                      máquina que executa a API

  IDE                                 Visual Studio 2022 ou superior com
                                      suporte ao .NET 10 (opcional para
                                      execução pelo terminal)

  Testes manuais                      Postman

  Controle de versão                  Git (opcional)
  -----------------------------------------------------------------------

> **Nota sobre a versão do Python:** o simulador utiliza `requests` e
> `python-dotenv`. Python 3.10+ é a versão recomendada para preparar o
> ambiente, mas a versão mínima exata deve ser confirmada caso o código
> do simulador utilize recursos específicos de uma versão mais recente.

### Dependências principais

**Backend** - ASP.NET Core Web API (.NET 10). - Entity Framework Core. -
Provedor Oracle para Entity Framework Core
(`Oracle.EntityFrameworkCore`). - Autenticação JWT
(`Microsoft.AspNetCore.Authentication.JwtBearer`). - `DotNetEnv` para
carregar as variáveis do arquivo `.env`.

**Simulador** - `requests`: comunicação HTTP com a API. -
`python-dotenv`: leitura das configurações do arquivo `.env`.

## 3. Organização do projeto

A estrutura principal do repositório é:

``` text
alertaja_cs/
├── backend/
│   └── AlertaJa.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── DTOs/
│       ├── Enums/
│       ├── Models/
│       ├── Properties/
│       ├── Services/
│       ├── appsettings.json
│       ├── Program.cs
│       └── AlertaJa.Api.csproj
├── simulator/
│   ├── simulate_sensors.py
│   └── como_executar.txt
├── .env
├── .gitignore
├── db_ddl.sql
├── db_dml.sql
└── README.md
```

### Responsabilidade de cada camada

**Controllers**

Os controllers recebem as requisições HTTP, fazem a ligação entre a
entrada da API e os serviços e retornam os códigos de status e os dados
apropriados. Eles não devem concentrar as regras de negócio.

Exemplos: `RegioesController`, `SensoresController`,
`MedicoesController`, `AlertasController` e `AuthController`.

**Services**

Os services concentram as regras de negócio e coordenam as operações.
Por exemplo:

-   `MedicaoService` valida e registra as medições recebidas.
-   `AlertaService` classifica as medições, cria ou atualiza alertas e
    recalcula o risco da região.
-   `AuthService` valida as credenciais e gera o token JWT.
-   `RegiaoService` e `SensorService` implementam as operações de
    negócio relacionadas a regiões e sensores.

**AppDbContext e a função de Repository**

O projeto não possui uma camada de Repository genérica separada. O
acesso aos dados é feito pelo Entity Framework Core por meio do
`AppDbContext`.

O `AppDbContext` disponibiliza os conjuntos de entidades (`DbSet`),
configura o mapeamento entre os models C# e as tabelas/colunas Oracle e
representa a unidade de trabalho das operações de persistência.

Na prática, o Entity Framework Core já oferece abstrações com
responsabilidades semelhantes às de Repository e Unit of Work: os
`DbSet` permitem consultar e manipular entidades, enquanto
`SaveChangesAsync()` persiste as alterações pendentes em conjunto.

Por isso, adicionar um Repository genérico que apenas repassasse
chamadas ao `AppDbContext` duplicaria uma abstração já fornecida pelo
ORM, sem necessariamente trazer benefício ao projeto. Os services
utilizam diretamente o contexto para executar as operações necessárias.

**Models e DTOs**

-   `Models/` contém as entidades utilizadas pelo domínio e pelo
    mapeamento do banco.
-   `DTOs/` contém os objetos usados para receber dados das requisições
    e devolver respostas. Isso evita expor diretamente as entidades do
    banco como contrato da API.

## 4. Fluxo de funcionamento da API

O fluxo principal de ingestão de dados é:

1.  O simulador Python prepara uma medição para um sensor cadastrado.
2.  O simulador envia uma requisição HTTPS `POST` para `/api/medicoes`,
    incluindo a API Key no cabeçalho `X-API-Key`.
3.  `MedicoesController` valida a API Key e encaminha os dados ao
    `MedicaoService`.
4.  `MedicaoService` valida os dados recebidos, verifica se o sensor
    existe e está ativo e registra a medição.
5.  O serviço atualiza a data da última leitura do sensor e chama o
    `AlertaService`.
6.  `AlertaService` compara o valor com os limites configurados para o
    sensor:
    -   abaixo do limite de atenção: `NORMAL`;
    -   a partir do limite de atenção: `ATENCAO`;
    -   a partir do limite de alerta: `ALERTA`;
    -   a partir do limite de perigo: `PERIGO`.
7.  Conforme a classificação, o sistema cria um alerta, atualiza um
    alerta ativo existente ou resolve automaticamente o alerta quando a
    medição retorna ao nível normal.
8.  O nível de risco atual da região é recalculado considerando os
    alertas ativos associados a ela.
9.  As alterações são persistidas no Oracle.

A API também disponibiliza operações para cadastrar e consultar regiões
e sensores, consultar medições e alertas, autenticar usuários e excluir
alertas conforme as permissões do usuário.

## 5. Endpoints

Os endpoints abaixo representam as operações implementadas no backend.
Os exemplos de corpos de requisição devem ser conferidos com os DTOs
correspondentes caso o contrato seja alterado.

  -----------------------------------------------------------------------
  Método                  Rota                    Finalidade
  ----------------------- ----------------------- -----------------------
  `POST`                  `/api/auth/login`       Autentica o usuário e
                                                  retorna um JWT.

  `GET`                   `/api/regioes`          Lista as regiões.

  `GET`                   `/api/regioes/{id}`     Consulta uma região
                                                  pelo identificador.

  `POST`                  `/api/regioes`          Cadastra uma região.

  `PUT`                   `/api/regioes/{id}`     Atualiza uma região.

  `GET`                   `/api/sensores`         Lista os sensores.

  `GET`                   `/api/sensores/{id}`    Consulta um sensor pelo
                                                  identificador.

  `POST`                  `/api/sensores`         Cadastra um sensor.

  `PUT`                   `/api/sensores/{id}`    Atualiza um sensor.

  `POST`                  `/api/medicoes`         Recebe uma medição
                                                  enviada pelo simulador;
                                                  exige API Key.

  `GET`                   `/api/medicoes`         Lista as medições.

  `GET`                   `/api/medicoes/{id}`    Consulta uma medição
                                                  pelo identificador.

  `GET`                   `/api/alertas`          Lista os alertas.

  `GET`                   `/api/alertas/{id}`     Consulta um alerta pelo
                                                  identificador.

  `DELETE`                `/api/alertas/{id}`     Exclui permanentemente
                                                  um alerta; exige
                                                  usuário administrador.
  -----------------------------------------------------------------------

### Exemplo de envio de medição

Requisição:

``` http
POST /api/medicoes
Content-Type: application/json
X-API-Key: SUA_API_KEY
```

Corpo JSON:

``` json
{
  "sensorId": "GUID-DO-SENSOR",
  "valor": 12.5
}
```

A API verifica a chave, valida o sensor e o valor, registra a medição e
executa o processamento de alertas. Em caso de sucesso, retorna
`201 Created`.

## 6. Simulador IoT

O simulador está localizado em `simulator/simulate_sensors.py`. Ele
representa sensores enviando medições à API, permitindo testar o fluxo
de ingestão e a geração de alertas sem depender de dispositivos físicos.

O simulador utiliza as configurações do `.env`, incluindo a URL da API,
a API Key, os identificadores dos sensores e o intervalo entre envios.

### Cenários disponíveis

Execute os comandos a partir da pasta `simulator/`:

**Cenário normal**

``` powershell
python simulate_sensors.py
```

Envia medições dentro de condições normais.

**Cenário de perigo**

``` powershell
python simulate_sensors.py --scenario perigo
```

Simula valores de perigo para exercitar a geração de alertas de maior
criticidade.

**Todos os cenários**

``` powershell
python simulate_sensors.py --scenario all
```

Executa os cenários previstos pelo simulador.

**Simulação contínua**

``` powershell
python simulate_sensors.py --continuous
```

Mantém o envio de medições em execução contínua, de acordo com o
intervalo configurado. Para interromper, utilize `Ctrl + C`.

> Os comandos acima usam o nome completo `simulate_sensors.py`. A
> extensão `.py` é necessária para executar o arquivo apresentado na
> estrutura do projeto.

## 7. Login e controle de permissões

A autenticação é realizada pelo endpoint `POST /api/auth/login`. O
cliente envia as credenciais do usuário e, se forem válidas, recebe um
token JWT.

O token deve ser enviado nas requisições protegidas pelo cabeçalho:

``` http
Authorization: Bearer SEU_TOKEN_JWT
```

O sistema possui dois papéis:

-   `ROLE_ADMIN`: usuário administrador, autorizado a executar operações
    administrativas, incluindo a exclusão de alertas.
-   `ROLE_CIDADAO`: usuário cidadão, com acesso limitado às operações
    permitidas pela API.

O controle de acesso é aplicado nos controllers por meio dos mecanismos
de autenticação e autorização do ASP.NET Core, como o atributo
`[Authorize]` e a restrição por papel (`Roles`).

A exclusão de alertas utiliza autorização específica para `ROLE_ADMIN`.
Um token válido sem o papel exigido não deve permitir essa operação.

## 8. Segurança

### HTTPS e TLS

A comunicação entre o simulador e a API deve utilizar HTTPS. O HTTPS
utiliza TLS para criptografar os dados durante o transporte, protegendo
o conteúdo das requisições contra leitura e alteração por terceiros no
caminho entre o cliente e o servidor.

A API de desenvolvimento utiliza um certificado HTTPS de desenvolvimento
do ASP.NET Core. Em produção, deve ser utilizado um certificado
apropriado para o ambiente publicado.

### API Key do simulador

O simulador envia uma chave no cabeçalho `X-API-Key`. A API compara a
chave recebida com a configuração armazenada no ambiente do backend. A
comparação utiliza `CryptographicOperations.FixedTimeEquals`, reduzindo
a possibilidade de vazamento de informação por diferenças de tempo na
comparação.

A API Key identifica e autoriza o simulador a enviar medições. Ela **não
substitui o HTTPS e não criptografa o conteúdo**: a criptografia do
transporte é fornecida pelo TLS.

### JWT

Após o login, a API emite um token JWT assinado. O token contém
informações de identidade e papel do usuário, utilizadas para autenticar
as requisições e aplicar as permissões.

O JWT é assinado, não criptografado por padrão. Portanto, não se deve
inserir nele senhas ou outros segredos. A assinatura permite verificar a
integridade e a autenticidade do token.

A chave usada para assinar os tokens é configurada por variável de
ambiente (`Jwt__Key`), evitando que o segredo precise ficar diretamente
no código-fonte.

### Senhas

As senhas dos usuários são armazenadas como hashes gerados pelo
`PasswordHasher<TUser>` do ASP.NET Core. A verificação é feita pelo
próprio mecanismo de hashing, sem armazenar a senha original.

### Cuidados com segredos

-   Não versionar o arquivo `.env`.
-   Não publicar a chave JWT ou a API Key em repositórios.
-   Não compartilhar credenciais reais do Oracle.
-   Em ambientes publicados, utilizar armazenamento seguro de segredos e
    uma configuração HTTPS adequada.

## 9. Configuração das variáveis

O arquivo `.env` fica na raiz do repositório. Ele contém configurações
utilizadas pelo backend e pelo simulador.

Exemplo ilustrativo:

``` env
Jwt__Key=SUBSTITUA_POR_UMA_CHAVE_FORTE
ALERTAJA_API_KEY=SUBSTITUA_POR_UMA_API_KEY_FORTE
API_BASE_URL=https://localhost:7275
SENSOR_PLUVIOMETRO_ID=GUID_DO_SENSOR_PLUVIOMETRO
SENSOR_NIVEL_AGUA_ID=GUID_DO_SENSOR_NIVEL
SIMULATION_INTERVAL_SECONDS=2.0
```

  -----------------------------------------------------------------------
  Variável                            Utilização
  ----------------------------------- -----------------------------------
  `Jwt__Key`                          Chave secreta usada pelo backend
                                      para assinar os JWTs. O duplo
                                      sublinhado representa a hierarquia
                                      `Jwt:Key` na configuração do .NET.

  `ALERTAJA_API_KEY`                  Chave compartilhada entre simulador
                                      e API para autorizar o envio de
                                      medições.

  `API_BASE_URL`                      Endereço base da API utilizado pelo
                                      simulador.

  `SENSOR_PLUVIOMETRO_ID`             GUID do sensor pluviométrico
                                      cadastrado no banco.

  `SENSOR_NIVEL_AGUA_ID`              GUID do sensor de nível de água
                                      cadastrado no banco.

  `SIMULATION_INTERVAL_SECONDS`       Intervalo, em segundos, entre os
                                      envios na simulação contínua.
  -----------------------------------------------------------------------

A string de conexão Oracle é mantida em
`backend/AlertaJa.Api/appsettings.json`, na seção de configuração
correspondente. Ela deve apontar para uma instância Oracle acessível e
conter as credenciais necessárias.

**Importante:** os valores acima são exemplos. Substitua os GUIDs e os
segredos pelos valores do ambiente em que o projeto será executado. Não
inclua valores reais de produção neste documento.

## 10. Como executar o backend

### 10.1. Preparar o ambiente

Instale o SDK .NET 10 e confirme a instalação:

``` powershell
dotnet --version
```

O comando deve retornar uma versão `10.0.x`.

Configure o arquivo `.env` na raiz do repositório e a string de conexão
Oracle em `appsettings.json`. Verifique se o banco está acessível e se o
schema e as tabelas necessários já foram criados.

### 10.2. Restaurar dependências

Abra o PowerShell na pasta do projeto da API:

``` powershell
cd backend/AlertaJa.Api
dotnet restore
```

### 10.3. Executar pelo terminal com HTTPS

O projeto possui os perfis `http` e `https` em
`Properties/launchSettings.json`. Para iniciar o perfil HTTPS
explicitamente:

``` powershell
dotnet run --launch-profile https
```

O perfil `https` está configurado para disponibilizar:

-   HTTPS: `https://localhost:7275`
-   HTTP: `http://localhost:5009`

A saída do terminal deve indicar que a aplicação está ouvindo nos dois
endereços.

#### Configurar o certificado HTTPS de desenvolvimento

Caso o .NET informe que não encontrou um certificado de desenvolvimento
válido, execute:

``` powershell
dotnet dev-certs https
dotnet dev-certs https --trust
dotnet dev-certs https --check --verbose
```

O primeiro comando gera o certificado, o segundo solicita ao Windows que
confie nele e o terceiro verifica o estado do certificado. Depois,
execute novamente:

``` powershell
dotnet run --launch-profile https
```

Se aparecer um erro informando que a porta já está em uso, encerre a
instância anterior da API no Visual Studio ou no terminal antes de
iniciar outra. Para identificar o processo que ocupa a porta 5009:

``` powershell
netstat -ano | findstr :5009
```

Depois, identifique o PID:

``` powershell
tasklist /FI "PID eq NUMERO_DO_PID"
```

Encerre somente se tiver confirmado que se trata de uma instância antiga
que pode ser fechada:

``` powershell
taskkill /PID NUMERO_DO_PID /F
```

### 10.4. Executar pelo Visual Studio

Também é possível abrir a solução `AlertaJa.Api.slnx` no Visual Studio e
selecionar o perfil `https` no seletor de execução. Não selecione o
perfil `http` se a intenção for testar a comunicação HTTPS.

## 11. Como executar o simulador

Instale o Python e confirme a instalação:

``` powershell
python --version
```

Abra outro terminal e acesse a pasta do simulador:

``` powershell
cd simulator
```

Instale as dependências:

``` powershell
pip install requests python-dotenv
```

Com a API em execução, execute um dos cenários:

``` powershell
python simulate_sensors.py
python simulate_sensors.py --scenario perigo
python simulate_sensors.py --scenario all
python simulate_sensors.py --continuous
```

O simulador deve apontar para a mesma API Key configurada no backend e
utilizar GUIDs de sensores existentes e ativos no banco.

## 12. Testes automatizados pelo simulador

O simulador permite testar o processamento de medições sem montar
requisições manualmente.

Sugestão de sequência:

1.  Inicie a API pelo perfil HTTPS.
2.  Confirme que as variáveis do `.env` estão preenchidas.
3.  Execute o cenário normal e verifique se as medições foram
    registradas.
4.  Execute `--scenario perigo` e verifique se o backend criou ou
    atualizou os alertas correspondentes.
5.  Execute `--scenario all` para exercitar os cenários disponibilizados
    pelo simulador.
6.  Execute `--continuous` para observar o comportamento da API ao
    receber medições sucessivas.
7.  Consulte `/api/medicoes`, `/api/alertas` e `/api/regioes` para
    verificar os registros e os níveis de risco.

A classificação depende dos limites cadastrados em cada sensor. Para o
pluviômetro, os valores de referência utilizados no ambiente de teste
são 10 (atenção), 25 (alerta) e 50 (perigo). Para o sensor de nível de
água, os valores são 100, 200 e 300, respectivamente. Os valores e as
unidades devem ser coerentes com a configuração dos sensores e com os
dados enviados pelo simulador.

## 13. Testes manuais com Postman

Os testes manuais podem ser reproduzidos no Postman. Antes de começar:

1.  Inicie a API pelo perfil HTTPS.
2.  Abra o Postman e crie uma collection chamada `AlertaJá`.
3.  Configure uma variável de collection, por exemplo `baseUrl`, com o
    valor `https://localhost:7275`.
4.  Para requisições protegidas por JWT, utilize a aba
    **Authorization**, selecione **Bearer Token** e informe o token
    obtido no login.
5.  Para o envio de medições, configure também o cabeçalho `X-API-Key`.

Se o Postman exibir um erro de certificado durante o desenvolvimento
local, verifique se o certificado foi confiado pelo sistema. Evite
desativar permanentemente a verificação SSL.

### 13.1. Login

Crie uma requisição:

``` http
POST {{baseUrl}}/api/auth/login
```

Selecione **Body → raw → JSON** e envie as credenciais de um usuário
cadastrado. O formato exato deve corresponder ao `LoginDto` do backend
(normalmente, e-mail e senha).

Se as credenciais forem válidas, a resposta conterá o token JWT. Copie o
valor do token para utilizar nas operações que exigem autenticação.

### 13.2. Configurar o Bearer Token

Nas requisições protegidas:

1.  Abra a aba **Authorization**.
2.  Em **Auth Type**, selecione **Bearer Token**.
3.  Cole o JWT no campo **Token**.
4.  Envie a requisição.

Não é necessário escrever manualmente `Bearer` no campo quando o tipo
Bearer Token já está selecionado; o Postman monta o cabeçalho.

### 13.3. Consultar regiões, sensores, medições e alertas

Exemplos de requisições:

``` http
GET {{baseUrl}}/api/regioes
GET {{baseUrl}}/api/sensores
GET {{baseUrl}}/api/medicoes
GET {{baseUrl}}/api/alertas
```

Para consultar um registro específico, utilize o GUID:

``` http
GET {{baseUrl}}/api/medicoes/GUID-DA-MEDICAO
GET {{baseUrl}}/api/alertas/GUID-DO-ALERTA
```

### 13.4. Enviar uma medição manualmente

Crie uma requisição:

``` http
POST {{baseUrl}}/api/medicoes
```

Em **Headers**, adicione:

  -----------------------------------------------------------------------
  Chave                               Valor
  ----------------------------------- -----------------------------------
  `Content-Type`                      `application/json`

  `X-API-Key`                         O mesmo valor de `ALERTAJA_API_KEY`
                                      configurado no `.env`
  -----------------------------------------------------------------------

Em **Body → raw → JSON**, envie:

``` json
{
  "sensorId": "GUID-DE-UM-SENSOR-ATIVO",
  "valor": 12.5
}
```

O `sensorId` deve corresponder a um sensor existente e ativo. O valor
deve respeitar as regras de validação da API.

### 13.5. Excluir um alerta

A exclusão é uma operação administrativa. Faça login com um usuário que
possua o papel `ROLE_ADMIN`, copie o JWT e configure o Bearer Token.

Envie:

``` http
DELETE {{baseUrl}}/api/alertas/GUID-DO-ALERTA
```

Não é necessário enviar corpo JSON. Se a exclusão for realizada com
sucesso, a API retorna `204 No Content`. Um usuário sem o papel de
administrador não deve conseguir executar essa operação.

### 13.6. Códigos de resposta comuns

  -----------------------------------------------------------------------
  Código                              Significado
  ----------------------------------- -----------------------------------
  `200 OK`                            Requisição concluída com sucesso.

  `201 Created`                       Recurso criado com sucesso, como no
                                      recebimento de uma medição.

  `204 No Content`                    Operação concluída sem conteúdo de
                                      resposta, como na exclusão.

  `400 Bad Request`                   Dados enviados inválidos ou que não
                                      atendem às validações.

  `401 Unauthorized`                  Token ausente, inválido, expirado
                                      ou não aceito.

  `403 Forbidden`                     Usuário autenticado, mas sem a
                                      permissão necessária.

  `404 Not Found`                     Recurso não encontrado.

  `405 Method Not Allowed`            Método HTTP não permitido para a
                                      rota informada.
  -----------------------------------------------------------------------

## 14. Observações para execução e entrega

-   A API depende de uma instância Oracle acessível e de um schema
    previamente preparado.
-   A string de conexão está em `appsettings.json`; não compartilhe
    credenciais reais em repositórios públicos.
-   O arquivo `.env` deve permanecer fora do controle de versão.
-   A API e o simulador precisam utilizar a mesma API Key.
-   Os identificadores configurados no simulador devem corresponder a
    sensores existentes no banco.
-   Para testar HTTPS localmente, o certificado de desenvolvimento deve
    estar instalado e confiável.
-   Os testes do Postman devem utilizar a mesma URL e as mesmas
    credenciais configuradas no ambiente em execução.
