using System.Drawing.Imaging;
using System.Text;

namespace MonsterSpawnStudio;

/// <summary>Nomes dos monstros (Monster.txt), nomes dos mapas, terreno e minimapa.</summary>
public static class Dados
{
	public static string PastaData = "";      // ...\MuServer1.00.18\Data
	public static string PastaCliente = "";   // ...\Cliente\Data  (para os minimapas)

	public static readonly Dictionary<int, string> Monstros = new();
	static readonly Dictionary<int, byte[]> s_terreno = new();
	static readonly Dictionary<int, Bitmap> s_minimapa = new();

	// Update SSeMU 92 2.4.9 -> 97K SSeMU Update 93 (2.4.9) - Mapas 97K oficiais (0 a 16)
	public static readonly string[] ArquivoTerreno =
	{
		"00 - Lorencia.att", "01 - Dungeon.att", "02 - Devias.att", "03 - Noria.att", "04 - Lost Tower.att",
		"05 - Exile.att", "06 - Arena.att", "07 - Atlans.att", "08 - Tarkan.att", "09 - Devil Square.att",
		"10 - Icarus.att", "11 - Blood Castle.att", "11 - Blood Castle.att", "11 - Blood Castle.att",
		"11 - Blood Castle.att", "11 - Blood Castle.att", "11 - Blood Castle.att"
	};

	public static readonly string[] NomeMapa =
	{
		"Lorencia", "Dungeon", "Devias", "Noria", "Lost Tower", "Exile", "Arena", "Atlans", "Tarkan",
		"Devil Square", "Icarus", "Blood Castle 1", "Blood Castle 2", "Blood Castle 3", "Blood Castle 4",
		"Blood Castle 5", "Blood Castle 6"
	};

	// Update SSeMU 92 2.4.9 -> 97K SSeMU Update 93 (2.4.9) - Suporte a mapas dinamicos do cliente e reconhecimento automatico
	static readonly SortedDictionary<int, string> s_mapasCadastrados = new();

	static Dados()
	{
		InicializarMapas();
	}

	public static void InicializarMapas()
	{
		s_mapasCadastrados.Clear();
		for (int i = 0; i < NomeMapa.Length; i++)
		{
			s_mapasCadastrados[i] = NomeMapa[i];
		}
	}

	public static void DescobrirMapasCliente(string pastaCliente)
	{
		if (s_mapasCadastrados.Count == 0) InicializarMapas();
		if (string.IsNullOrEmpty(pastaCliente) || !Directory.Exists(pastaCliente)) return;

		try
		{
			var dirs = Directory.GetDirectories(pastaCliente, "World*", SearchOption.TopDirectoryOnly);
			foreach (var dir in dirs)
			{
				var nomeDir = Path.GetFileName(dir);
				if (nomeDir.Length > 5 && int.TryParse(nomeDir.Substring(5), out int numWorld) && numWorld >= 1)
				{
					int idMapa = numWorld - 1;
					if (!s_mapasCadastrados.ContainsKey(idMapa))
					{
						s_mapasCadastrados[idMapa] = $"Mapa {idMapa} ({nomeDir})";
					}
				}
			}
		}
		catch { }
	}

	public static void RegistrarNomeMapa(int mapa, string nome)
	{
		if (s_mapasCadastrados.Count == 0) InicializarMapas();
		if (mapa >= 0 && !string.IsNullOrWhiteSpace(nome))
		{
			s_mapasCadastrados[mapa] = nome.Trim();
		}
	}

	public static bool ExisteMapa(int mapa)
	{
		if (s_mapasCadastrados.Count == 0) InicializarMapas();
		return s_mapasCadastrados.ContainsKey(mapa);
	}

	public static List<(int Id, string Nome)> ListaMapas()
	{
		if (s_mapasCadastrados.Count == 0) InicializarMapas();
		return s_mapasCadastrados.Select(kv => (kv.Key, kv.Value)).ToList();
	}

	// mapa do servidor -> pasta World do cliente (World = mapa + 1, com Blood Castle 1..6 usando World12 se nao houver World individual)
	public static string PastaWorld(int mapa)
	{
		if (mapa >= 11 && mapa <= 16)
		{
			if (string.IsNullOrEmpty(PastaCliente) || !Directory.Exists(Path.Combine(PastaCliente, "World" + (mapa + 1))))
				return "World12";
		}
		return "World" + (mapa + 1);
	}

	public static string Mapa(int m)
	{
		if (s_mapasCadastrados.TryGetValue(m, out var nome)) return nome;
		if (m >= 0 && m < NomeMapa.Length) return NomeMapa[m];
		return "Mapa " + m;
	}

	public static string Monstro(int id) => Monstros.TryGetValue(id, out var n) ? n : "?";

