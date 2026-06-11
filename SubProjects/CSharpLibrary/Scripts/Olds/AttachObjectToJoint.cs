public class AttachObjectToJoint : MonoScript {

	public string jointName = "JointName"; // Attach to this joint
	public Entity attachedEntity; // Entity to attach

	public override void Initialize() {
		
	}

	public override void Update() {
		if(attachedEntity == null) {
			return;
		}

		SkinMeshRenderer smr = entity.GetComponent<SkinMeshRenderer>();
		if(smr == null) {
			return;
		}

		TransformData jointTransform = smr.GetJointTransform(jointName);
		if (jointTransform == null) {
			return;
		}

		Transform t = attachedEntity.transform;
		// t.position = jointTransform.position;
		// t.rotate = jointTransform.rotate;
		//t.scale = jointTransform.scale;

	}


}

