using System.Drawing;
using UnityEngine;
using UnityEngine.U2D;

public class Edge {
    public Vector2 start, end;
    public float length;
    public Edge(Vector3 start, Vector3 end) {
        this.start = start.y <= end.y ? start : end;
        this.end = start.y > end.y ? start : end;
        length = Vector3.Distance(start, end);
    }
}

public class CupComponent : MonoBehaviour
{
    [Range(0,1)]
    public float fillingAmount = 0.5f;
    [Range(0, 1)]
    public float thikness = 0;
    
    [Range(-170, 170)]
    public float slopeValue = 0;
    public float maxSlopeValue = 170;

    public GameObject glassBody;
    public GameObject waterSurface;
    public LineRenderer lineRenderer;

    Edge leftEdge;
    Edge rightEdge;
    Vector3 leftWaterSurfacePoint = new Vector3(-1, 1, 0);
    Vector3 rightWaterSurfacePoint = new Vector3(1, 1, 0);

    SpriteShapeController spriteShapeController;
    WaterShapeController waterShapeController = null;

    float minY = .0f;
    float maxY = .0f;
    float secondY = .0f;

    float waterHeight = 0;


    void OnValidate()
    {
        if(GetComponent<SpriteShapeController>() == null) gameObject.AddComponent<SpriteShapeController>();
        spriteShapeController = GetComponent<SpriteShapeController>();
    }

    void Awake()
    {
        spriteShapeController = GetComponent<SpriteShapeController>();
        lineRenderer = waterSurface.GetComponent<LineRenderer>();
        UpdateWaterSurface();

        waterShapeController = GetComponentInChildren<WaterShapeController>();
    }

    void Start()
    {
        UpdateFilling();
    }

    void Update()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="amount"> range is 0~1 </param>
    public void SetFillingAmount(float amount)
    {
        fillingAmount = Mathf.Clamp(amount, 0, 1);
        UpdateWaterSurface();
        UpdateFilling();
    }

    public void SetRotation(float targetRotation)
    {
        int pointCount = spriteShapeController.spline.GetPointCount();
        Vector3 pivot = CalculateSplineCenter(); // 회전의 중심을 계산

        // 현재 회전 각도를 계산
        float currentRotation = CalculateCurrentRotation();
        float distanceOfRotation = targetRotation - currentRotation;
        RotateDegree(distanceOfRotation);
    }

    public void RotateDegree(float degree)
    {
        int pointCount = spriteShapeController.spline.GetPointCount();
        Vector3 pivot = CalculateSplineCenter(); // 회전의 중심을 계산

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 point = spriteShapeController.spline.GetPosition(i);
            Vector3 rotatedPoint = RotatePointAroundPivot(point, pivot, degree);
            spriteShapeController.spline.SetPosition(i, rotatedPoint);
        }

