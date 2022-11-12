namespace Ktisis.Overlay
{
	/* Guizmo State machine:
								^
								|
								|event: OnGuizmoChange(GuizmoState)
								|
								|
								|
					IsUsing()	|
			 +----------------+---------+
			 |                          |
			 |                          v
			Idle                    Editing
			 ^                          |
			 |                          |
			 +------+-------------------+
					|      !IsUsing()
					|
					|event: OnGuizmoChange(GuizmoState)
					|
					v
 */
	public enum GizmoState
	{
		IDLE,
		EDITING,
	}
}
