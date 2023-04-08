using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;

public class HexMapCamera : MonoBehaviour
{
    [SerializeField]
    private float StickMinZoom, StickMaxZoom;
    
    [SerializeField] 
    private float SwivelMinZoom, SwivelMaxZoom;
    
    [SerializeField]
    private float MoveSpeedMinZoom, MoveSpeedMaxZoom;

    [SerializeField]
    private float RotationSpeed;

    [SerializeField]
    private HexGrid Grid;

    public static bool Looked { set { instance.enabled = !value; } }

    private Transform mySwivel;
    private Transform myStick;

    private float myZoom = 1f;
    private float rotationAngle;

    static HexMapCamera instance;

    private void Awake()
    {
        mySwivel = transform.GetChild(0);
        myStick = mySwivel.GetChild(0);
    }

    private void OnEnable()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if(rotationDelta != 0f)
            AdjustRotation(rotationDelta);

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
            AdjustPosition(xDelta, zDelta);
    }

    private void AdjustZoom(float delta)
    {
        myZoom = Mathf.Clamp01(myZoom + delta);

        float distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, myZoom);
        myStick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, myZoom);
        mySwivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    private void AdjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance = Mathf.Lerp(MoveSpeedMinZoom, MoveSpeedMaxZoom, myZoom) * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    private void AdjustRotation(float delta)
    {
        rotationAngle += delta * RotationSpeed * Time.deltaTime;
        
        if (rotationAngle < 0f)
            rotationAngle += 360f;
        else if (rotationAngle > 360f)
            rotationAngle -= 360f;

        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (Grid.cellCountX * HexMetrics.ChunkSizeX - .5f) * (2f * HexMetrics.InnerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (Grid.cellCountZ * HexMetrics.ChunkSizeZ - 1f) * (2f * HexMetrics.OuterRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }
}
