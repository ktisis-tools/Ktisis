using System;
using System.Linq;
using System.Text;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok.Animation.Playback.Control.Default;
using FFXIVClientStructs.Havok.Animation.Rig;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Editor.Context.Types;
using Ktisis.Actions.Attributes;
using Ktisis.Core;
using Ktisis.Core.Types;
using Ktisis.Core.Attributes;
using Ktisis.Actions.Types;

namespace Ktisis.Common.Extensions;

public static class GameObjectEx {
	public unsafe static bool IsPcCharacter(this IGameObject gameObject) {
		var csPtr = (CSGameObject*)gameObject.Address;
		return csPtr != null && csPtr->GetObjectKind() == ObjectKind.Pc;
	}

	public unsafe static string GetNameOrFallback(this IGameObject gameObject, IEditorContext ctx, bool? forceIncognito = null) {
		// forceIncognito: if null, use Config.Editor.IncognitoPlayerNames
		// 	if true, return censored Actor #
		// 	if false, return realname or fallback

		bool incognito = forceIncognito ?? ctx.Config.Editor.IncognitoPlayerNames;
		bool isPc = IsPcCharacter(gameObject);

		// force the fallback text if we're incognito and looking at a PC
		if (incognito && isPc) {
			return $"Actor #{gameObject.ObjectIndex}";
		}

		var name = gameObject.Name.TextValue;
		return !name.IsNullOrEmpty() ? name : $"Actor #{gameObject.ObjectIndex}";
	}

	public unsafe static DrawObject* GetDrawObject(this IGameObject gameObject) {
		var csPtr = (CSGameObject*)gameObject.Address;
		return csPtr != null ? csPtr->DrawObject : null;
	}

	public unsafe static Skeleton* GetSkeleton(this IGameObject gameObject) {
		if (!gameObject.IsValid()) return null;

		var csPtr = (CSGameObject*)gameObject.Address;
		if (csPtr == null || csPtr->DrawObject == null)
			return null;

		var drawObject = csPtr->DrawObject;
		if (drawObject->Object.GetObjectType() != ObjectType.CharacterBase)
			return null;
		
		return ((CharacterBase*)drawObject)->Skeleton;
	}

	public unsafe static hkaDefaultAnimationControl* GetDefaultControlForIndex(this IGameObject gameObject, int animationIndex) {
		// hacky reimplement of GetAnimationControl's clientstructs hka traversal
		// todo: find a neater way to get the scrub & duration values
		if (!gameObject.IsValid()) return null;

		var skeleton = GetSkeleton(gameObject);
		if (skeleton == null) return null;

		// iterate all partials to try and find an animating skeleton that's valid for the provided index
		var partials = new Span<PartialSkeleton>(skeleton->PartialSkeletons, skeleton->PartialSkeletonCount);
		foreach (var partial in partials) {
			var hkaAnimated = partial.GetHavokAnimatedSkeleton(0);
			if (hkaAnimated == null) continue;
			if (hkaAnimated->AnimationControls.Length == 0) continue;
			if (animationIndex >= hkaAnimated->AnimationControls.Length) continue;
			if (hkaAnimated->AnimationControls[animationIndex].Value == null) continue;

			var defaultControl = hkaAnimated->AnimationControls[animationIndex].Value;
			if (defaultControl->hkaAnimationControl.Binding.ptr == null) continue;
			if (defaultControl->hkaAnimationControl.Binding.ptr->Animation.ptr == null) continue;
			return defaultControl;
		}

		return null;
	}

	public unsafe static bool IsDrawing(this IGameObject gameObject) {
		var csActor = (CSGameObject*)gameObject.Address;
		if (csActor == null) return false;
		// Ktisis.Log.Info($"RenderFlags: {csActor->RenderFlags:X}");
		return csActor->RenderFlags == 0x00;
	}

	public unsafe static bool IsEnabled(this IGameObject gameObject) {
		var csActor = (CSGameObject*)gameObject.Address;
		if (csActor == null) return false;
		return (csActor->RenderFlags & 2) == 0;
	}
	
	public unsafe static void SetWorld(this IGameObject gameObject, ushort world) {
		var charaPtr = (Character*)gameObject.Address;
		if (charaPtr == null || !charaPtr->GameObject.IsCharacter()) return;
		charaPtr->CurrentWorld = world;
		charaPtr->HomeWorld = world;
	}
	
	public unsafe static void SetName(this IGameObject gameObject, string name) {
		var gameObjectPtr = (CSGameObject*)gameObject.Address;
		if (gameObjectPtr == null) return;

		var bytes = Encoding.UTF8.GetBytes(name).Append((byte)0).ToArray();
		for (var i = 0; i < bytes.Length; i++)
			gameObjectPtr->Name[i] = bytes[i];
	}
	
	public unsafe static void SetTargetable(this IGameObject gameObject, bool targetable) {
		var charaPtr = (CSGameObject*)gameObject.Address;
		if (charaPtr == null) return;
		
		if (targetable)
			charaPtr->TargetableStatus |= ObjectTargetableFlags.IsTargetable;
		else
			charaPtr->TargetableStatus &= ~ObjectTargetableFlags.IsTargetable;
	}

	public unsafe static void SetGPoseTarget(this IGameObject gameObject) {
		if (!gameObject.IsValid()) return;

		var target = TargetSystem.Instance();
		if (target == null || target->GPoseTarget == null) return;

		target->GPoseTarget = (CSGameObject*)gameObject.Address;
	}

	public unsafe static void Redraw(this IGameObject gameObject) {
		var csPtr = (CSGameObject*)gameObject.Address;
		if (csPtr == null) return;
		csPtr->DisableDraw();
		csPtr->EnableDraw();
	}
}
