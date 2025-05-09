using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GenerateTextOnStructure : MonoBehaviour
{
    public static GenerateTextOnStructure Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    int idx; //positon idx
    public void PositionObjectSetter(TextObj TextObj)
    {
        if(spawnedObjects.Count > idx)
        {
            TextObj.HeadObject = spawnedObjects[idx];
            idx++;
            char c = baseText[Random.Range(0, baseText.Length)];
            var text = TextObj.txt_Data;
            if (text != null)
            {
                text.text = c.ToString();
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1.0f);
                textComponents.Add(text);
            }
        }
    }

    public GameObject textPrefab;
    public Transform parentTransform;
    public string baseText = "flowrevo"; 

    public enum ShapeType { Sphere, Torus, Plane, Cylinder, Helix }
    [SerializeField] private ShapeType shape = ShapeType.Sphere;

    [System.Serializable]
    public class SphereSettings
    {
        public int latitudeSteps = 20;
        public int longitudeSteps = 20;
        public float radius = 500f;
        public float fadeDistance = 1000f;
        public float minAlpha = 0.2f;
        public SphereSettings Clone() => (SphereSettings)this.MemberwiseClone();
    }

    [System.Serializable]
    public class TorusSettings
    {
        public int latitudeSteps = 20;
        public int longitudeSteps = 40;
        public float majorRadius = 550f;
        public float minorRadius = 200f;
        public float fadeDistance = 1000f;
        public float minAlpha = 0.2f;
        public TorusSettings Clone() => (TorusSettings)this.MemberwiseClone();
    }

    [System.Serializable]
    public class PlaneSettings
    {
        public int latitudeSteps = 50;
        public int longitudeSteps = 50;
        public float spacing = 50f;
        public float fadeDistance = 1000f;
        public float minAlpha = 0.2f;
        public PlaneSettings Clone() => (PlaneSettings)this.MemberwiseClone();
    }

    [System.Serializable]
    public class CylinderSettings
    {
        public int latitudeSteps = 30;
        public int longitudeSteps = 35;
        public float radius = 450f;
        public float height = 1500f;
        public float fadeDistance = 1000f;
        public float minAlpha = 0.2f;
        public CylinderSettings Clone() => (CylinderSettings)this.MemberwiseClone();
    }

    [System.Serializable]
    public class HelixSettings
    {
        public int latitudeSteps = 80;
        public float radius = 450f;
        public float turns = 25f;
        public float height = 800f;
        public float fadeDistance = 1000f;
        public float minAlpha = 0.2f;
        public HelixSettings Clone() => (HelixSettings)this.MemberwiseClone();
    }

    [SerializeField] private SphereSettings sphereSettings;
    [SerializeField] private TorusSettings torusSettings;
    [SerializeField] private PlaneSettings planeSettings;
    [SerializeField] private CylinderSettings cylinderSettings;
    [SerializeField] private HelixSettings helixSettings;

    public float rotationSpeed = 15f;
    public int updateInterval = 2;

    [SerializeField] private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<TextMeshPro> textComponents = new List<TextMeshPro>();
    private int frameCounter = 0;

    private SphereSettings prevSphere;
    private TorusSettings prevTorus;
    private PlaneSettings prevPlane;
    private CylinderSettings prevCylinder;
    private HelixSettings prevHelix;
    private ShapeType prevShape;

    void Start()
    {
        StoreCurrentSettings();
        Regenerate();
    }

    void Update()
    {
        parentTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        if (Application.isPlaying)
        {
            frameCounter++;
            if (frameCounter >= updateInterval)
            {
                UpdateObjectTransparency();
                frameCounter = 0;
            }

            if (HasSettingsChanged())
            {
                Regenerate();
                StoreCurrentSettings();
            }
        }
    }

    public void Regenerate()
    {
        ClearPreviousTexts();
        spawnedObjects.Clear();
        textComponents.Clear();
        GenerateTextStructure();
    }

    void ClearPreviousTexts()
    {
        for (int i = parentTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(parentTransform.GetChild(i).gameObject);
        }
    }

    void GenerateTextStructure()
    {
        if (shape == ShapeType.Sphere)
        {
            GenerateFibonacciSphere();
            return;
        }

        int latSteps = GetLatSteps();
        int lonSteps = GetLonSteps();

        for (int lat = 0; lat < latSteps; lat++)
        {
            int lonStepsThisLat = (lat == 0 || lat == latSteps - 1) ? 1 : lonSteps;
            for (int lon = 0; lon < lonStepsThisLat; lon++)
            {
                Vector3 pos = GetPositionByShape(lat, lon);
                GameObject txtObj = Instantiate(textPrefab, parentTransform.position + pos, Quaternion.identity, parentTransform);
                spawnedObjects.Add(txtObj);
            }
        }
    }

    void GenerateFibonacciSphere()
    {
        int N = Mathf.Clamp(sphereSettings.latitudeSteps, 1, 120) * Mathf.Clamp(sphereSettings.longitudeSteps, 1, 120);
        float offset = 2f / N;
        float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < N; i++)
        {
            float y = i * offset - 1 + (offset / 2f);
            float r = Mathf.Sqrt(1 - y * y);
            float phi = i * increment;

            float x = Mathf.Cos(phi) * r;
            float z = Mathf.Sin(phi) * r;

            Vector3 pos = new Vector3(x, y, z) * sphereSettings.radius;
            GameObject txtObj = Instantiate(textPrefab, parentTransform.position + pos, Quaternion.identity, parentTransform);
            spawnedObjects.Add(txtObj);

        }
    }

    void UpdateObjectTransparency()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float fadeDistance = GetCurrentFadeDistance();
        float minAlpha = GetCurrentMinAlpha();

        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            var obj = spawnedObjects[i];
            var text = textComponents[i];
            if (obj == null || text == null) continue;

            float dist = Vector3.Distance(cam.transform.position, obj.transform.position);
            float t = Mathf.Clamp01(dist / fadeDistance);
            float alpha = Mathf.Lerp(1.0f, minAlpha, t);

            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }

    float GetCurrentFadeDistance()
    {
        switch (shape)
        {
            case ShapeType.Sphere: return sphereSettings.fadeDistance;
            case ShapeType.Torus: return torusSettings.fadeDistance;
            case ShapeType.Plane: return planeSettings.fadeDistance;
            case ShapeType.Cylinder: return cylinderSettings.fadeDistance;
            case ShapeType.Helix: return helixSettings.fadeDistance;
            default: return 1000f;
        }
    }

    float GetCurrentMinAlpha()
    {
        switch (shape)
        {
            case ShapeType.Sphere: return sphereSettings.minAlpha;
            case ShapeType.Torus: return torusSettings.minAlpha;
            case ShapeType.Plane: return planeSettings.minAlpha;
            case ShapeType.Cylinder: return cylinderSettings.minAlpha;
            case ShapeType.Helix: return helixSettings.minAlpha;
            default: return 0.2f;
        }
    }



