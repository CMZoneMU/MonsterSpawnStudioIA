# Estrutura do Programa MonsterSpawn Studio

## 1. Visao Geral
O **MonsterSpawn Studio** e um utilitario visual desenvolvido originalmente pelo desenvolvedor **Kellington** para gerenciamento, adicao, edicao e organizacao de monstros, NPCs e spots em servidores **MuOnline**.

O programa combina a edicao direta em tabela dos arquivos de script do servidor com uma visualizacao grafica 2D interativa do mapa em tempo real. Ele sincroniza o minimapa do cliente com o mapa de colisao do terreno (`.att`) do servidor, permitindo que os spawns sejam posicionados com precisao milimetrica e sem risco de ficarem presos em paredes ou zonas invalidas.

---

## 2. Ficha Tecnica do Software
- **Nome do Projeto**: MonsterSpawn Studio
- **Autor Original**: Kellington
- **Linguagem**: C# (C-Sharp)
- **Plataforma / Framework**: .NET 10.0 Windows Forms (`net10.0-windows`)
- **Arquitetura de Compilacao**: x64 / Any CPU
- **Modo de DPI**: `PerMonitorV2` (alta definicao para telas modernas)
- **Encoding de Arquivos**: Windows-1252 (`CP1252` / ANSI latino), compativel nativamente com GameServer MuOnline
- **Armazenamento de Configuracoes**: `MonsterSpawnStudio.ini` (UTF-8 com caminhos de servidor e cliente)

---

## 3. Arquitetura Geral do Sistema

O sistema opera de forma desacoplada entre os dados de configuracao do **MuServer** e os ativos graficos do **Client**:

```
+-----------------------------------------------------------------------------+
|                           MonsterSpawn Studio                               |
+-----------------------------------------------------------------------------+
         |                                                 |
         v                                                 v
+-----------------------------+              +-----------------------------+
|    MuServer Data Folder     |              |     Client Data Folder      |
+-----------------------------+              +-----------------------------+
| Data\Monster\               |              | Data\World1\Map1.ozj        |
|   - MonsterSetBase.txt      |              | Data\World2\Map1.ozj        |
|   - MonsterSetBaseCS.txt    |              | Data\World3\Map1.ozj        |
|   - KanturuMonsterSetBase   |              | ...                         |
|   - Monster.txt             |              | Data\World40\Map1.ozj       |
| Data\Terrain\               |              +-----------------------------+
|   - 00 - Lorencia.att       |
|   - 01 - Dungeon.att        |
|   - ...                     |
| Data\Gate.txt (ou Move\)    |
| Data\Lang\*\MoveReq(*).txt  |
+-----------------------------+
```

### 3.1. Fontes de Dados Lidas do MuServer
1. **Pasta `Data\Monster`**:
   - `MonsterSetBase.txt`: Arquivo principal contendo os blocos de NPCs, monstros soltos, spots de monstros e monstros dourados/invasoes.
   - `MonsterSetBaseCS.txt`: Spawns especificos para os mapas de Castle Siege e eventos de cerco (Valley of Loren, Land of Trials).
   - `KanturuMonsterSetBase.txt`: Spawns especificos do evento Kanturu agrupados por fase/estado.
   - `Monster.txt`: Dicionario contendo o catalogo de todos os monstros e NPCs do servidor, seus IDs, nomes e 28 colunas de atributos de combate.
2. **Pasta `Data\Terrain`**:
   - Arquivos de atributos de terreno (`.att`), contendo 65.536 bytes cada (matriz 256x256).
   - Define quais coordenadas sao solo caminhavel, parede, zona segura ou abismo.
3. **Pasta `Data` / `Data\Move` / `Data\Lang`**:
   - `Gate.txt`: Coordenadas de gates e teleports de mapas.
   - `MoveReq.txt`: Nomes de sub-areas (ex: Devias 2, Lost Tower 3, Atlans 2) e niveis de entrada para sobreposicao no mapa.

