# Estrutura da Source - MonsterSpawn Studio

## 1. Visao Geral da Solucao

A solucao do **MonsterSpawn Studio** foi desenvolvida em **C#** utilizando o ecossistema moderno do **.NET 10.0** e **Windows Forms**. Todo o codigo-fonte foi estruturado com foco em simplicidade, alto desempenho no processamento de texto e independencia de bibliotecas externas pesadas, utilizando a API grafica nativa do Windows (**GDI+ / System.Drawing**).

O codigo-fonte esta localizado na pasta `Source/` e e composto pelos seguintes arquivos:

```
Source/
├── MonsterSpawnStudio.csproj   # Configuracoes do projeto e build
├── Program.cs                  # Ponto de entrada e registro de encodings
├── Modelo.cs                   # Modelos de dados e parsers de scripts
├── Dados.cs                    # Gerenciador de terrenos (.att), minimapas (.ozj) e caches
├── MapaView.cs                 # Controle grafico customizado (renderizador 2D do mapa)
├── Dialogos.cs                 # Janelas de dialogo, design system escuro e forms auxiliares
├── MainForm.cs                 # Formulario principal, eventos, tabela e regras de negocio
└── app.ico                     # Icone do aplicativo
```

---

## 2. Analise Detalhada dos Arquivos da Source

### 2.1. `MonsterSpawnStudio.csproj`
Arquivo de definicao do projeto no padrao SDK Moderno do .NET:
- **Target Framework**: `net10.0-windows`
- **Output Type**: `WinExe`
- **UseWindowsForms**: `true`
- **Nullable**: `disable`
- **ApplicationHighDpiMode**: `PerMonitorV2` (garante que os componentes e desenhos do mapa permaneçam nitidos em resolucoes Full HD, 2K e 4K sem distorcao de escala).

---

### 2.2. `Program.cs`
Ponto de entrada (`Main`) do aplicativo:
- **Encoding Provider**: Invoca `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`.
  > **Relevancia**: Por padrao, o .NET moderno (.NET Core/5/6/7/8/9/10) nao carrega paginas de codigo legadas. Essa linha e essencial para habilitar a leitura e gravacao em `Windows-1252 (CP1252)`, padrao obrigatorio dos arquivos do MuServer.
- Inicializa a configuracao do Windows Forms e instancia `MainForm`.

---

### 2.3. `Modelo.cs` (Camada de Dados e Parsers)
Contem as definicoes das estruturas em memoria e as rotinas de leitura/escrita de arquivos.

#### Classes e Tipos:
1. **`enum TipoArquivo`**:
   - `SetBase`: Arquivo convencional de spawns (`MonsterSetBase.txt`, `MonsterSetBaseCS.txt`).
   - `Kanturu`: Arquivo de spawn com coluna de estado (`KanturuMonsterSetBase.txt`).
   - `Monster`: Catalogo de monstros com 28 colunas (`Monster.txt`).
2. **`class Linha`**:
   - Representa uma linha de dados do script.
   - Campos:
     - `int Secao`: Secao do script a qual pertence (0 a 4).
     - `List<string> Campos`: Lista dos valores tabulados.
     - `string Nome`: Texto do comentario inline apos `//`.
     - `string Cru`: Linha bruta (utilizada para preservar comentarios de cabecalho ou linhas nao interpretadas).
   - Metodos:
     - `Num(int i)`: Converte o campo no indice $i$ para inteiro de forma segura.
     - `SetNum(int i, int v)`: Atribui um valor numerico no indice $i$, expandindo a lista de campos se necessario.
     - `Clonar()`: Clona a linha para operacoes de duplicacao.
3. **`class Bloco`**:
   - Representa um agrupamento delimitado por `secao ... end`.
   - Campos:
     - `List<string> Antes`: Comentarios e quebras de linha que precedem a secao.
     - `int Secao`: Indice numerico da secao.
     - `List<Linha> Linhas`: Linhas de dados contidas no bloco.
     - `int MapaReservado`: Permite manter um mapa visivel na arvore com 0 linhas (ex: apos limpeza ou ao criar secao nova).
4. **`class ArquivoSpawn`**:
   - Representa o arquivo completo aberto em memoria.
   - Definicoes de Colunas:
     - `Col6`: `{"Mob", "Mapa", "Raio", "X", "Y", "Dir"}` (Secoes 0, 2 e 4).
     - `Col9`: `{"Mob", "Mapa", "Raio", "X1", "Y1", "X2", "Y2", "Dir", "Qtd"}` (Secoes 1 e 3 - spots).
     - `ColKanturu`: `{"Estado", "Mob", "Mapa", "Raio", "X", "Y", "Dir"}`.
     - `ColMonster`: 28 colunas detalhadas de atributos de combate do `Monster.txt`.
   - Metodo `Indices(int secao)`:
     - Retorna uma tupla com os indices exatos de cada campo essencial: `(mob, mapa, x, y, x2, y2, qtd)`.
   - Metodo `Abrir(string caminho)`:
     - Realiza o parser do arquivo linha a linha.
     - Suporta comentarios `//` em qualquer posicao.
     - Trata nomes entre aspas no `Monster.txt` (ex: `"Bull Fighter"`).
     - Agrupa blocos delimitados pela palavra-chave `end`.
   - Metodo `Salvar(string caminho)`:
     - Formata todas as secoes, blocos e linhas separadas por tabulacoes (`\t`).
     - Preserva comentarios originais e grava em codificacao `CP1252` com `CRLF`.
   - Metodo `TotalObjetos()`:
     - Calcula a soma real de monstros gerados pelo arquivo, multiplicando o valor da coluna `Qtd` nos spots para comparacao com o limite do servidor.

