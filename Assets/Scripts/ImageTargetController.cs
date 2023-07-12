using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTargetController : MonoBehaviour
{
    private ARTrackedImageManager _arTrackedImageManager;
    public Vector3? TrackedImagePosition = null;
    public GameObject Placeholder;
    public Vector3 PlaceholderSpawnOffset = new Vector3(0, 0.05f, 0);

    private void Awake()
    {
        _arTrackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        _arTrackedImageManager.trackedImagesChanged += OnImageChanged;
    }

    private void OnDisable()
    {
        _arTrackedImageManager.trackedImagesChanged -= OnImageChanged;
    }

    private void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach(var trackedImage in args.added)
        {
            Debug.Log($"Added tracked image: {trackedImage.name}");
        }

        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                TrackedImagePosition = trackedImage.gameObject.transform.position;
                Placeholder.transform.position = TrackedImagePosition.Value + PlaceholderSpawnOffset;
                Placeholder.SetActive(true);
            } else
            {
                TrackedImagePosition = null;
                Placeholder.SetActive(false);
            }
        }

        foreach (var trackedImage in args.removed)
        {
            Debug.Log($"Removed tracked image: {trackedImage.name}");
        }
    }

    public void Enable()
    {
        _arTrackedImageManager.enabled = true;
    }

    public void Disable()
    {
        _arTrackedImageManager.enabled = false;
        Placeholder.SetActive(false);
    }
}
