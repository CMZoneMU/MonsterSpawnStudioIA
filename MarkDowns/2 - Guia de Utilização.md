# Guia de Utilizacao - MonsterSpawn Studio

## 1. Introducao
O **MonsterSpawn Studio** foi projetado para facilitar a criacao, edicao e organizacao de monstros, NPCs e spots nos servidores de MuOnline. Este guia apresenta o passo a passo completo para utilizar todas as ferramentas e funcionalidades do programa.

---

## 2. Requisitos do Sistema e Instalacao
- **Sistema Operacional**: Windows 10 ou Windows 11 (64-bit).
- **Ambiente de Execucao**: .NET 10.0 Desktop Runtime (ou SDK correspondente).
- **Execucao**: O programa nao requer instalador formal. Basta executar o binario `MonsterSpawnStudio.exe`.

---

## 3. Primeira Utilizacao (Configurando as Pastas)

Ao abrir o programa pela primeira vez, a tela estara vazia. E necessario vincular as pastas do **MuServer** e do **Cliente**:

### Passo 1: Apontar a pasta do Servidor
1. No menu superior, clique em **Arquivo > Abrir pasta do servidor...** (ou pressione `Ctrl+O`).
2. Selecione a pasta `Data` do seu MuServer (por exemplo: `C:\MuServer\Data`).
   > *Nota*: O programa aceita tanto a pasta `Data` quanto a subpasta `Data\Monster`.

### Passo 2: Apontar a pasta do Cliente
1. No menu superior, clique em **Arquivo > Apontar a pasta Data do cliente (minimapas)...**.
2. Selecione a pasta `Data` do seu cliente de jogo (onde se encontram as pastas `World1`, `World2`, etc.).

### Gravacao Automatica
Os caminhos selecionados sao salvos no arquivo de configuracao `MonsterSpawnStudio.ini`. Nas proximas inicializacoes, o programa abrira diretamente as pastas salvas.

---

## 4. Conhecendo a Interface

A tela principal do MonsterSpawn Studio e dividida em 5 paineis integrados:

```
+-----------------------------------------------------------------------------+
| ARVORE (Esq. Cima)        | TABELA (Centro Cima)   | MAPA (Direita)         |
| Navegacao por arquivos,   | Edicao de colunas,     | Renderizacao 2D,       |
| secoes e mapas.           | valores e comentarios. | paredes e pontos.      |
+---------------------------+------------------------+------------------------+
| PALETA (Esq. Baixo)       | CONFERENCIA (Centro)   | CONTROLES DO MAPA      |
| Lista de mobs com filtros | Validacao em tempo     | Camadas, brilho, zoom  |
| e modo rapido de clique.  | real de erros (F5).    | e coordenadas do jogo. |
+----------------------------------------------------+------------------------+
| RODAPE: Monitor de capacidade de objetos do GameServer (OBJ_MAXMONSTER)     |
+-----------------------------------------------------------------------------+
```

---

## 5. Operando o Mapa Grafico Interativo

O visualizador a direita e o coracao do programa. Ele projeta com exatidao as coordenadas do MuOnline ($X=0..255$, $Y=0..255$).

### Controles do Mouse:
- **Roda do Mouse (Scroll)**: Ajusta o Zoom aproximando ou afastando o mapa com base na posicao do ponteiro.
- **Botao do Meio (Scroll Pressionado)** ou **Espaco + Clique Esquerdo**: Arrastar e mover a visualizacao pelo mapa (Pan).
- **Clique Esquerdo em um Ponto**: Seleciona o monstro ou spot no mapa e seleciona automaticamente a linha correspondente na tabela.
- **Arrastar um Ponto**: Move a coordenada do monstro no mapa. As colunas $X$ e $Y$ na tabela central sao atualizadas em tempo real.
- **Clique Esquerdo no Vazio**: Se o modo rapido estiver ativo, insere o monstro escolhido naquelas coordenadas. Se nao, abre a janela de cadastro preenchida com a coordenada.