        // Spline 갱신
        spriteShapeController.BakeMesh();
        UpdateWaterSurface();
        UpdateFilling();
    }

    private Vector3 CalculateSplineCenter()
    {
        int pointCount = spriteShapeController.spline.GetPointCount();
        Vector3 center = Vector3.zero;
        for (int i = 0; i < pointCount; i++)
        {
            center += spriteShapeController.spline.GetPosition(i);
        }
        //return center / pointCount;
        return Vector3.zero;
    }

    private float CalculateCurrentRotation()
    {
        // 바닥의 점들을 선택하여 회전 각도를 계산 (시작과 끝 점)
        Vector3 point1 = spriteShapeController.spline.GetPosition(0);
        Vector3 point2 = spriteShapeController.spline.GetPosition(spriteShapeController.spline.GetPointCount() - 1);

        // 벡터의 각도를 계산 (2D 평면에서의 각도 계산)
        Vector3 direction = point2 - point1;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        return angle;
    }

    private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, float angle)
    {
        Vector3 dir = point - pivot; // 중심으로부터의 벡터
        float rad = angle * Mathf.Deg2Rad; // 각도를 라디안으로 변환
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        float newX = dir.x * cos - dir.y * sin;
        float newY = dir.x * sin + dir.y * cos;
        return new Vector3(newX, newY, point.z) + pivot;
    }

    

    public Vector3 GetWaterSurfacePositionLeft()
    {
        return leftWaterSurfacePoint;
    }

    public Vector3 GetWaterSurfacePositionRight()
    {
        return rightWaterSurfacePoint;
    }

    void UpdateWaterSurface()
    {
        if (spriteShapeController == null || lineRenderer == null) return;

        // 컵 모양의 경계를 가져오기
        Spline spline = spriteShapeController.spline;
        Vector3[] cupBoundary = new Vector3[spline.GetPointCount()];
        for (int i = 0; i < spline.GetPointCount(); i++)
        {
            cupBoundary[i] = spline.GetPosition(i);
        }

        System.Array.Sort(cupBoundary, (a, b) => a.y.CompareTo(b.y));
        if (cupBoundary[2].y < 0) Debug.LogError("반드시 두 점은 높이가 0보다 높아야 합니다.");

        System.Array.Sort(cupBoundary, (a, b) => a.x.CompareTo(b.x));
        if (cupBoundary[2].x < 0) Debug.LogError("반드시 두 점은 위치가 0보다 오른쪽에 있어야 합니다.");
        leftEdge = new Edge(cupBoundary[0], cupBoundary[1]);
        rightEdge = new Edge(cupBoundary[2], cupBoundary[3]);

        if (leftEdge.start.y < leftEdge.end.y)
        {
            minY = Mathf.Min(leftEdge.start.y,  rightEdge.start.y);
            maxY = Mathf.Max(leftEdge.end.y, rightEdge.end.y);
            secondY = Mathf.Min(leftEdge.end.y, rightEdge.end.y); // Second
        }

        // 수면의 높이 계산
        waterHeight = Mathf.Lerp(minY, secondY, fillingAmount);

        // 수면의 넓이 계산 (바닥이 ㄷ모양이 아니고 V 모양인 경우도 필요
        leftWaterSurfacePoint = GetPointWaterSurface(leftEdge.start, leftEdge.end, waterHeight);
        rightWaterSurfacePoint = GetPointWaterSurface(rightEdge.start, rightEdge.end, waterHeight);

        // 수면 라인 설정
        lineRenderer.positionCount = 2;
        //lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, leftWaterSurfacePoint);
        lineRenderer.SetPosition(1, rightWaterSurfacePoint);
    }

    void UpdateFilling()
    {
        if (waterShapeController != null)
        {
            waterShapeController.RemoveAllWavepoints();
            if (waterShapeController.selfForm == false)
            {
                SpriteShapeController childShapeController = waterShapeController.GetComponent<SpriteShapeController>();
                WaterCommon.CopySpline(GetComponent<SpriteShapeController>(), childShapeController);
                childShapeController.spline.SetPosition(1, GetWaterSurfacePositionLeft());
                childShapeController.spline.SetPosition(2, GetWaterSurfacePositionRight());

                // 소스 스플라인의 모든 점 복사
                int countOfPoints = childShapeController.spline.GetPointCount();
                float centerX = 0;
                float centerY = 0;
                float centerZ = 0;

                for (int i = 0; i < countOfPoints; i++)
                {
                    centerX += childShapeController.spline.GetPosition(i).x;
                    centerY += childShapeController.spline.GetPosition(i).y;
                    centerZ += childShapeController.spline.GetPosition(i).z;
                }

                Vector3 centerV = new Vector3(
                    centerX / countOfPoints,
                    centerY / countOfPoints,
                    centerZ / countOfPoints
                    );

                for (int i = 0; i < countOfPoints; i++)
                {
                    childShapeController.spline.SetPosition(
                        i, Vector3.Lerp(childShapeController.spline.GetPosition(i), centerV, thikness)
                        );
                }
            }
            waterShapeController.InitWaves();
        }
    }

    Vector3 GetPointWaterSurface(Vector3 start, Vector3 end, float targetY)
    {
        // 선분의 전체 높이
        float totalHeight = end.y - start.y;

        // 높이 비율 계산
        float heightRatio = (targetY - start.y) / totalHeight;

        // 제한된 위치 계산 (비율에 따라 선형 보간)
        float x = Mathf.Lerp(start.x, end.x, heightRatio);
        float z = Mathf.Lerp(end.z, start.z, heightRatio);

        return new Vector3(x, targetY, z);
    }
}