using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GestureInteractionController : MonoBehaviour
{
    public GameObject InfoCanvas;
    public GameObject TargetNameLabel;
    private TextMeshProUGUI _targetNameLabelText;
    public GameObject TargetSpeedLabel;
    private TextMeshProUGUI _targetSpeedLabelText;
    public GameObject TargetDistanceLabel;
    private TextMeshProUGUI _targetDistanceLabelText;
    //private GameObject _targetDescriptionLabel;

    private bool HasHitData = false;
    private GameObject HitGameObject;
    private Rigidbody HitGameObjectRigidBody;

    [Tooltip("Whether the center of the screen or the touch position should be used as a root for the ray")]
    public bool UseScreenCenterSelection = false;

    private void Awake()
    {
        _targetNameLabelText = TargetNameLabel.GetComponent<TextMeshProUGUI>();
        _targetSpeedLabelText = TargetSpeedLabel.GetComponent<TextMeshProUGUI>();
        _targetDistanceLabelText = TargetDistanceLabel.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        InfoCanvas.SetActive(false);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 viewportPosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
            ShootRay(viewportPosition);
        }
#endif
        if (HasHitData)
        {
            _targetNameLabelText.text = $"{HitGameObject.name}";
            _targetSpeedLabelText.text = $"{(HitGameObjectRigidBody.velocity.magnitude).ToString("0.00")} km/s";
            _targetDistanceLabelText.text = $"{Vector3.Distance(Camera.main.transform.position, HitGameObject.transform.position).ToString("0.00")} km";
        }
    }

    void ShootRay(Vector3? screenPos = null)
    {
        if (screenPos == null || UseScreenCenterSelection)
        {
            // use center pos
            screenPos = new Vector3(0.5f, 0.5f, 0);
        }
        Ray ray = Camera.main.ViewportPointToRay(screenPos.Value);
        RaycastHit hitData;
        Debug.DrawRay(ray.origin, ray.direction * 9999);

        bool isRayHit = Physics.Raycast(ray, out hitData);

        if (isRayHit && hitData.collider.CompareTag("CelestialBody"))
        {
            InfoCanvas.SetActive(true);
            HasHitData = true;
            HitGameObject = hitData.transform.gameObject;
            HitGameObjectRigidBody = hitData.transform.gameObject.GetComponent<Rigidbody>();
        }
        else
        {
            InfoCanvas.SetActive(false);
            HasHitData = false;
        }
    }

#if !UNITY_EDITOR
    private void OnEnable()
    {
        UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += FingerDown;
    }

    private void OnDisable()
    {
        UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Disable();
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= FingerDown;
    }

    private void FingerDown(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
    {
        if (finger.index != 0) return;

        Vector3 viewportPosition = Camera.main.ScreenToViewportPoint(finger.screenPosition);
        ShootRay(viewportPosition);
    }
#endif

    private void DrawClosestObjectText()
    {
        //var screenCenter = new Vector3(0.5f, 0.5f, 0);
        //Ray ray = Camera.main.ViewportPointToRay(screenCenter);
        //RaycastHit hitData;
        //Debug.DrawRay(ray.origin, ray.direction * 9999);
        //if (Physics.Raycast(ray, out hitData) && hitData.collider.CompareTag("CelestialBody"))
        //{
        //    planetText.SetActive(true);

        //    //this is the ui element
        //    RectTransform UI_Element = planetText.GetComponent<RectTransform>();
        //    Rigidbody rb = hitData.transform.gameObject.GetComponent<Rigidbody>();
        //    planetText.GetComponent<TextMeshProUGUI>().text = $"{hitData.transform.gameObject.name} {Mathf.RoundToInt(rb.velocity.magnitude)} km/s";

        //    //first you need the RectTransform component of your canvas
        //    RectTransform CanvasRect = canvas.GetComponent<RectTransform>();

        //    //then you calculate the position of the UI element
        //    //0,0 for the canvas is at the center of the screen, whereas WorldToViewPortPoint treats the lower left corner as 0,0. Because of this, you need to subtract the height / width of the canvas * 0.5 to get the correct position.
        //    Vector3 ViewportPosition = camera.WorldToViewportPoint(hitData.transform.position);
        //    Vector3 WorldObject_ScreenPosition = new Vector3(
        //    ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
        //    ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)),
        //    (0));

        //    //now you can set the position of the ui element
        //    UI_Element.anchoredPosition3D = WorldObject_ScreenPosition;
        //}
        //else
        //{
        //    planetText.SetActive(false);
        //}

        //var closestObj = universe.celestialBodies.OrderBy(
        //    obj =>
        //    {
        //        var targetDir = obj.transform.position - camera.transform.position;
        //        var angleBetween = Vector3.Angle(camera.transform.forward, targetDir);
        //        if (angleBetween < 90)
        //        {
        //            // Object is in front of the camera
        //            return (camera.WorldToViewportPoint(obj.transform.position) - screenCenter).sqrMagnitude;
        //        } else
        //        {
        //            return float.MaxValue;
        //        }
        //    }
        //    ).First();
        //Debug.Log(closestObj);

        //if (closestObj != null)
        //{
        //    planetText.transform.position = closestObj.transform.position;
        //    planetText.SetActive(true);
        //} else
        //{
        //    planetText.SetActive(false);
        //}
    }
}