	// ------------------------------------------------------------- Monster.txt
	public static void CarregarMonstros(string caminho)
	{
		Monstros.Clear();
		if (!File.Exists(caminho)) return;

		foreach (var l in File.ReadAllLines(caminho, Encoding.GetEncoding(1252)))
		{
			var i = l.IndexOf("//", StringComparison.Ordinal);
			var corpo = i >= 0 ? l.Substring(0, i) : l;
			if (corpo.Trim().Length == 0) continue;

			var asp1 = corpo.IndexOf('"');
			var asp2 = asp1 >= 0 ? corpo.IndexOf('"', asp1 + 1) : -1;
			if (asp1 < 0 || asp2 < 0) continue;

			var antes = corpo.Substring(0, asp1).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (antes.Length == 0 || !int.TryParse(antes[0], out var id)) continue;

			Monstros[id] = corpo.Substring(asp1 + 1, asp2 - asp1 - 1);
		}
	}

	// ------------------------------------------------------------------ areas
	public class Area
	{
		public string Nome;
		public int Mapa, X1, Y1, X2, Y2, Level;
		public override string ToString() => Nome;
	}

	static readonly Dictionary<int, List<Area>> s_areas = new();

	/// <summary>
	/// Areas com nome de cada mapa, montadas com o MoveReq (nome, level) mais o
	/// Gate.txt (mapa e retangulo). E o mesmo que o /move usa no jogo.
	/// </summary>
	public static List<Area> Areas(int mapa)
	{
		if (s_areas.Count == 0) CarregarAreas();
		return s_areas.TryGetValue(mapa, out var l) ? l : new List<Area>();
	}

	public static void CarregarAreas()
	{
		s_areas.Clear();
		if (PastaData == "") return;

		// Gate.txt: Numero Flag Mapa X1 Y1 X2 Y2 Destino Dir Level
		var gates = new Dictionary<int, int[]>();
		var pg = Path.Combine(PastaData, "Move", "Gate.txt");
		if (!File.Exists(pg)) pg = Path.Combine(PastaData, "Gate.txt");

		if (File.Exists(pg))
		{
			foreach (var l in File.ReadAllLines(pg, Encoding.GetEncoding(1252)))
			{
				var s = l.Split(new[] { "//" }, StringSplitOptions.None)[0].Trim();
				if (s.Length == 0) continue;
				var c = s.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
				if (c.Length < 7 || !int.TryParse(c[0], out var n)) continue;

				var v = new int[6];
				bool ok = true;
				for (int i = 0; i < 6; i++) ok &= int.TryParse(c[i + 2], out v[i]);
				if (ok) gates[n] = v;   // mapa, x1, y1, x2, y2, destino
			}
		}

		// MoveReq: indice Nome Nome Zen Level Gate
		foreach (var idioma in new[] { "Kor", "Eng", "Por" })
		{
			var pm = Path.Combine(PastaData, "Lang", idioma, $"MoveReq({idioma}).txt");
			if (!File.Exists(pm)) continue;

			foreach (var l in File.ReadAllLines(pm, Encoding.GetEncoding(1252)))
			{
				var s = l.Split(new[] { "//" }, StringSplitOptions.None)[0].Trim();
				if (s.Length == 0) continue;
				var c = s.Split(new[] { ' ', '	' }, StringSplitOptions.RemoveEmptyEntries);
				if (c.Length < 6 || !int.TryParse(c[0], out _)) continue;
				if (!int.TryParse(c[^1], out var gate) || !gates.TryGetValue(gate, out var g)) continue;

				int.TryParse(c[^2], out var level);

				var a = new Area
				{
					Nome = c[1].Trim('"'), Mapa = g[0],
					X1 = Math.Min(g[1], g[3]), Y1 = Math.Min(g[2], g[4]),
					X2 = Math.Max(g[1], g[3]), Y2 = Math.Max(g[2], g[4]),
					Level = level
				};

				if (!s_areas.TryGetValue(a.Mapa, out var lista)) s_areas[a.Mapa] = lista = new List<Area>();
				if (!lista.Any(x => x.Nome == a.Nome)) lista.Add(a);
			}
			break;
		}
	}

	/// <summary>Nome da area onde a coordenada cai (vazio quando nao cai em nenhuma).</summary>
	public static string AreaDe(int mapa, int x, int y)
	{
		foreach (var a in Areas(mapa))
			if (x >= a.X1 && x <= a.X2 && y >= a.Y1 && y <= a.Y2) return a.Nome;
		return "";
	}

