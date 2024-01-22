using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Interface.Types;

namespace Ktisis.Legacy.Interface;

public class MigratorWindow : KtisisWindow {
	private readonly DalamudPluginInterface _dpi;
	private readonly LegacyMigrator _migrator;

	public MigratorWindow(
		DalamudPluginInterface dpi,
		LegacyMigrator migrator
	) : base(
		"Ktisis Development Preview",
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._dpi = dpi;
		this._migrator = migrator;
	}
	
	private readonly Stopwatch _timer = new();
	private bool _elapsed;
	
	private const uint ColorYellow = 0xFF00FFFF;
	
	private const int WaitTime = 10;

	private bool CanBegin => this._timer.Elapsed.TotalSeconds >= WaitTime || this._elapsed;

	public override void OnOpen() {
		this._timer.Reset();
		this._timer.Start();
		this._elapsed = false;
	}
	
	public override void Draw() {
		if (!this._elapsed && this.CanBegin) {
			this._timer.Stop();
			this._elapsed = true;
		}
		
		var style = ImGui.GetStyle();
		
		Icons.DrawIcon(FontAwesomeIcon.ExclamationCircle);
		ImGui.SameLine();
		ImGui.Text("You are about to install a development version of Ktisis.");
		
		ImGui.Spacing();
		
		ImGui.Text("This version is currently a ");
		ImGui.SameLine(0, style.ItemInnerSpacing.X);
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, ColorYellow))
			ImGui.Text("development preview");
		ImGui.SameLine(0, style.ItemInnerSpacing.X);
		ImGui.Text(" - it is primarily a testbed for new features.");
		
		ImGui.Text("Only the bare essentials have been implemented at this stage so a lot of UI/UX will be missing.");
		
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		ImGui.SameLine();
		ImGui.Text("What to expect:");
		
		ImGui.Spacing();
		
		ImGui.Text("This is not the full feature set of the final release.");
		ImGui.Text(
			"The following will be introduced at a later point during testing:\n" +
			"	• Everything missing from the current release\n" +
			"	• Editing spawned weapons and props\n" +
			"	• Equipment model manipulation\n" +
			"	• Importing and exporting light presets\n" +
			"	• Inverse kinematics\n" +
			"	• Animation controls\n" +
			"	• Copy & paste\n"
		);
		
		ImGui.Spacing();
		ImGui.Text("Undo and redo is currently only implemented for object transforms.");
		ImGui.Text("Support is planned for edits made to objects, such as appearance changes.");
		ImGui.Spacing();
		ImGui.Text("Character appearance edits may also conflict with changes made by Glamourer.");
		ImGui.Text("I hope to discuss with its developer about implementing an IPC to resolve this.");
		ImGui.Spacing();
		ImGui.Text("Configuration options for the overlay, keybinds and bone categories will also be implemented during testing.");
		ImGui.Text("Your current configuration will not be carried over into this version.");
		
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		
		using (var _ = ImRaii.Disabled(!this.CanBegin && !(ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift)))) {
			var text = this.CanBegin ? "Begin" : $"Begin ({Math.Ceiling((decimal)WaitTime - this._timer.Elapsed.Seconds)}s)";
			if (ImGui.Button(text)) {
				this._migrator.Begin();
				this.Close();
			}
		}
		
		ImGui.SameLine();
		
		if (ImGui.Button("Close"))
			this.Close();
		
		ImGui.Spacing();
	}
}