---

### 2.4. `Dados.cs` (Servicos de Terreno, Minimapa e Caches)
Modulo estatico responsavel pelo processamento e armazenamento de recursos binarios do servidor e do cliente.

#### Principais Funcionalidades:
1. **Dicionario de Monstros (`Monstros`)**:
   - Carrega o `Monster.txt` mapeando `ID -> Nome`.
2. **Leitura e Processamento de Terreno (`.att`)**:
   - Metodo `Terreno(int mapa)`:
     - Localiza o arquivo na pasta `Data\Terrain\<ArquivoTerreno[mapa]>`.
     - Le os ultimos 65.536 bytes do arquivo (matriz 256x256).
     - Cada byte contem mascaras de bits (bitmasks) de colisoes:
       - `Bit 4 (0x04)`: Nao anda / Parede intransitavel.
       - `Bit 1 (0x01)`: Zona Segura (Safe Zone / Cidade).
       - `Bit 8 (0x08)`: Sem chao / Abismo.
   - Metodos de conveniencia: `Parede(mapa, x, y)` e `ZonaSegura(mapa, x, y)`.
3. **Decodificacao do Minimapa do Cliente (`.ozj`)**:
   - Metodo `Minimapa(int mapa)`:
     - Acessa `Data\World<mapa+1>\Map1.ozj`.
     - O formato OZJ do MuOnline possui um cabecalho de 24 bytes com dados internos de protecao/identificacao da Webzen, seguido diretamente por um fluxo de imagem JPEG padrao.
     - O leitor descarta os 24 bytes iniciais (`MemoryStream(d, 24, d.Length - 24)`) e cria uma instancia `Bitmap` GDI+ de 24 bits.
4. **Mapeamento de Sub-areas (`Gate.txt` e `MoveReq.txt`)**:
   - Metodo `CarregarAreas()`:
     - Le o arquivo `Gate.txt` para descobrir as coordenadas retangulares de destino.
     - Le o `MoveReq.txt` nos idiomas `Kor`, `Eng` ou `Por` para vincular o nome amigavel (ex: "Lost Tower 7") e o nivel minimo ao retangulo do gate.

---

### 2.5. `MapaView.cs` (Renderizador Grafico 2D Interativo)
Controle customizado derivado de `System.Windows.Forms.Control`, otimizado para desenho via hardware com double buffering (`OptimizedDoubleBuffer`).

#### Mecanismos Graficos:
1. **Sistema de Coordenadas Invertido**:
   - No cliente MuOnline, a coordenada $(0,0)$ fica no canto inferior esquerdo e o eixo $Y$ cresce para cima ate $255$.
   - Na tela do Windows (GDI+), o $Y$ cresce para baixo.
   - A conversao e feita pelas funcoes:
     - `JogoParaTela(x, y)`: $Y_{tela} = Pan_Y + (255 - Y) \times Zoom$.
     - `TelaParaJogo(p)`: $Y_{jogo} = 255 - \left(\frac{Y_{tela} - Pan_Y}{Zoom}\right)$.
2. **Algoritmo de Paredes e Contorno Inteligente (`GerarParedes`)**:
   - Gera um `Bitmap` intermediario de 256x256 com canais de transparencia alfa.
   - Executa uma busca em largura (BFS / Flood Fill) para calcular o tamanho de cada aglomerado de celulas bloqueadas.
   - Utiliza a propriedade `MinimoParede`: se uma mancha de celulas bloqueadas tiver tamanho menor que o limite, e considerada pedrinha/obstaculo isolado e desenhada com transparencia sutil; se for uma parede solida, desenha uma borda vermelha destacada.
3. **Pipeline de Renderizacao (`OnPaint`)**:
   - Camada 1: Minimapa em JPEG clareado via `ColorMatrix` com base no slider de brilho.
   - Camada 2: Bitmap de paredes e zonas seguras.
   - Camada 3: Retangulos tracejados de sub-areas com rotulo e nivel.
   - Camada 4: Grade quadriculada de 10 em 10 coordenadas (quando o zoom for suficiente).
   - Camada 5: Retangulos amarelos semitransparentes representando areas de spot.
   - Camada 6: Circulos de spawn individuais (verde ou dourado se selecionado).
   - Camada 7: Tooltip flutuante com nome do monstro e coordenadas sob o cursor.
