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
    [SerializeField]
    public GameObject planetText;


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

    private LineRenderer trajectoryLineRenderer;
    private GameObject tempSpawnObjectBuffer;

    private Universe universe;
    private Camera camera;
    private Canvas canvas;

    public Vector3 spawnOffset = new Vector3(0, -10, 0);

    private void OnEnable()
    {
        lastTimeScale = Time.timeScale;

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

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

        timeActionButton.clicked += OnTimeActionButtonClicked;
        timeActionSlowerButton.clicked += OnTimeActionSlowerButtonClicked;
        timeActionFasterButton.clicked += OnTimeActionFasterButtonClicked;
        objectActionAddButton.clicked += OnObjectActionAddButtonClicked;
        spawnMenuModalCloseButton.clicked += OnSpawnMenuModalCloseButtonClicked;
        spawnMenuCloseButton.clicked += OnSpawnMenuCloseButtonClicked;
        spawnMenuSpawnButton.clicked += OnSpawnMenuSpawnButtonClicked;

        Init();

        trajectoryLineRenderer = GetComponent<LineRenderer>();
        universe = GameObject.FindGameObjectWithTag("CelestialBodyContainer").GetComponent<Universe>();
        camera = Camera.main; //camera = GameObject.FindGameObjectWithTag("MainCamera");
        canvas = GameObject.FindObjectOfType<Canvas>();
    }

    private void Update()
    {
        if (approximateBodySpawnPath && tempSpawnObjectBuffer != null)
        {
            DrawSpawnTrajectory();
        }
        //drawClosestObjectText();
    }

    private void drawClosestObjectText()
    {
        var screenCenter = new Vector3(0.5f, 0.5f, 0);
        Ray ray = Camera.main.ViewportPointToRay(screenCenter);
        RaycastHit hitData;
        Debug.DrawRay(ray.origin, ray.direction * 9999);
        if (Physics.Raycast(ray, out hitData) && hitData.collider.CompareTag("CelestialBody"))
        {
            planetText.SetActive(true);

            //this is the ui element
            RectTransform UI_Element = planetText.GetComponent<RectTransform>();
            Rigidbody rb = hitData.transform.gameObject.GetComponent<Rigidbody>();
            planetText.GetComponent<TextMeshProUGUI>().text = $"{hitData.transform.gameObject.name} {Mathf.RoundToInt(rb.velocity.magnitude)} km/s";

            //first you need the RectTransform component of your canvas
            RectTransform CanvasRect = canvas.GetComponent<RectTransform>();

            //then you calculate the position of the UI element
            //0,0 for the canvas is at the center of the screen, whereas WorldToViewPortPoint treats the lower left corner as 0,0. Because of this, you need to subtract the height / width of the canvas * 0.5 to get the correct position.
            Vector3 ViewportPosition = camera.WorldToViewportPoint(hitData.transform.position);
            Vector3 WorldObject_ScreenPosition = new Vector3(
            ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
            ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)),
            (0));

            //now you can set the position of the ui element
            UI_Element.anchoredPosition3D = WorldObject_ScreenPosition;
        } else
        {
            planetText.SetActive(false);
        }

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

    private void OnSpawnMenuSpawnButtonClicked()
    {
        // instantiate as child of universe container
        GameObject child = Instantiate(tempSpawnObjectBuffer, universe.gameObject.transform);
        if (universe.disableTrails) {
            var lineRenderer = child.GetComponent<TrailRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
        Rigidbody childRigidBody = child.GetComponent<Rigidbody>();
        Vector3 cameraNormal = camera.transform.forward.normalized;
        cameraNormal *= spawnMenuVelocitySlider.value;
        childRigidBody.velocity += cameraNormal;
        //childRigidBody.mass = 99999999;
        child.transform.position = camera.transform.position + spawnOffset;
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
        } else
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

    //private void DrawSpawnTrajectory()
    //{
    //    float stepTime = Time.timeScale;
    //    float delta = Time.fixedDeltaTime;

    //    Vector3 initialPosition = camera.transform.position + spawnOffset;
    //    Vector3 initialVelocity = camera.transform.forward.normalized * spawnMenuVelocitySlider.value;
    //    float mass = tempSpawnObjectBuffer.GetComponent<Rigidbody>().mass;

    //    Vector3 currentPosition = initialPosition;
    //    Vector3 currentVelocity = initialVelocity;
    //    var trajectoryPoints = new List<Vector3>();
    //    for (int i = 0; i < lineSegments; i++)
    //    {
    //        float stepTimePassed = stepTime * i;
    //        trajectoryPoints.Add(currentPosition);
    //        currentVelocity += universe.CalculateNextBodyVelocity(mass, initialPosition) * stepTime / mass;
    //        currentPosition += currentVelocity * stepTime;
    //    }

    //    trajectoryLineRenderer.positionCount = trajectoryPoints.Count();
    //    trajectoryLineRenderer.SetPositions(trajectoryPoints.ToArray());
    //}

    private void DrawSpawnTrajectory()
    {
        if (tempSpawnObjectBuffer == null) return;

        //float stepTime = Time.timeScale;
        float stepTime = 0.05f;
        Vector3 initialPosition = camera.transform.position + spawnOffset;
        Vector3 initialVelocity = camera.transform.forward.normalized * spawnMenuVelocitySlider.value;
        float mass = tempSpawnObjectBuffer.GetComponent<Rigidbody>().mass;
        float radius = tempSpawnObjectBuffer.GetComponent<SphereCollider>().radius * Mathf.Max(tempSpawnObjectBuffer.transform.lossyScale.x, tempSpawnObjectBuffer.transform.lossyScale.y, tempSpawnObjectBuffer.transform.lossyScale.z);
        Vector3? hitPos = null;

        var trajectoryPoints = universe.SimulateNextGravitySteps(
            lineSegments, 
            stepTime, 
            initialPosition, 
            initialVelocity, 
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
