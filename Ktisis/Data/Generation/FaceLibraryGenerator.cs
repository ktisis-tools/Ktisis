using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;

using JetBrains.Annotations;

using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Characters;

namespace Ktisis.Data.Generation;

[Singleton]
public class FaceLibraryGenerator {
	// Data

	private readonly static Expression[] Expressions = [
		new( "BrowUpL", "Brow Up (L)", 6261, [ "j_f_miken_l", "j_f_mayu_l", "j_f_mmayu_l", "j_f_miken_01_l", "j_f_miken_02_l" ] ),
		new( "BrowUpR", "Brow Up (R)", 6261, [ "j_f_miken_r", "j_f_mayu_r", "j_f_mmayu_r", "j_f_miken_01_r", "j_f_miken_02_r" ] ),
		new( "BrowFurrowL", "Brow Furrow (L)", 6228, [ "j_f_dmemoto_l", "j_f_dmiken_l", "j_f_miken_01_l", "j_f_miken_02_l", "j_f_miken_l" ] ),
		new( "BrowFurrowR", "Brow Furror (R)", 6228, [ "j_f_dmemoto_r", "j_f_dmiken_r", "j_f_miken_01_r", "j_f_miken_02_r", "j_f_miken_r" ] ),
		new( "BlinkL", "Blink (L)", 611, [ "j_f_mab_l", "j_f_mabdn_01_l", "j_f_mabdn_02out_l", "j_f_mabdn_03in_l", "j_f_mabup_01_l", "j_f_mabup_02out_l", "j_f_mabup_03in_l" ] ),
		new( "BlinkR", "Blink (R)", 611, [ "j_f_mab_r", "j_f_mabdn_01_r", "j_f_mabdn_02out_r", "j_f_mabdn_03in_r", "j_f_mabup_01_r", "j_f_mabup_02out_r", "j_f_mabup_03in_r" ] ),
		new( "EyeWideL", "Eye Wide (L)", 618, [ "j_f_mab_l", "j_f_mabdn_01_l", "j_f_mabdn_02out_l", "j_f_mabdn_03in_l", "j_f_mabup_01_l", "j_f_mabup_02out_l", "j_f_mabup_03in_l" ] ),
		new( "EyeWideR", "Eye Wide (R)", 618, [ "j_f_mab_r", "j_f_mabdn_01_r", "j_f_mabdn_02out_r", "j_f_mabdn_03in_r", "j_f_mabup_01_r", "j_f_mabup_02out_r", "j_f_mabup_03in_r" ] ),
		new( "CheekRaiseL", "Cheek Raise (L)", 616, [ "j_f_dhoho_l", "j_f_hoho_l", "j_f_shoho_l" ] ),
		new( "CheekRaiseR", "Cheek Raise (R)", 616, [ "j_f_dhoho_r", "j_f_hoho_r", "j_f_shoho_r" ] ),
		new( "SmileL", "Smile (L)", 606, [ "j_f_dlip_01_l", "j_f_dlip_02_l", "j_f_dmlip_01_l", "j_f_dmlip_02_l", "j_f_dslip_l", "j_f_ulip_01_l", "j_f_ulip_02_l", "j_f_umlip_01_l", "j_f_umlip_02_l", "j_f_uslip_l" ] ),
		new( "SmileR", "Smile (R)", 606, [ "j_f_dlip_01_r", "j_f_dlip_02_r", "j_f_dmlip_01_r", "j_f_dmlip_02_r", "j_f_dslip_r", "j_f_ulip_01_r", "j_f_ulip_02_r", "j_f_umlip_01_r", "j_f_umlip_02_r", "j_f_uslip_r" ] ),
		new( "GrinL", "Grin (L)", 8021, [ "j_f_dlip_01_l", "j_f_dlip_02_l", "j_f_dmlip_01_l", "j_f_dmlip_02_l", "j_f_dslip_l", "j_f_ulip_01_l", "j_f_ulip_02_l", "j_f_umlip_01_l", "j_f_umlip_02_l", "j_f_uslip_l" ] ),
		new( "GrinR", "Grin (R)", 8021, [ "j_f_dlip_01_r", "j_f_dlip_02_r", "j_f_dmlip_01_r", "j_f_dmlip_02_r", "j_f_dslip_r", "j_f_ulip_01_r", "j_f_ulip_02_r", "j_f_umlip_01_r", "j_f_umlip_02_r", "j_f_uslip_r" ] ),
		new( "FrownL", "Frown (L)", 625, [ "j_f_dlip_01_l", "j_f_dlip_02_l", "j_f_dmlip_01_l", "j_f_dmlip_02_l", "j_f_dslip_l", "j_f_ulip_01_l", "j_f_ulip_02_l", "j_f_umlip_01_l", "j_f_umlip_02_l", "j_f_uslip_l" ] ),
		new( "FrownR", "Frown (R)", 625, [ "j_f_dlip_01_r", "j_f_dlip_02_r", "j_f_dmlip_01_r", "j_f_dmlip_02_r", "j_f_dslip_r", "j_f_ulip_01_r", "j_f_ulip_02_r", "j_f_umlip_01_r", "j_f_umlip_02_r", "j_f_uslip_r" ] ),
		new( "JawOpen", "Jaw Open", 618, [ "j_f_ago", "j_f_dago", "j_f_hagukidn" ] ),
		new( "UpperLipOpen", "Upper Lip Open", 609, [ "j_f_ulip_a", "j_f_ulip_b", "j_f_ulip_01_l", "j_f_ulip_02_l", "j_f_ulip_01_r", "j_f_ulip_02_r", "j_f_umlip_01_l", "j_f_umlip_02_l", "j_f_umlip_01_r", "j_f_umlip_02_r" ] ),
		new( "LowerLipOpen", "Lower Lip Open", 609, [ "j_f_dlip_a", "j_f_dlip_b", "j_f_dlip_01_l", "j_f_dlip_02_l", "j_f_dlip_01_r", "j_f_dlip_02_r", "j_f_dmlip_01_l", "j_f_dmlip_02_l", "j_f_dmlip_01_r", "j_f_dmlip_02_r"] ),
		new( "LipPucker", "Lip Pucker", 623, [ "j_f_dlip_01_l", "j_f_dlip_01_r", "j_f_dlip_02_l", "j_f_dlip_02_r", "j_f_dlip_a", "j_f_dlip_b", "j_f_dmlip_01_l", "j_f_dmlip_01_r", "j_f_dmlip_02_l", "j_f_dmlip_02_r", "j_f_dslip_l", "j_f_dslip_r", "j_f_ulip_01_l",  "j_f_ulip_01_r", "j_f_ulip_02_l", "j_f_ulip_02_r", "j_f_ulip_a", "j_f_ulip_b", "j_f_umlip_01_l", "j_f_umlip_01_r", "j_f_umlip_02_l", "j_f_umlip_02_r", "j_f_uslip_l", "j_f_uslip_r" ] )
	];
	
