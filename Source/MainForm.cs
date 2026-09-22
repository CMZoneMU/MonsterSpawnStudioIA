using System.Text;

namespace MonsterSpawnStudio;

public class MainForm : Form
{
	static readonly Color CFundo = Color.FromArgb(24, 24, 27);
	static readonly Color CPainel = Color.FromArgb(32, 32, 36);
	static readonly Color CTexto = Color.Gainsboro;
	static readonly Color CDestaque = Color.FromArgb(240, 190, 80);
	static readonly Color COk = Color.FromArgb(120, 210, 130);
	static readonly Color CErro = Color.FromArgb(235, 110, 110);

	readonly List<ArquivoSpawn> m_arquivos = new();
	readonly TreeView m_arvore = new();
	readonly DataGridView m_grade = new();
	readonly ListBox m_problemas = new();
	readonly MapaView m_mapa = new();
	readonly ListBox m_paleta = new();
	readonly TextBox m_filtroMob = new();
	readonly TextBox m_qtdNova = new();
	readonly TextBox m_raioNovo = new();
	readonly CheckBox m_colocar = new();
	readonly CheckBox m_soDoMapa = new();
	readonly Label m_status = new();
	readonly Label m_coord = new();
	readonly ToolStripStatusLabel m_contador = new();

	ArquivoSpawn m_arqAtual;
	int m_secaoAtual = -1;
	int m_mapaAtual = -1;
	bool m_carregando;
	ModoServidor m_modoServidor = ModoServidor.Auto;
	ToolStripMenuItem m_menuModoAuto;
	ToolStripMenuItem m_menuModoMuEmu;
	ToolStripMenuItem m_menuModoSSeMU;

	string PastaMonstros => Dados.PastaData == "" ? "" : Path.Combine(Dados.PastaData, "Monster");
	static string ArquivoConfig => Path.Combine(AppContext.BaseDirectory, "MonsterSpawnStudio.ini");

	public MainForm()
	{
		Text = "MonsterSpawn Studio";
		BackColor = CFundo;
		ForeColor = CTexto;
		StartPosition = FormStartPosition.CenterScreen;
		Size = new Size(1500, 900);
		MinimumSize = new Size(1100, 700);
		Font = new Font("Segoe UI", 9f);
		KeyPreview = true;

		Montar();
		CarregarConfig();
	}

	// =====================================================================  UI
	void Montar()
	{
		var menu = new MenuStrip { BackColor = CPainel, ForeColor = CTexto, Renderer = new ToolStripProfessionalRenderer(new CoresMenu()) };

		var mArq = new ToolStripMenuItem("&Arquivo");
		mArq.DropDownItems.Add(Item("Abrir pasta do servidor...", Keys.Control | Keys.O, (_, _) => EscolherPastaServidor()));
		mArq.DropDownItems.Add(Item("Apontar a pasta Data do cliente (minimapas)...", Keys.None, (_, _) => EscolherPastaCliente()));
		mArq.DropDownItems.Add(new ToolStripSeparator());

		var mModo = new ToolStripMenuItem("Estrutura do Servidor");
		m_menuModoAuto = Item("Auto-Detectar (Recomendado)", Keys.None, (_, _) => DefinirModoServidor(ModoServidor.Auto));
		m_menuModoMuEmu = Item("MuEmu / 97D (MonsterSetBase.txt)", Keys.None, (_, _) => DefinirModoServidor(ModoServidor.MuEmu));
		m_menuModoSSeMU = Item("SSeMU (Spawn/*.txt e MonsterList.txt)", Keys.None, (_, _) => DefinirModoServidor(ModoServidor.SSeMU));
		mModo.DropDownItems.AddRange(new ToolStripItem[] { m_menuModoAuto, m_menuModoMuEmu, m_menuModoSSeMU });
		mArq.DropDownItems.Add(mModo);
		mArq.DropDownItems.Add(new ToolStripSeparator());

		mArq.DropDownItems.Add(Item("Salvar arquivo atual", Keys.Control | Keys.S, (_, _) => Salvar(false)));
		mArq.DropDownItems.Add(Item("Salvar todos", Keys.Control | Keys.Shift | Keys.S, (_, _) => Salvar(true)));
		mArq.DropDownItems.Add(new ToolStripSeparator());
		mArq.DropDownItems.Add(Item("Sair", Keys.None, (_, _) => Close()));
		menu.Items.Add(mArq);

		var mEdit = new ToolStripMenuItem("&Editar");
		mEdit.DropDownItems.Add(Item("Adicionar monstro / spot...", Keys.Control | Keys.N, (_, _) => AdicionarSpawn()));
		mEdit.DropDownItems.Add(Item("Criar monstro novo no Monster.txt...", Keys.Control | Keys.M, (_, _) => CriarMonstro()));
		mEdit.DropDownItems.Add(Item("Trocar o monstro da linha...", Keys.Control | Keys.E, (_, _) => TrocarMonstro()));
		mEdit.DropDownItems.Add(Item("Criar secao de um mapa (comeca vazia)...", Keys.Control | Keys.K, (_, _) => CriarSecaoMapa()));
		mEdit.DropDownItems.Add(new ToolStripSeparator());
		mEdit.DropDownItems.Add(Item("Copiar a linha selecionada", Keys.Control | Keys.Shift | Keys.N, (_, _) => NovaLinha()));
		mEdit.DropDownItems.Add(Item("Duplicar linha", Keys.Control | Keys.D, (_, _) => Duplicar()));
		mEdit.DropDownItems.Add(Item("Remover linha", Keys.Control | Keys.Delete, (_, _) => Remover()));
		mEdit.DropDownItems.Add(new ToolStripSeparator());

		var mLimpar = new ToolStripMenuItem("Remover em massa");
		mLimpar.DropDownItems.Add(Item("Remover todos os SPOTS (secao 1)...", Keys.None, (_, _) => RemoverEmMassa(1, "todos os spots")));
		mLimpar.DropDownItems.Add(Item("Remover todos os MONSTROS soltos (secao 2)...", Keys.None, (_, _) => RemoverEmMassa(2, "todos os monstros soltos")));
		mLimpar.DropDownItems.Add(Item("Remover todos os NPCs (secao 0)...", Keys.None, (_, _) => RemoverEmMassa(0, "todos os NPCs e guardas")));
		mLimpar.DropDownItems.Add(new ToolStripSeparator());
		mLimpar.DropDownItems.Add(Item("Remover todos os GOLDEN / Bone King (secao 3)...", Keys.None, (_, _) => RemoverEmMassa(3, "todos os Golden e Bone King")));
		mEdit.DropDownItems.Add(mLimpar);

		menu.Items.Add(mEdit);

		var mFer = new ToolStripMenuItem("&Ferramentas");
		mFer.DropDownItems.Add(Item("Validar tudo", Keys.F5, (_, _) => Validar()));
		mFer.DropDownItems.Add(Item("Organizar arquivo atual (agrupa por mapa e ordena)", Keys.None, (_, _) => Organizar()));
		mFer.DropDownItems.Add(new ToolStripSeparator());
		mFer.DropDownItems.Add(Item("Recarregar do disco", Keys.F6, (_, _) => { if (Dados.PastaData != "") CarregarPasta(Dados.PastaData); }));
		menu.Items.Add(mFer);

		var mAjuda = new ToolStripMenuItem("A&juda");
		mAjuda.DropDownItems.Add(Item("Como usar", Keys.F1, (_, _) => Ajuda()));
		menu.Items.Add(mAjuda);

		MainMenuStrip = menu;
		Controls.Add(menu);

		// ------------------------------------------------------------- arvore
		m_arvore.Dock = DockStyle.Fill;
		m_arvore.BackColor = CPainel;
		m_arvore.ForeColor = CTexto;
		m_arvore.BorderStyle = BorderStyle.None;
		m_arvore.HideSelection = false;
		m_arvore.AfterSelect += (_, e) => SelecionouNo(e.Node);

		var painelArvore = new Panel { Dock = DockStyle.Fill, BackColor = CPainel, Padding = new Padding(6, 0, 6, 0) };
		painelArvore.Controls.Add(m_arvore);
		painelArvore.Controls.Add(new Label
		{
			Text = "  ARQUIVOS", Dock = DockStyle.Top, Height = 26, ForeColor = CDestaque,
			TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		});

		// ------------------------------------------------- paleta de monstros
		m_paleta.Dock = DockStyle.Fill;
		m_paleta.BackColor = Color.FromArgb(28, 28, 32);
		m_paleta.ForeColor = CTexto;
		m_paleta.BorderStyle = BorderStyle.None;
		m_paleta.IntegralHeight = false;
		m_paleta.DrawMode = DrawMode.OwnerDrawFixed;
		m_paleta.ItemHeight = 17;
		m_paleta.DrawItem += (_, e) =>
		{
			if (e.Index < 0) return;
			var texto = Convert.ToString(m_paleta.Items[e.Index]) ?? "";
			bool titulo = texto.StartsWith("▬");

			var fundo = titulo ? Color.FromArgb(44, 44, 52)
							   : ((e.State & DrawItemState.Selected) != 0 ? Color.FromArgb(70, 70, 82) : Color.FromArgb(28, 28, 32));
			using (var b = new SolidBrush(fundo)) e.Graphics.FillRectangle(b, e.Bounds);

			using var pincel = new SolidBrush(titulo ? CDestaque : CTexto);
			using var fonte = titulo ? new Font(m_paleta.Font, FontStyle.Bold) : (Font)m_paleta.Font.Clone();
			e.Graphics.DrawString(texto, fonte, pincel, e.Bounds.Left + 2, e.Bounds.Top + 1);
		};
		m_paleta.SelectedIndexChanged += (_, _) =>
		{
			// titulo de mapa nao e escolha
			if (MobDaPaleta() < 0) { m_colocar.Checked = false; return; }
			m_colocar.Checked = true;
			AtualizarDica();
		};

		m_filtroMob.Dock = DockStyle.Top;
		m_filtroMob.BackColor = CPainel;
		m_filtroMob.ForeColor = CTexto;
		m_filtroMob.BorderStyle = BorderStyle.FixedSingle;
		m_filtroMob.PlaceholderText = "filtrar por nome ou numero";
		m_filtroMob.TextChanged += (_, _) => EncherPaleta();

		var linhaOpc = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, BackColor = CPainel };
		m_colocar.Text = "clique no mapa adiciona";
		m_colocar.AutoSize = true;
		m_colocar.ForeColor = CDestaque;
		m_colocar.Margin = new Padding(2, 6, 8, 0);
		linhaOpc.Controls.Add(m_colocar);

