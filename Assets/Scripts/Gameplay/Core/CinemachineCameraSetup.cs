using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CinemachineCameraSetup : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Collider2D boundingShape;

    [Header("Follow")]
    [SerializeField, Min(0f)] private float damping = 0.25f;

    [Header("Boundary")]
    [SerializeField, Min(0f)] private float boundaryDamping;
    [SerializeField, Min(0f)] private float slowingDistance;

    private CinemachineCamera cinemachineCamera;

    private void Awake()
    {
        if (followTarget == null || boundingShape == null)
        {
            Debug.LogError(
                "CinemachineCameraSetup requires a Follow Target and Bounding Shape.",
                this
            );
            enabled = false;
            return;
        }

        Camera outputCamera = GetComponent<Camera>();

        if (!TryGetComponent(out CinemachineBrain _))
            gameObject.AddComponent<CinemachineBrain>();

        GameObject cameraObject = new GameObject("Gameplay Cinemachine Camera");
        cameraObject.transform.SetPositionAndRotation(
            transform.position,
            transform.rotation
        );

        cinemachineCamera = cameraObject.AddComponent<CinemachineCamera>();
        cinemachineCamera.Follow = followTarget;

        LensSettings lens = cinemachineCamera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        lens.OrthographicSize = outputCamera.orthographicSize;
        lens.NearClipPlane = outputCamera.nearClipPlane;
        lens.FarClipPlane = outputCamera.farClipPlane;
        cinemachineCamera.Lens = lens;

        CinemachinePositionComposer composer =
            cameraObject.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance =
            Mathf.Abs(transform.position.z - followTarget.position.z);
        composer.Damping = new Vector3(damping, damping, 0f);

        CinemachineConfiner2D confiner =
            cameraObject.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = boundingShape;
        confiner.Damping = boundaryDamping;
        confiner.SlowingDistance = slowingDistance;
        confiner.InvalidateBoundingShapeCache();
    }
}
