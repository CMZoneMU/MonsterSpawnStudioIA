namespace MonsterSpawnStudio;

static class Estilo
{
	public static readonly Color Fundo = Color.FromArgb(24, 24, 27);
	public static readonly Color Painel = Color.FromArgb(32, 32, 36);
	public static readonly Color Texto = Color.Gainsboro;
	public static readonly Color Destaque = Color.FromArgb(240, 190, 80);

	public static Form Janela(string titulo, int largura, int altura)
	{
		return new Form
		{
			Text = titulo,
			BackColor = Fundo,
			ForeColor = Texto,
			Font = new Font("Segoe UI", 9f),
			FormBorderStyle = FormBorderStyle.FixedDialog,
			MaximizeBox = false,
			MinimizeBox = false,
			StartPosition = FormStartPosition.CenterParent,
			ClientSize = new Size(largura, altura)
		};
	}

	public static Button Botao(string texto, int x, int y, int largura = 110)
	{
		var b = new Button
		{
			Text = texto, Left = x, Top = y, Width = largura, Height = 30,
			FlatStyle = FlatStyle.Flat, ForeColor = Texto, BackColor = Painel
		};
		b.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
		return b;
	}

	public static Label Rotulo(string texto, int x, int y, int largura = 110)
		=> new() { Text = texto, Left = x, Top = y + 3, Width = largura, ForeColor = Texto, AutoSize = false };

	public static TextBox Campo(string valor, int x, int y, int largura = 90)
		=> new() { Text = valor, Left = x, Top = y, Width = largura, BackColor = Painel, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle };
}

/// <summary>Monta o texto "17 - Cyclops   [Lost Tower, Devil Square]".</summary>
static class ListaMonstros
{
	public static Dictionary<int, SortedSet<int>> Mapas(ArquivoSpawn arq)
	{
		var onde = new Dictionary<int, SortedSet<int>>();
		if (arq == null || arq.Tipo == TipoArquivo.Monster) return onde;

		foreach (var (b, l) in arq.Todas())
		{
			var mapa = arq.MapaDaLinha(l, b.Secao);
			if (mapa < 0) continue;

			var ix = arq.Indices(b.Secao);
			var mob = l.Num(ix.mob);
			if (!onde.TryGetValue(mob, out var s)) onde[mob] = s = new SortedSet<int>();
			s.Add(mapa);
		}
		return onde;
	}

	public static string Texto(int id, string nome, Dictionary<int, SortedSet<int>> onde, string separador = " - ")
	{
		var s = $"{id}{separador}{nome}";
		if (onde != null && onde.TryGetValue(id, out var mapas) && mapas.Count > 0)
		{
			var nomes = mapas.Take(3).Select(Dados.Mapa);
			s += "   [" + string.Join(", ", nomes) + (mapas.Count > 3 ? ", +" + (mapas.Count - 3) : "") + "]";
		}
		return s;
	}
}

/// <summary>Lista de monstros do Monster.txt com filtro, para escolher o id.</summary>
public static class EscolherMonstro
{
	public static int Mostrar(IWin32Window pai, int idAtual, ArquivoSpawn arq = null)
	{
		using var f = Estilo.Janela("Escolher monstro", 420, 520);
		f.FormBorderStyle = FormBorderStyle.Sizable;

		var filtro = Estilo.Campo("", 12, 12, 396);
		filtro.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		filtro.PlaceholderText = "digite parte do nome ou o numero";

		var lista = new ListBox
		{
			Left = 12, Top = 46, Width = 396, Height = 420,
			BackColor = Estilo.Painel, ForeColor = Estilo.Texto, BorderStyle = BorderStyle.FixedSingle,
			Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
			IntegralHeight = false
		};

		var ok = Estilo.Botao("Usar", 188, 476);
		var cancelar = Estilo.Botao("Cancelar", 302, 476);
		ok.Anchor = cancelar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

		var onde = ListaMonstros.Mapas(arq);
		var todos = Dados.Monstros.OrderBy(p => p.Key)
					.Select(p => ListaMonstros.Texto(p.Key, p.Value, onde, "    ")).ToList();

		void Encher()
		{
			var t = filtro.Text.Trim();
			lista.BeginUpdate();
			lista.Items.Clear();
			foreach (var s in todos)
				if (t.Length == 0 || s.Contains(t, StringComparison.OrdinalIgnoreCase)) lista.Items.Add(s);
			lista.EndUpdate();
		}

		filtro.TextChanged += (_, _) => Encher();
		Encher();

		foreach (var o in lista.Items)
			if (o is string s && int.TryParse(s.Split(' ')[0], out var id) && id == idAtual) { lista.SelectedItem = o; break; }

		int escolhido = -1;
		void Confirmar()
		{
			if (lista.SelectedItem is string s && int.TryParse(s.Split(' ')[0], out var id)) escolhido = id;
			f.DialogResult = DialogResult.OK;
			f.Close();
		}

		ok.Click += (_, _) => Confirmar();
		lista.DoubleClick += (_, _) => Confirmar();
		cancelar.Click += (_, _) => { f.DialogResult = DialogResult.Cancel; f.Close(); };

		f.Controls.AddRange(new Control[] { filtro, lista, ok, cancelar });
		f.AcceptButton = ok;
		f.CancelButton = cancelar;
		f.ShowDialog(pai);
		return escolhido;
	}
}