	private record Expression(
		string Id,
		string Label,
		uint TimelineId,
		string[] Bones
	);
	
	// Constants
	
	private const int RaceCt = 8;
	private const int StepCt = RaceCt * 2 + 2;
	
	// Dependencies + ctor
	
	private readonly IFramework _framework;
	private readonly IDataManager _data;
	private readonly JsonFileSerializer _json;

	public IEditorContext Context { get; set; } = null!;
	
	public FaceLibraryGenerator(
		IFramework framework,
		IDataManager data,
		JsonFileSerializer json
	) {
		this._framework = framework;
		this._data = data;
		this._json = json;
	}
	
	// Public getters
	
	public bool InProgress => this._state != null;

	public (int Current, int Max) GetStep {
		get {
			lock (this._lock)
				return (Current: this._state?.Step ?? 0, Max: StepCt);
		}
	}
	
	// State
	
	private record TaskState {
		public required Task Task;
		public required CancellationTokenSource TokenSource;
		
		public int Step;

		public readonly Dictionary<string, SkeletonInfo> Data = [];
	}

	private record SkeletonInfo {
		public byte Race;
		public byte Sex;
		public byte Tribe;
		public ExpressionData[] Data = [];
	}

	private record ExpressionData {
		public required string Id;
		public required string Label;
		public required Dictionary<string, Transform> Transforms = [];
	}

	private record FileData(
		[UsedImplicitly] ExpressionData[] Data
	);

	private readonly Lock _lock = new();
	private TaskState? _state;

	private readonly Dictionary<uint, List<int>> _animToExpressions = [];
	
	// Create task

