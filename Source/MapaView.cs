using System.ComponentModel;

namespace MonsterSpawnStudio;

/// <summary>
/// Desenha o minimapa do mapa escolhido, com as paredes por cima e os pontos de spawn.
/// As coordenadas sao as do jogo: X para a direita, Y para cima (a linha 0 da imagem
/// e o Y 255), igual ao minimapa que o jogador ve.
/// </summary>
public class MapaView : Control
{
	public class Ponto
	{
		public int X, Y, X2 = -1, Y2 = -1;
		public string Texto = "";
		public object Tag;
		public bool Selecionado;
	}

	readonly List<Ponto> m_pontos = new();

	public IReadOnlyList<Ponto> Pontos => m_pontos;
	Bitmap m_paredes;
	int m_mapa = -1;
	float m_zoom = 2f;
	PointF m_pan = new(0, 0);
	Point m_arrasteIni;
	bool m_arrastando, m_movendoPonto;
	Ponto m_sobre;

	public event EventHandler<Ponto> PontoSelecionado;
	public event EventHandler<Ponto> PontoMovido;
	public event EventHandler<Point> CliqueVazio;

	public enum ModoParede { Contorno, Cheio, Nenhum }

	ModoParede m_modoParede = ModoParede.Contorno;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ModoParede Paredes
	{
		get => m_modoParede;
		set { m_modoParede = value; RefazerParedes(); Invalidate(); }
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool MostrarAreas { get; set; } = true;

	float m_brilho = 0.45f;

	/// <summary>Clareia o minimapa do cliente, que vem bem escuro (0 = original).</summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public float Brilho
	{
		get => m_brilho;
		set { m_brilho = Math.Clamp(value, 0f, 1.2f); Invalidate(); }
	}

	int m_minimoParede = 12;

	/// <summary>
	/// Tamanho minimo do bloqueio para desenhar o contorno forte. Mapa como Noria tem
	/// milhares de pedrinhas bloqueadas; elas viram um ponto fraco em vez de contorno.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int MinimoParede
	{
		get => m_minimoParede;
		set { m_minimoParede = Math.Max(0, value); RefazerParedes(); Invalidate(); }
	}
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool MostrarMinimapa { get; set; } = true;
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool MostrarGrade { get; set; } = false;
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Point CursorJogo { get; private set; }

	public MapaView()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		BackColor = Color.FromArgb(18, 18, 20);
	}

	public int MapaAtual => m_mapa;

	public void DefinirMapa(int mapa)
	{
		if (m_mapa == mapa) return;
		m_mapa = mapa;
		RefazerParedes();
		Invalidate();
	}

	void RefazerParedes()
	{
		m_paredes?.Dispose();
		m_paredes = GerarParedes(m_mapa, m_modoParede, m_minimoParede);
	}

	public void Limpar() { m_pontos.Clear(); Invalidate(); }

	public void Adicionar(Ponto p) { m_pontos.Add(p); }

	public void Atualizar() => Invalidate();

	public void Enquadrar()
	{
		m_zoom = Math.Max(0.5f, Math.Min(Width, Height) / 256f);
		m_pan = new PointF((Width - 256 * m_zoom) / 2f, (Height - 256 * m_zoom) / 2f);
		Invalidate();
	}

	/// <summary>
	/// No modo contorno so a BORDA do bloqueio e pintada, para o minimapa continuar
	/// visivel. No modo cheio pinta tudo, como um mapa de colisao.
	/// </summary>
	static Bitmap GerarParedes(int mapa, ModoParede modo, int minimo)
	{
		if (modo == ModoParede.Nenhum) return null;

		var t = Dados.Terreno(mapa);
		if (t == null) return null;

		bool Bloq(int x, int y) => x < 0 || x > 255 || y < 0 || y > 255 || (t[y * 256 + x] & 4) != 0;

		// mede cada mancha bloqueada, para separar parede de verdade de pedrinha solta
		var tamanho = new int[65536];
		if (modo == ModoParede.Contorno && minimo > 1)
		{
			var visto = new bool[65536];
			var fila = new Queue<int>();
			var mancha = new List<int>();

			for (int i = 0; i < 65536; i++)
			{
				if (visto[i] || (t[i] & 4) == 0) continue;

				mancha.Clear();
				fila.Clear();
				fila.Enqueue(i);
				visto[i] = true;

				while (fila.Count > 0)
				{
					var p = fila.Dequeue();
					mancha.Add(p);
					int px = p & 255, py = p >> 8;

					void Ver(int nx, int ny)
					{
						if (nx < 0 || nx > 255 || ny < 0 || ny > 255) return;
						int q = ny * 256 + nx;
						if (visto[q] || (t[q] & 4) == 0) return;
						visto[q] = true;
						fila.Enqueue(q);
					}

					Ver(px - 1, py); Ver(px + 1, py); Ver(px, py - 1); Ver(px, py + 1);
				}

				foreach (var p in mancha) tamanho[p] = mancha.Count;
			}
		}

		var bmp = new Bitmap(256, 256);
		for (int y = 0; y < 256; y++)
		{
			for (int x = 0; x < 256; x++)
			{
				int i = y * 256 + x;
				var v = t[i];
				Color c = Color.Transparent;

				if ((v & 4) != 0)
				{
					if (modo == ModoParede.Cheio)
					{
						c = Color.FromArgb(150, 200, 40, 40);
					}
					else if (minimo > 1 && tamanho[i] < minimo)
					{
						c = Color.FromArgb(90, 235, 90, 90);    // obstaculo pequeno: so um ponto fraco
					}
					else
					{
						bool borda = !Bloq(x - 1, y) || !Bloq(x + 1, y) || !Bloq(x, y - 1) || !Bloq(x, y + 1);
						c = borda ? Color.FromArgb(210, 235, 60, 60) : Color.FromArgb(24, 120, 20, 20);
					}
				}
				else if ((v & 1) != 0) c = Color.FromArgb(modo == ModoParede.Cheio ? 110 : 55, 60, 150, 255);
				else if ((v & 8) != 0) c = Color.FromArgb(modo == ModoParede.Cheio ? 120 : 65, 250, 200, 60);

				bmp.SetPixel(x, 255 - y, c);   // Y do jogo -> linha da imagem
			}
		}
		return bmp;
	}

