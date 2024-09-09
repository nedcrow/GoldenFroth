using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;

[ExecuteAlways]
public class WaterShapeController : MonoBehaviour
{
    public float spread = 0.006f;
    public float dampening = 0.03f;
    public float springStiffness = 0.1f;
    public bool selfForm = false;

    public GameObject wavePointPref;

    [SerializeField]
    [Range(-1, 1)]
    public float tensionValue = 0;

    [SerializeField]
    [Range(1, 100)]
    int wavesCount = 6;
    [SerializeField]
    string wavePointsName = "WavePoints";
    [SerializeField]
    GameObject wavePointGroup;
    [SerializeField]
    List<WaterSpring> waterSprings = new();
    int cornersCount = 2;
    SpriteShapeController spriteShapeController = null;


    void Awake()
    {
        spriteShapeController = GetComponent<SpriteShapeController>();
        if (spriteShapeController == null) Debug.LogWarning("Null exception SpriteShapeController from " + name);

        if (wavePointPref == null) Debug.LogWarning("Null exception wavePointPref from " + name);

        if (wavePointGroup == null)
        {
            Transform child = transform.Find(wavePointsName + "_" + name);
            if (child == null)
            {
                wavePointGroup = new GameObject(wavePointsName + "_" + name);
                wavePointGroup.transform.parent = transform;
                wavePointGroup.transform.localPosition = Vector3.zero;
            }
            else
            {
                wavePointGroup = child.gameObject;
                wavePointGroup.transform.localPosition = Vector3.zero;
            }
        }
    }

    void Start()
    {
        InitWaves();   
    }

    void OnValidate()
    {
    }

    void FixedUpdate()
    {
        foreach (WaterSpring waterSpringComponent in waterSprings)
        {
            if (waterSpringComponent == null) break;
            waterSpringComponent.WaveSpringUpdate(springStiffness, dampening);
            waterSpringComponent.WavePointUpdate();
        }

        UpdateWaterSprings();
    }

    public void InitWaves()
    {
        RemoveAllWavepoints();

        SetWaves();

        foreach (WaterSpring waterSpringComponent in waterSprings)
        {
            waterSpringComponent.Init(spriteShapeController);
        }
    }

    public void RemoveAllWavepoints()
    {
        GameObject[] wavepointArr = new GameObject[wavePointGroup.transform.childCount];

        for (int i = 0; i < wavepointArr.Length; i++)
        {
            wavepointArr[i] = wavePointGroup.transform.GetChild(i).gameObject;
        }

        foreach (var child in wavepointArr)
        {
            DestroyImmediate(child);
        }
    }

    void SetWaves()
    {
        Spline waterSpline = spriteShapeController.spline;
        int waterPointsCount = waterSpline.GetPointCount();

        // Remove middle points for the waves
        // Keep only the corners
        // Removing 1 point at a time we can remove only the 1st point
        // This means every time we remove 1st point the 2nd point becomes first
        for (int i = cornersCount; i < waterPointsCount - cornersCount; i++)
        {
            waterSpline.RemovePointAt(cornersCount);
        }

        Vector3 waterTopLeftCorner =  waterSpline.GetPosition(1);
        Vector3 waterTopRightCorner =  waterSpline.GetPosition(2);
        float waterWidth = waterTopRightCorner.x - waterTopLeftCorner.x;

        float spacingPerWave = waterWidth / (wavesCount + 1);
        float margin = spacingPerWave * tensionValue;
        float marginABS = Mathf.Abs(margin);
        float widthWithoutMargin = waterWidth - (marginABS * 2);
        float spacingPerWaveWithoutMargin = widthWithoutMargin / (wavesCount - 1);

        Vector3 newWaterTopLeftCorner = waterSpline.GetPosition(1) + (Vector3.up * margin);
        Vector3 newWaterTopRightCorner = waterSpline.GetPosition(2) + (Vector3.up * margin);

        // Set new points for the waves (started right)
        for (int i = wavesCount; i > 0; i--)
        {
            int index = cornersCount;
            float value = marginABS == 0 ? spacingPerWave : spacingPerWaveWithoutMargin;
            float xPosition = newWaterTopLeftCorner.x + (value * i) - (marginABS == 0 ? 0 : spacingPerWaveWithoutMargin - Mathf.Abs(marginABS));
            Vector3 wavePoint = new Vector3(xPosition, newWaterTopLeftCorner.y, newWaterTopLeftCorner.z);
            waterSpline.InsertPointAt(index, wavePoint);
            waterSpline.SetHeight(index, 0.1f);
            waterSpline.SetCorner(index, false);
            waterSpline.SetTangentMode(index, ShapeTangentMode.Continuous);
        }
        // loop through all the wave points
        // plus the both top left and right corners
        CreateSprings(waterSpline);
        Splash(2, 1);
    }
    void CreateSprings(Spline waterSpline)
    {
        List<GameObject> tempGameObjList = new List<GameObject>();
        waterSprings = new List<WaterSpring>();

        for (int i = 0; i <= wavesCount + 1; i++)
        {
            int index = i + 1;

            Smoothen(waterSpline, index);

            GameObject wavePoint = Instantiate(wavePointPref, wavePointGroup.transform, false);
            wavePoint.transform.localPosition = waterSpline.GetPosition(index);
            tempGameObjList.Add(wavePoint);
        }

        foreach (var item in tempGameObjList)
        {
            WaterSpring waterSpring = item.GetComponent<WaterSpring>();
            waterSpring.Init(spriteShapeController);
            waterSprings.Add(waterSpring);
        }
    }

    void Smoothen(Spline waterSpline, int index)
    {
        Vector3 position = waterSpline.GetPosition(index);
        Vector3 positionPrev = position;
        Vector3 positionNext = position;
        if (index > 1)
        {
            positionPrev = waterSpline.GetPosition(index - 1);
        }
        if (index - 1 <= wavesCount)
        {
            positionNext = waterSpline.GetPosition(index + 1);
        }

        Vector3 forward = gameObject.transform.forward;

        float scale = Mathf.Min((positionNext - position).magnitude, (positionPrev - position).magnitude) * 0.33f;

        Vector3 leftTangent = (positionPrev - position).normalized * scale;
        Vector3 rightTangent = (positionNext - position).normalized * scale;

        SplineUtility.CalculateTangents(position, positionPrev, positionNext, forward, scale, out rightTangent, out leftTangent);

        waterSpline.SetLeftTangent(index, leftTangent);
        waterSpline.SetRightTangent(index, rightTangent);
    }
    void UpdateWaterSprings()
    {
        int count = waterSprings.Count;
        float[] left_deltas = new float[count];
        float[] right_deltas = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                left_deltas[i] = spread * (waterSprings[i].height - waterSprings[i - 1].height);
                waterSprings[i - 1].velocity += left_deltas[i];
            }
            if (i < waterSprings.Count - 1)
            {
                right_deltas[i] = spread * (waterSprings[i].height - waterSprings[i + 1].height);
                waterSprings[i + 1].velocity += right_deltas[i];
            }
            //Splash(i, 0.01f);
        }
    }

    void Splash(int index, float speed)
    {
        if (index >= 0 && index < waterSprings.Count)
        {
            waterSprings[index].velocity += speed;
        }
    }

    IEnumerator CreateWaves()
    {
        foreach (Transform child in wavePointGroup.transform)
        {
            Destroy(child.gameObject);
            //StartCoroutine(Destroy(child.gameObject));
        }
        yield return null;
        SetWaves();
        yield return null;
    }
    IEnumerator Destroy(GameObject go)
    {
        yield return null;
        DestroyImmediate(go);
    }
}