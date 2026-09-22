using System.Text;

namespace MonsterSpawnStudio;

public enum ModoServidor { Auto, MuEmu, SSeMU }

public enum TipoArquivo { SetBase, Kanturu, Monster, SSeMUSpawn }

/// <summary>Uma linha de dados do arquivo (um monstro, um spot ou um monstro do Monster.txt).</summary>
public class Linha
{
	public int Secao;                 // secao a que pertence (0..4)
	public List<string> Campos = new();
	public string Nome = "";          // texto do comentario // depois dos dados
	public string Cru;                // linha original, usada quando nao da para interpretar

	public int Num(int i)
	{
		if (i < 0 || i >= Campos.Count) return 0;
		var c = Campos[i];
		if (c == "*") return -1;
		return int.TryParse(c, out var v) ? v : 0;
	}

	public void SetNum(int i, int v) { while (Campos.Count <= i) Campos.Add("0"); Campos[i] = v.ToString(); }

	public Linha Clonar()
	{
		return new Linha { Secao = Secao, Campos = new List<string>(Campos), Nome = Nome, Cru = Cru };
	}
}

/// <summary>Um bloco "numero ... end" do arquivo, com os comentarios que vem antes dele.</summary>
public class Bloco
{
	public List<string> Antes = new();   // comentarios/linhas em branco antes do bloco
	public int Secao;
	public List<Linha> Linhas = new();

	/// <summary>
	/// Mapa deste bloco quando ele esta vazio. Serve para o mapa continuar aparecendo
	/// na arvore depois de uma limpeza, para voce ir clicando e enchendo de novo.
	/// </summary>
	public int MapaReservado = -1;

	public bool TemDados => Linhas.Any(l => l.Cru == null);
}

public class ArquivoSpawn
{
	public string Caminho = "";
	public TipoArquivo Tipo;
	public int MapaFixo = -1;          // Usado quando o arquivo representa um mapa especifico (ex: SSeMU Spawn)
	public List<Bloco> Blocos = new();
	public List<string> Rodape = new();  // o que sobrar depois do ultimo end
	public bool Alterado;

	public string NomeCurto => Path.GetFileName(Caminho);

	// ---------------------------------------------------------------- colunas
	public static readonly string[] Col6 = { "Mob", "Mapa", "Raio", "X", "Y", "Dir" };
	public static readonly string[] Col9 = { "Mob", "Mapa", "Raio", "X1", "Y1", "X2", "Y2", "Dir", "Qtd" };
	public static readonly string[] Col5_SSeMU = { "Mob", "Raio", "X", "Y", "Dir" };
	public static readonly string[] Col8_SSeMU = { "Mob", "Raio", "X1", "Y1", "X2", "Y2", "Dir", "Qtd" };
	public static readonly string[] ColKanturu = { "Estado", "Mob", "Mapa", "Raio", "X", "Y", "Dir" };
	public static readonly string[] ColMonster =
	{
		"Numero", "Rate/Tipo", "Nome", "Level", "HP", "MP", "DanoMin", "DanoMax", "Defesa", "DefMagica",
		"Ataque", "Sucesso", "Move", "TipoAtaque", "Alcance", "Visao", "VelMove", "VelAtaque",
		"Regen", "Atributo", "TaxaItem", "TaxaZen", "MaxItem", "ResVento", "ResVeneno", "ResGelo",
		"ResAgua", "ResFogo"
	};

	public string[] Colunas(int secao)
	{
		if (Tipo == TipoArquivo.Monster) return ColMonster;
		if (Tipo == TipoArquivo.Kanturu) return ColKanturu;
		if (Tipo == TipoArquivo.SSeMUSpawn) return (secao == 1 || secao == 3) ? Col8_SSeMU : Col5_SSeMU;
		return (secao == 1 || secao == 3) ? Col9 : Col6;
	}

	/// <summary>Indice da coluna do mob, do mapa, do X e do Y (-1 quando nao existe).</summary>
	public (int mob, int mapa, int x, int y, int x2, int y2, int qtd) Indices(int secao)
	{
		if (Tipo == TipoArquivo.Monster) return (0, -1, -1, -1, -1, -1, -1);
		if (Tipo == TipoArquivo.Kanturu) return (1, 2, 4, 5, -1, -1, -1);
		if (Tipo == TipoArquivo.SSeMUSpawn)
		{
			if (secao == 1 || secao == 3) return (0, -1, 2, 3, 4, 5, 7);
			return (0, -1, 2, 3, -1, -1, -1);
		}
		if (secao == 1 || secao == 3) return (0, 1, 3, 4, 5, 6, 8);
		return (0, 1, 3, 4, -1, -1, -1);
	}

	public int MapaDaLinha(Linha l, int secao)
	{
		if (MapaFixo >= 0) return MapaFixo;
		var ix = Indices(secao);
		return ix.mapa >= 0 ? l.Num(ix.mapa) : -1;
	}

	public static TipoArquivo DetectarTipo(string caminho)
	{
		var n = Path.GetFileName(caminho).ToLowerInvariant();
		if (n.Contains("kanturu")) return TipoArquivo.Kanturu;
		if (n.StartsWith("monster.") || n.StartsWith("monsterlist.")) return TipoArquivo.Monster;
		var dir = Path.GetDirectoryName(caminho) ?? "";
		if (Path.GetFileName(dir).Equals("Spawn", StringComparison.OrdinalIgnoreCase)) return TipoArquivo.SSeMUSpawn;
		return TipoArquivo.SetBase;
	}