### 3.2. Fontes de Dados Lidas do Cliente (Client)
- **Pastas `Data\World<N>` (onde N = ID do Mapa + 1)**:
  - Arquivo `Map1.ozj` (ou variante de nome iniciado por `map`): Formato proprietario da Webzen que consiste em um cabecalho binario de 24 bytes seguido por uma imagem padrao JPEG.
  - A imagem e decodificada diretamente em memoria para compor o fundo visual do mapa (minimapa).

---

## 4. Divisao da Interface Grafica (Layout)

A janela do MonsterSpawn Studio possui uma interface moderna em tema escuro (Dark Theme, cinza grafite `#18181B` / `#202024` com detalhes em dourado `#F0BE50`), organizada em quatro modulos principais:

```
+-----------------------------------------------------------------------------------------------+
| Barra de Menus: Arquivo | Editar | Ferramentas | Ajuda                                        |
+--------------------------+-------------------------------------+------------------------------+
| ARVORE DE ARQUIVOS       | TABELA DE DADOS (GRID)              | VISUALIZADOR DE MAPA         |
| - MonsterSetBase.txt     | Colunas dinamicas por secao:        | - Minimapa com zoom / pan    |
|   |- 0 - NPCs (45)       | Mob | Mapa | Raio | X | Y | Dir... | - Paredes (contorno ou cheio)|
|   |  |- Lorencia (12)    | Linha 1                             | - Areas nomeadas             |
|   |  \- Noria (8)        | Linha 2                             | - Pontos de Spawn (circulos) |
|   \- 1 - Spots (120)     | Linha 3                             | - Caixas de Spot (retangulo) |
|                          |                                     | - Coordenadas sob o mouse    |
+--------------------------+-------------------------------------+------------------------------+
| PALETA DE MONSTROS       | CONFERENCIA (F5)                    | CONTROLES DO MAPA            |
| - Filtro de busca        | Painel de auditoria e logs:         | [x] Minimapa  Paredes: [v]   |
| [x] clique no mapa add   | - Monstros sem cadastro             | [x] Areas     [x] Grade      |
| [x] so os de [mapa]      | - Coordenadas em paredes            | Brilho: [---o---]            |
| Qtd: [10]  Raio: [30]    | - Spots com pouco chao livre        | Detalhe: [so as paredes]     |
| Lista de Mobs (ID/Nome)  | - Clique duplo vai para a linha     | [Enquadrar]                  |
+--------------------------+-------------------------------------+------------------------------+
| Barra de Status: MonsterSetBase: 1.450 objetos de 6.800 (OBJ_MAXMONSTER)                      |
+-----------------------------------------------------------------------------------------------+
```

### Detalhamento dos Paineis
1. **Painel Superior Esquerdo (Arvore)**:
   - Apresenta uma hierarquia limpa: `Arquivo > Secao > Mapa (Qtd)`.
   - Permite filtrar a exibicao apenas para a secao ou mapa desejado.
   - Mapas reservados vazios continuam visiveis com `(0)` em cinza para permitir alimentacao rapida.
2. **Painel Inferior Esquerdo (Paleta de Monstros)**:
   - Catalogo instantaneo de monstros com base no `Monster.txt`.
   - Modo de busca instantanea por nome ou numero.
   - Opcao inteligente `"so os de <mapa>"`: exibe apenas os monstros que ja pertencem a esse mapa no arquivo, agilizando o preenchimento de spots repetidos.
   - Caixa `"clique no mapa adiciona"` com campos de Quantidade (`Qtd`) e `Raio`.
3. **Painel Superior Central (Grade / Tabela)**:
   - Tabela editavel celula a celula com mapeamento automatico de colunas conforme a secao.
   - Coluna de conveniencia com o Nome do Monstro (`_mob`) gerada automaticamente por lookup no `Monster.txt`.
   - Coluna de Comentario (`_com`) para documentar a linha no script.