	// ---------------------------------------------------------------- terreno
	/// <summary>Bytes de atributo do mapa (256x256). Bit 4 = nao anda, bit 1 = zona segura.</summary>
	public static byte[] Terreno(int mapa)
	{
		if (s_terreno.TryGetValue(mapa, out var t)) return t;
		s_terreno[mapa] = null;

		if (mapa < 0) return null;

		// Busca arquivo de terreno no servidor ou no cliente
		string p = null;
		if (PastaData != "")
		{
			if (mapa < ArquivoTerreno.Length)
			{
				var cand = Path.Combine(PastaData, "Terrain", ArquivoTerreno[mapa]);
				if (File.Exists(cand)) p = cand;
			}
			if (p == null)
			{
				string[] candidatos =
				{
					Path.Combine(PastaData, "Terrain", $"Terrain{mapa + 1}.att"),
					Path.Combine(PastaData, "Terrain", $"Terrain{mapa}.att"),
					Path.Combine(PastaData, "Terrain", $"Terrain{mapa:D2}.att"),
					Path.Combine(PastaData, $"Terrain{mapa + 1}.att"),
					Path.Combine(PastaData, $"Terrain{mapa}.att")
				};
				p = candidatos.FirstOrDefault(File.Exists);
			}
		}

		if (p == null && PastaCliente != "")
		{
			var pastaW = Path.Combine(PastaCliente, PastaWorld(mapa));
			if (Directory.Exists(pastaW))
			{
				string[] candidatosCli =
				{
					Path.Combine(pastaW, $"Terrain{mapa + 1}.att"),
					Path.Combine(pastaW, $"Terrain{mapa}.att"),
					Path.Combine(pastaW, $"EncTerrain{mapa + 1}.att"),
					Path.Combine(pastaW, $"EncTerrain{mapa}.att")
				};
				p = candidatosCli.FirstOrDefault(File.Exists);
				if (p == null)
				{
					var atts = Directory.GetFiles(pastaW, "*.att");
					if (atts.Length > 0) p = atts[0];
				}
			}
		}

		if (p == null || !File.Exists(p)) return null;

		var d = File.ReadAllBytes(p);
		if (d.Length < 65536) return null;

		var buf = new byte[65536];
		Array.Copy(d, d.Length - 65536, buf, 0, 65536);
		s_terreno[mapa] = buf;
		return buf;
	}

	public static bool Parede(int mapa, int x, int y)
	{
		var t = Terreno(mapa);
		if (t == null || x < 0 || x > 255 || y < 0 || y > 255) return false;
		return (t[y * 256 + x] & 4) != 0;
	}

	public static bool ZonaSegura(int mapa, int x, int y)
	{
		var t = Terreno(mapa);
		if (t == null || x < 0 || x > 255 || y < 0 || y > 255) return false;
		return (t[y * 256 + x] & 1) != 0;
	}

	// --------------------------------------------------------------- minimapa
	/// <summary>
	/// Minimapa do cliente (Data\World&lt;N&gt;\Map1.ozj ou MiniMap.ozj). O OZJ e um JPEG com 24 bytes de
	/// cabecalho. A imagem vem como aparece no jogo: a linha 0 dela e o Y 255 do servidor.
	/// </summary>
	public static Bitmap Minimapa(int mapa)
	{
		if (s_minimapa.TryGetValue(mapa, out var b)) return b;
		s_minimapa[mapa] = null;

		if (PastaCliente == "") return null;

		var pasta = Path.Combine(PastaCliente, PastaWorld(mapa));
		if (!Directory.Exists(pasta)) return null;

		string arq = null;
		foreach (var f in Directory.GetFiles(pasta, "*.ozj"))
		{
			var n = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
			if (n.Contains("map")) { arq = f; break; }
		}
		if (arq == null)
		{
			var ozjs = Directory.GetFiles(pasta, "*.ozj");
			if (ozjs.Length > 0) arq = ozjs[0];
		}
		if (arq == null) return null;

		try
		{
			var d = File.ReadAllBytes(arq);
			if (d.Length <= 24) return null;

			using var ms = new MemoryStream(d, 24, d.Length - 24);
			using var tmp = new Bitmap(ms);
			var img = new Bitmap(tmp.Width, tmp.Height, PixelFormat.Format24bppRgb);
			using (var g = Graphics.FromImage(img)) g.DrawImage(tmp, 0, 0, tmp.Width, tmp.Height);
			s_minimapa[mapa] = img;
			return img;
		}
		catch { return null; }
	}

	public static void Limpar()
	{
		s_areas.Clear();
		s_terreno.Clear();
		foreach (var b in s_minimapa.Values) b?.Dispose();
		s_minimapa.Clear();
		InicializarMapas();
		if (!string.IsNullOrEmpty(PastaCliente))
			DescobrirMapasCliente(PastaCliente);
	}
}