		m_soDoMapa.Text = "so os deste mapa";
		m_soDoMapa.AutoSize = true;
		m_soDoMapa.Checked = true;
		m_soDoMapa.ForeColor = CTexto;
		m_soDoMapa.Margin = new Padding(2, 6, 8, 0);
		m_soDoMapa.CheckedChanged += (_, _) => EncherPaleta();
		linhaOpc.Controls.Add(m_soDoMapa);

		var linhaNum = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, BackColor = CPainel };
		linhaNum.Controls.Add(new Label { Text = "Qtd", AutoSize = true, ForeColor = CTexto, Margin = new Padding(2, 7, 2, 0) });
		m_qtdNova.Text = "10"; m_qtdNova.Width = 48; m_qtdNova.BackColor = CPainel; m_qtdNova.ForeColor = CTexto; m_qtdNova.BorderStyle = BorderStyle.FixedSingle;
		linhaNum.Controls.Add(m_qtdNova);
		linhaNum.Controls.Add(new Label { Text = "Raio", AutoSize = true, ForeColor = CTexto, Margin = new Padding(10, 7, 2, 0) });
		m_raioNovo.Text = "30"; m_raioNovo.Width = 48; m_raioNovo.BackColor = CPainel; m_raioNovo.ForeColor = CTexto; m_raioNovo.BorderStyle = BorderStyle.FixedSingle;
		linhaNum.Controls.Add(m_raioNovo);

		var painelPaleta = new Panel { Dock = DockStyle.Fill, BackColor = CPainel, Padding = new Padding(6, 0, 6, 6) };
		painelPaleta.Controls.Add(m_paleta);
		painelPaleta.Controls.Add(linhaNum);
		painelPaleta.Controls.Add(linhaOpc);
		painelPaleta.Controls.Add(m_filtroMob);
		painelPaleta.Controls.Add(new Label
		{
			Text = "  MONSTROS  (escolha aqui e clique no mapa)", Dock = DockStyle.Top, Height = 26, ForeColor = CDestaque,
			TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		});

		var divEsq = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BackColor = CFundo, SplitterWidth = 6 };
		divEsq.Panel1.Controls.Add(painelArvore);
		divEsq.Panel2.Controls.Add(painelPaleta);

		var painelEsq = new Panel { Dock = DockStyle.Fill, BackColor = CPainel };
		painelEsq.Controls.Add(divEsq);

		// -------------------------------------------------------------- grade
		m_grade.Dock = DockStyle.Fill;
		m_grade.BackgroundColor = CPainel;
		m_grade.ForeColor = CTexto;
		m_grade.GridColor = Color.FromArgb(55, 55, 60);
		m_grade.BorderStyle = BorderStyle.None;
		m_grade.EnableHeadersVisualStyles = false;
		m_grade.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 44, 50);
		m_grade.ColumnHeadersDefaultCellStyle.ForeColor = CDestaque;
		m_grade.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(44, 44, 50);
		m_grade.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		m_grade.DefaultCellStyle.BackColor = CPainel;
		m_grade.DefaultCellStyle.ForeColor = CTexto;
		m_grade.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 70);
		m_grade.DefaultCellStyle.SelectionForeColor = Color.White;
		m_grade.RowHeadersVisible = false;
		m_grade.AllowUserToAddRows = false;
		m_grade.AllowUserToResizeRows = false;
		m_grade.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		m_grade.MultiSelect = false;
		m_grade.CellEndEdit += (_, e) => GravarCelula(e.RowIndex, e.ColumnIndex);
		m_grade.SelectionChanged += (_, _) => MarcarNoMapa();

		// ----------------------------------------------------------- problemas
		m_problemas.Dock = DockStyle.Fill;
		m_problemas.BackColor = Color.FromArgb(28, 28, 32);
		m_problemas.ForeColor = CTexto;
		m_problemas.BorderStyle = BorderStyle.None;
		m_problemas.IntegralHeight = false;
		m_problemas.DoubleClick += (_, _) => IrParaProblema();

		var painelProb = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 28, 32) };
		painelProb.Controls.Add(m_problemas);
		painelProb.Controls.Add(new Label
		{
			Text = "  CONFERENCIA  (F5 valida  -  clique duplo vai para a linha)", Dock = DockStyle.Top, Height = 24,
			ForeColor = CDestaque, TextAlign = ContentAlignment.MiddleLeft
		});

		var divCentro = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BackColor = CFundo, SplitterWidth = 6 };
		divCentro.Panel1.Controls.Add(m_grade);
		divCentro.Panel2.Controls.Add(painelProb);

		// --------------------------------------------------------------- mapa
		m_mapa.Dock = DockStyle.Fill;
		m_mapa.PontoSelecionado += (_, p) => SelecionarLinhaDoPonto(p);
		m_mapa.PontoMovido += (_, p) => PontoMovido(p);
		m_mapa.CliqueVazio += (_, j) => CliqueNoVazio(j);
		m_mapa.MouseMove += (_, _) =>
		{
			var mob = MobDaPaleta();
			var extra = (m_colocar.Checked && mob >= 0) ? $"   -   clique coloca {Dados.Monstro(mob)}" : "";
			var area = Dados.AreaDe(m_mapa.MapaAtual, m_mapa.CursorJogo.X, m_mapa.CursorJogo.Y);
			if (area.Length > 0) area = "   [" + area + "]";
			m_coord.Text = $"  X {m_mapa.CursorJogo.X}   Y {m_mapa.CursorJogo.Y}{area}{extra}";
		};

		// duas fileiras: com uma so, o brilho e o detalhe ficavam cortados
		var opcoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 62, BackColor = CPainel, Padding = new Padding(4, 4, 4, 0) };
		opcoes.Controls.Add(Check("Minimapa", true, v => { m_mapa.MostrarMinimapa = v; m_mapa.Atualizar(); }));

		opcoes.Controls.Add(new Label { Text = "Paredes", AutoSize = true, ForeColor = CTexto, Margin = new Padding(4, 7, 2, 0) });
		var cbParede = new ComboBox
		{
			Width = 92, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CPainel, ForeColor = CTexto,
			Margin = new Padding(0, 3, 10, 0), FlatStyle = FlatStyle.Flat
		};
		cbParede.Items.AddRange(new object[] { "contorno", "cheio", "nenhuma" });
		cbParede.SelectedIndex = 0;
		cbParede.SelectedIndexChanged += (_, _) =>
			m_mapa.Paredes = (MapaView.ModoParede)cbParede.SelectedIndex;
		opcoes.Controls.Add(cbParede);

		opcoes.Controls.Add(Check("Areas", true, v => { m_mapa.MostrarAreas = v; m_mapa.Atualizar(); }));
		opcoes.Controls.Add(Check("Grade", false, v => { m_mapa.MostrarGrade = v; m_mapa.Atualizar(); }));

		opcoes.Controls.Add(new Label { Text = "Brilho", AutoSize = true, ForeColor = CTexto, Margin = new Padding(8, 7, 2, 0) });
		var barraBrilho = new TrackBar
		{
			Minimum = 0, Maximum = 120, Value = 45, TickStyle = TickStyle.None,
			Width = 110, Height = 24, BackColor = CPainel, Margin = new Padding(0, 2, 8, 0)
		};
		barraBrilho.ValueChanged += (_, _) => m_mapa.Brilho = barraBrilho.Value / 100f;
		opcoes.Controls.Add(barraBrilho);

		opcoes.Controls.Add(new Label { Text = "Detalhe", AutoSize = true, ForeColor = CTexto, Margin = new Padding(4, 7, 2, 0) });
		var cbDetalhe = new ComboBox
		{
			Width = 108, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CPainel, ForeColor = CTexto,
			Margin = new Padding(0, 3, 8, 0), FlatStyle = FlatStyle.Flat
		};
		cbDetalhe.Items.AddRange(new object[] { "so as paredes", "medio", "tudo" });
		cbDetalhe.SelectedIndex = 0;
		cbDetalhe.SelectedIndexChanged += (_, _) =>
			m_mapa.MinimoParede = cbDetalhe.SelectedIndex switch { 0 => 12, 1 => 4, _ => 0 };
		opcoes.Controls.Add(cbDetalhe);
		var bEnq = new Button { Text = "Enquadrar", AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = CTexto };
		bEnq.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 80);
		bEnq.Click += (_, _) => m_mapa.Enquadrar();
		opcoes.Controls.Add(bEnq);

		m_coord.Dock = DockStyle.Bottom;
		m_coord.Height = 22;
		m_coord.ForeColor = CDestaque;
		m_coord.TextAlign = ContentAlignment.MiddleLeft;

		var painelDir = new Panel { Dock = DockStyle.Fill, BackColor = CPainel, Padding = new Padding(6) };
		painelDir.Controls.Add(m_mapa);
		painelDir.Controls.Add(m_coord);
		painelDir.Controls.Add(opcoes);
		painelDir.Controls.Add(new Label
		{
			Text = "  MAPA  (roda = zoom, botao do meio = arrastar, clique no vazio = novo)", Dock = DockStyle.Top, Height = 26,
			ForeColor = CDestaque, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		});

		// ------------------------------------------------------------ divisoes
		var divDir = new SplitContainer { Dock = DockStyle.Fill, BackColor = CFundo, SplitterWidth = 6 };
		divDir.Panel1.Controls.Add(divCentro);
		divDir.Panel2.Controls.Add(painelDir);

		var divGeral = new SplitContainer { Dock = DockStyle.Fill, BackColor = CFundo, SplitterWidth = 6 };
		divGeral.Panel1.Controls.Add(painelEsq);
		divGeral.Panel2.Controls.Add(divDir);

		Controls.Add(divGeral);
		divGeral.BringToFront();

		var barra = new StatusStrip { BackColor = CPainel, ForeColor = CTexto, SizingGrip = false };
		m_contador.ForeColor = CTexto;
		barra.Items.Add(m_contador);
		Controls.Add(barra);

		m_status.Dock = DockStyle.Bottom;
		m_status.Height = 0;

		Shown += (_, _) =>
		{
			divGeral.SplitterDistance = 300;
			divEsq.SplitterDistance = Math.Max(240, divEsq.Height - 320);
			divDir.SplitterDistance = Math.Max(500, divDir.Width - 560);
			divCentro.SplitterDistance = Math.Max(300, divCentro.Height - 160);
			m_mapa.Enquadrar();
		};
	}

	static ToolStripMenuItem Item(string texto, Keys atalho, EventHandler acao)
	{
		var i = new ToolStripMenuItem(texto) { ShortcutKeys = atalho };
		i.Click += acao;
		return i;
	}

	static CheckBox Check(string texto, bool valor, Action<bool> mudou)
	{
		var c = new CheckBox { Text = texto, Checked = valor, AutoSize = true, ForeColor = CTexto, Margin = new Padding(4, 4, 10, 0) };
		c.CheckedChanged += (_, _) => mudou(c.Checked);
		return c;
	}

	class CoresMenu : ProfessionalColorTable
	{
		public override Color MenuItemSelected => Color.FromArgb(60, 60, 70);
		public override Color MenuItemBorder => Color.FromArgb(90, 90, 100);
		public override Color MenuBorder => Color.FromArgb(70, 70, 80);
		public override Color ToolStripDropDownBackground => CPainel;
		public override Color ImageMarginGradientBegin => CPainel;
		public override Color ImageMarginGradientMiddle => CPainel;
		public override Color ImageMarginGradientEnd => CPainel;
	}

	void DefinirModoServidor(ModoServidor modo)
	{
		m_modoServidor = modo;
		AtualizarMenuModoServidor();
		GravarConfig();
		if (Dados.PastaData != "") CarregarPasta(Dados.PastaData);
	}

	void AtualizarMenuModoServidor()
	{
		if (m_menuModoAuto != null) m_menuModoAuto.Checked = m_modoServidor == ModoServidor.Auto;
		if (m_menuModoMuEmu != null) m_menuModoMuEmu.Checked = m_modoServidor == ModoServidor.MuEmu;
		if (m_menuModoSSeMU != null) m_menuModoSSeMU.Checked = m_modoServidor == ModoServidor.SSeMU;
	}

	// ================================================================  arquivos
	void EscolherPastaServidor()
	{
		using var d = new FolderBrowserDialog { Description = "Aponte para a pasta Data do MuServer (a que tem a pasta Monster)" };
		if (Dados.PastaData != "") d.SelectedPath = Dados.PastaData;
		if (d.ShowDialog(this) != DialogResult.OK) return;
		CarregarPasta(d.SelectedPath);
	}

	void EscolherPastaCliente()
	{
		using var d = new FolderBrowserDialog { Description = "Aponte para a pasta Data do CLIENTE (a que tem as pastas World1, World2...)" };
		if (Dados.PastaCliente != "") d.SelectedPath = Dados.PastaCliente;
		if (d.ShowDialog(this) != DialogResult.OK) return;
		Dados.PastaCliente = d.SelectedPath;
		Dados.Limpar();
		var atual = m_mapa.MapaAtual;
		m_mapa.DefinirMapa(-1);
		m_mapa.DefinirMapa(atual);
		GravarConfig();
	}

	void CarregarPasta(string pastaData)
	{
		// aceita ...\Data, ...\Data\Monster, ...\Data\Monster\Spawn ou ...\Data\Spawn
		if (Path.GetFileName(pastaData).Equals("Spawn", StringComparison.OrdinalIgnoreCase))
			pastaData = Path.GetDirectoryName(pastaData) ?? pastaData;
		if (Path.GetFileName(pastaData).Equals("Monster", StringComparison.OrdinalIgnoreCase))
			pastaData = Path.GetDirectoryName(pastaData) ?? pastaData;

		var pastaMon = Path.Combine(pastaData, "Monster");
		if (!Directory.Exists(pastaMon))
		{
			if (File.Exists(Path.Combine(pastaData, "Monster.txt")) ||
				File.Exists(Path.Combine(pastaData, "MonsterList.txt")) ||
				Directory.Exists(Path.Combine(pastaData, "Spawn")) ||
				File.Exists(Path.Combine(pastaData, "MonsterSetBase.txt")))
			{
				pastaMon = pastaData;
			}
			else
			{
				MessageBox.Show(this, "Nao achei a pasta Monster ou arquivos de monstro dentro de:\n" + pastaData, "MonsterSpawn Studio",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
		}

		Dados.PastaData = pastaData;
		Dados.Limpar();

		// Localiza catalogo de monstros (MonsterList.txt ou Monster.txt)
		string arqMonstros = null;
		string[] candMon =
		{
			Path.Combine(pastaMon, "MonsterList.txt"),
			Path.Combine(pastaData, "MonsterList.txt"),
			Path.Combine(pastaMon, "Monster.txt"),
			Path.Combine(pastaData, "Monster.txt")
		};
		arqMonstros = candMon.FirstOrDefault(File.Exists);

		if (arqMonstros != null)
			Dados.CarregarMonstros(arqMonstros);

		Dados.CarregarAreas();

		m_arquivos.Clear();

		// Descobre pasta de spawns modulares (SSeMU)
		var pastaSpawn = Path.Combine(pastaMon, "Spawn");
		if (!Directory.Exists(pastaSpawn)) pastaSpawn = Path.Combine(pastaData, "Spawn");

		bool ehSSeMU = false;
		if (m_modoServidor == ModoServidor.SSeMU) ehSSeMU = true;
		else if (m_modoServidor == ModoServidor.MuEmu) ehSSeMU = false;
		else
		{
			// Auto-deteccao: verifica presenca da subpasta Spawn com arquivos
			ehSSeMU = Directory.Exists(pastaSpawn) && Directory.GetFiles(pastaSpawn, "*.txt").Length > 0;
		}

		if (ehSSeMU && Directory.Exists(pastaSpawn))
		{
			foreach (var f in Directory.GetFiles(pastaSpawn, "*.txt").OrderBy(x => x))
			{
				m_arquivos.Add(ArquivoSpawn.Abrir(f, TipoArquivo.SSeMUSpawn));
			}

			if (arqMonstros != null)
			{
				m_arquivos.Add(ArquivoSpawn.Abrir(arqMonstros, TipoArquivo.Monster));
			}
		}
		else
		{
			// Modo MuEmu / 97D / Season 4
			foreach (var nome in new[] { "MonsterSetBase.txt", "MonsterSetBaseCS.txt", "KanturuMonsterSetBase.txt" })
			{
				var p = Path.Combine(pastaMon, nome);
				if (!File.Exists(p)) p = Path.Combine(pastaData, nome);
				if (File.Exists(p)) m_arquivos.Add(ArquivoSpawn.Abrir(p, TipoArquivo.SetBase));
			}

			if (arqMonstros != null)
			{
				m_arquivos.Add(ArquivoSpawn.Abrir(arqMonstros, TipoArquivo.Monster));
			}
		}

		MontarArvore();
		EncherPaleta();
		GravarConfig();
		Validar();
	}

	void MontarArvore()
	{
		m_arvore.BeginUpdate();
		m_arvore.Nodes.Clear();

		foreach (var a in m_arquivos)
		{
			var nA = m_arvore.Nodes.Add(a.NomeCurto);
			nA.Tag = a;
			nA.ForeColor = CDestaque;

			if (a.Tipo == TipoArquivo.Monster)
			{
				nA.Nodes.Add(new TreeNode($"Monstros ({a.Blocos[0].Linhas.Count(l => l.Cru == null)})") { Tag = (a, -1, -1) });
				continue;
			}

			if (a.Tipo == TipoArquivo.SSeMUSpawn)
			{
				int mapa = a.MapaFixo;
				var total = a.Todas().Count();
				nA.Text = $"{a.NomeCurto}  ({total})";
				nA.Tag = (a, -1, mapa);

				foreach (var secao in a.Blocos.Select(b => b.Secao).Distinct().OrderBy(s => s))
				{
					var linhas = a.Todas().Where(t => t.bloco.Secao == secao).ToList();
					var nS = nA.Nodes.Add($"{NomeSecao(a, secao)}  ({linhas.Count})");
					nS.Tag = (a, secao, mapa);
					if (linhas.Count == 0) nS.ForeColor = Color.FromArgb(150, 150, 160);
				}
				continue;
			}

			foreach (var secao in a.Blocos.Select(b => b.Secao).Distinct().OrderBy(s => s))
			{
				var linhas = a.Todas().Where(t => t.bloco.Secao == secao).ToList();
				_ = linhas;
				var nS = nA.Nodes.Add($"{NomeSecao(a, secao)}  ({linhas.Count})");
				nS.Tag = (a, secao, -1);

				var ix = a.Indices(secao);
				if (ix.mapa < 0) continue;

				var mapas = linhas.Select(t => a.MapaDaLinha(t.linha, secao)).ToList();

				// mapas de bloco vazio tambem entram, para dar para clicar e encher
				foreach (var b in a.Blocos)
					if (b.Secao == secao && b.MapaReservado >= 0 && !mapas.Contains(b.MapaReservado))
						mapas.Add(b.MapaReservado);

				foreach (var mapa in mapas.Distinct().OrderBy(m => m))
				{
					var qtd = linhas.Count(t => a.MapaDaLinha(t.linha, secao) == mapa);
					var nM = nS.Nodes.Add($"{Dados.Mapa(mapa)}  ({qtd})");
					nM.Tag = (a, secao, mapa);
					if (qtd == 0) nM.ForeColor = Color.FromArgb(150, 150, 160);
				}
			}
		}

		m_arvore.EndUpdate();
		if (m_arvore.Nodes.Count > 0) m_arvore.Nodes[0].Expand();
		AtualizarContador();
	}

	static string NomeSecao(ArquivoSpawn a, int secao)
	{
		if (a.Tipo == TipoArquivo.Kanturu) return "Monstros do Kanturu";
		return secao switch
		{
			0 => "0 - NPCs, guardas e armadilhas",
			1 => "1 - Spots (area com quantidade)",
			2 => "2 - Monstros soltos",
			3 => "3 - Bone King / Golden",
			4 => "4 - Blood Castle / portao / outros",
			_ => "Secao " + secao
		};
	}

	// ==================================================================  grade
	void SelecionouNo(TreeNode no)
	{
		if (no?.Tag is ArquivoSpawn a1)
		{
			m_arqAtual = a1;
			m_secaoAtual = -1;
			m_mapaAtual = a1.MapaFixo;
			PreencherGrade();
			return;
		}
		if (no?.Tag is ValueTuple<ArquivoSpawn, int, int> t)
		{
			m_arqAtual = t.Item1;
			m_secaoAtual = t.Item2;
			m_mapaAtual = t.Item3 >= 0 ? t.Item3 : m_arqAtual.MapaFixo;
			PreencherGrade();
		}
	}

	IEnumerable<(Bloco bloco, Linha linha)> LinhasVisiveis()
	{
		if (m_arqAtual == null) yield break;
		foreach (var t in m_arqAtual.Todas())
		{
			if (m_secaoAtual >= 0 && t.bloco.Secao != m_secaoAtual) continue;
			if (m_mapaAtual >= 0)
			{
				var mapa = m_arqAtual.MapaDaLinha(t.linha, t.bloco.Secao);
				if (mapa >= 0 && mapa != m_mapaAtual) continue;
			}
			yield return t;
		}
	}

	void PreencherGrade()
	{
		m_carregando = true;
		m_grade.Rows.Clear();
		m_grade.Columns.Clear();

		if (m_arqAtual == null) { m_carregando = false; return; }

		var secao = m_secaoAtual >= 0 ? m_secaoAtual : (m_arqAtual.Blocos.Count > 0 ? m_arqAtual.Blocos[0].Secao : 0);
		var colunas = m_arqAtual.Colunas(secao);

		foreach (var c in colunas)
			m_grade.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = c, Name = c, Width = c.Length <= 3 ? 58 : 76 });

		if (m_arqAtual.Tipo != TipoArquivo.Monster)
			m_grade.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Monstro", Name = "_mob", ReadOnly = true, Width = 150 });

		m_grade.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Comentario", Name = "_com", Width = 220 });

		foreach (var (b, l) in LinhasVisiveis())
		{
			var i = m_grade.Rows.Add();
			var linha = m_grade.Rows[i];
			linha.Tag = (b, l);

			for (int c = 0; c < colunas.Length; c++)
				linha.Cells[c].Value = c < l.Campos.Count ? l.Campos[c] : "";

			if (m_arqAtual.Tipo != TipoArquivo.Monster)
			{
				var ix = m_arqAtual.Indices(b.Secao);
				linha.Cells["_mob"].Value = Dados.Monstro(l.Num(ix.mob));
			}
			linha.Cells["_com"].Value = l.Nome;
		}

		m_carregando = false;
		DesenharMapa();
		EncherPaleta();
		AtualizarContador();
	}

	void GravarCelula(int iLinha, int iCol)
	{
		if (m_carregando || iLinha < 0 || iCol < 0) return;
		var linhaG = m_grade.Rows[iLinha];
		if (linhaG.Tag is not ValueTuple<Bloco, Linha> t) return;

		var nome = m_grade.Columns[iCol].Name;
		var valor = Convert.ToString(linhaG.Cells[iCol].Value) ?? "";

		if (nome == "_com") { t.Item2.Nome = valor; }
		else if (nome == "_mob") { return; }
		else
		{
			while (t.Item2.Campos.Count <= iCol) t.Item2.Campos.Add("0");
			t.Item2.Campos[iCol] = valor.Trim();

			if (m_arqAtual.Tipo != TipoArquivo.Monster)
			{
				var ix = m_arqAtual.Indices(t.Item1.Secao);
				linhaG.Cells["_mob"].Value = Dados.Monstro(t.Item2.Num(ix.mob));
			}
		}

		m_arqAtual.Alterado = true;
		AtualizarTitulo();
		DesenharMapa();
		AtualizarContador();
	}

	// ===================================================================  mapa
	void DesenharMapa()
	{
		m_mapa.Limpar();
		if (m_arqAtual == null || m_arqAtual.Tipo == TipoArquivo.Monster) { m_mapa.Atualizar(); return; }

		int mapa = m_mapaAtual;
		if (mapa < 0 && m_arqAtual.MapaFixo >= 0) mapa = m_arqAtual.MapaFixo;
		if (mapa < 0)
		{
			// usa o mapa da primeira linha visivel
			foreach (var (b, l) in LinhasVisiveis())
			{
				var m = m_arqAtual.MapaDaLinha(l, b.Secao);
				if (m >= 0) { mapa = m; break; }
			}
		}
		if (mapa < 0) { m_mapa.Atualizar(); return; }

		m_mapa.DefinirMapa(mapa);

		foreach (var (b, l) in LinhasVisiveis())
		{
			var m = m_arqAtual.MapaDaLinha(l, b.Secao);
			if (m >= 0 && m != mapa) continue;
			var ix = m_arqAtual.Indices(b.Secao);
			if (ix.x < 0) continue;

			var p = new MapaView.Ponto
			{
				X = l.Num(ix.x),
				Y = l.Num(ix.y),
				Tag = l,
				Texto = $"{Dados.Monstro(l.Num(ix.mob))}  ({l.Num(ix.x)}, {l.Num(ix.y)})"
			};

			if (ix.x2 >= 0)
			{
				p.X2 = l.Num(ix.x2);
				p.Y2 = l.Num(ix.y2);
				p.Texto += $"  x{l.Num(ix.qtd)}";
			}

			m_mapa.Adicionar(p);
		}

		MarcarNoMapa();
	}

	void MarcarNoMapa()
	{
		if (m_grade.CurrentRow?.Tag is not ValueTuple<Bloco, Linha> t) { m_mapa.Atualizar(); return; }
		foreach (var p in PontosDoMapa()) p.Selecionado = ReferenceEquals(p.Tag, t.Item2);
		m_mapa.Atualizar();
	}

	IEnumerable<MapaView.Ponto> PontosDoMapa() => m_mapa.Pontos;

	void SelecionarLinhaDoPonto(MapaView.Ponto p)
	{
		foreach (DataGridViewRow r in m_grade.Rows)
		{
			if (r.Tag is ValueTuple<Bloco, Linha> t && ReferenceEquals(t.Item2, p.Tag))
			{
				m_grade.CurrentCell = r.Cells[0];
				r.Selected = true;
				return;
			}
		}
	}

	void PontoMovido(MapaView.Ponto p)
	{
		if (p.Tag is not Linha l) return;
		var bloco = m_arqAtual.Blocos.FirstOrDefault(b => b.Linhas.Contains(l));
		if (bloco == null) return;

		var ix = m_arqAtual.Indices(bloco.Secao);
		l.SetNum(ix.x, p.X);
		l.SetNum(ix.y, p.Y);

		m_arqAtual.Alterado = true;
		AtualizarTitulo();

		m_carregando = true;
		foreach (DataGridViewRow r in m_grade.Rows)
		{
			if (r.Tag is ValueTuple<Bloco, Linha> t && ReferenceEquals(t.Item2, l))
			{
				r.Cells[ix.x].Value = p.X.ToString();
				r.Cells[ix.y].Value = p.Y.ToString();
				break;
			}
		}
		m_carregando = false;

		m_coord.Text = $"  X {p.X}   Y {p.Y}";
	}

	/// <summary>
	/// Cria um bloco vazio para um mapa. Ele aparece na arvore com 0 linhas e voce
	/// enche clicando no mapa.
	/// </summary>
	void CriarSecaoMapa()
	{
		if (m_arqAtual == null || m_arqAtual.Tipo == TipoArquivo.Monster)
		{
			MessageBox.Show(this, "Escolha antes um arquivo de spawn na arvore da esquerda.",
				"Criar secao", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		var (secao, mapa) = NovaSecaoMapa.Mostrar(this, m_arqAtual, m_secaoAtual, m_mapaAtual >= 0 ? m_mapaAtual : (m_arqAtual.MapaFixo >= 0 ? m_arqAtual.MapaFixo : 0));
		if (secao < 0 || mapa < 0) return;

		var arqAlvo = m_arqAtual;
		if (arqAlvo.Tipo == TipoArquivo.SSeMUSpawn && arqAlvo.MapaFixo != mapa)
		{
			var achado = m_arquivos.FirstOrDefault(a => a.Tipo == TipoArquivo.SSeMUSpawn && a.MapaFixo == mapa);
			if (achado != null) arqAlvo = achado;
		}

		bool jaTem = arqAlvo.Blocos.Any(b => b.Secao == secao &&
			(b.MapaReservado == mapa || arqAlvo.MapaFixo == mapa || b.Linhas.Any(l => l.Cru == null && arqAlvo.MapaDaLinha(l, b.Secao) == mapa)));

		if (!jaTem)
		{
			var b = new Bloco { Secao = secao, MapaReservado = mapa };
			b.Antes.Add("");
			b.Antes.Add("//=========================================================================================");
			b.Antes.Add("//  " + Dados.Mapa(mapa) + " - " + NomeSecao(arqAlvo, secao));
			b.Antes.Add("//=========================================================================================");
			b.Antes.Add("//" + string.Join("	", arqAlvo.Colunas(secao)) + "	Nome");
			arqAlvo.Blocos.Add(b);
			arqAlvo.Alterado = true;
			AtualizarTitulo();
		}

		MontarArvore();

		// ja deixa o mapa novo escolhido
		foreach (TreeNode nA in m_arvore.Nodes)
			foreach (TreeNode nS in nA.Nodes)
				foreach (TreeNode nM in nS.Nodes)
					if (nM.Tag is ValueTuple<ArquivoSpawn, int, int> t &&
						t.Item1 == arqAlvo && t.Item2 == secao && t.Item3 == mapa)
					{ m_arvore.SelectedNode = nM; return; }
	}

	/// <summary>
	/// Apaga de uma vez todas as linhas de uma secao: so no mapa da tela ou no
	/// arquivo inteiro. Cada secao e separada, uma nao mexe na outra.
	/// </summary>
	void RemoverEmMassa(int secao, string oQue)
	{
		if (m_arqAtual == null || m_arqAtual.Tipo == TipoArquivo.Monster)
		{
			MessageBox.Show(this, "Escolha antes um arquivo de spawn na arvore da esquerda.",
				"Remover em massa", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		var ix = m_arqAtual.Indices(secao);
		var daSecao = m_arqAtual.Todas().Where(t => t.bloco.Secao == secao).ToList();

		if (daSecao.Count == 0)
		{
			MessageBox.Show(this, $"O {m_arqAtual.NomeCurto} nao tem nada na secao {secao}.",
				"Remover em massa", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		int objetos = daSecao.Sum(t => ix.qtd >= 0 ? Math.Max(1, t.linha.Num(ix.qtd)) : 1);
		int mapaAlvo = m_mapaAtual >= 0 ? m_mapaAtual : (m_arqAtual.MapaFixo >= 0 ? m_arqAtual.MapaFixo : m_mapa.MapaAtual);
		int noMapa = (mapaAlvo >= 0) ? daSecao.Count(t => m_arqAtual.MapaDaLinha(t.linha, secao) == mapaAlvo) : 0;

		var alcance = EscolherAlcance.Mostrar(this, oQue, mapaAlvo >= 0 ? Dados.Mapa(mapaAlvo) : "-",
			noMapa, daSecao.Count, objetos);
		if (alcance < 0) return;

		int apagadas = 0;
		foreach (var b in m_arqAtual.Blocos.Where(b => b.Secao == secao))
		{
			var fora = b.Linhas.Where(l => l.Cru == null &&
				(alcance == 1 || (m_arqAtual.MapaDaLinha(l, secao) == mapaAlvo))).ToList();

			foreach (var l in fora) { b.Linhas.Remove(l); apagadas++; }
		}

		if (alcance == 0)
		{
			// limpou so um mapa: o bloco fica vazio, mas continua na arvore com 0
			foreach (var b in m_arqAtual.Blocos.Where(b => b.Secao == secao && !b.TemDados))
				if (b.MapaReservado < 0) b.MapaReservado = mapaAlvo;
		}
		else
		{
			// limpou o arquivo todo: tira os blocos que ficaram sem nada
			m_arqAtual.Blocos.RemoveAll(b => b.Secao == secao && !b.TemDados && b.MapaReservado < 0);
		}

		m_arqAtual.Alterado = true;
		AtualizarTitulo();
		MontarArvore();
		m_secaoAtual = -1;
		m_mapaAtual = -1;
		PreencherGrade();
		Validar();

		m_contador.Text = $"{apagadas} linha(s) removida(s). Salve com Ctrl+S para valer no servidor.";
	}

	/// <summary>Poe a linha no bloco do mesmo mapa (cria o bloco se precisar) e atualiza a tela.</summary>
	void InserirLinha(Linha nova, int secao, int mapa)
	{
		var arq = m_arqAtual;
		if (arq == null) return;

		if (arq.Tipo == TipoArquivo.SSeMUSpawn && arq.MapaFixo != mapa)
		{
			var achado = m_arquivos.FirstOrDefault(a => a.Tipo == TipoArquivo.SSeMUSpawn && a.MapaFixo == mapa);
			if (achado != null) arq = achado;
		}

		var ix = arq.Indices(secao);

		var bloco = arq.Blocos.LastOrDefault(b => b.Secao == secao && b.MapaReservado == mapa);
		bloco ??= arq.Blocos.LastOrDefault(b => b.Secao == secao &&
			(arq.MapaFixo == mapa || b.Linhas.Any(l => l.Cru == null && arq.MapaDaLinha(l, b.Secao) == mapa)));
		bloco ??= arq.Blocos.LastOrDefault(b => b.Secao == secao);

		if (bloco == null)
		{
			bloco = new Bloco { Secao = secao };
			bloco.Antes.Add("");
			bloco.Antes.Add("//  " + Dados.Mapa(mapa) + " - " + NomeSecao(arq, secao));
			arq.Blocos.Add(bloco);
		}

		bloco.Linhas.Add(nova);
		if (bloco.MapaReservado == mapa) bloco.MapaReservado = -1;

		arq.Alterado = true;
		m_arqAtual = arq;
		AtualizarTitulo();
		MontarArvore();
		PreencherGrade();
		AtualizarContador();

		foreach (DataGridViewRow linha in m_grade.Rows)
			if (linha.Tag is ValueTuple<Bloco, Linha> tt && ReferenceEquals(tt.Item2, nova))
			{
				m_grade.CurrentCell = linha.Cells[0];
				linha.Selected = true;
				break;
			}
	}

	/// <summary>Quantos de cada monstro ja existem no mapa que esta na tela.</summary>
	Dictionary<int, int> ContarDoMapa()
	{
		var conta = new Dictionary<int, int>();
		if (m_arqAtual == null || m_arqAtual.Tipo == TipoArquivo.Monster) return conta;

		int mapa = m_mapaAtual >= 0 ? m_mapaAtual : (m_arqAtual.MapaFixo >= 0 ? m_arqAtual.MapaFixo : m_mapa.MapaAtual);
		if (mapa < 0) return conta;

		var arquivos = m_arqAtual.Tipo == TipoArquivo.SSeMUSpawn
			? m_arquivos.Where(a => a.Tipo == TipoArquivo.SSeMUSpawn && a.MapaFixo == mapa)
			: new[] { m_arqAtual };

		foreach (var a in arquivos)
		{
			foreach (var (b, l) in a.Todas())
			{
				var m = a.MapaDaLinha(l, b.Secao);
				if (m != mapa) continue;

				var ix = a.Indices(b.Secao);
				var mob = l.Num(ix.mob);
				var qtd = ix.qtd >= 0 ? Math.Max(1, l.Num(ix.qtd)) : 1;
				conta[mob] = conta.TryGetValue(mob, out var v) ? v + qtd : qtd;
			}
		}
		return conta;
	}

	/// <summary>Em quais mapas cada monstro nasce, no arquivo aberto.</summary>
	Dictionary<int, SortedSet<int>> MonstrosPorMapa()
	{
		var onde = new Dictionary<int, SortedSet<int>>();
		var lista = m_arquivos.Where(a => a.Tipo != TipoArquivo.Monster);

		foreach (var a in lista)
		{
			foreach (var (b, l) in a.Todas())
			{
				var mapa = a.MapaDaLinha(l, b.Secao);
				if (mapa < 0) continue;

				var ix = a.Indices(b.Secao);
				var mob = l.Num(ix.mob);
				if (!onde.TryGetValue(mob, out var s)) onde[mob] = s = new SortedSet<int>();
				s.Add(mapa);
			}
		}
		return onde;
	}

	void EncherPaleta()
	{
		var texto = m_filtroMob.Text.Trim();
		var doMapa = ContarDoMapa();
		var mapa = m_mapaAtual >= 0 ? m_mapaAtual : (m_arqAtual != null && m_arqAtual.MapaFixo >= 0 ? m_arqAtual.MapaFixo : m_mapa.MapaAtual);
		var escolhido = MobDaPaleta();

		bool filtrarMapa = m_soDoMapa.Checked && doMapa.Count > 0;

		bool Passa(int id, string s) => texto.Length == 0 || s.Contains(texto, StringComparison.OrdinalIgnoreCase);
		string Linha(int id, string nome, int qtd) => $"{id,-4} {nome}" + (qtd > 0 ? $"   ({qtd})" : "");

		m_paleta.BeginUpdate();
		m_paleta.Items.Clear();

		if (filtrarMapa)
		{
			m_paleta.Items.Add("▬ " + Dados.Mapa(mapa).ToUpperInvariant());

			foreach (var p in Dados.Monstros.OrderBy(p => p.Key))
			{
				if (!doMapa.TryGetValue(p.Key, out var q)) continue;
				var s = Linha(p.Key, p.Value, q);
				if (Passa(p.Key, s)) m_paleta.Items.Add(s);
			}
		}
		else
		{
			// lista inteira, separada por mapa
			var onde = MonstrosPorMapa();
			var usados = new HashSet<int>();

			foreach (var m in onde.SelectMany(p => p.Value).Distinct().OrderBy(m => m))
			{
				var doGrupo = Dados.Monstros.Keys.Where(id => onde.TryGetValue(id, out var s) && s.Contains(m))
											.OrderBy(id => id).ToList();

				var linhas = new List<string>();
				foreach (var id in doGrupo)
				{
					var s = Linha(id, Dados.Monstro(id), m == mapa && doMapa.TryGetValue(id, out var q) ? q : 0);
					if (Passa(id, s)) linhas.Add(s);
					usados.Add(id);
				}

				if (linhas.Count == 0) continue;
				m_paleta.Items.Add("▬ " + Dados.Mapa(m).ToUpperInvariant());
				foreach (var s in linhas) m_paleta.Items.Add(s);
			}

			// os que ainda nao estao em mapa nenhum deste arquivo
			var sobra = new List<string>();
			foreach (var p in Dados.Monstros.OrderBy(p => p.Key))
			{
				if (usados.Contains(p.Key)) continue;
				var s = Linha(p.Key, p.Value, 0);
				if (Passa(p.Key, s)) sobra.Add(s);
			}

			if (sobra.Count > 0)
			{
				m_paleta.Items.Add("▬ SEM SPAWN NESTE ARQUIVO");
				foreach (var s in sobra) m_paleta.Items.Add(s);
			}
		}

		m_paleta.EndUpdate();

		m_soDoMapa.Text = mapa >= 0 ? $"so os de {Dados.Mapa(mapa)}" : "so os deste mapa";
		m_soDoMapa.Enabled = doMapa.Count > 0;

		if (escolhido >= 0)
			foreach (var o in m_paleta.Items)
				if (o is string s2 && int.TryParse(s2.Split(' ')[0], out var id) && id == escolhido)
				{ m_paleta.SelectedItem = o; break; }
	}

	/// <summary>Numero do monstro escolhido na paleta (-1 quando nenhum).</summary>
	int MobDaPaleta()
	{
		if (m_paleta.SelectedItem is string s && int.TryParse(s.Split(' ')[0], out var id)) return id;
		return -1;
	}

	void AtualizarDica()
	{
		var id = MobDaPaleta();
		if (id < 0) return;
		m_coord.Text = $"  {Dados.Monstro(id)} escolhido - clique no mapa para colocar";
	}

	/// <summary>
	/// Clique num ponto vazio do mapa: com um monstro escolhido na paleta e a opcao
	/// ligada, cria a linha na hora, sem perguntar nada.
	/// </summary>
	void CliqueNoVazio(Point j)
	{
		if (m_arqAtual == null || m_arqAtual.Tipo == TipoArquivo.Monster) return;

		var secao = m_secaoAtual >= 0 ? m_secaoAtual : 1;
		var mapa = m_mapaAtual >= 0 ? m_mapaAtual : (m_arqAtual.MapaFixo >= 0 ? m_arqAtual.MapaFixo : m_mapa.MapaAtual);

		if (secao < 0 || mapa < 0)
		{
			m_coord.Text = "  escolha antes a secao e o mapa na arvore da esquerda";
			return;
		}

		var mob = MobDaPaleta();

		// sem o modo rapido ligado, o clique abre a tabela ja com a coordenada
		if (!m_colocar.Checked || mob < 0)
		{
			var pelaJanela = NovoSpawn.Mostrar(this, m_arqAtual, secao, mapa, j, mob);
			if (pelaJanela == null) return;

			var targetMapa = m_arqAtual.MapaDaLinha(pelaJanela, secao);
			if (targetMapa < 0) targetMapa = mapa;
			InserirLinha(pelaJanela, secao, targetMapa);
			var ixp = m_arqAtual.Indices(secao);
			m_coord.Text = $"  {Dados.Monstro(pelaJanela.Num(ixp.mob))} adicionado em " +
						   $"{pelaJanela.Num(ixp.x)}, {pelaJanela.Num(ixp.y)}";
			return;
		}

		var ix = m_arqAtual.Indices(secao);
		var nova = new Linha { Secao = secao };
		for (int i = 0; i < m_arqAtual.Colunas(secao).Length; i++) nova.Campos.Add("0");

		int raio = int.TryParse(m_raioNovo.Text, out var r) ? r : 30;
		int qtd = int.TryParse(m_qtdNova.Text, out var q) ? Math.Max(1, q) : 10;

		if (m_arqAtual.Tipo == TipoArquivo.SSeMUSpawn)
		{
			nova.SetNum(0, mob);
			nova.SetNum(1, raio);
			nova.SetNum(2, j.X);
			nova.SetNum(3, j.Y);
			if (ix.x2 >= 0)
			{
				nova.SetNum(4, j.X);
				nova.SetNum(5, j.Y);
				nova.SetNum(6, -1);
				nova.SetNum(7, qtd);
			}
			else
			{
				nova.SetNum(4, -1);
			}
		}
		else
		{
			nova.SetNum(ix.mob, mob);
			nova.SetNum(ix.mapa, mapa);
			nova.SetNum(m_arqAtual.Tipo == TipoArquivo.Kanturu ? 3 : 2, raio);
			nova.SetNum(ix.x, j.X);
			nova.SetNum(ix.y, j.Y);

			if (ix.x2 >= 0)
			{
				nova.SetNum(ix.x2, j.X);
				nova.SetNum(ix.y2, j.Y);
				nova.SetNum(7, -1);
				nova.SetNum(ix.qtd, qtd);
			}
			else nova.SetNum(m_arqAtual.Tipo == TipoArquivo.Kanturu ? 6 : 5, -1);
		}

		nova.Nome = Dados.Monstro(mob);

		InserirLinha(nova, secao, mapa);

		var aviso = Dados.Parede(mapa, j.X, j.Y) ? "  (atencao: essa celula e parede)" : "";
		m_coord.Text = $"  {Dados.Monstro(mob)} colocado em {j.X}, {j.Y}{aviso}";
	}

	// =============================================================  operacoes
	void NovaLinha()
	{
		if (m_arqAtual == null) return;
		if (m_grade.CurrentRow?.Tag is not ValueTuple<Bloco, Linha> t)
		{
			MessageBox.Show(this, "Escolha uma linha de referencia primeiro (ou clique num ponto vazio do mapa).",
				"Nova linha", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		var nova = t.Item2.Clonar();
		t.Item1.Linhas.Insert(t.Item1.Linhas.IndexOf(t.Item2) + 1, nova);
		m_arqAtual.Alterado = true;
		AtualizarTitulo();
		PreencherGrade();
	}

	void Duplicar() => NovaLinha();

	/// <summary>Tabela de adicionar: escolhe monstro, mapa, posicao e quantidade.</summary>
	void AdicionarSpawn()
	{
		if (m_arqAtual == null || m_arqAtual.Tipo == TipoArquivo.Monster)
		{
			MessageBox.Show(this, "Escolha antes uma secao de um arquivo de spawn na arvore da esquerda.",
				"Adicionar", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		var secao = m_secaoAtual >= 0 ? m_secaoAtual : (m_arqAtual.Blocos.Count > 0 ? m_arqAtual.Blocos[0].Secao : 0);
		var mapa = m_mapaAtual >= 0 ? m_mapaAtual : (m_arqAtual.MapaFixo >= 0 ? m_arqAtual.MapaFixo : (m_mapa.MapaAtual >= 0 ? m_mapa.MapaAtual : 0));
		var sugestao = m_mapa.CursorJogo;
		if (sugestao.X == 0 && sugestao.Y == 0) sugestao = new Point(128, 128);

		var nova = NovoSpawn.Mostrar(this, m_arqAtual, secao, mapa, sugestao, MobDaPaleta());
		if (nova == null) return;

		var targetMapa = m_arqAtual.MapaDaLinha(nova, secao);
		if (targetMapa < 0) targetMapa = mapa;
		InserirLinha(nova, secao, targetMapa);
	}

	/// <summary>Tabela de criar monstro novo no Monster.txt.</summary>
	void CriarMonstro()
	{
		var arq = m_arquivos.FirstOrDefault(a => a.Tipo == TipoArquivo.Monster);
		if (arq == null)
		{
			MessageBox.Show(this, "Abra uma pasta que tenha o Monster.txt.", "Criar monstro",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		var nova = NovoMonstro.Mostrar(this, arq);
		if (nova == null) return;

		arq.Blocos[0].Linhas.Add(nova);
		arq.Alterado = true;
		AtualizarTitulo();
		MontarArvore();

		MessageBox.Show(this, $"Monstro {nova.Num(0)} criado. Salve o Monster.txt (Ctrl+S com ele aberto) " +
			"e lembre que o cliente precisa ter o modelo dele para aparecer.",
			"Criar monstro", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	/// <summary>Troca o monstro da linha selecionada pela lista do Monster.txt.</summary>
	void TrocarMonstro()
	{
		if (m_grade.CurrentRow?.Tag is not ValueTuple<Bloco, Linha> t) return;
		if (m_arqAtual.Tipo == TipoArquivo.Monster) return;

		var ix = m_arqAtual.Indices(t.Item1.Secao);
		var id = EscolherMonstro.Mostrar(this, t.Item2.Num(ix.mob), m_arqAtual);
		if (id < 0) return;

		t.Item2.SetNum(ix.mob, id);
		if (string.IsNullOrWhiteSpace(t.Item2.Nome) || Dados.Monstros.ContainsValue(t.Item2.Nome))
			t.Item2.Nome = Dados.Monstro(id);

		m_arqAtual.Alterado = true;
		AtualizarTitulo();
		PreencherGrade();
	}

	void Remover()
	{
		if (m_grade.CurrentRow?.Tag is not ValueTuple<Bloco, Linha> t) return;
		if (MessageBox.Show(this, "Remover esta linha?", "Remover", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
		t.Item1.Linhas.Remove(t.Item2);
		m_arqAtual.Alterado = true;
		AtualizarTitulo();
		PreencherGrade();
		MontarArvore();
	}

	void Salvar(bool todos)
	{
		var lista = todos ? m_arquivos : new List<ArquivoSpawn> { m_arqAtual };
		int n = 0;
		foreach (var a in lista)
		{
			if (a == null || (!a.Alterado && !todos)) continue;
			a.Salvar();
			n++;
		}
		m_contador.Text = n == 0 ? "Nada para salvar." : $"{n} arquivo(s) salvo(s).";
		AtualizarTitulo();
	}

	/// <summary>Reagrupa as linhas por secao e mapa e ordena por mob, X e Y.</summary>
	void Organizar()
	{
		if (m_arqAtual == null || m_arqAtual.Tipo == TipoArquivo.Monster)
		{
			MessageBox.Show(this, "A organizacao vale para os arquivos de spawn.", "Organizar",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		if (MessageBox.Show(this,
			"Vou reescrever o arquivo agrupando por secao e mapa e ordenando por monstro.\n\n" +
			"Os comentarios de cada linha sao mantidos, mas os cabecalhos soltos do arquivo\n" +
			"sao trocados por um padrao. Continuar?",
			"Organizar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

		var todas = m_arqAtual.Todas().Select(t => (t.bloco.Secao, t.linha)).ToList();
		var novos = new List<Bloco>();

		foreach (var secao in todas.Select(t => t.Secao).Distinct().OrderBy(s => s))
		{
			var ix = m_arqAtual.Indices(secao);
			var doSetor = todas.Where(t => t.Secao == secao).Select(t => t.linha).ToList();

			IEnumerable<IGrouping<int, Linha>> grupos = m_arqAtual.Tipo == TipoArquivo.SSeMUSpawn
				? doSetor.GroupBy(_ => m_arqAtual.MapaFixo)
				: (ix.mapa >= 0 ? doSetor.GroupBy(l => l.Num(ix.mapa)).OrderBy(g => g.Key) : new[] { doSetor.GroupBy(_ => -1).First() });

			foreach (var g in grupos)
			{
				var b = new Bloco { Secao = secao };
				var titulo = g.Key >= 0 ? $"{Dados.Mapa(g.Key)} - {NomeSecao(m_arqAtual, secao)}" : NomeSecao(m_arqAtual, secao);

				b.Antes.Add("");
				b.Antes.Add("//=========================================================================================");
				b.Antes.Add("//  " + titulo);
				b.Antes.Add("//=========================================================================================");
				b.Antes.Add("//" + string.Join("\t", m_arqAtual.Colunas(secao)) + "\tNome");

				b.Linhas.AddRange(g.OrderBy(l => l.Num(ix.mob))
									.ThenBy(l => ix.x >= 0 ? l.Num(ix.x) : 0)
									.ThenBy(l => ix.y >= 0 ? l.Num(ix.y) : 0));

				// preenche o comentario com o nome do monstro quando estiver vazio
				foreach (var l in b.Linhas)
					if (string.IsNullOrWhiteSpace(l.Nome) && ix.mob >= 0)
						l.Nome = Dados.Monstro(l.Num(ix.mob));

				novos.Add(b);
			}
		}

		var cabecalho = m_arqAtual.Blocos.Count > 0 ? m_arqAtual.Blocos[0].Antes.TakeWhile(x => x.TrimStart().StartsWith("//") || x.Trim() == "").ToList() : new List<string>();
		if (novos.Count > 0 && cabecalho.Count > 0) novos[0].Antes.InsertRange(0, cabecalho);

		m_arqAtual.Blocos = novos;
		m_arqAtual.Rodape.Clear();
		m_arqAtual.Alterado = true;

		AtualizarTitulo();
		MontarArvore();
		PreencherGrade();
		m_contador.Text = "Arquivo organizado. Confira e salve com Ctrl+S.";
	}

	// ================================================================  validar
	readonly List<(ArquivoSpawn arq, Bloco bloco, Linha linha)> m_achados = new();

	void Validar()
	{
		m_problemas.Items.Clear();
		m_achados.Clear();

		int totalSetBase = 0;

		foreach (var a in m_arquivos)
		{
			if (a.Tipo == TipoArquivo.Monster) continue;

			foreach (var (b, l) in a.Todas())
			{
				var ix = a.Indices(b.Secao);
				var mob = l.Num(ix.mob);
				var mapa = a.MapaDaLinha(l, b.Secao);

				if (Dados.Monstros.Count > 0 && !Dados.Monstros.ContainsKey(mob))
					Add(a, b, l, $"ERRO  {a.NomeCurto}: monstro {mob} nao existe no Monster.txt");

				if (mapa >= 0 && (mapa < 0 || mapa >= Dados.NomeMapa.Length))
					Add(a, b, l, $"ERRO  {a.NomeCurto}: mapa {mapa} fora da faixa 0-{Dados.NomeMapa.Length - 1} (97K suporta 0 a 16)");

				if (ix.x < 0) continue;
				int x = l.Num(ix.x), y = l.Num(ix.y);

				if (x < 0 || x > 255 || y < 0 || y > 255)
				{
					Add(a, b, l, $"ERRO  {a.NomeCurto}: coordenada fora do mapa ({x},{y})");
					continue;
				}

				if (ix.x2 < 0)
				{
					// NPC parado em celula bloqueada e normal (ele nao anda), entao so
					// avisa para monstro de verdade
					var ehNpc = a.Tipo == TipoArquivo.Kanturu || b.Secao == 0;

					if (!ehNpc && Dados.Parede(mapa, x, y))
					{
						var area = Dados.AreaDe(mapa, x, y);
						Add(a, b, l, $"AVISO {Dados.Mapa(mapa)}{(area.Length > 0 ? " [" + area + "]" : "")}: " +
									 $"{Dados.Monstro(mob)} em ({x},{y}) esta em parede");
					}
				}
				else
				{
					int x2 = l.Num(ix.x2), y2 = l.Num(ix.y2);
					int x1 = Math.Min(x, x2), y1 = Math.Min(y, y2);
					x2 = Math.Max(x, x2); y2 = Math.Max(y, y2);

					int livres = 0, total = 0;
					for (int px = x1; px <= x2; px++)
						for (int py = y1; py <= y2; py++)
						{
							total++;
							if (!Dados.Parede(mapa, px, py)) livres++;
						}

					if (total > 0 && livres * 100 / total < 20)
						Add(a, b, l, $"AVISO {Dados.Mapa(mapa)}: spot de {Dados.Monstro(mob)} ({x1},{y1})-({x2},{y2}) so tem {livres * 100 / total}% de chao livre");

					if (l.Num(ix.qtd) <= 0)
						Add(a, b, l, $"AVISO {a.NomeCurto}: spot de {Dados.Monstro(mob)} com quantidade {l.Num(ix.qtd)}");
				}
			}
		}

		if (m_problemas.Items.Count == 0) m_problemas.Items.Add("Nenhum problema encontrado.");

		AtualizarContador(totalSetBase);
	}

	void Add(ArquivoSpawn a, Bloco b, Linha l, string texto)
	{
		m_achados.Add((a, b, l));
		m_problemas.Items.Add(texto);
	}

	void IrParaProblema()
	{
		var i = m_problemas.SelectedIndex;
		if (i < 0 || i >= m_achados.Count) return;
		var (a, b, l) = m_achados[i];

		foreach (TreeNode nA in m_arvore.Nodes)
		{
			if (nA.Tag is not ArquivoSpawn arq || arq != a)
			{
				if (nA.Tag is ValueTuple<ArquivoSpawn, int, int> ta && ta.Item1 != a) continue;
				if (nA.Tag is not ArquivoSpawn && nA.Tag is not ValueTuple<ArquivoSpawn, int, int>) continue;
			}

			var mapa = a.MapaDaLinha(l, b.Secao);

			if (a.Tipo == TipoArquivo.SSeMUSpawn)
			{
				foreach (TreeNode nS in nA.Nodes)
					if (nS.Tag is ValueTuple<ArquivoSpawn, int, int> ts && ts.Item2 == b.Secao)
					{
						m_arvore.SelectedNode = nS;
						goto achou;
					}
				m_arvore.SelectedNode = nA;
				goto achou;
			}

			foreach (TreeNode nS in nA.Nodes)
			{
				if (nS.Tag is not ValueTuple<ArquivoSpawn, int, int> ts || ts.Item2 != b.Secao) continue;

				foreach (TreeNode nM in nS.Nodes)
					if (nM.Tag is ValueTuple<ArquivoSpawn, int, int> tm && tm.Item3 == mapa) { m_arvore.SelectedNode = nM; goto achou; }

				m_arvore.SelectedNode = nS;
				goto achou;
			}
		}
	achou:
		foreach (DataGridViewRow r in m_grade.Rows)
			if (r.Tag is ValueTuple<Bloco, Linha> t && ReferenceEquals(t.Item2, l))
			{
				m_grade.CurrentCell = r.Cells[0];
				r.Selected = true;
				break;
			}
	}

	void AtualizarContador(int totalSetBase = -1)
	{
		if (totalSetBase < 0)
		{
			totalSetBase = m_arquivos
				.Where(x => x.Tipo == TipoArquivo.SetBase || x.Tipo == TipoArquivo.SSeMUSpawn)
				.Sum(x => x.TotalObjetos());
		}

		const int limite = 6800;
		m_contador.ForeColor = totalSetBase > limite ? CErro : (totalSetBase > limite * 0.9 ? CDestaque : COk);
		m_contador.Text = $"Total de Objetos: {totalSetBase} de {limite} (OBJ_MAXMONSTER)" +
						  (totalSetBase > limite ? "  -  PASSOU DO LIMITE: monstro nao vai nascer!" : "");
	}

	void AtualizarTitulo()
	{
		var sujos = m_arquivos.Count(a => a.Alterado);
		Text = "MonsterSpawn Studio" + (sujos > 0 ? $"  -  {sujos} arquivo(s) com mudanca nao salva" : "");
	}

	void Ajuda()
	{
		MessageBox.Show(this,
			"MAPA\n" +
			"  roda do mouse .......... zoom\n" +
			"  botao do meio .......... arrastar\n" +
			"  clique no ponto ........ seleciona a linha\n" +
			"  arrastar o ponto ....... muda X e Y\n" +
			"  clique no vazio ........ cria uma linha nova ali\n\n" +
			"As coordenadas sao as do jogo. O minimapa vem do cliente\n" +
			"(Data\\World<N>\\Map1.ozj) e as paredes vem do terreno do\n" +
			"servidor (Data\\Terrain), por cima em vermelho.\n\n" +
			"F5 confere tudo: monstro que nao existe, coordenada fora do\n" +
			"mapa, ponto em parede, spot sem chao livre e o total de\n" +
			"objetos contra o limite do GameServer.",
			"Como usar", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	// ==================================================================  config
	void CarregarConfig()
	{
		try
		{
			if (!File.Exists(ArquivoConfig)) return;

			string servidor = "", cliente = "";
			foreach (var l in File.ReadAllLines(ArquivoConfig, Encoding.UTF8))
			{
				var i = l.IndexOf('=');
				if (i < 0) continue;
				var chave = l.Substring(0, i).Trim();
				var valor = l.Substring(i + 1).Trim();
				if (chave == "Servidor") servidor = valor;
				if (chave == "Cliente") cliente = valor;
				if (chave == "Estrutura")
				{
					if (Enum.TryParse<ModoServidor>(valor, true, out var m))
						m_modoServidor = m;
				}
			}

			AtualizarMenuModoServidor();

			// o cliente primeiro: a pasta do servidor ja manda desenhar o mapa
			if (Directory.Exists(cliente)) Dados.PastaCliente = cliente;
			if (Directory.Exists(servidor)) CarregarPasta(servidor);
		}
		catch { }
	}

	void GravarConfig()
	{
		try
		{
			File.WriteAllText(ArquivoConfig,
				$"Servidor={Dados.PastaData}\r\nCliente={Dados.PastaCliente}\r\nEstrutura={m_modoServidor}\r\n", Encoding.UTF8);
		}
		catch { }
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (m_arquivos.Any(a => a.Alterado))
		{
			var r = MessageBox.Show(this, "Tem mudanca nao salva. Salvar antes de sair?", "MonsterSpawn Studio",
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (r == DialogResult.Cancel) { e.Cancel = true; return; }
			if (r == DialogResult.Yes) Salvar(true);
		}
		GravarConfig();
		base.OnFormClosing(e);
	}
}