	public void StartCreateLibrary(ActorEntity actor) {
		this.PopulateAnimToExpressions();
		
		lock (this._lock) {
			if (this._state != null) {
				this._state.TokenSource.Cancel();
				this._state.Task.Wait();
			}
		}

		var cts = new CancellationTokenSource();
		cts.CancelAfter(60_000);
		var task = this.ProcGenerateLibrary(actor, cts.Token);
		lock (this._lock) {
			this._state = new TaskState {
				Task = task,
				TokenSource = cts
			};
		}
	}

	private void PopulateAnimToExpressions() {
		if (this._animToExpressions.Count > 0) return;
		
		for (var i = 0; i < Expressions.Length; i++) {
			var anim = Expressions[i].TimelineId;
			if (this._animToExpressions.TryGetValue(anim, out var list))
				list.Add(i);
			else
				this._animToExpressions[anim] = [i];
		}
	}
	
	// Handle generation

	private async Task ProcGenerateLibrary(ActorEntity actor, CancellationToken ct) {
		await this.ProcCaptureAll(actor, ct);
		
		var tempDir = Directory.CreateTempSubdirectory("Ktisis-");

		Dictionary<string, SkeletonInfo> data;
		lock (this._lock)
			data = this._state?.Data.ToDictionary() ?? [];
		
		foreach (var (key, info) in data) {
			var fileData = new FileData(info.Data);
			var text = this._json.Serialize(fileData);
			
			var raceName = info.Tribe != 0
				? ((Tribe)info.Tribe).ToString()
				: ((Race)info.Race).ToString();
			
			var filePath = Path.Join(tempDir.FullName, $"{key}_{raceName}_{(Gender)info.Sex}.json");
			await File.WriteAllTextAsync(filePath, text, ct);
		}
		
		Ktisis.Log.Info($"Opening {tempDir.FullName}");
		Process.Start(new ProcessStartInfo {
			FileName = tempDir.FullName,
			UseShellExecute = true,
			Verb = "open"
		});
	}
	
	private async Task ProcCaptureAll(ActorEntity actor, CancellationToken ct) {
		for (var i = 0; i < StepCt; i++) {
			ct.ThrowIfCancellationRequested();
			
			var race = (byte)(Math.Floor((decimal)Math.Max(i - 2, 0) / 2) + 1);
			var sex = (byte)(i % 2);
			var tribe = (race, i) switch {
				(1, < 2) => (byte)Tribe.Midlander,
				(1, _) => (byte)Tribe.Highlander,
				_ => (byte)0
			};
			await this.ProcCaptureData(actor, race, sex, tribe, ct);
		}

		// Revert changes to race + sex
		await this._framework.Run(() => {
			actor.Appearance.Customize.Unset(CustomizeIndex.Race);
			actor.Appearance.Customize.Unset(CustomizeIndex.Gender);
			actor.Appearance.Customize.Unset(CustomizeIndex.Tribe);
			actor.Redraw();
		}, ct);
	}
	
	private async Task ProcCaptureData(ActorEntity actor, byte race, byte sex, byte tribe, CancellationToken ct) {
		// Set actor race and sex
		await this._framework.Run(() => {
			actor.Appearance.Customize[CustomizeIndex.Race] = race;
			actor.Appearance.Customize[CustomizeIndex.Gender] = sex;
			if (tribe != 0)
				actor.Appearance.Customize[CustomizeIndex.Tribe] = tribe;
			else
				actor.Appearance.Customize.Unset(CustomizeIndex.Tribe);
			actor.Redraw();
		}, ct);

		await this.WaitForRedraw(actor, ct);

		var data = new List<ExpressionData>();
		await foreach (var test in this.CaptureData(actor, ct))
			data.Add(test);

		lock (this._lock) {
			var raceSexId = actor.GetRaceSexId() ?? string.Empty;
			this._state!.Data[raceSexId] = new SkeletonInfo {
				Race = race,
				Sex = sex,
				Tribe = tribe,
				Data = data.ToArray()
			};
			this._state.Step++;
		}
	}
	
	private async Task WaitForRedraw(ActorEntity actor, CancellationToken ct) {
		var isDrawing = false;
		while (!isDrawing && !ct.IsCancellationRequested) {
			isDrawing = await this._framework.RunOnTick(() => IsRendering(actor), cancellationToken: ct);
		}
	}
	
