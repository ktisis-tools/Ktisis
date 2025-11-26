using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Transforms;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Modules.Actors;
using Newtonsoft.Json;

namespace Ktisis.Interop.Ipc;

[Singleton]
public class IpcProvider(ContextManager ctxManager, IDalamudPluginInterface dpi)
{
    private ICallGateProvider<(int, int)> IpcVersion { get; } = dpi.GetIpcProvider<(int, int)>("Ktisis.ApiVersion");
    private ICallGateProvider<bool> IpcRefreshActions { get; } = dpi.GetIpcProvider<bool>("Ktisis.RefreshActors");
    private ICallGateProvider<bool> IpcIsPosing { get; } = dpi.GetIpcProvider<bool>("Ktisis.IsPosing");
    private ICallGateProvider<uint, string, Task<bool>> IpcLoadPose { get; } = dpi.GetIpcProvider<uint, string, Task<bool>>("Ktisis.LoadPose");
    private ICallGateProvider<uint, Task<string?>> IpcSavePose { get; } = dpi.GetIpcProvider<uint, Task<string?>>("Ktisis.SavePose");
    private ICallGateProvider<uint, string, Matrix4x4, Task<bool>> IpcSetMatrix { get; } = dpi.GetIpcProvider<uint, string, Matrix4x4, Task<bool>>("Ktisis.SetMatrix");
    private ICallGateProvider<uint, string, Task<Matrix4x4?>> IpcGetMatrix { get; } = dpi.GetIpcProvider<uint, string, Task<Matrix4x4?>>("Ktisis.GetMatrix");
    private ICallGateProvider<Task<Dictionary<int, HashSet<string>>>> IpcSelectedBones { get; } = dpi.GetIpcProvider<Task<Dictionary<int, HashSet<string>>>>("Ktisis.SelectedBones");

    private (int, int) GetVersion() => (1, 0);

    private bool RefreshActors()
    {
        ctxManager.Current?.Scene.GetModule<ActorModule>().RefreshGPoseActors();
        return true;
    }

    private bool IsActive() => ctxManager.Current?.Posing.IsEnabled ?? false;

    private async Task<bool> LoadPose(uint index, string json)
    {
        if (ctxManager.Current is null)
            return false;

        var file = JsonConvert.DeserializeObject<PoseFile>(json);
        var actor = ctxManager.Current.Scene.GetEntityForIndex(index);

        if (actor is null || file is null)
            return false;

        await ctxManager.Current.Posing.ApplyPoseFile(
            actor.Pose!,
            file,
            transforms: PoseTransforms.Position | PoseTransforms.Rotation | PoseTransforms.Scale
        );
        
        return true;
    }

    private async Task<string?> SavePose(uint index)
    {
        if (ctxManager.Current is null)
            return null;

        var actor = ctxManager.Current.Scene.GetEntityForIndex(index);
        if (actor?.Pose is null)
            return null;

        var file = await ctxManager.Current.Posing.SavePoseFile(actor.Pose);
        return JsonConvert.SerializeObject(file);
    }

    private async Task<Dictionary<int, HashSet<string>>> SelectedBones()
    {
        var sceneChildren = ctxManager.Current?.Scene?.Children
            .OfType<ActorEntity>()
            .ToList();
        
        if (sceneChildren is null || sceneChildren.Count == 0)
            return new ();

        var ret = new Dictionary<int, HashSet<string>>();
        
        foreach (var actor in sceneChildren)
        {
            if (!actor.IsValid || actor.Pose is null)
                continue;

            ret[actor.Actor.ObjectIndex] = 
                actor.Children.OfType<EntityPose>()
                    .SelectMany(x => x.Recurse())
                    .Where(s => s.IsSelected)
                    .OfType<BoneNode>()
                    .Select(s => s.Info.Name)
                    .ToHashSet();
        }

        return ret;
    }
    #if DEBUG
    private async Task<Matrix4x4?> GetMatrix(uint index, string boneName)
    {
        var actor = ctxManager.Current?.Scene?.GetEntityForIndex(index);

        var bone = actor?.Pose?.Recurse().OfType<BoneNode>().FirstOrDefault(b => b.Info.Name == boneName);
        return bone?.GetMatrix();
    }
    
    private async Task<bool> SetMatrix(uint index, string boneName, Matrix4x4 matrix)
    {
        var ctx = ctxManager.Current;
        if (ctx is null)
            return false;
        
        var actor = ctx.Scene?.GetEntityForIndex(index);

        var bone = actor?.Pose?.Recurse().OfType<BoneNode>().FirstOrDefault(b => b.Info.Name == boneName);
        if (bone is null)
            return false;

        var target = new TransformTarget(actor, [bone]);
        var transform = ctx.Transform.Begin(target, setup =>
        {
            setup.MirrorRotation = MirrorMode.Inverse;
            setup.ParentBones = true;
            setup.RelativeBones = true;
        });
        
        Ktisis.Log.Debug(target.GetTransform().ToString());
        
        transform.SetMatrix(matrix);
        transform.Dispatch();
        
        
        //
        // unsafe
        // {
        //     var hkaPose = bone.GetPose();
        //     if (hkaPose is null) return false;
        //     
        //     var initial = HavokPosing.GetModelTransform(hkaPose, bone.Info.BoneIndex);
        //     
        //     var transform = ctx.Transform.Begin(transformTarget);
        //     
        //     transform.SetTransform(new Transform(matrix));
        //     transform.Dispatch();
        //
        //     if (!propogate)
        //         return true;
        //    
        //     var target = HavokPosing.GetModelTransform(hkaPose, bone.Info.BoneIndex);
        //     if (target is null) return false;
        //         
        //     HavokPosing.Propagate(bone.GetSkeleton(), bone.Info.PartialIndex, bone.Info.BoneIndex, target, initial);
        // }


        // bone.SetMatrix(matrix);
        return true;
    }
    #else
    private async Task<Matrix4x4?> GetMatrix(uint index, string boneName) => null;
    private async Task<bool> SetMatrix(uint index, string boneName, Matrix4x4 matrix) => false;
    #endif
    
    public void RegisterIpc()
    {
        IpcVersion.RegisterFunc(GetVersion);
        IpcRefreshActions.RegisterFunc(RefreshActors);
        IpcIsPosing.RegisterFunc(IsActive);
        IpcLoadPose.RegisterFunc(LoadPose);
        IpcSavePose.RegisterFunc(SavePose);
        IpcGetMatrix.RegisterFunc(GetMatrix);
        IpcSetMatrix.RegisterFunc(SetMatrix);
        IpcSelectedBones.RegisterFunc(SelectedBones);
    }
}