	// ------------------------------------------------------------ conversoes
	public PointF JogoParaTela(float x, float y) => new(m_pan.X + x * m_zoom, m_pan.Y + (255 - y) * m_zoom);

	public Point TelaParaJogo(Point p)
	{
		var x = (int)Math.Round((p.X - m_pan.X) / m_zoom);
		var y = 255 - (int)Math.Round((p.Y - m_pan.Y) / m_zoom);
		return new Point(Math.Clamp(x, 0, 255), Math.Clamp(y, 0, 255));
	}

	// -------------------------------------------------------------- desenho
	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		g.Clear(BackColor);
		g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
		g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

		var destino = new RectangleF(m_pan.X, m_pan.Y, 256 * m_zoom, 256 * m_zoom);

		var mini = MostrarMinimapa ? Dados.Minimapa(m_mapa) : null;
		if (mini != null)
		{
			if (m_brilho > 0.001f)
			{
				// clareia sem estourar: soma no brilho e abre um pouco o contraste
				float ganho = 1f + m_brilho * 0.8f;
				float soma = m_brilho * 0.30f;
				var matriz = new System.Drawing.Imaging.ColorMatrix(new[]
				{
					new[] { ganho, 0f, 0f, 0f, 0f },
					new[] { 0f, ganho, 0f, 0f, 0f },
					new[] { 0f, 0f, ganho, 0f, 0f },
					new[] { 0f, 0f, 0f, 1f, 0f },
					new[] { soma, soma, soma, 0f, 1f }
				});

				using var atrib = new System.Drawing.Imaging.ImageAttributes();
				atrib.SetColorMatrix(matriz);
				g.DrawImage(mini, new Rectangle((int)destino.X, (int)destino.Y, (int)destino.Width, (int)destino.Height),
					0, 0, mini.Width, mini.Height, GraphicsUnit.Pixel, atrib);
			}
			else g.DrawImage(mini, destino);
		}
		else { using var b = new SolidBrush(Color.FromArgb(205, 205, 210)); g.FillRectangle(b, destino); }   // sem minimapa: chao claro

		if (m_paredes != null) g.DrawImage(m_paredes, destino);

