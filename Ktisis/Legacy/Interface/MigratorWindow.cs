using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;

using GLib.Widgets;


using Ktisis.Interface.Types;

namespace Ktisis.Legacy.Interface;

public class MigratorWindow : KtisisWindow {
	private readonly IDalamudPluginInterface _dpi;
	private readonly LegacyMigrator _migrator;

	public MigratorWindow(
		IDalamudPluginInterface dpi,
		LegacyMigrator migrator
	) : base(
		"Ktisis Development Preview",
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._dpi = dpi;
		this._migrator = migrator;
		this.ShowCloseButton = false;
		this.RespectCloseHotkey = false;
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
		ImGui.Text("You have installed the testing version of Ktisis.");
		
		ImGui.Spacing();
		
		ImGui.Text("This version is currently a ");
		ImGui.SameLine(0, style.ItemInnerSpacing.X);
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, ColorYellow))
			ImGui.Text("development preview");
		ImGui.SameLine(0, style.ItemInnerSpacing.X);
		ImGui.Text(" - it is primarily a testbed for new features.");
		
		ImGui.Text("In the coming months, it will officially replace the v0.2/Alpha version.");
		
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		ImGui.SameLine();
		ImGui.Text("What to expect:");
		
		ImGui.Spacing();
		
		ImGui.Text(
			"This is not the final settings migrator - in the full release:\n" +
			"	• Applicable v0.2 settings may be converted to v0.3 counterparts\n" +
			"	• More info will be provided on changed functions and new locations for v0.2 features\n" +
			"	• Our wiki and discord will be linked for further support\n"
		);
		
		ImGui.Spacing();
		ImGui.Text("If you're coming to Ktisis from v0.2, we recommend trying the Toolbar UI.");
		ImGui.Text("This alternate UI mode consolidates the many new windows & editors into one master window to save on screenspace.");
		ImGui.Text("Thank you for bearing with us and for your ongoing support!");
		
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
		
		ImGui.Spacing();
	}
}