### Camadas Visuais e Legenda de Cores:
1. **Fundo do Minimapa**:
   - Imagem do mapa extraida dos arquivos `.ozj` do cliente (`Data\World<N>\Map1.ozj`).
   - Nove mapas possuem textura de minimapa nativa no cliente (Lorencia, Dungeon, Devias, Noria, Lost Tower, Arena, Atlans, Tarkan e Icarus). Nos demais mapas, o fundo e cinza claro e a navegacao e orientada pelas paredes do servidor.
2. **Camada de Terreno do Servidor (`.att`)**:
   - **Vermelho**: Parede ou obstaculo bloqueado (o jogador e monstros normais nao andam).
   - **Azul**: Zona Segura (Safe Zone / Cidade).
   - **Amarelo**: Sem chao / abismo / agua intransitavel.
3. **Paredes e Filtro de Pedrinhas**:
   - O menu suspenso **Paredes** permite alternar entre `contorno` (apenas a borda externa da parede para nao tampar o minimapa), `cheio` (visibilidade de colisao total) e `nenhuma`.
   - O controle **Detalhe** (`so as paredes`, `medio`, `tudo`) remove ruidos visuais como pequenas pedras soltas (ex: em Noria) para destacar as paredes principais.
4. **Marcadores de Monstros e Spots**:
   - **Circulo Verde**: Monstro individual ou posicao base de spawn.
   - **Circulo Amarelo/Dourado**: Monstro atualmente selecionado.
   - **Retangulo Amarelo Semitransparente**: Area limite do spot (delimitada de $X_1, Y_1$ ate $X_2, Y_2$).

---

## 6. Criando e Adicionando Monstros / Spots

Existem duas formas principais de adicionar monstros no mapa:

### 6.1. O Modo Rapido (Dia a Dia)
Ideal para criar spots rapida e visualmente:
1. Na **Arvore (esquerda)**, clique na secao (ex: `1 - Spots`) e selecione o mapa desejado (ex: `Lorencia`).
2. Na **Paleta de Monstros (embaixo)**, localize o monstro desejado (use o campo de busca se necessario).
3. Marque a caixa de selecao **"clique no mapa adiciona"**.
4. Ajuste os campos **Qtd** (quantidade de mobs no spot) e **Raio** (raio de movimentacao).
5. Va clicando nos locais desejados no mapa.
6. A cada clique, uma nova linha e inserida no script, ja com as coordenadas corretas e o comentario preenchido com o nome do monstro.

### 6.2. O Modo Janela (Edicao Precisa)
Ideal para configuracoes manuais:
1. Pressione `Ctrl+N` ou va em **Editar > Adicionar monstro / spot...**.
2. Na janela que se abre:
   - Escolha o monstro no menu suspenso.
   - Selecione o mapa.
   - Digite as coordenadas iniciais ($X_1, Y_1$) e finais ($X_2, Y_2$) se for um spot.
   - Defina raio, quantidade e direcao.
3. A janela avisa na hora se a coordenada digitada cai em parede ou zona segura.
4. Clique em **Adicionar**.

---

## 7. Outras Operacoes de Edicao

- **Trocar o Monstro de uma Linha (`Ctrl+E`)**:
  Selecione a linha na tabela e pressione `Ctrl+E`. Escolha o novo monstro na lista. O ID e o comentario da linha serao atualizados automaticamente sem alterar as coordenadas do spot.
- **Duplicar Linha (`Ctrl+D` ou `Ctrl+Shift+N`)**:
  Cria uma copia identica da linha selecionada imediatamente abaixo.
- **Remover Linha (`Ctrl+Delete`)**:
  Exclui a linha selecionada da tabela e remove o ponto visual do mapa.
- **Cadastrar Novo Monstro no `Monster.txt` (`Ctrl+M`)**:
  Abre o formulario completo com os 28 campos de atributos do monstro (HP, MP, Level, Defesa, Dano Min/Max, Resistencias, etc.). O programa calcula e sugere automaticamente o proximo ID livre disponivel.

---

## 8. Criacao de Secoes e Remocao em Massa