4. **Manipulacao com o Mouse**:
   - Pan (arraste do mapa) e Zoom com foco na posicao do mouse.
   - Deteccao de clique no ponto mais proximo com raio de tolerancia (`Achar`).
   - Eventos expostos: `PontoSelecionado`, `PontoMovido` e `CliqueVazio`.

---

### 2.6. `Dialogos.cs` (Design System e Modais)
Define o padrao estatico `Estilo` e as janelas de dialogo:
- **`Estilo`**: Centraliza as cores padronizadas da interface escura (Fundo `#18181B`, Painel `#202024`, Destaque `#F0BE50`).
- **`EscolherMonstro`**: Dialogo com campo de busca incremental para selecao rapida de monstro com base no `Monster.txt`.
- **`NovoMonstro`**: Modal com formulario completo contendo 28 campos para cadastrar uma nova linha de monstro no `Monster.txt`, calculando o primeiro ID vago.
- **`NovaSecaoMapa`**: Modal para adicionar uma secao reservada com contagem zero para um mapa especifico.
- **`EscolherAlcance`**: Confirmacao de seguranca para remocao em massa de spots/monstros (escopo local vs global).
- **`NovoSpawn`**: Modal detalhado para criacao de spots com validacao dinamica que alerta instantaneamente se a celula for parede ou zona segura.

---

### 2.7. `MainForm.cs` (Controlador Principal)
E a classe central que amarra a interface, os eventos e as regras de negocio:
- **Montagem da Arvore (`MontarArvore`)**: Varre os arquivos carregados e constroi a arvore `Arquivo -> Secao -> Mapa`.
- **Preenchimento da Grade (`PreencherGrade`)**: Atualiza o `DataGridView` de acordo com a secao ativa e os campos do arquivo.
- **Sincronizacao Bidirecional**:
  - Clicar na linha da tabela seleciona o ponto no mapa.
  - Clicar no ponto do mapa seleciona a linha na tabela.
  - Mover o ponto no mapa atualiza os campos $X$ e $Y$ na tabela e marca o arquivo como alterado.
- **Motor de Validacao (`Validar`)**:
  - Executado via tecla `F5`.
  - Percorre todas as linhas e valida: existencia do mob no `Monster.txt`, coordenadas de 0 a 255, monstros em paredes, spots com menos de 20% de piso livre, e checa se o total de monstros ultrapassa a constante de limite do GameServer (`limite = 6800`).
- **Motor de Organizacao (`Organizar`)**:
  - Reorganiza todo o arquivo de spawn agrupando por secao e mapa, ordenando por ID do monstro e coordenadas, e gerando cabecalhos legiveis.
- **Persistencia de Configuracao**:
  - `CarregarConfig` e `GravarConfig` salvam os ultimos caminhos utilizados em `MonsterSpawnStudio.ini`.

---

## 3. Roteiro Tecnico para os Upgrades (SSeMU 97K e MueMU Kayito 97K)

Ao atualizar este projeto para suporte nativo as versoes **97K SSeMU** e **97K MueMU Kayito**, os seguintes arquivos e metodos precisarao de intervencao tecnica:

### 3.1. Adaptacoes em `Dados.cs`:
1. **Matriz de Mapas (`NomeMapa`)**:
   - Ajustar para a lista de mapas do 97K:
     - 0: Lorencia, 1: Dungeon, 2: Devias, 3: Noria, 4: Lost Tower, 5: Exile, 6: Arena, 7: Atlans, 8: Tarkan, 9: Devil Square, 10: Icarus, 11 a 16: Blood Castle 1 a 6.
2. **Matriz de Terrenos (`ArquivoTerreno`)**:
   - No 97K, os arquivos `.att` estao geralmente em formato `Terrain1.att`, `Terrain01.att` ou extraidos do cliente como `EncTerrain*.att`.
   - Adicionar mecanismo de deteccao flexivel de nomes de arquivos de terreno.

### 3.2. Adaptacoes em `Modelo.cs`:
1. **Estrutura do `Monster.txt`**:
   - Nas sources 97K (Kayito / SSeMU), o `Monster.txt` possui colunas diferentes das 28 da Season 4 (frequentemente nao existem resistencias avancadas ou certos tipos de ataque implementados em seasons mais novas).
   - O array `ColMonster` e a funcao de escrita devem se adaptar ao formato da versao selecionada.
2. **Arquivos Inexistentes**:
   - No 97K nao existem `MonsterSetBaseCS.txt` e `KanturuMonsterSetBase.txt`.
   - Em `MainForm.CarregarPasta()`, a carga desses arquivos deve ser tratada como opcional ou condicional.

### 3.3. Adaptacoes em `MainForm.cs`:
1. **Limite de Monstros (`OBJ_MAXMONSTER`)**:
   - O limite padrao da Season 4 e 6.800. No 97K o limite do GameServer costuma variar entre 1.024 e 4.096 objetos. Tornar essa constante configuravel via `.ini` ou conforme a versao selecionada.
2. **Secoes do `MonsterSetBase.txt` 97K**:
   - Verificar a sintaxe exata das secoes utilizadas na source 97K (Secao 0 = NPCs, Secao 1 = Spots, Secao 2 = Monstros soltos, etc.).