	private unsafe static bool IsRendering(ActorEntity actor) {
		var chara = actor.Character;
		var drawObj = chara != null ? chara->DrawObject : null;
		return drawObj != null && drawObj->IsVisible;
	}
	
	// Play animation and capture pose data
	
	private async IAsyncEnumerable<ExpressionData> CaptureData(ActorEntity actor, [EnumeratorCancellation] CancellationToken ct) {
		foreach (var (animId, expKeys) in this._animToExpressions) {
			ct.ThrowIfCancellationRequested();
			
			Ktisis.Log.Info($"Animation: {animId}");
			
			// Set emote
			await this.PlayTimeline(actor, animId, ct);
			//await this._framework.DelayTicks(30, ct);
			var data = await this._framework.RunOnTick(() => CapturePose(actor), cancellationToken: ct);
			
			// Capture expressions
			foreach (var index in expKeys) {
				var expression = Expressions[index];
				
				var expressionData = new ExpressionData {
					Id = expression.Id,
					Label = expression.Label,
					Transforms = []
				};

				foreach (var bone in expression.Bones) {
					if (data.TryGetValue(bone, out var transform))
						expressionData.Transforms[bone] = transform;
				}

				yield return expressionData;
			}
		}
	}
	
	private async Task PlayTimeline(ActorEntity actor, uint animId, CancellationToken ct) {
		await this._framework.Run(() => this.Context.Animation.PlayTimeline(actor, animId), ct);
		
		var waiting = true;
		while (waiting && !ct.IsCancellationRequested) {
			waiting = await this._framework.RunOnTick(() => !IsAnimPlaying(actor), cancellationToken: ct);
		}
	}

	private unsafe static bool IsAnimPlaying(ActorEntity actor) {
		var human = actor.GetHuman();
		var skele = human != null ? actor.GetHuman()->Skeleton : null;
		if (skele == null) return false;
		
		var partial = skele->PartialSkeletons[1];
		
		var hkAnimSkele = partial.GetHavokAnimatedSkeleton(0);
		if (hkAnimSkele == null || hkAnimSkele->AnimationControls.Length == 0)
			hkAnimSkele = partial.GetHavokAnimatedSkeleton(1);
		return hkAnimSkele != null && hkAnimSkele->AnimationControls.Length <= 1;
	}

	private unsafe static Dictionary<string, Transform> CapturePose(ActorEntity actor) {
		var result = new Dictionary<string, Transform>();
		
		var skele = actor.GetHuman()->Skeleton;
		var partial = skele->PartialSkeletons[1];
		
		var hkAnimSkele = partial.GetHavokAnimatedSkeleton(0);
		if (hkAnimSkele->AnimationControls.Length == 0)
			hkAnimSkele = partial.GetHavokAnimatedSkeleton(1);
		if (hkAnimSkele->AnimationControls.Length == 0) {
			Ktisis.Log.Warning("Failed to find animation control!");
			return result;
		}
		var ctrlIndex = hkAnimSkele->AnimationControls.Length - 1;
		var ctrl = hkAnimSkele->AnimationControls[ctrlIndex].Value;
		var duration = ctrl->Binding.ptr->Animation.ptr->Duration;
		ctrl->hkaAnimationControl.Weight = 1.0f;
		ctrl->hkaAnimationControl.LocalTime = duration;
		for (var i = 0; i < ctrlIndex; i++) {
			var prev = hkAnimSkele->AnimationControls[i].Value;
			prev->hkaAnimationControl.Weight = 0.0f;
		}
		
		var pose = partial.GetHavokPose(0);
		hkAnimSkele->sampleAndCombineAnimations(pose->LocalPose.Data, pose->FloatSlotValues.Data);
		
		for (var i = 0; i < pose->Skeleton->Bones.Length; i++) {
			var bone = pose->Skeleton->Bones[i];

			var tRef = new Transform(pose->Skeleton->ReferencePose[i]);
			var tLocal = new Transform(pose->LocalPose[i]);
			var tDelta = new Transform {
				Position = tLocal.Position - tRef.Position,
				Rotation = tLocal.Rotation / tRef.Rotation,
				Scale = tLocal.Scale / tRef.Scale
			};
			result[bone.Name.String!] = tDelta;
		}

		return result;
	}
}