### 8.1. Criar Secao de um Mapa (`Ctrl+K`)
Quando voce quer comecar a colocar monstros em um mapa que ainda nao tem nenhuma entrada no arquivo:
1. Va em **Editar > Criar secao de um mapa...** (`Ctrl+K`).
2. Selecione a Secao (ex: `1 - Spots`) e o Mapa (ex: `Tarkan`).
3. O mapa passara a constar na arvore com a indicacao `(0)`.
4. Basta clicar sobre ele, escolher os monstros na paleta e ir clicando no mapa para preencher.

### 8.2. Remocao em Massa
Permite limpar rapidamente conteudos sem apagar manualmente centenas de linhas:
1. Va em **Editar > Remover em massa** e escolha a categoria:
   - Remover todos os SPOTS (secao 1)
   - Remover todos os MONSTROS soltos (secao 2)
   - Remover todos os NPCs (secao 0)
   - Remover todos os GOLDEN / Bone King (secao 3)
2. Uma caixa de dialogo exibira o total de linhas afetadas e permitira escolher o alcance:
   - **"So o mapa [Nome]"**: Limpa apenas os monstros daquele mapa. O mapa continuara na arvore com `(0)` pronto para ser repovoado.
   - **"O arquivo inteiro"**: Limpa todas as linhas daquela secao em todos os mapas do arquivo.

---

## 9. Ferramentas: Conferencia e Organizacao

### 9.1. Conferencia do Servidor (`F5`)
Pressione `F5` a qualquer momento para executar uma varredura automatica de integridade nos arquivos abertos. O programa verifica:
- Monstros com ID que nao existe no `Monster.txt`.
- Coordenadas invalidas menores que 0 ou maiores que 255.
- Monstros normais posicionados dentro de paredes no terreno do servidor.
- Spots com menos de 20% de piso livre (onde os monstros ficariam presos ou nao nasceriam).
- Spots com quantidade zerada (`0`) ou negativa.
- Total de monstros gerados contra o limite maximo do GameServer (`OBJ_MAXMONSTER = 6800`).

> **Dica**: Dando um **duplo clique** sobre qualquer item da lista de problemas, a interface seleciona automaticamente o arquivo, a secao, o mapa e foca diretamente na linha problematica.

### 9.2. Organizar Arquivo Atual
Localizado em **Ferramentas > Organizar arquivo atual**:
- Reestrutura todo o arquivo selecionado, agrupando os blocos por secao e por mapa de forma uniforme.
- Ordena as linhas por ID do monstro e por coordenadas ($X$ e $Y$).
- Preenche automaticamente o comentario de cada linha com o nome correto do monstro caso esteja vazio.
- Padroniza os cabecalhos de bloco em molduras legiveis.

---

## 10. Salvando os Arquivos

- **Salvar Arquivo Atual**: `Ctrl+S`
- **Salvar Todos os Arquivos**: `Ctrl+Shift+S`

Os arquivos sao codificados em **Windows-1252 (CP1252)** com quebras de linha padrao Windows (`CRLF`), garantindo 100% de compatibilidade com os binarios do GameServer.

> **Importante**: Apos salvar as alteracoes, e necessario reiniciar o GameServer ou utilizar a opcao de recarregamento de eventos/monstros no menu do GameServer para que as alteracoes entrem em vigor no servidor.

---

## 11. Tabela de Atalhos de Teclado

| Atalho | Acao |
|:-------|:-----|
| `Ctrl + O` | Abrir pasta Data do MuServer |
| `Ctrl + S` | Salvar arquivo atualmente selecionado |
| `Ctrl + Shift + S` | Salvar todos os arquivos abertos |
| `Ctrl + N` | Adicionar monstro ou spot via janela detalhada |
| `Ctrl + M` | Criar novo monstro no `Monster.txt` |
| `Ctrl + E` | Trocar monstro da linha selecionada |
| `Ctrl + K` | Criar secao vazia de um mapa |
| `Ctrl + D` | Duplicar linha selecionada |
| `Ctrl + Shift + N`| Copiar linha selecionada |
| `Ctrl + Delete` | Remover linha selecionada |
| `F5` | Executar conferencia e validacao de integridade |
| `F6` | Recarregar arquivos do disco |
| `F1` | Exibir guia rapido de ajuda |
