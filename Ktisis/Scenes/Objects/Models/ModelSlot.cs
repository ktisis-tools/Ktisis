using Ktisis.Common.Utility;
using Ktisis.Interface.SceneUi.Logic;
using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Scenes.Objects.Models;

public class ModelSlot : SceneObject, IManipulable {
	// Trees

	public override uint Color => 0xFFBAFFB2;

	// Model

	public readonly int SlotIndex;

	private new CharaModels? Parent => base.Parent as CharaModels;

	// Constructor

	internal ModelSlot(int index) {
		SlotIndex = index;
		SortPriority = index;
		Name = $"Slot {index}";
	}

	// Transform

	public unsafe Transform? GetTransform() {
		var mdl = GetModel();
		ModelObject* obj;
		if (mdl == null || (obj = mdl->Object) == null)
			return null;

		return new Transform(
			obj->Position,
			obj->Rotation,
			obj->Scale
		);
	}

	public unsafe void SetTransform(Transform trans) {
		var mdl = GetModel();
		ModelObject* obj;
		if (mdl == null || (obj = mdl->Object) == null)
			return;

		for (var i = 0; i < mdl->ObjectCount; i++) {
			obj[i].Position = trans.Position;
			obj[i].Rotation = trans.Rotation;
			obj[i].Scale = trans.Scale;
		}
	}

	// Unmanaged helpers

	private unsafe Model* GetModel()
		=> Parent != null ? Parent.GetModelIndex(SlotIndex) : null;
}
