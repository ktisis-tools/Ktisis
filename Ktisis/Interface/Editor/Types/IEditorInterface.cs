using System;
using System.Threading.Tasks;

using Ktisis.Data.Files;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Editor.Types;

public interface IEditorInterface {
	public void Prepare();
	
	public void OpenConfigWindow();
	public void ToggleWorkspaceWindow();

	public void OpenCameraWindow();
	public void OpenEnvironmentWindow();
	public void OpenObjectEditor();
	public void OpenPosingWindow();

	public void OpenSceneCreateMenu();
	public void OpenSceneEntityMenu(SceneEntity entity);

	public void OpenAssignCollection(ActorEntity entity);
	public void OpenAssignCProfile(ActorEntity entity);
	public void OpenOverworldActorList();
	
	public void RefreshGposeActors();

	public void OpenRenameEntity(SceneEntity entity);
	public void OpenSavePreset(ActorEntity actorEntity);
	
	
	public void OpenActorEditor(ActorEntity actor);
	public void OpenLightEditor(LightEntity light);
	
	public void OpenEditor<T, TA>(TA entity) where T : EntityEditWindow<TA> where TA : SceneEntity;
	
	public void OpenEditorFor(SceneEntity entity);

	public void OpenCharaImport(ActorEntity actor, bool toNpc = false);
	public Task OpenCharaExport(ActorEntity actor);
	public void OpenPoseImport(ActorEntity actor);
	public Task OpenPoseExport(EntityPose pose);

	public void OpenCharaFile(Action<string, CharaFile> handler);
	public void OpenPoseFile(Action<string, PoseFile> handler);
	public void OpenMcdfFile(Action<string> handler);

	public void OpenReferenceImages(Action<string> handler);
	
	public void ExportCharaFile(CharaFile file);
	public void ExportPoseFile(PoseFile file);
}
