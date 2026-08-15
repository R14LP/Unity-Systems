using UnityEngine;

public class HeadCameraFollow : MonoBehaviour
{
    public Animator animator;
    public Transform cameraHolder;
    public Transform orientation;
    public Vector3 offset = new Vector3(0f, 0.1f, 0.05f);

    private Transform headBone;

    private void Start()
    {
        headBone = animator.GetBoneTransform(HumanBodyBones.Head);
    }

    private void LateUpdate()
    {
        if (headBone == null) return;

        Vector3 worldOffset = orientation.forward * offset.z + Vector3.up * offset.y;
        cameraHolder.position = headBone.position + worldOffset;
    }
}
