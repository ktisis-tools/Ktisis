using Dalamud.Plugin.Services;

using GLib.Lists;

using Lumina.Excel;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Animation.Types;
using Ktisis.Structs.Actors;

using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class AnimationEditorTab {
	private readonly static PoseModeEnum[] Modes = [
		PoseModeEnum.Idle, PoseModeEnum.SitGround, PoseModeEnum.SitChair, PoseModeEnum.Sleeping
	];

	private readonly IDataManager _data;

	private readonly ListBox<Emote> _emoteList;
	
	private ExcelSheet<Emote>? Emotes;
	
	public IAnimationEditor Editor { set; private get; } = null!;

	public AnimationEditorTab(
		IDataManager data
	) {
		this._data = data;

		this._emoteList = new ListBox<Emote>(
			"Emotes",
			DrawEmote
		);
	}

	private ushort Id;
	
	public void Draw() {
		var id = (int)this.Id;
		ImGui.SetNextItemWidth(100);
		if (ImGui.InputInt("##id", ref id))
			this.Id = (ushort)id;
		ImGui.SameLine();
		if (ImGui.Button("Play"))
			this.Editor.SetTimelineId(this.Id);

		this.Emotes ??= this._data.GetExcelSheet<Emote>()!;

		if (this._emoteList.Draw(this.Emotes, (int)this.Emotes.RowCount, out var emote))
			this.Editor.PlayEmote(emote!);

		ImGui.Spacing();

		if (!this.Editor.TryGetModeAndPose(out var mode, out var pose))
			return;

		if (ImGui.BeginCombo("Mode", mode.ToString())) {
			foreach (var modeType in Modes) {
				if (ImGui.Selectable(modeType.ToString(), modeType == mode))
					this.Editor.SetPose(modeType, 0);
			}
			ImGui.EndCombo();
		}
		
		if (ImGui.InputInt("Pose", ref pose)) {
			var count = this.Editor.GetPoseCount(mode);
			pose = pose < 0 ? count - 1 : pose % count;
			this.Editor.SetPose(mode, (byte)pose);
		}

		var isWepDrawn = this.Editor.IsWeaponDrawn;
		if (ImGui.Button(isWepDrawn ? "Sheathe Weapon" : "Draw Weapon"))
			this.Editor.ToggleWeapon();
	}

	private static bool DrawEmote(Emote emote, bool isFocus)
		=> ImGui.Selectable($"{emote.RowId} {emote.Name}", isFocus);
}
