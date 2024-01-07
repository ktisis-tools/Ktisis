using System.IO;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Core.Attributes;

namespace Ktisis.Interface.Components.Files;

[Transient]
public class FileSelect<T> where T : notnull {
	// Events

	public OpenDialogHandler? OpenDialog;
	public delegate void OpenDialogHandler(FileSelect<T> sender);
	
	// State
	
	public bool IsFileOpened => this.Selected != null;
	public FileSelectState? Selected { get; private set; }

	public void SetFile(string path, T file) {
		this.Selected = new FileSelectState {
			Name = Path.GetFileName(path),
			Path = path,
			File = file
		};
	}

	public void Clear() => this.Selected = null;
	
	// Draw UI

	public void Draw() {
		const string DefaultText = "Select a file to open..."; // TODO: Localize
		
		var path = this.Selected?.Name ?? DefaultText;
		ImGui.InputText("##FileSelectPath", ref path, 256, ImGuiInputTextFlags.ReadOnly);
		
		ImGui.SameLine();

		if (Buttons.IconButton(FontAwesomeIcon.FileImport))
			this.OpenDialog?.Invoke(this);

		using (var _ = ImRaii.Disabled(!this.IsFileOpened)) {
			ImGui.SameLine();
			if (Buttons.IconButton(FontAwesomeIcon.UndoAlt))
				this.Selected = null;
		}
	}

	public class FileSelectState {
		public string Name;
		public string Path;
		public T File;
	}
}
