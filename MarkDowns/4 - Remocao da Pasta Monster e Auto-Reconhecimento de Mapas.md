# 4 - Remoção da Pasta Legada Monster e Auto-Reconhecimento de Mapas do Cliente

## 1. Visão Geral da Atualização
Esta atualização reformulou a estrutura de distribuição e o sistema de gerenciamento de mapas do **MonsterSpawn Studio IA**, eliminando resquícios de arquivos legados e implementando um motor de detecção dinâmica e obrigatória dos mapas do cliente.

---

## 2. O Que Mudou

### 2.1 Eliminação Definitiva da Pasta Legada `\Monster\`
- **Antes**: A distribuição portátil (`MonsterSpawnStudio/`) continha uma subpasta `Monster\Monster\` com arquivos antigos de Season 4 (`Crywolf Fortress MovePath.dat`, `KanturuMonsterSetBase.txt`, etc.).
- **Agora**: Toda a pasta legada `Monster\` foi deletada da distribuição e do repositório. O executável opera de forma independente, sem arquivos mortos.

### 2.2 Estrutura Oficial Obrigatória de Mapas do Cliente
- A distribuição portátil agora possui uma pasta dedicada exclusivamente aos mapas do cliente:
  ```text
  MonsterSpawnStudio/
  ├── Arquivos/
  │   └── Mapas - Cliente/
  │       └── Data/
  │           ├── World1/       (Lorencia: Terrain1.att, EncTerrain1.att, MiniMap.ozj, etc.)
  │           ├── World2/       (Dungeon)
  │           ├── World3/       (Devias)
  │           ├── World4/       (Noria)
  │           ├── World5/       (Lost Tower)
  │           ├── World6/       (Exile)
  │           ├── World7/       (Arena / Stadium)
  │           ├── World8/       (Atlans)
  │           ├── World9/       (Tarkan)
  │           ├── World10/      (Devil Square)
  │           ├── World11/      (Icarus)
  │           └── World12/      (Blood Castle)
  ├── MonsterSpawnStudio.exe
  ├── MonsterSpawnStudio.dll
  ├── MonsterSpawnStudio.pdb
  └── MonsterSpawnStudio.ini
  ```

### 2.3 Resolução Automática Inteligente
Ao iniciar, o programa busca automaticamente a pasta de mapas nos seguintes caminhos relativos ao executável (`AppContext.BaseDirectory`):
1. `Arquivos\Mapas - Cliente\Data\`
2. `3 - Arquivos\Mapas - Cliente\Data\`
3. `3 - Mapas - Cliente\Data\`
4. `Mapas - Cliente\Data\`
5. `Arquivos SSeMUe MuEmu\Client\Data\`
6. Caminho salvo no `MonsterSpawnStudio.ini` (`Cliente=...`)

Se nenhuma pasta válida for encontrada, o programa exibe um alerta explicativo e obriga o administrador a apontar a pasta `Data` contendo as pastas `World1..N`.

### 2.4 Reconhecimento Dinâmico de Novos Mapas
- O sistema varre todas as subpastas `World{N}` dentro da pasta de mapas do cliente.
- Cada pasta encontrada é associada ao seu ID correspondente: `ID = N - 1` (ex: `World1` = mapa 0, `World18` = mapa 17).
- Caso o administrador adicione um novo mapa (exemplo: `World18`), ele é cadastrado automaticamente como `Mapa 17 (World18)`.
- Se o servidor for SSeMU e possuir um arquivo de spawn associado (ex: `017 - ArenaPvP.txt`), o nome `ArenaPvP` é extraído e cadastrado dinamicamente.
- **Validação (F5)**: O validador agora checa `Dados.ExisteMapa(mapa)` contra a coleção de mapas dinamicamente descobertos. Não ocorrem mais falsos erros de "mapa fora da faixa" em mapas customizados.
- **Seletores Modais**: Os dropdowns de mapa em `NovoSpawn` e `CriarSecaoMapa` são preenchidos dinamicamente com todos os mapas detectados.
- **Carregamento Flexível**: Suporta carregamento de qualquer `.att` e `.ozj` contido na pasta do World customizado.

---

## 3. Matriz de Arquivos para Atualização na VPS

| Ação | Arquivo / Diretório | Descrição |
| :--- | :--- | :--- |
| **ATUALIZAR** | `MonsterSpawnStudio/MonsterSpawnStudio.exe` | Executável compilado em Release (.NET 10) |
| **ATUALIZAR** | `MonsterSpawnStudio/MonsterSpawnStudio.dll` | Biblioteca compilada em Release |
| **ATUALIZAR** | `MonsterSpawnStudio/MonsterSpawnStudio.pdb` | Símbolos de depuração |
| **ATUALIZAR** | `MonsterSpawnStudio/MonsterSpawnStudio.ini` | Configuração com `Cliente=Arquivos\Mapas - Cliente\Data` |
| **CRIAR** | `MonsterSpawnStudio/Arquivos/Mapas - Cliente/Data/` | Pasta dedicada com `World1` a `World12` |
| **EXCLUIR** | `MonsterSpawnStudio/Monster/` | Deletar antiga pasta legada na VPS |

---

## 4. Testes e Validação Técnica
- **Compilação**: Executado `dotnet build -c Release` com **0 erros** e **0 avisos**.
- **Testes de Auto-Detecção**:
  - Teste de presença dos mapas 0 a 16: **Aprovado**.
  - Simulação de criação do mapa 17 (`World18` com `Terrain18.att`): **Aprovado** e detectado como `Mapa 17 (World18)`.
  - Rejeição de mapas inexistentes (`ExisteMapa(99)`): **Aprovado** (retorna false).
  - Carregamento de terreno e minimapa em tempo real: **Aprovado**.