int GetLatSteps()
    {
        switch (shape)
        {
            case ShapeType.Torus: return Mathf.Clamp(torusSettings.latitudeSteps, 1, 120);
            case ShapeType.Plane: return Mathf.Clamp(planeSettings.latitudeSteps, 1, 120);
            case ShapeType.Cylinder: return Mathf.Clamp(cylinderSettings.latitudeSteps, 1, 120);
            case ShapeType.Helix: return Mathf.Clamp(helixSettings.latitudeSteps, 1, 120);
            default: return 1;
        }
    }

    int GetLonSteps()
    {
        switch (shape)
        {
            case ShapeType.Torus: return Mathf.Clamp(torusSettings.longitudeSteps, 1, 120);
            case ShapeType.Plane: return Mathf.Clamp(planeSettings.longitudeSteps, 1, 120);
            case ShapeType.Cylinder: return Mathf.Clamp(cylinderSettings.longitudeSteps, 1, 120);
            default: return 1;
        }
    }

    Vector3 GetPositionByShape(int lat, int lon)
    {
        switch (shape)
        {
            case ShapeType.Torus:
                float thetaT = 2f * Mathf.PI * lat / Mathf.Clamp(torusSettings.latitudeSteps, 1, 120);
                float phiT = 2f * Mathf.PI * lon / Mathf.Clamp(torusSettings.longitudeSteps, 1, 120);
                return new Vector3(
                    (torusSettings.majorRadius + torusSettings.minorRadius * Mathf.Cos(thetaT)) * Mathf.Cos(phiT),
                    (torusSettings.majorRadius + torusSettings.minorRadius * Mathf.Cos(thetaT)) * Mathf.Sin(phiT),
                    torusSettings.minorRadius * Mathf.Sin(thetaT)
                );

            case ShapeType.Plane:
                return new Vector3(
                    (lon - planeSettings.longitudeSteps / 2) * planeSettings.spacing,
                    (lat - planeSettings.latitudeSteps / 2) * planeSettings.spacing,
                    0f
                );

            case ShapeType.Cylinder:
                float phiC = 2f * Mathf.PI * lon / Mathf.Clamp(cylinderSettings.longitudeSteps, 1, 120);
                float yC = -cylinderSettings.height / 2f + cylinderSettings.height * lat / (Mathf.Clamp(cylinderSettings.latitudeSteps, 1, 120) - 1);
                return new Vector3(
                    cylinderSettings.radius * Mathf.Cos(phiC),
                    yC,
                    cylinderSettings.radius * Mathf.Sin(phiC)
                );

            case ShapeType.Helix:
                float t = (float)lat / (Mathf.Clamp(helixSettings.latitudeSteps, 1, 120) - 1);
                float angle = 2f * Mathf.PI * helixSettings.turns * t;
                float yH = -helixSettings.height / 2f + helixSettings.height * t;
                return new Vector3(
                    helixSettings.radius * Mathf.Cos(angle),
                    yH,
                    helixSettings.radius * Mathf.Sin(angle)
                );
        }

        return Vector3.zero;
    }

    void StoreCurrentSettings()
    {
        prevShape = shape;
        prevSphere = sphereSettings.Clone();
        prevTorus = torusSettings.Clone();
        prevPlane = planeSettings.Clone();
        prevCylinder = cylinderSettings.Clone();
        prevHelix = helixSettings.Clone();
    }

    bool HasSettingsChanged()
    {
        if (shape != prevShape) return true;

        switch (shape)
        {
            case ShapeType.Sphere:
                return sphereSettings.latitudeSteps != prevSphere.latitudeSteps ||
                       sphereSettings.longitudeSteps != prevSphere.longitudeSteps ||
                       sphereSettings.radius != prevSphere.radius ||
                       sphereSettings.fadeDistance != prevSphere.fadeDistance;

            case ShapeType.Torus:
                return torusSettings.latitudeSteps != prevTorus.latitudeSteps ||
                       torusSettings.longitudeSteps != prevTorus.longitudeSteps ||
                       torusSettings.majorRadius != prevTorus.majorRadius ||
                       torusSettings.minorRadius != prevTorus.minorRadius ||
                       torusSettings.fadeDistance != prevTorus.fadeDistance;

            case ShapeType.Plane:
                return planeSettings.latitudeSteps != prevPlane.latitudeSteps ||
                       planeSettings.longitudeSteps != prevPlane.longitudeSteps ||
                       planeSettings.spacing != prevPlane.spacing ||
                       planeSettings.fadeDistance != prevPlane.fadeDistance;

            case ShapeType.Cylinder:
                return cylinderSettings.latitudeSteps != prevCylinder.latitudeSteps ||
                       cylinderSettings.longitudeSteps != prevCylinder.longitudeSteps ||
                       cylinderSettings.radius != prevCylinder.radius ||
                       cylinderSettings.height != prevCylinder.height ||
                       cylinderSettings.fadeDistance != prevCylinder.fadeDistance;

            case ShapeType.Helix:
                return helixSettings.latitudeSteps != prevHelix.latitudeSteps ||
                       helixSettings.radius != prevHelix.radius ||
                       helixSettings.turns != prevHelix.turns ||
                       helixSettings.height != prevHelix.height ||
                       helixSettings.fadeDistance != prevHelix.fadeDistance;
        }

        return false;
    }
}
