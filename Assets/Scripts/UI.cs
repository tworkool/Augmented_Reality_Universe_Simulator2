using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class UI : MonoBehaviour
{
    private bool isSimulationStropped = false;
    private float lastTimeScale;

    [SerializeField]
    private float timeStep = 0.1f;
    [SerializeField]
    private float MIN_TIMESCALE = 0.1f;
    [SerializeField]
    private float MAX_TIMESCALE = 1.5f;
    [SerializeField]
    [Range(3, 30)]
    private int lineSegments = 26;
    [SerializeField]
    private bool approximateBodySpawnPath = true;
    [SerializeField]
    public GameObject hitIndicator;
    public float trackedImageTargetVeloctiyMultiplier = 5;


    private Label timeLabel;
    private Button timeActionButton;
    private Button timeActionSlowerButton;
    private Button timeActionFasterButton;
    private Button objectActionAddButton;
    private Button spawnMenuModalCloseButton;
    private VisualElement spawnMenuModal;
    private ScrollView spawnMenuScrollView;
    private VisualElement spawnMenu;
    private VisualElement defaultMenu;
    private Button spawnMenuCloseButton;
    private Button spawnMenuSpawnButton;
    private Slider spawnMenuVelocitySlider;
    private VisualElement spawnMenuImageTrackerPositionContainer;
    private Toggle spawnMenuImageTrackerToggle;
    private Button spawnMenuPos1Button;
    private Button spawnMenuPos2Button;

    private LineRenderer trajectoryLineRenderer;
    private GameObject tempSpawnObjectBuffer;

    private Universe universe;
    private Camera camera;
    private ImageTargetController imageTargetController;

    public Vector3 spawnOffset = new Vector3(0, -0.5f, 0);

    private Vector3? trackedImageTargetStartPos = null;
    private Vector3? trackedImageTargetEndPos = null;
    private Vector3? trackedImageTargetVeloctiy = null;

    private void Awake()
    {
        lastTimeScale = Time.timeScale;

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        imageTargetController = GameObject.Find("AR Session Origin").GetComponent<ImageTargetController>();

        timeLabel = root.Q<Label>("TimeLabel");
        timeActionButton = root.Q<Button>("TimeActionButton");
        timeActionSlowerButton = root.Q<Button>("TimeActionSlowerButton");
        timeActionFasterButton = root.Q<Button>("TimeActionFasterButton");
        objectActionAddButton = root.Q<Button>("ObjectActionAddButton");
        spawnMenuModalCloseButton = root.Q<Button>("SpawnMenuModalCloseButton");
        spawnMenuModal = root.Q<VisualElement>("SpawnMenuModal");
        spawnMenuScrollView = root.Q<ScrollView>("SpawnMenuScrollView");
        spawnMenu = root.Q<VisualElement>("SpawnMenu");
        defaultMenu = root.Q<VisualElement>("DefaultMenu");
        spawnMenuCloseButton = root.Q<Button>("SpawnMenuCloseButton");
        spawnMenuSpawnButton = root.Q<Button>("SpawnMenuSpawnButton");
        spawnMenuVelocitySlider = root.Q<Slider>("SpawnMenuVelocitySlider");
        spawnMenuImageTrackerPositionContainer = root.Q<VisualElement>("SpawnMenuPosContainer");
        spawnMenuImageTrackerToggle = root.Q<Toggle>("ImageTrackingToggle");
        spawnMenuPos1Button = root.Q<Button>("SpawnMenuPos1Button");
        spawnMenuPos2Button = root.Q<Button>("SpawnMenuPos2Button");

        timeActionButton.clicked += OnTimeActionButtonClicked;
        timeActionSlowerButton.clicked += OnTimeActionSlowerButtonClicked;
        timeActionFasterButton.clicked += OnTimeActionFasterButtonClicked;
        objectActionAddButton.clicked += OnObjectActionAddButtonClicked;
        spawnMenuModalCloseButton.clicked += OnSpawnMenuModalCloseButtonClicked;
        spawnMenuCloseButton.clicked += OnSpawnMenuCloseButtonClicked;
        spawnMenuSpawnButton.clicked += OnSpawnMenuSpawnButtonClicked;
        spawnMenuImageTrackerToggle.RegisterValueChangedCallback(HandleImageTrackerToggleCallback);
        spawnMenuPos1Button.clicked += OnSpawnMenuPos1ButtonClicked;
        spawnMenuPos2Button.clicked += OnSpawnMenuPos2ButtonClicked;

        trajectoryLineRenderer = GetComponent<LineRenderer>();
        universe = GameObject.FindGameObjectWithTag("CelestialBodyContainer").GetComponent<Universe>();
        camera = Camera.main;

        Init();
    }

    private void Update()
    {
        bool isImageTrackerSpawningSystemEnabled = spawnMenuImageTrackerToggle.value == true;
        if (isImageTrackerSpawningSystemEnabled)
        {
            // spawn from image tracker target
            if (approximateBodySpawnPath && tempSpawnObjectBuffer != null)
            {
                if (trackedImageTargetStartPos != null && trackedImageTargetVeloctiy != null)
                {
                    DrawSpawnTrajectory(trackedImageTargetStartPos, trackedImageTargetVeloctiy * trackedImageTargetVeloctiyMultiplier);
                }
            }
        }
        else
        {
            // spawn from camera
            if (approximateBodySpawnPath && tempSpawnObjectBuffer != null)
            {
                DrawSpawnTrajectory();
            }
        }
    }

    #region Helpers
    private string GetFormattedTimeLabelText(float timeScale)
    {
        return $"Speed: {Mathf.RoundToInt(lastTimeScale * 100)}%";
    }

    private void StopTime()
    {
        spawnMenuModal.visible = true;
        lastTimeScale = Time.timeScale;
        Time.timeScale = 0;
    }
    private void StartTime()
    {
        spawnMenuModal.visible = false;
        Time.timeScale = lastTimeScale;
    }

    #endregion

    #region Events

    private void OnSpawnMenuPos1ButtonClicked()
    {
        trackedImageTargetStartPos = imageTargetController.TrackedImagePosition ?? null;
        Debug.Log($"Start Pos: {trackedImageTargetStartPos}");
        if (trackedImageTargetEndPos != null && trackedImageTargetStartPos != null)
            trackedImageTargetVeloctiy = trackedImageTargetEndPos.Value - trackedImageTargetStartPos.Value;
    }

    private void OnSpawnMenuPos2ButtonClicked()
    {
        trackedImageTargetEndPos = imageTargetController.TrackedImagePosition ?? null;
        Debug.Log($"End Pos: {trackedImageTargetEndPos}");
        if (trackedImageTargetEndPos != null && trackedImageTargetStartPos != null)
            trackedImageTargetVeloctiy = trackedImageTargetEndPos.Value - trackedImageTargetStartPos.Value;
    }

    private void HandleImageTrackerToggleCallback(ChangeEvent<bool> e)
    {
        bool isImageTrackerSpawningSystemEnabled = spawnMenuImageTrackerToggle.value == true;
        if (isImageTrackerSpawningSystemEnabled)
        {
            spawnMenuImageTrackerPositionContainer.style.display = DisplayStyle.Flex;
            spawnMenuVelocitySlider.style.display = DisplayStyle.None;
            trajectoryLineRenderer.positionCount = 0;
            imageTargetController.Enable();
        }
        else
        {
            spawnMenuImageTrackerPositionContainer.style.display = DisplayStyle.None;
            spawnMenuVelocitySlider.style.display = DisplayStyle.Flex;
            imageTargetController.Disable();
        }
    }

    private void OnSpawnMenuSpawnButtonClicked()
    {
        bool isImageTrackerSpawningSystemEnabled = spawnMenuImageTrackerToggle.value == true;
        if (isImageTrackerSpawningSystemEnabled && (trackedImageTargetStartPos == null || trackedImageTargetEndPos == null || trackedImageTargetVeloctiy == null)) return;

        // instantiate as child of universe container
        GameObject child = Instantiate(tempSpawnObjectBuffer, universe.gameObject.transform);
        if (universe.disableTrails)
        {
            var lineRenderer = child.GetComponent<TrailRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
        Rigidbody childRigidBody = child.GetComponent<Rigidbody>();

        if (isImageTrackerSpawningSystemEnabled)
        {
            child.transform.position = trackedImageTargetStartPos.Value;
            childRigidBody.velocity += trackedImageTargetVeloctiy.Value * trackedImageTargetVeloctiyMultiplier;
        }
        else
        {
            Vector3 cameraNormal = camera.transform.forward.normalized;
            cameraNormal *= spawnMenuVelocitySlider.value;
            childRigidBody.velocity += cameraNormal;
            //childRigidBody.mass = 99999999;
            child.transform.position = camera.transform.position + spawnOffset;
        }
    }

    void OnAsyncOperationHandleCompleted(AsyncOperationHandle<GameObject> asyncOperationHandle)
    {
        if (asyncOperationHandle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject result = asyncOperationHandle.Result;
            if (result == null)
            {
                Debug.LogError("Could not load addressable object, reason: GameObject not OK");
                return;
            }
            tempSpawnObjectBuffer = result;
            Debug.Log("Loaded temp spawn object into buffer");
        }
        else
        {
            Debug.LogError("Could not load addressable object");
        }
    }

    private void OnSpawnMenuCloseButtonClicked()
    {
        spawnMenu.visible = false;
        defaultMenu.visible = true;
        tempSpawnObjectBuffer = null;
        trajectoryLineRenderer.positionCount = 0;
        imageTargetController.Disable();
        trackedImageTargetStartPos = null;
        trackedImageTargetEndPos = null;
        trackedImageTargetVeloctiy = null;
    }

    private void OnSpawnMenuModalCloseButtonClicked()
    {
        StartTime();
    }

    private void OnObjectActionAddButtonClicked()
    {
        StopTime();
    }

    private void OnTimeActionSlowerButtonClicked()
    {
        float newTimeScale = lastTimeScale - timeStep;
        if (newTimeScale < MIN_TIMESCALE)
            return;
        lastTimeScale = newTimeScale;
        Time.timeScale = newTimeScale;

        timeLabel.text = GetFormattedTimeLabelText(newTimeScale);
    }

    private void OnTimeActionFasterButtonClicked()
    {
        float newTimeScale = lastTimeScale + timeStep;
        if (newTimeScale > MAX_TIMESCALE)
            return;
        lastTimeScale = newTimeScale;
        Time.timeScale = newTimeScale;

        timeLabel.text = GetFormattedTimeLabelText(newTimeScale);
    }

    private void OnTimeActionButtonClicked()
    {
        if (isSimulationStropped)
        {
            isSimulationStropped = false;
            Time.timeScale = lastTimeScale;
            timeActionButton.text = "Stop";
            timeActionSlowerButton.SetEnabled(true);
            timeActionFasterButton.SetEnabled(true);
        }
        else
        {
            isSimulationStropped = true;
            Time.timeScale = 0f;
            timeActionButton.text = "Start";
            timeActionSlowerButton.SetEnabled(false);
            timeActionFasterButton.SetEnabled(false);
        }
    }

    private void OnSpawnMenuModalFoldoutElementClick(SpawnMenuFoldoutContentElement spawnMenuFoldoutContentElement)
    {
        defaultMenu.visible = false;
        spawnMenuModal.visible = false;
        spawnMenu.visible = true;
        //spawnMenu.userData = spawnMenuFoldoutContentElement;

        StartTime();

        spawnMenu.Q<Label>("SpawnMenuObjectName").text = spawnMenuFoldoutContentElement.Name;

        var asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>(spawnMenuFoldoutContentElement.ResourcePath);
        asyncOperationHandle.Completed += OnAsyncOperationHandleCompleted;
    }

    #endregion

    #region Initializers

    private class SpawnMenuFoldoutContentElement
    {
        public string ResourcePath;
        public string Name;
    }

    private void DrawSpawnTrajectory(Vector3? initialPosition = null, Vector3? initialVelocity = null)
    {
        if (tempSpawnObjectBuffer == null) return;

        //float stepTime = Time.timeScale;
        float stepTime = 0.05f;
        if (initialPosition == null) initialPosition = camera.transform.position + spawnOffset;
        if (initialVelocity == null) initialVelocity = camera.transform.forward.normalized * spawnMenuVelocitySlider.value;
        float mass = tempSpawnObjectBuffer.GetComponent<Rigidbody>().mass;
        float radius = tempSpawnObjectBuffer.GetComponent<SphereCollider>().radius * Mathf.Max(tempSpawnObjectBuffer.transform.lossyScale.x, tempSpawnObjectBuffer.transform.lossyScale.y, tempSpawnObjectBuffer.transform.lossyScale.z);
        Vector3? hitPos = null;

        var trajectoryPoints = universe.SimulateNextGravitySteps(
            lineSegments,
            stepTime,
            initialPosition.Value,
            initialVelocity.Value,
            mass,
            radius,
            out hitPos
        );

        if (hitPos != null)
        {
            hitIndicator.SetActive(true);
            hitIndicator.transform.position = hitPos.Value;
        }
        else
        {
            hitIndicator.SetActive(false);
        }

        trajectoryLineRenderer.positionCount = trajectoryPoints.Count();
        trajectoryLineRenderer.SetPositions(trajectoryPoints.ToArray());
    }

    private void Init()
    {
        timeLabel.text = GetFormattedTimeLabelText(lastTimeScale);
        spawnMenuModal.visible = false;
        spawnMenu.visible = false;

        InitSpawnMenuFoldoutContent();

        imageTargetController.Disable();
    }

    private void InitSpawnMenuFoldoutContent()
    {
        var spawnMenuObjects = new Dictionary<string, SpawnMenuFoldoutContentElement[]>()
        {
            { "Manmade Bodies", new SpawnMenuFoldoutContentElement[]
                {
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Prefabs/Manmade CelestialBodies/Cassini_66.prefab",
                        Name="Cassini"
                    },
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Prefabs/Manmade CelestialBodies/Dawn_19.prefab",
                        Name="Dawn"
                    },
                }
            },
            { "Planets", new SpawnMenuFoldoutContentElement[]
                {
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Planets of the Solar System 3D/Prefabs/Mars.prefab",
                        Name="Mars"
                    },
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Planets of the Solar System 3D/Prefabs/Earth.prefab",
                        Name="Earth"
                    },
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Planets of the Solar System 3D/Prefabs/Saturn.prefab",
                        Name="Saturn"
                    },
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Planets of the Solar System 3D/Prefabs/Neptune.prefab",
                        Name="Neptune"
                    },
                }
            },
            { "Asteroids, Comets, Wharf Planets, Moons", new SpawnMenuFoldoutContentElement[]
                {
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Planets of the Solar System 3D/Prefabs/Pluto.prefab",
                        Name="Pluto"
                    },
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Planets of the Solar System 3D/Prefabs/Moon.prefab",
                        Name="Moon"
                    },
                }
            },
            { "Other", new SpawnMenuFoldoutContentElement[]
                {
                    new SpawnMenuFoldoutContentElement{
                        ResourcePath= "Assets/Prefabs/Natural CelestialBodies/BlackHole.prefab",
                        Name="Black Hole"
                    },
                }
            },
        };

        foreach (KeyValuePair<string, SpawnMenuFoldoutContentElement[]> entry in spawnMenuObjects.Reverse())
        {
            Foldout sectionFoldout = new Foldout();
            sectionFoldout.text = entry.Key;
            sectionFoldout.AddToClassList("spawn-menu-foldout");

            foreach (SpawnMenuFoldoutContentElement entryValue in entry.Value)
            {
                Label sectionFoldoutLabel = new Label();
                sectionFoldoutLabel.text = entryValue.Name;
                sectionFoldoutLabel.userData = entryValue.ResourcePath;
                sectionFoldoutLabel.AddToClassList("spawn-menu-foldout__element");
                sectionFoldoutLabel.RegisterCallback<ClickEvent>(e =>
                {
                    OnSpawnMenuModalFoldoutElementClick(entryValue);
                });

                sectionFoldout.Insert(0, sectionFoldoutLabel);
            }

            spawnMenuScrollView.Insert(0, sectionFoldout);
        }
    }

    #endregion
}
