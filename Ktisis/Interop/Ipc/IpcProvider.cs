using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Scene.Modules.Actors;
using Newtonsoft.Json;

namespace Ktisis.Interop.Ipc;

[Singleton]
public class IpcProvider(ContextManager ctxManager, IDalamudPluginInterface dpi)
{
    private ICallGateProvider<bool> IpcRefreshActions { get; } = dpi.GetIpcProvider<bool>("Ktisis.RefreshActors");
    private ICallGateProvider<bool> IpcIsPosing { get; } = dpi.GetIpcProvider<bool>("Ktisis.IsPosing");
    private ICallGateProvider<IGameObject, string, Task<bool>> IpcLoadPose { get; } = dpi.GetIpcProvider<IGameObject, string, Task<bool>>("Ktisis.LoadPose");
    private ICallGateProvider<IGameObject, Task<string?>> IpcSavePose { get; } = dpi.GetIpcProvider<IGameObject, Task<string?>>("Ktisis.SavePose");
   
    private bool RefreshActors()
    {
        ctxManager.Current?.Scene.GetModule<ActorModule>().RefreshGPoseActors();
        return true;
    }

    private bool IsActive()
    {
        return ctxManager.Current?.Posing.IsEnabled ?? false;
    }

    private async Task<bool> LoadPose(IGameObject gameObject, string json)
    {
        if (ctxManager.Current is null)
            return false;

        var file = JsonConvert.DeserializeObject<PoseFile>(json);
        var actor = ctxManager.Current.Scene.GetEntityForActor(gameObject);

        if (actor is null || file is null)
            return false;

        await ctxManager.Current.Posing.ApplyPoseFile(actor.Pose!, file);
        return true;
    }

    private async Task<string?> SavePose(IGameObject gameObject)
    {
        if (ctxManager.Current is null)
            return null;

        var actor = ctxManager.Current.Scene.GetEntityForActor(gameObject);
        if (actor?.Pose is null)
            return null;

        var file = await ctxManager.Current.Posing.SavePoseFile(actor.Pose);
        return JsonConvert.SerializeObject(file);
    }

    public void RegisterIpc()
    {
        IpcRefreshActions.RegisterFunc(RefreshActors);
        IpcIsPosing.RegisterFunc(IsActive);
        IpcLoadPose.RegisterFunc(LoadPose);
        IpcSavePose.RegisterFunc(SavePose);
    }
}
