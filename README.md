# MonsterSpawn Studio IA

Editor visual e gerenciador de Spawn de Monstros e NPCs para servidores **MuOnline** (suporte a **97D Original**, **MuEmu Kayito 97K** e **SSeMU 97K**).

Projeto originalmente concebido por **Kellington**, com upgrades e arquitetura modular desenvolvidos pela **CMZoneMU**.

---

## 🚀 Funcionalidades

- **Visualização Gráfica Completa**: Renderização de mapas com colisão de terreno (`.att`) e texturas de minimapa (`.ozj`).
- **Suporte Híbrido de Estruturas**:
  - **MuEmu / 97D Clássico**: Suporte ao arquivo mestre unificado `MonsterSetBase.txt` e lista `Monster.txt`.
  - **SSeMU 97K**: Suporte à estrutura modular com pasta `Spawn/*.txt` (um arquivo por mapa) e lista `MonsterList.txt`.
  - **Auto-Detecção**: O programa identifica automaticamente o formato do servidor ao selecionar a pasta de dados.
  - **Seleção Manual**: Alternância direta via menu **Arquivo > Estrutura do Servidor**.
- **Edição Intuitiva**:
  - Clique duplo no mapa para posicionar mobs ou criar áreas de spot.
  - Arrastar e soltar (drag-and-drop) para reposicionar mobs diretamente no terreno.
  - Identificação de Safe Zones e paredes intransponíveis.
  - Contagem total e por seção de monstros em tempo real.

---

## 📁 Estrutura do Repositório

```text
MonsterSpawnStudioIA/
├── Source/                 # Codigo-fonte em C# .NET 10 (WinForms)
├── MarkDowns/              # Documentacao tecnica e guias de utilizacao
├── MonsterSpawnStudio/     # Distribuicao portatil (Release executavel)
└── Arquivos SSeMUe MuEmu/  # Exemplos e arquivos de teste (MuServer e Client)
```

---

## 🛠️ Compilação

Para compilar em modo **Release**:

```powershell
cd Source
dotnet build -c Release
```

Para publicar a distribuição portátil:

```powershell
dotnet publish -c Release -o ../MonsterSpawnStudio
```

---

## 📖 Documentação

Consulte os guias na pasta [`MarkDowns/`](./MarkDowns/):
1. **[1 - Estrutura do Programa MonsterSpawnStudio.md](./MarkDowns/1%20-%20Estrutura%20do%20Programa%20MonsterSpawnStudio.md)**
2. **[2 - Guia de Utilização.md](./MarkDowns/2%20-%20Guia%20de%20Utilização.md)**
3. **[3 - Estrutura da Source MonsterSpawnStudio.md](./MarkDowns/3%20-%20Estrutura%20da%20Source%20MonsterSpawnStudio.md)**
