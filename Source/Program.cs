using System.Text;

namespace MonsterSpawnStudio;

static class Program
{
	[STAThread]
	static void Main()
	{
		// os arquivos do MuServer sao cp1252
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm());
	}
}