4. **Painel Inferior Central (Conferencia F5)**:
   - Scanner de integridade que aponta inconsistencias em tempo real:
     - IDs de monstros inexistentes no `Monster.txt`.
     - Coordenadas fora da faixa valida (0-255).
     - Monstros posicionados dentro de paredes.
     - Spots posicionados em locais com menos de 20% de piso livre.
     - Spots com quantidade zerada ou negativa.
   - Clique duplo sobre qualquer aviso seleciona imediatamente o no e a linha correspondente na tabela.
5. **Painel Direito (Mapa Interativo)**:
   - Exibicao bidimensional com zoom suave pela roda do mouse e pan pelo botao do meio.
   - Conversao matematica automatica de coordenadas (Y do jogo invertido em relacao ao eixo de desenho da tela).
   - Movimentacao de pontos com o mouse (arraste com atualizacao automatica da tabela).
   - Clique no vazio para criacao de novo spawn.
   - Botoes de controle de camadas: minimizar brilho, filtro de pedras soltas, grade 10x10 e enquadramento.
6. **Barra de Rodape (StatusStrip)**:
   - Contador geral de objetos ativos contra o limite maximo suportado pelo GameServer (`OBJ_MAXMONSTER = 6800`).
   - Alerta visual com cores: Verde (<90%), Amarelo (>=90%) e Vermelho (>6800 - limite ultrapassado).

---

## 5. Mapeamento Atual de Mapas e Terrenos (Season 4 / 1.00.18)

Na versao atual do programa, estao mapeados 40 mapas (indices de 0 a 39), com seus respectivos arquivos de terreno em `Data\Terrain`:

| ID | Nome do Mapa | Arquivo de Terreno (.att) | Minimapa no Client |
|:--:|:-------------|:--------------------------|:-------------------|
| 0 | Lorencia | `00 - Lorencia.att` | `World1\Map1.ozj` (Sim) |
| 1 | Dungeon | `01 - Dungeon.att` | `World2\Map1.ozj` (Sim) |
| 2 | Devias | `02 - Devias.att` | `World3\Map1.ozj` (Sim) |
| 3 | Noria | `03 - Noria.att` | `World4\Map1.ozj` (Sim) |
| 4 | Lost Tower | `04 - Lost Tower.att` | `World5\Map1.ozj` (Sim) |
| 5 | Exile | `05 - Exile.att` | Sem minimapa nativo (Fundo claro) |
| 6 | Arena | `06 - Arena.att` | `World7\Map1.ozj` (Sim) |
| 7 | Atlans | `07 - Atlans.att` | `World8\Map1.ozj` (Sim) |
| 8 | Tarkan | `08 - Tarkan.att` | `World9\Map1.ozj` (Sim) |
| 9 | Devil Square | `09 - Devil Square.att` | Sem minimapa nativo (Fundo claro) |
| 10 | Icarus | `10 - Icarus.att` | `World11\Map1.ozj` (Sim) |
| 11 | Blood Castle 1 | `11 - Blood Castle.att` | Sem minimapa nativo |
| 12 | Blood Castle 2 | `11 - Blood Castle.att` | Sem minimapa nativo |
| 13 | Blood Castle 3 | `11 - Blood Castle.att` | Sem minimapa nativo |
| 14 | Blood Castle 4 | `11 - Blood Castle.att` | Sem minimapa nativo |
| 15 | Blood Castle 5 | `11 - Blood Castle.att` | Sem minimapa nativo |
| 16 | Blood Castle 6 | `11 - Blood Castle.att` | Sem minimapa nativo |
| 17 | Blood Castle 7 | `11 - Blood Castle.att` | Sem minimapa nativo |
| 18 | Chaos Castle 1 | `18 - Chaos Castle.att` | Sem minimapa nativo |
| 19 | Chaos Castle 2 | `18 - Chaos Castle.att` | Sem minimapa nativo |
| 20 | Chaos Castle 3 | `18 - Chaos Castle.att` | Sem minimapa nativo |
| 21 | Chaos Castle 4 | `18 - Chaos Castle.att` | Sem minimapa nativo |
| 22 | Chaos Castle 5 | `18 - Chaos Castle.att` | Sem minimapa nativo |
| 23 | Chaos Castle 6 | `18 - Chaos Castle.att` | Sem minimapa nativo |
| 24 | Kalima 1 | `24 - Kalima.att` | Sem minimapa nativo |
| 25 | Kalima 2 | `24 - Kalima.att` | Sem minimapa nativo |
| 26 | Kalima 3 | `24 - Kalima.att` | Sem minimapa nativo |
| 27 | Kalima 4 | `24 - Kalima.att` | Sem minimapa nativo |
| 28 | Kalima 5 | `24 - Kalima.att` | Sem minimapa nativo |
| 29 | Kalima 6 | `24 - Kalima.att` | Sem minimapa nativo |
| 30 | Valley of Loren | `30 - Valley of Loren.att` | Sem minimapa nativo |
| 31 | Land of Trials | `31 - Land of Trials.att` | Sem minimapa nativo |
| 32 | Devil Square 2 | `32 - Devil Square 2.att` | Sem minimapa nativo |
| 33 | Aida | `33 - Aida.att` | Sem minimapa nativo |
| 34 | Crywolf Fortress| `34 - Crywolf Fortress.att`| Sem minimapa nativo |
| 35 | Crywolf 2 | `35 - Crywolf Second Zone.att`| Sem minimapa nativo |
| 36 | Kalima 7 | `36 - Kalima 7.att` | Sem minimapa nativo |
| 37 | Kanturu Ruins | `37 - Kanturu Ruins.att` | Sem minimapa nativo |
| 38 | Kanturu Relics | `38 - Kanturu Relics.att` | Sem minimapa nativo |
| 39 | Kanturu Refinery| `39 - Kanturu Refinery Tower.att`| Sem minimapa nativo |

