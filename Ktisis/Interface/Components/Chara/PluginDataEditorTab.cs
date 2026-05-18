using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Ipc;
using Ktisis.Scene.Types;

using GLib.Widgets;

using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class PluginDataEditorTab {
	
	private readonly IpcManager _ipcManager;
	private readonly IEditorContext _ctx;
	private readonly IList<IPCProfileDataTuple> _cPlusProfiles = new List<IPCProfileDataTuple>();
	private readonly Dictionary<Guid, string> _penumbraCollections = new Dictionary<Guid, string>();
	private readonly Dictionary<Guid, string> _glamourerCollections = new Dictionary<Guid, string>();
	
	private (Guid Id, string Name) _currentPenumbra = (Guid.Empty, string.Empty);
	private Guid? _selectedGlamourer = null;
	
	public PluginDataEditorTab(
		IEditorContext ctx
	) {
		this._ctx = ctx;
		this._ipcManager = ctx.Plugin.Ipc;
		if (this._ipcManager.IsCustomizeActive)
			this._cPlusProfiles = this._ipcManager.GetCustomizeIpc().GetProfileList().OrderBy(x => x.Name).ToList();
		if (this._ipcManager.IsPenumbraActive)
			this._penumbraCollections = this._ipcManager.GetPenumbraIpc().GetCollections();
		if (this._ipcManager.IsGlamourerActive)
			this._glamourerCollections = this._ipcManager.GetGlamourerIpc().GetDesignList();
	}
	public void Draw() {
		var actor = (ActorEntity)this._ctx.Selection.GetSelected().FirstOrDefault(a => a.Type == EntityType.Actor);
		if (actor == null) {
			ImGui.Text("Please select an actor!");
			return;
		}
		
		if (ImGui.Button("Load MCDF")) {
			this._ctx.Interface.OpenMcdfFile(path => this.ImportMcdf(actor, path));
		}
		
		ImGui.SameLine();
		if (ImGui.Button("Revert all IPC data")) {
			actor.AssignedProfile = null;
			this._ctx.Characters.Mcdf.Revert(actor.Actor);
		}

		if (this._ipcManager.IsCustomizeActive) {
			Separators.SeparatorText("Customize+", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
			this.DrawCustomizePlus(actor);
		}

		if (this._ipcManager.IsPenumbraActive) {
			Separators.SeparatorText("Penumbra", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
			this.DrawPenumbra(actor);
		} 

		if (this._ipcManager.IsGlamourerActive) {
			Separators.SeparatorText("Glamourer", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
			this.DrawGlamourer(actor);
		} 
	}

	public unsafe void DrawCustomizePlus(ActorEntity actor) {
		var cPlus = this._ipcManager.GetCustomizeIpc();
		
		var currentId = cPlus.GetActiveProfileId(actor.Actor.ObjectIndex).Id;
		if (actor.AssignedProfile != null)
			currentId = actor.AssignedProfile;

		if(ImGui.BeginCombo("##CPlus", currentId != null ? this._cPlusProfiles.FirstOrDefault(p => p.UniqueId == currentId).Name : ""))
		{
			/*ImGuiTextFilter filter = new ImGuiTextFilter();  TODO: read for inspo https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/Internal/Windows/ConsoleWindow.cs#L33
			filter.Draw("##Filter");*/

			foreach (var profile in this._cPlusProfiles) {
				bool selected = profile.UniqueId == currentId;
				//if (filter.PassFilter(profile.Name)) {
					if (ImGui.Selectable(profile.Name, selected)) {
						if (selected) {
							cPlus.DeleteTemporaryProfile(actor.Character->ObjectIndex);
							actor.AssignedProfile= null;
							break;
						}
						cPlus.DeleteTemporaryProfile(actor.Character->ObjectIndex);
						var o = cPlus.SetTemporaryProfile(actor.Character->ObjectIndex, cPlus.GetProfileByUniqueId(profile.UniqueId).Data);
						actor.AssignedProfile = profile.UniqueId;
					//}
				}
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		ImGui.Text("C+ Profile");
	}

	public void DrawPenumbra(ActorEntity actor) {
		var pen = this._ipcManager.GetPenumbraIpc();
		
		var currentCollection = pen.GetCollectionForObject(actor.Actor);
		if (currentCollection.Id != Guid.Empty) {
			foreach (var profile in this._penumbraCollections) {
				if (profile.Key != currentCollection.Id) continue;
				this._currentPenumbra = (profile.Key, profile.Value);
			}
		}

		if(ImGui.BeginCombo("##Penumbra", this._currentPenumbra != default ? this._currentPenumbra.Name : ""))
		{
			/*ImGuiTextFilter filter = new ImGuiTextFilter();  TODO: read for inspo https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/Internal/Windows/ConsoleWindow.cs#L33
			filter.Draw("##Filter");*/

			foreach (var profile in this._penumbraCollections) {
				bool selected = profile.Key == this._currentPenumbra.Id;
				//if (filter.PassFilter(profile.Name)) {
				if (ImGui.Selectable(profile.Value, selected)) {
					if (selected) {
						pen.SetCollectionForObject(actor.Actor, null);
						break;
					}
					pen.SetCollectionForObject(actor.Actor, profile.Key);
					this._currentPenumbra.Id = profile.Key;
					this._currentPenumbra.Name = profile.Value;
					//}
				}
			}
			ImGui.EndCombo();
		}
		ImGui.SameLine();
		ImGui.Text("Penumbra Collection");

		if (ImGui.Button("Apply Invisible skin")) {
			this._ctx.Characters.Mcdf.SetInvisibleSkin(actor);
		}
	}

	public void DrawGlamourer(ActorEntity actor) {
		var glam = this._ipcManager.GetGlamourerIpc();

		using (var group = ImRaii.Group()) {
			ImGuiTextFilter filter = new ImGuiTextFilter();
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

		using var _ = ImRaii.Group();
		using (ImRaii.Disabled(this._selectedGlamourer == null)) {
			ImGui.Text($"Currently selected:\n{(this._selectedGlamourer == null? "None" : this._glamourerCollections[this._selectedGlamourer.Value])}");
			
			if (ImGui.Button("Apply")) {
				glam.ApplyDesignToObject(actor.Actor, this._selectedGlamourer!.Value);
				this._selectedGlamourer = null;
			}
		}
	}
	
	private void ImportMcdf(ActorEntity actor, string path) {
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, actor.Actor);
	}
}
