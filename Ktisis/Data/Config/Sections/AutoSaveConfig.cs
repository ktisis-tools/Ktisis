namespace Ktisis.Data.Config.Sections;

public class AutoSaveConfig {
	public bool Enabled = false;
	public int Interval = 60;
	public int Count = 5;
	public string FilePath = string.Empty;
	public string FolderFormat = "AutoSave - %Date% %Time%";
	public bool ClearOnExit = false;
}