/// <summary>Tabela para criar um monstro novo no Monster.txt.</summary>
public static class NovoMonstro
{
	// valores de partida, tirados de um monstro comum
	static readonly string[] Padrao =
	{
		"0", "1", "\"Novo Monstro\"", "1", "100", "0", "10", "15", "5", "0", "20", "5", "3", "0", "1", "5",
		"400", "1800", "10", "2", "120", "10", "6", "0", "0", "0", "0", "0"
	};

	public static Linha Mostrar(IWin32Window pai, ArquivoSpawn arqMonster)
	{
		using var f = Estilo.Janela("Novo monstro (Monster.txt)", 720, 560);

		var colunas = ArquivoSpawn.ColMonster;
		var campos = new TextBox[colunas.Length];

		// sugere o primeiro numero livre
		int livre = 0;
		while (Dados.Monstros.ContainsKey(livre)) livre++;

		var painel = new Panel { Left = 10, Top = 10, Width = 698, Height = 480, AutoScroll = true, BackColor = Estilo.Fundo };

		for (int i = 0; i < colunas.Length; i++)
		{
			int col = i % 2, lin = i / 2;
			int x = 8 + col * 345, y = 8 + lin * 30;

			painel.Controls.Add(Estilo.Rotulo(colunas[i], x, y, 120));

			var valor = Padrao[i];
			if (i == 0) valor = livre.ToString();
			var c = Estilo.Campo(valor.Trim('"'), x + 126, y, 190);
			campos[i] = c;
			painel.Controls.Add(c);
		}

		var ok = Estilo.Botao("Criar", 486, 500);
		var cancelar = Estilo.Botao("Cancelar", 600, 500);

		Linha nova = null;
		ok.Click += (_, _) =>
		{
			if (!int.TryParse(campos[0].Text.Trim(), out var id))
			{
				MessageBox.Show(f, "O numero do monstro tem que ser inteiro.", "Novo monstro"); return;
			}
			if (Dados.Monstros.ContainsKey(id) &&
				MessageBox.Show(f, $"Ja existe o monstro {id} ({Dados.Monstro(id)}). Criar assim mesmo?",
					"Novo monstro", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

			nova = new Linha { Secao = -1 };
			for (int i = 0; i < colunas.Length; i++)
			{
				var v = campos[i].Text.Trim();
				nova.Campos.Add(i == 2 ? "\"" + v.Trim('"') + "\"" : (v.Length == 0 ? "0" : v));
			}

			Dados.Monstros[id] = campos[2].Text.Trim().Trim('"');
			f.DialogResult = DialogResult.OK;
			f.Close();
		};
		cancelar.Click += (_, _) => { f.DialogResult = DialogResult.Cancel; f.Close(); };

		f.Controls.AddRange(new Control[] { painel, ok, cancelar });
		f.AcceptButton = ok;
		f.CancelButton = cancelar;
		f.ShowDialog(pai);
		return nova;
	}
}

/// <summary>Cria uma secao vazia para um mapa, para encher clicando.</summary>
public static class NovaSecaoMapa
{
	/// <returns>(secao, mapa) ou (-1,-1) se cancelou</returns>
	public static (int secao, int mapa) Mostrar(IWin32Window pai, ArquivoSpawn arq, int secaoAtual, int mapaAtual)
	{
		using var f = Estilo.Janela("Criar secao de um mapa", 430, 210);

		var cbSecao = new ComboBox { Left = 130, Top = 16, Width = 270, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Estilo.Painel, ForeColor = Estilo.Texto };
		var secoes = arq.Tipo == TipoArquivo.Kanturu
			? new[] { (0, "0 - Monstros do Kanturu") }
			: new[] { (0, "0 - NPCs, guardas e armadilhas"), (1, "1 - Spots (area com quantidade)"),
					  (2, "2 - Monstros soltos"), (3, "3 - Bone King / Golden"), (4, "4 - Blood Castle / outros") };
		foreach (var s in secoes) cbSecao.Items.Add(s.Item2);
		cbSecao.SelectedIndex = Math.Max(0, Array.FindIndex(secoes, s => s.Item1 == secaoAtual));

		var cbMapa = new ComboBox { Left = 130, Top = 56, Width = 270, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Estilo.Painel, ForeColor = Estilo.Texto };
		var listaMapas = Dados.ListaMapas();
		int idxSel = 0;
		for (int i = 0; i < listaMapas.Count; i++)
		{
			var (id, nome) = listaMapas[i];
			cbMapa.Items.Add($"{id} - {nome}");
			if (id == mapaAtual) idxSel = i;
		}
		if (cbMapa.Items.Count > 0) cbMapa.SelectedIndex = idxSel;

		var dica = new Label
		{
			Left = 16, Top = 96, Width = 396, Height = 40, ForeColor = Estilo.Destaque,
			Text = "O mapa passa a aparecer na arvore com 0 linhas. Escolha um monstro na lista e va clicando no mapa."
		};

		var ok = Estilo.Botao("Criar", 196, 148);
		var cancelar = Estilo.Botao("Cancelar", 310, 148);

		int rs = -1, rm = -1;
		ok.Click += (_, _) =>
		{
			rs = secoes[cbSecao.SelectedIndex].Item1;
			rm = cbMapa.SelectedIndex >= 0 && cbMapa.SelectedIndex < listaMapas.Count ? listaMapas[cbMapa.SelectedIndex].Id : 0;
			f.DialogResult = DialogResult.OK;
			f.Close();
		};
		cancelar.Click += (_, _) => { f.DialogResult = DialogResult.Cancel; f.Close(); };

		f.Controls.AddRange(new Control[] { Estilo.Rotulo("Secao", 16, 16), cbSecao,
			Estilo.Rotulo("Mapa", 16, 56), cbMapa, dica, ok, cancelar });
		f.AcceptButton = ok;
		f.CancelButton = cancelar;
		f.ShowDialog(pai);
		return (rs, rm);
	}
}

/// <summary>Pergunta se a limpeza e so no mapa da tela ou no arquivo inteiro.</summary>
public static class EscolherAlcance
{
	/// <returns>0 = so o mapa, 1 = o arquivo inteiro, -1 = cancelou</returns>
	public static int Mostrar(IWin32Window pai, string oQue, string mapa, int noMapa, int noArquivo, int objetos)
	{
		using var f = Estilo.Janela("Remover " + oQue, 470, 230);

		var texto = new Label
		{
			Left = 16, Top = 16, Width = 438, Height = 96, ForeColor = Estilo.Texto,
			Text = $"Remover {oQue}." + Environment.NewLine + Environment.NewLine +
				   $"No mapa {mapa}: {noMapa} linha(s)" + Environment.NewLine +
				   $"No arquivo inteiro: {noArquivo} linha(s), {objetos} monstro(s)" + Environment.NewLine + Environment.NewLine +
				   "Isso nao mexe nas outras secoes do arquivo."
		};

		var bMapa = Estilo.Botao("So o mapa " + mapa, 16, 130, 200);
		var bTudo = Estilo.Botao("O arquivo inteiro", 228, 130, 130);
		var bNao = Estilo.Botao("Cancelar", 366, 130, 88);

		bMapa.Enabled = noMapa > 0;
		if (noMapa > 0) bMapa.ForeColor = Estilo.Destaque;
		bTudo.ForeColor = Color.FromArgb(235, 110, 110);

		int r = -1;
		bMapa.Click += (_, _) => { r = 0; f.DialogResult = DialogResult.OK; f.Close(); };
		bTudo.Click += (_, _) => { r = 1; f.DialogResult = DialogResult.OK; f.Close(); };
		bNao.Click += (_, _) => { r = -1; f.DialogResult = DialogResult.Cancel; f.Close(); };

		f.Controls.AddRange(new Control[] { texto, bMapa, bTudo, bNao });
		f.CancelButton = bNao;
		f.ShowDialog(pai);
		return r;
	}
}

/// <summary>Tabela para adicionar um spawn (NPC, monstro solto ou spot).</summary>
public static class NovoSpawn
{
	public static Linha Mostrar(IWin32Window pai, ArquivoSpawn arq, int secao, int mapa, Point sugestao, int mobSugerido = -1)
	{
		var ix = arq.Indices(secao);
		bool ehSpot = ix.x2 >= 0;
		bool ehKanturu = arq.Tipo == TipoArquivo.Kanturu;

		using var f = Estilo.Janela("Adicionar " + (ehSpot ? "spot" : "monstro"), 560, ehSpot ? 350 : 300);

		int y = 16;
		var cbMonstro = new ComboBox
		{
			Left = 130, Top = y, Width = 400, DropDownStyle = ComboBoxStyle.DropDown,
			BackColor = Estilo.Painel, ForeColor = Estilo.Texto, AutoCompleteMode = AutoCompleteMode.SuggestAppend,
			AutoCompleteSource = AutoCompleteSource.ListItems
		};
		var ondeNasce = ListaMonstros.Mapas(arq);
		foreach (var p in Dados.Monstros.OrderBy(p => p.Key))
			cbMonstro.Items.Add(ListaMonstros.Texto(p.Key, p.Value, ondeNasce));

		if (mobSugerido >= 0)
		{
			foreach (var o in cbMonstro.Items)
				if (o is string s && s.StartsWith(mobSugerido + " - ", StringComparison.Ordinal))
				{ cbMonstro.SelectedItem = o; break; }

			if (cbMonstro.SelectedIndex < 0) cbMonstro.Text = $"{mobSugerido} - {Dados.Monstro(mobSugerido)}";
		}

		f.Controls.Add(Estilo.Rotulo("Monstro", 16, y));
		f.Controls.Add(cbMonstro);

		y += 34;
		var cbMapa = new ComboBox { Left = 130, Top = y, Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Estilo.Painel, ForeColor = Estilo.Texto };
		var listaMapas = Dados.ListaMapas();
		int idxSel = 0;
		for (int i = 0; i < listaMapas.Count; i++)
		{
			var (id, nome) = listaMapas[i];
			cbMapa.Items.Add($"{id} - {nome}");
			if (id == mapa) idxSel = i;
		}
		if (cbMapa.Items.Count > 0) cbMapa.SelectedIndex = idxSel;
		if (arq.Tipo == TipoArquivo.SSeMUSpawn) cbMapa.Enabled = false;
		f.Controls.Add(Estilo.Rotulo("Mapa", 16, y));
		f.Controls.Add(cbMapa);

		y += 34;
		var tX = Estilo.Campo(sugestao.X.ToString(), 130, y, 80);
		var tY = Estilo.Campo(sugestao.Y.ToString(), 240, y, 80);
		f.Controls.Add(Estilo.Rotulo(ehSpot ? "Canto 1 (X, Y)" : "Posicao (X, Y)", 16, y));
		f.Controls.Add(tX); f.Controls.Add(tY);

		TextBox tX2 = null, tY2 = null, tQtd = null;
		if (ehSpot)
		{
			y += 34;
			tX2 = Estilo.Campo((sugestao.X + 10).ToString(), 130, y, 80);
			tY2 = Estilo.Campo((sugestao.Y + 10).ToString(), 240, y, 80);
			f.Controls.Add(Estilo.Rotulo("Canto 2 (X, Y)", 16, y));
			f.Controls.Add(tX2); f.Controls.Add(tY2);

			y += 34;
			tQtd = Estilo.Campo("10", 130, y, 80);
			f.Controls.Add(Estilo.Rotulo("Quantidade", 16, y));
			f.Controls.Add(tQtd);
		}

		y += 34;
		var tRaio = Estilo.Campo("30", 130, y, 80);
		var tDir = Estilo.Campo("-1", 240, y, 80);
		f.Controls.Add(Estilo.Rotulo("Raio / Dir", 16, y));
		f.Controls.Add(tRaio); f.Controls.Add(tDir);

		TextBox tEstado = null;
		if (ehKanturu)
		{
			y += 34;
			tEstado = Estilo.Campo("0", 130, y, 80);
			f.Controls.Add(Estilo.Rotulo("Estado", 16, y));
			f.Controls.Add(tEstado);
		}

		y += 44;
		var aviso = new Label { Left = 16, Top = y, Width = 520, Height = 34, ForeColor = Estilo.Destaque, Text = "" };
		f.Controls.Add(aviso);

		void Conferir()
		{
			if (!int.TryParse(tX.Text, out var px) || !int.TryParse(tY.Text, out var py)) { aviso.Text = ""; return; }
			var m = cbMapa.SelectedIndex >= 0 && cbMapa.SelectedIndex < listaMapas.Count ? listaMapas[cbMapa.SelectedIndex].Id : 0;
			aviso.Text = Dados.Parede(m, px, py)
				? "Atencao: essa celula e bloqueada no terreno do servidor."
				: (Dados.ZonaSegura(m, px, py) ? "Atencao: essa celula e zona segura." : "");
		}
		tX.TextChanged += (_, _) => Conferir();
		tY.TextChanged += (_, _) => Conferir();
		cbMapa.SelectedIndexChanged += (_, _) => Conferir();
		Conferir();

		var ok = Estilo.Botao("Adicionar", 326, f.ClientSize.Height - 42);
		var cancelar = Estilo.Botao("Cancelar", 440, f.ClientSize.Height - 42);

		Linha nova = null;
		ok.Click += (_, _) =>
		{
			var texto = cbMonstro.Text.Trim();
			var numero = texto.Split(' ')[0];
			if (!int.TryParse(numero, out var mob))
			{
				MessageBox.Show(f, "Escolha o monstro na lista.", "Adicionar"); return;
			}

			nova = new Linha { Secao = secao };
			for (int i = 0; i < arq.Colunas(secao).Length; i++) nova.Campos.Add("0");

			int r = int.TryParse(tRaio.Text, out var vr) ? vr : 30;
			int px = int.TryParse(tX.Text, out var vx) ? vx : 0;
			int py = int.TryParse(tY.Text, out var vy) ? vy : 0;
			int d = int.TryParse(tDir.Text, out var vd) ? vd : -1;
			int idMapaEscolhido = cbMapa.SelectedIndex >= 0 && cbMapa.SelectedIndex < listaMapas.Count ? listaMapas[cbMapa.SelectedIndex].Id : 0;

			if (arq.Tipo == TipoArquivo.SSeMUSpawn)
			{
				nova.SetNum(0, mob);
				nova.SetNum(1, r);
				nova.SetNum(2, px);
				nova.SetNum(3, py);
				if (ehSpot)
				{
					nova.SetNum(4, int.TryParse(tX2.Text, out var px2) ? px2 : px);
					nova.SetNum(5, int.TryParse(tY2.Text, out var py2) ? py2 : py);
					nova.SetNum(6, d);
					nova.SetNum(7, int.TryParse(tQtd.Text, out var q) ? q : 1);
				}
				else
				{
					nova.SetNum(4, d);
				}
			}
			else
			{
				nova.SetNum(ix.mob, mob);
				if (ix.mapa >= 0) nova.SetNum(ix.mapa, idMapaEscolhido);
				nova.SetNum(2 + (ehKanturu ? 1 : 0), r);
				nova.SetNum(ix.x, px);
				nova.SetNum(ix.y, py);

				if (ehSpot)
				{
					nova.SetNum(ix.x2, int.TryParse(tX2.Text, out var px2) ? px2 : px);
					nova.SetNum(ix.y2, int.TryParse(tY2.Text, out var py2) ? py2 : py);
					nova.SetNum(7, d);
					nova.SetNum(ix.qtd, int.TryParse(tQtd.Text, out var q) ? q : 1);
				}
				else
				{
					nova.SetNum(ehKanturu ? 6 : 5, d);
					if (ehKanturu) nova.SetNum(0, int.TryParse(tEstado.Text, out var e) ? e : 0);
				}
			}

			nova.Nome = Dados.Monstro(mob);
			f.DialogResult = DialogResult.OK;
			f.Close();
		};
		cancelar.Click += (_, _) => { f.DialogResult = DialogResult.Cancel; f.Close(); };

		f.Controls.Add(ok);
		f.Controls.Add(cancelar);
		f.AcceptButton = ok;
		f.CancelButton = cancelar;
		f.ShowDialog(pai);
		return nova;
	}
}