		// areas com nome (Devias 2, Lost Tower 3, Atlans 2...)
		if (MostrarAreas && m_zoom >= 1.2f)
		{
			using var canetaA = new Pen(Color.FromArgb(200, 120, 220, 255), 1.4f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
			using var fundoA = new SolidBrush(Color.FromArgb(190, 10, 30, 50));
			using var txtA = new SolidBrush(Color.FromArgb(235, 170, 230, 255));

			foreach (var a in Dados.Areas(m_mapa))
			{
				var p1 = JogoParaTela(a.X1, a.Y2);
				var p2 = JogoParaTela(a.X2 + 1, a.Y1 - 1);
				var r = RectangleF.FromLTRB(p1.X, p1.Y, p2.X, p2.Y);
				g.DrawRectangle(canetaA, r.X, r.Y, r.Width, r.Height);

				var rotulo = a.Level > 0 ? $"{a.Nome}  (lv {a.Level})" : a.Nome;
				var tam = g.MeasureString(rotulo, Font);
				g.FillRectangle(fundoA, r.X, r.Y - tam.Height - 2, tam.Width + 6, tam.Height + 2);
				g.DrawString(rotulo, Font, txtA, r.X + 3, r.Y - tam.Height - 1);
			}
		}

		if (MostrarGrade && m_zoom >= 3f)
		{
			using var caneta = new Pen(Color.FromArgb(40, 255, 255, 255));
			for (int i = 0; i <= 256; i += 10)
			{
				g.DrawLine(caneta, m_pan.X + i * m_zoom, m_pan.Y, m_pan.X + i * m_zoom, m_pan.Y + 256 * m_zoom);
				g.DrawLine(caneta, m_pan.X, m_pan.Y + i * m_zoom, m_pan.X + 256 * m_zoom, m_pan.Y + i * m_zoom);
			}
		}

		// areas de spot primeiro, para os pontos ficarem por cima
		foreach (var p in m_pontos)
		{
			if (p.X2 < 0) continue;
			var a = JogoParaTela(Math.Min(p.X, p.X2), Math.Max(p.Y, p.Y2));
			var b = JogoParaTela(Math.Max(p.X, p.X2) + 1, Math.Min(p.Y, p.Y2) - 1);
			var r = RectangleF.FromLTRB(a.X, a.Y, b.X, b.Y);

			using var pincel = new SolidBrush(Color.FromArgb(p.Selecionado ? 70 : 35, 255, 230, 120));
			using var caneta = new Pen(p.Selecionado ? Color.Gold : Color.FromArgb(160, 255, 230, 120), p.Selecionado ? 2f : 1f);
			g.FillRectangle(pincel, r);
			g.DrawRectangle(caneta, r.X, r.Y, r.Width, r.Height);
		}

		foreach (var p in m_pontos)
		{
			var t = JogoParaTela(p.X, p.Y);
			float raio = p.Selecionado ? 6f : 4f;
			using var pincel = new SolidBrush(p.Selecionado ? Color.Gold : Color.FromArgb(230, 90, 220, 120));
			using var caneta = new Pen(Color.FromArgb(200, 0, 0, 0));
			g.FillEllipse(pincel, t.X - raio, t.Y - raio, raio * 2, raio * 2);
			g.DrawEllipse(caneta, t.X - raio, t.Y - raio, raio * 2, raio * 2);
		}

		if (m_sobre != null)
		{
			var t = JogoParaTela(m_sobre.X, m_sobre.Y);
			var texto = m_sobre.Texto;
			var tam = g.MeasureString(texto, Font);
			var cx = Math.Min(t.X + 10, Width - tam.Width - 6);
			var cy = Math.Max(t.Y - tam.Height - 8, 2);
			using var fundo = new SolidBrush(Color.FromArgb(230, 20, 20, 24));
			using var txt = new SolidBrush(Color.Gainsboro);
			g.FillRectangle(fundo, cx - 4, cy - 2, tam.Width + 8, tam.Height + 4);
			g.DrawString(texto, Font, txt, cx, cy);
		}
	}

	// -------------------------------------------------------------- eventos
	protected override void OnMouseDown(MouseEventArgs e)
	{
		Focus();
		if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Space)))
		{
			m_arrastando = true; m_arrasteIni = e.Location; return;
		}

		if (e.Button == MouseButtons.Left)
		{
			var achado = Achar(e.Location);
			if (achado != null)
			{
				foreach (var p in m_pontos) p.Selecionado = false;
				achado.Selecionado = true;
				m_movendoPonto = true;
				PontoSelecionado?.Invoke(this, achado);
				Invalidate();
			}
			else
			{
				CliqueVazio?.Invoke(this, TelaParaJogo(e.Location));
			}
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		CursorJogo = TelaParaJogo(e.Location);

		if (m_arrastando)
		{
			m_pan = new PointF(m_pan.X + (e.X - m_arrasteIni.X), m_pan.Y + (e.Y - m_arrasteIni.Y));
			m_arrasteIni = e.Location;
			Invalidate();
			return;
		}

		if (m_movendoPonto && e.Button == MouseButtons.Left)
		{
			var sel = m_pontos.FirstOrDefault(p => p.Selecionado);
			if (sel != null)
			{
				var j = TelaParaJogo(e.Location);
				if (sel.X != j.X || sel.Y != j.Y)
				{
					sel.X = j.X; sel.Y = j.Y;
					PontoMovido?.Invoke(this, sel);
					Invalidate();
				}
			}
			return;
		}

		var antes = m_sobre;
		m_sobre = Achar(e.Location);
		if (antes != m_sobre) Invalidate();

		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		m_arrastando = false;
		m_movendoPonto = false;
		base.OnMouseUp(e);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		var antes = TelaParaJogo(e.Location);
		m_zoom = Math.Clamp(m_zoom * (e.Delta > 0 ? 1.2f : 1 / 1.2f), 0.4f, 16f);
		var depois = JogoParaTela(antes.X, antes.Y);
		m_pan = new PointF(m_pan.X + (e.X - depois.X), m_pan.Y + (e.Y - depois.Y));
		Invalidate();
		base.OnMouseWheel(e);
	}

	Ponto Achar(Point tela)
	{
		Ponto melhor = null;
		float melhorD = 10f;
		foreach (var p in m_pontos)
		{
			var t = JogoParaTela(p.X, p.Y);
			var d = (float)Math.Sqrt(Math.Pow(t.X - tela.X, 2) + Math.Pow(t.Y - tela.Y, 2));
			if (d < melhorD) { melhorD = d; melhor = p; }
		}
		return melhor;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing) m_paredes?.Dispose();
		base.Dispose(disposing);
	}
}