---

## 6. Pontos de Atencao para a Adaptacao ao 97K (SSeMU e MueMU Kayito)

Ao migrar e expandir o MonsterSpawn Studio para as fontes **SSeMU 97K** e **MueMU Kayito 97K**, os seguintes pontos estruturais deverao ser ajustados:

1. **Numero e Nomes de Mapas**:
   - O 97K classico suporta do mapa 0 ao 16 (0 a 10 mapas principais: Lorencia, Dungeon, Devias, Noria, Lost Tower, Exile, Arena, Atlans, Tarkan, Devil Square, Icarus; e mapas 11 a 16 para Blood Castle 1 a 6).
   - Nao existem Chaos Castle, Kalima, Crywolf, Valley of Loren ou Kanturu na versao 97K pura.
2. **Nomenclatura e Localizacao dos Arquivos de Terreno**:
   - No MuServer 97K, os arquivos de terreno costumam estar nomeados como `Terrain1.att`, `Terrain2.att` ou `Terrain01.att` dentro de `Data\Terrain\` ou diretamente no cliente como `EncTerrain*.att`.
3. **Arquivos Inexistentes no 97K**:
   - `KanturuMonsterSetBase.txt` e `MonsterSetBaseCS.txt` nao existem no 97K. O programa devera tratar a ausencia desses arquivos sem gerar erros ou travamentos.
4. **Formato das Colunas do Monster.txt**:
   - Na Season 4 ha 28 colunas no `Monster.txt`. Em versoes 97K ou downgrades, o numero de colunas de atributos pode variar (costuma ter entre 20 a 24 colunas dependendo do GameServer).
5. **Limite de Monstros (OBJ_MAXMONSTER)**:
   - Em servidores 97K, a constante `OBJ_MAXMONSTER` no GameServer costuma ser menor (geralmente entre 1.024 e 4.096, dependendo de customizacoes da source) em vez de 6.800.
