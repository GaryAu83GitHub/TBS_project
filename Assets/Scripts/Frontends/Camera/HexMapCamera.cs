using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public float StickMinZoom, StickMaxZoom;
    public float SwivelMinZoom, SwivelMaxZoom;

    private Transform mySwivel;
    private Transform myStick;

    private float myZoom = 1f;

    private void Awake()
    {
        mySwivel = transform.GetChild(0);
        myStick = mySwivel.GetChild(0);
    }
    
    // Update is called once per frame
    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }
    }

    private void AdjustZoom(float delta)
    {
        myZoom = Mathf.Clamp01(myZoom + delta);

        float distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, myZoom);
        myStick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, myZoom);
        mySwivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
