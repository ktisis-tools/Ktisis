using System;
using System.IO;

namespace Ktisis.Data.Config.Sections;

public class AutoSaveConfig {
	public bool Enabled = false;
	public int Interval = 60;
	public int Count = 5;
	public string FilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
		"Ktisis", "PoseAutoBackup"
	);
	public string FolderFormat = "AutoSave - %Date% %Time%";
	public bool ClearOnExit = false;
	public bool OnDisconnect = true;
	public bool OnDisable = true;
}
