using System.Collections.Generic;

using Dalamud.Interface;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Interop.Unmanaged;
using Ktisis.Interop.Structs.Objects;
using Ktisis.Scenes.Objects.World;

namespace Ktisis.Scenes.Objects.Models; 

public class CharaModels : SceneObject {
	// Trees

	public override uint Color => 0xFFBAFFB2;

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.CubesStacked;
	
	// Model
	
	private new Character? Parent => base.Parent as Character;
	
	// Constructor

	public CharaModels() {
		Name = "Models";
	}
	
	// Models

	private readonly List<uint> Models = new();

	internal unsafe override void Update() {
		// TODO: CharacterBase->Models
		// Model +0x58 can be moved
		// * -> +0xC0 to decouple it from the skeleton

		var models = GetModelArray();

		if (models.Count < Models.Count) {
			Models.RemoveRange(models.Count - 1, Models.Count - models.Count);
			Children.RemoveAll(item => item is ModelSlot slot && slot.SlotIndex >= models.Count);
		}

		var update = false;

		for (var i = 0; i < models.Count; i++) {
			var model = models[i];

			var id = 0u;
			if (model != null && model->Base.ModelResourceHandle != null)
				id = model->Base.ModelResourceHandle->ResourceHandle.Id;

			var prev = 0u;
			if (i >= Models.Count)
				Models.Add(id);
			else
				prev = Models[i];

			if (id == prev) continue;
			Models[i] = id;

			var item = Children
				.Find(x => x is ModelSlot slot && slot.SlotIndex == i);

			switch (item) {
				case ModelSlot when id == 0:
					Children.Remove(item);
					break;
				case null when id != 0:
					item = new ModelSlot(i);
					AddChild(item);
					break;
				default:
					continue;
			}

			update = true;
		}
		
		if (update)
			SortChildren();
	}

	// Unmanaged helpers
	
	private unsafe CharacterBase* GetParentChara()
		=> (CharacterBase*)(Parent?.Address ?? 0);

	private unsafe PtrArray<Model> GetModelArray() {
		var result = new PtrArray<Model>();

		var chara = GetParentChara();
		Model** models;
		if (chara == null || (models = (Model**)chara->Models) == null)
			return result;

		for (var i = 0; i < chara->SlotCount; i++)
			result.Add(models[i]);

		return result;
	}

	internal unsafe Model* GetModelIndex(int index) {
		var chara = GetParentChara();
		if (chara == null || chara->Models == null || index < 0 || index >= chara->SlotCount)
			return null;
		return (Model*)chara->Models[index];
	}
}