	// ----------------------------------------------------------------- leitura
	public static ArquivoSpawn Abrir(string caminho, TipoArquivo? tipoForcado = null, int mapaFixo = -1)
	{
		var a = new ArquivoSpawn { Caminho = caminho, Tipo = tipoForcado ?? DetectarTipo(caminho), MapaFixo = mapaFixo };
		if (a.Tipo == TipoArquivo.SSeMUSpawn && a.MapaFixo < 0)
		{
			var fn = Path.GetFileNameWithoutExtension(caminho);
			var partes = fn.Split(new[] { " - ", " ", "_" }, StringSplitOptions.RemoveEmptyEntries);
			if (partes.Length > 0 && int.TryParse(partes[0], out var m))
			{
				a.MapaFixo = m;
			}
		}
		var enc = Encoding.GetEncoding(1252);
		var linhas = File.ReadAllLines(caminho, enc);

		if (a.Tipo == TipoArquivo.Monster)
		{
			// lista solta, sem "numero ... end"
			var b = new Bloco { Secao = -1 };
			foreach (var l in linhas)
			{
				var dados = Corpo(l);
				if (dados.Length == 0) { b.Antes.Add(l); continue; }
				b.Linhas.Add(Interpretar(l, -1));
			}
			a.Blocos.Add(b);
			return a;
		}

		var pendentes = new List<string>();
		Bloco atual = null;

		foreach (var l in linhas)
		{
			var dados = Corpo(l);

			if (dados.Length == 0) { (atual == null ? pendentes : a.Rodape).Add(l); if (atual != null) { a.Rodape.RemoveAt(a.Rodape.Count - 1); atual.Linhas.Add(new Linha { Cru = l, Secao = atual.Secao }); } continue; }

			if (atual == null)
			{
				if (int.TryParse(dados.Trim(), out var sec) && dados.Trim().Length <= 2)
				{
					atual = new Bloco { Secao = sec, Antes = new List<string>(pendentes) };
					pendentes.Clear();
					continue;
				}
				pendentes.Add(l);
				continue;
			}

			if (dados.Trim().Equals("end", StringComparison.OrdinalIgnoreCase))
			{
				// tira as linhas em branco/comentario do fim do bloco e devolve para o proximo
				while (atual.Linhas.Count > 0 && atual.Linhas[^1].Cru != null)
				{
					pendentes.Insert(0, atual.Linhas[^1].Cru);
					atual.Linhas.RemoveAt(atual.Linhas.Count - 1);
				}
				a.Blocos.Add(atual);
				atual = null;
				continue;
			}

			atual.Linhas.Add(Interpretar(l, atual.Secao));
		}

		if (atual != null) a.Blocos.Add(atual);
		a.Rodape = pendentes;
		return a;
	}

	static string Corpo(string l)
	{
		var i = l.IndexOf("//", StringComparison.Ordinal);
		var s = (i >= 0 ? l.Substring(0, i) : l);
		return s.Trim().Length == 0 ? "" : s;
	}

	static Linha Interpretar(string l, int secao)
	{
		var linha = new Linha { Secao = secao };
		var i = l.IndexOf("//", StringComparison.Ordinal);
		var corpo = i >= 0 ? l.Substring(0, i) : l;
		linha.Nome = i >= 0 ? l.Substring(i + 2).Trim() : "";

		// o Monster.txt tem o nome entre aspas no meio dos numeros
		var partes = new List<string>();
		var sb = new StringBuilder();
		bool aspas = false;
		foreach (var c in corpo)
		{
			if (c == '"') { aspas = !aspas; sb.Append(c); continue; }
			if (!aspas && (c == ' ' || c == '\t'))
			{
				if (sb.Length > 0) { partes.Add(sb.ToString()); sb.Clear(); }
				continue;
			}
			sb.Append(c);
		}
		if (sb.Length > 0) partes.Add(sb.ToString());

		linha.Campos = partes;
		return linha;
	}

	// ------------------------------------------------------------------ escrita
	public void Salvar(string caminho = null)
	{
		caminho ??= Caminho;
		var enc = Encoding.GetEncoding(1252);
		var sb = new StringBuilder();

		foreach (var b in Blocos)
		{
			foreach (var c in b.Antes) sb.Append(c).Append("\r\n");
			if (b.Secao >= 0) sb.Append(b.Secao).Append("\r\n");

			foreach (var l in b.Linhas)
			{
				if (l.Cru != null) { sb.Append(l.Cru).Append("\r\n"); continue; }
				sb.Append(Formatar(l)).Append("\r\n");
			}

			if (b.Secao >= 0) sb.Append("end\r\n");
		}

		foreach (var c in Rodape) sb.Append(c).Append("\r\n");

		File.WriteAllText(caminho, sb.ToString(), enc);
		Alterado = false;
	}

	public string Formatar(Linha l)
	{
		var sb = new StringBuilder();
		if (Tipo != TipoArquivo.Monster) sb.Append('\t');

		for (int i = 0; i < l.Campos.Count; i++)
		{
			if (i > 0) sb.Append('\t');
			sb.Append(l.Campos[i]);
		}

		if (!string.IsNullOrWhiteSpace(l.Nome)) sb.Append("\t//").Append(l.Nome);
		return sb.ToString();
	}

	// ------------------------------------------------------------------ apoio
	public IEnumerable<(Bloco bloco, Linha linha)> Todas()
	{
		foreach (var b in Blocos)
			foreach (var l in b.Linhas)
				if (l.Cru == null) yield return (b, l);
	}

	/// <summary>Quantos objetos o servidor cria com este arquivo.</summary>
	public int TotalObjetos()
	{
		if (Tipo == TipoArquivo.Monster) return 0;
		int t = 0;
		foreach (var (b, l) in Todas())
		{
			var ix = Indices(b.Secao);
			t += ix.qtd >= 0 ? Math.Max(1, l.Num(ix.qtd)) : 1;
		}
		return t;
	}
}
