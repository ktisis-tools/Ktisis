using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Ipc;

using GLib.Widgets;

using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class PluginDataEditorTab {

	private readonly IpcManager _ipcManager;
	private readonly IEditorContext _ctx;
	private readonly IDalamudPluginInterface _dpi;

	private ActorEntity? _actor;

	private readonly IList<IPCProfileDataTuple> _cPlusProfiles = new List<IPCProfileDataTuple>();
	private readonly Dictionary<Guid, string> _penumbraCollections = new Dictionary<Guid, string>();
	private readonly Dictionary<Guid, string> _glamourerCollections = new Dictionary<Guid, string>();

	private (Guid Id, string Name) _currentPenumbra = (Guid.Empty, string.Empty);
	private Guid? _selectedGlamourer = null;

	public PluginDataEditorTab(
		IEditorContext ctx,
		IDalamudPluginInterface dpi
	) {
		this._ctx = ctx;
		this._ipcManager = ctx.Plugin.Ipc;
		this._dpi = dpi;
		this._actor = null;

		if (this._ipcManager.IsCustomizeActive)
			this._cPlusProfiles = this._ipcManager.GetCustomizeIpc().GetProfileList().OrderBy(x => x.Name).ToList();
		if (this._ipcManager.IsPenumbraActive)
			this._penumbraCollections = this._ipcManager.GetPenumbraIpc().GetCollections();
		if (this._ipcManager.IsGlamourerActive)
			this._glamourerCollections = this._ipcManager.GetGlamourerIpc().GetDesignList();
	}

	public void SetTarget(ActorEntity actor) => this._actor = actor;

	public unsafe void Draw() {
		if (this._actor == null) {
			ImGui.Text("Please select an actor!");
			return;
		}

		using (ImRaii.Disabled(!this._ipcManager.IsAnyMcdfActive && this._actor.GetHuman() != null)) {
			if (ImGui.Button("Load MCDF"))
				this._ctx.Interface.OpenMcdfFile(path => this.ImportMcdf(this._actor, path));
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
				using (ImRaii.Tooltip())
					ImGui.Text("A MCDF or 'Mare Character Data File' contains all of a players Penumbra, Glamourer\nand CustomizePlus data that is applied to them at the time of the capture.");
			ImGui.SameLine();
			if (ImGui.Button("Revert all IPC data")) {
				this._actor.AssignedProfile = null;
				this._ctx.Characters.Mcdf.Revert(this._actor.Actor);
			}

		}

		if (this._ipcManager.IsCustomizeActive) {
			Separators.SeparatorText("Customize+", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
			this.DrawCustomizePlus(this._actor);
		}

		if (this._ipcManager.IsPenumbraActive) {
			Separators.SeparatorText("Penumbra", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
			this.DrawPenumbra(this._actor);
		}

		if (this._ipcManager.IsGlamourerActive) {
			Separators.SeparatorText("Glamourer", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
			this.DrawGlamourer(this._actor);
		}
	}

	private unsafe void DrawCustomizePlus(ActorEntity actor) {
		var cPlus = this._ipcManager.GetCustomizeIpc();

		var currentId = cPlus.GetActiveProfileId(actor.Actor.ObjectIndex).Id;
		if (actor.AssignedProfile != null)
			currentId = actor.AssignedProfile;

		if (ImGui.BeginCombo("##CPlus", currentId != null ? this._cPlusProfiles.FirstOrDefault(p => p.UniqueId == currentId).Name : "")) {
			foreach (var profile in this._cPlusProfiles) {
				var selected = profile.UniqueId == currentId;

				if (ImGui.Selectable(profile.Name, selected)) {
					if (selected) {
						cPlus.DeleteTemporaryProfile(actor.Character->ObjectIndex);
						actor.AssignedProfile = null;
						break;
					}
					cPlus.DeleteTemporaryProfile(actor.Character->ObjectIndex);

					var profileJson = cPlus.GetProfileByUniqueId(profile.UniqueId).Data;
					if (profileJson is null) continue; // do nothing if we can't fetch a valid profile to use for the Guid
					cPlus.SetTemporaryProfile(actor.Character->ObjectIndex, profileJson);
					actor.AssignedProfile = profile.UniqueId;
				}
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		ImGui.Text("C+ Profile");

		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Buttons.CalcSize() - .1f);
		if (Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
			this._dpi.InstalledPlugins.FirstOrDefault(p => p is { InternalName: "CustomizePlus", IsLoaded: true })!.OpenMainUi();
		}
	}

	private unsafe void DrawPenumbra(ActorEntity actor) {
		var pen = this._ipcManager.GetPenumbraIpc();

		var currentCollection = pen.GetCollectionForObject(actor.Actor);
		if (currentCollection.Id != Guid.Empty) {
			foreach (var profile in this._penumbraCollections) {
				if (profile.Key != currentCollection.Id) continue;
				this._currentPenumbra = (profile.Key, profile.Value);
			}
		}

		if (ImGui.BeginCombo("##Penumbra", this._currentPenumbra != default ? this._currentPenumbra.Name : "")) {
			foreach (var profile in this._penumbraCollections) {
				bool selected = profile.Key == this._currentPenumbra.Id;

				if (ImGui.Selectable(profile.Value, selected)) {
					if (selected) {
						pen.SetCollectionForObject(actor.Actor, null);
						break;
					}
					pen.SetCollectionForObject(actor.Actor, profile.Key);
					this._currentPenumbra.Id = profile.Key;
					this._currentPenumbra.Name = profile.Value;
				}
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		ImGui.Text("Collection");
		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Buttons.CalcSize() - .1f);
		if (Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
			this._dpi.InstalledPlugins.FirstOrDefault(p => p is { InternalName: "Penumbra", IsLoaded: true })!.OpenMainUi();
		}

		using (ImRaii.Disabled(actor.GetHuman() != null))
			if (ImGui.Button("Apply Invisible skin"))
				this._ctx.Characters.Mcdf.SetInvisibleSkin(actor);
	}

	private void DrawGlamourer(ActorEntity actor) {
		var glam = this._ipcManager.GetGlamourerIpc();

		using (var group = ImRaii.Group()) {
			var filter = new ImGuiTextFilter();
			filter.Draw("##Filter");

			using (ImRaii.ListBox("##Glamourer")) {
				foreach (var profile in this._glamourerCollections.OrderBy(p => p.Value)) {
					if (filter.PassFilter(profile.Value)) {
						if (ImGui.Selectable(profile.Value, profile.Key == this._selectedGlamourer)) {
							if (this._selectedGlamourer.HasValue && this._selectedGlamourer.Value == profile.Key)
								this._selectedGlamourer = null;
							else
								this._selectedGlamourer = profile.Key;
						}
					}
				}
			}
		}
		ImGui.SameLine();

		using (ImRaii.Group()) {
			using (ImRaii.Disabled(this._selectedGlamourer == null)) {
				ImGui.Text($"Current selection:");

				ImGui.TextWrapped($"{(this._selectedGlamourer == null ? "None" : this._glamourerCollections[this._selectedGlamourer.Value])}");

				if (ImGui.Button("Apply")) {
					glam.ApplyDesignToObject(actor.Actor, this._selectedGlamourer!.Value);
					this._selectedGlamourer = null;
				}
			}
		}
		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Buttons.CalcSize() - .1f);
		if (Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
			this._dpi.InstalledPlugins.FirstOrDefault(p => p is { InternalName: "Glamourer", IsLoaded: true })!.OpenMainUi();
		}
	}

	private void ImportMcdf(ActorEntity actor, string path) {
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, actor.Actor);
	}
}
