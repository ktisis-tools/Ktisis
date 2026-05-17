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
	
	private (Guid Id, string Name) _currentCPlus = (Guid.Empty, string.Empty);
	private (Guid Id, string Name) _currentPenumbra = (Guid.Empty, string.Empty);
	
	
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
		if (actor == null)
			return;
		
		if (ImGui.Button("Load MCDF")) {
			this._ctx.Interface.OpenMcdfFile(path => this.ImportMcdf(actor, path));
		}
		ImGui.SameLine();
		if (ImGui.Button("Revert all IPC data")) {
			this._ctx.Characters.Mcdf.Revert(actor.Actor);
		}
		Separators.SeparatorText("Customize+", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
		if (this._ipcManager.IsCustomizeActive) {
			this.DrawCustomizePlus(actor);
		} else {
			ImGui.Text("Customize+ wasn't found");
		}
		Separators.SeparatorText("Penumbra", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
		if (this._ipcManager.IsCustomizeActive) {
			this.DrawPenumbra(actor);
		} else {
			ImGui.Text("Penumbra wasn't found");
		}
		Separators.SeparatorText("Glamourer", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
		if (this._ipcManager.IsCustomizeActive) {
			this.DrawGlamourer(actor);
		} else {
			ImGui.Text("Glamourer wasn't found");
		}
	}

	public unsafe void DrawCustomizePlus(ActorEntity actor) {
		var cPlus = this._ipcManager.GetCustomizeIpc();
		
		var currentId = cPlus.GetActiveProfileId(actor.Actor.ObjectIndex).Id;
		if (currentId != null) {
			foreach (var profile in this._cPlusProfiles) {
				if (profile.UniqueId != currentId) continue;
				this._currentCPlus = (profile.UniqueId, profile.Name);
			}
		}

		if(ImGui.BeginCombo("##CPlus", this._currentCPlus != default ? this._currentCPlus.Name : ""))
		{
			/*ImGuiTextFilter filter = new ImGuiTextFilter();  TODO: read for inspo https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/Internal/Windows/ConsoleWindow.cs#L33
			filter.Draw("##Filter");*/

			foreach (var profile in this._cPlusProfiles) {
				bool selected = profile.UniqueId == this._currentCPlus.Id;
				//if (filter.PassFilter(profile.Name)) {
					if (ImGui.Selectable(profile.Name, selected)) {
						if (selected) {
							cPlus.DeleteTemporaryProfile(actor.Character->ObjectIndex);
							this._currentCPlus = (Guid.Empty, string.Empty);
							break;
						}
						cPlus.DeleteTemporaryProfile(actor.Character->ObjectIndex);
						cPlus.SetTemporaryProfile(actor.Character->ObjectIndex, cPlus.GetProfileByUniqueId(profile.UniqueId).Data);
						this._currentCPlus.Id = profile.UniqueId;
						this._currentCPlus.Name = profile.Name;
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
		
		if(ImGui.BeginCombo("##Glamourer", ""))
		{
			/*ImGuiTextFilter filter = new ImGuiTextFilter();  TODO: read for inspo https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/Internal/Windows/ConsoleWindow.cs#L33
			filter.Draw("##Filter");*/

			foreach (var profile in this._glamourerCollections) {
				//if (filter.PassFilter(profile.Name)) {
				if (ImGui.Selectable(profile.Value, false)) {
					glam.ApplyDesignToObject(actor.Actor, profile.Key);
					//}
				}
			}
			ImGui.EndCombo();
		}
	}
	
	private void ImportMcdf(ActorEntity actor, string path) {
		this._ctx.Characters.Mcdf.LoadAndApplyTo(path, actor.Actor);
	}
}
