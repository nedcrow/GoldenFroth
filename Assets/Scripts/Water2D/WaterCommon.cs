using UnityEngine;
using UnityEngine.U2D;

public static class WaterCommon
{
    public static void CopySpline(SpriteShapeController source, SpriteShapeController target)
    {
        Spline sourceSpline = source.spline;
        Spline targetSpline = target.spline;

        // 기존 점들 삭제
        targetSpline.Clear();

        // 소스 스플라인의 모든 점 복사
        Vector3[] boundary = new Vector3[sourceSpline.GetPointCount()];
        Vector3[] newBoundary = new Vector3[sourceSpline.GetPointCount()];
        for (int i = 0; i < sourceSpline.GetPointCount(); i++)
        {
            boundary[i] = sourceSpline.GetPosition(i);
        }

        System.Array.Sort(boundary, (a, b) => a.y.CompareTo(b.y));
        newBoundary[0] = boundary[0].x < boundary[1].x ? boundary[0] : boundary[1];
        newBoundary[0] = boundary[0].x < boundary[1].x ? boundary[0] : boundary[1];

        if(boundary[0].x < boundary[1].x)
        {
            newBoundary[0] = boundary[0];
            newBoundary[3] = boundary[1];
        }
        else
        {
            newBoundary[0] = boundary[1];
            newBoundary[3] = boundary[0];
        }

        if (boundary[2].x < boundary[3].x)
        {
            newBoundary[1] = boundary[2];
            newBoundary[2] = boundary[3];
        }
        else
        {
            newBoundary[1] = boundary[3];
            newBoundary[2] = boundary[2];
        }

        for (int i = 0; i < sourceSpline.GetPointCount(); i++)
        {
            Vector3 position = newBoundary[i];
            Vector3 leftTangent = sourceSpline.GetLeftTangent(i);
            Vector3 rightTangent = sourceSpline.GetRightTangent(i);
            bool corner = sourceSpline.GetTangentMode(i) == ShapeTangentMode.Broken;
            targetSpline.InsertPointAt(i, position);
            targetSpline.SetTangentMode(i, sourceSpline.GetTangentMode(i));
            targetSpline.SetLeftTangent(i, leftTangent);
            targetSpline.SetRightTangent(i, rightTangent);
            targetSpline.SetCorner(i, corner);
        }

        // 스플라인 업데이트
        target.RefreshSpriteShape();
    }
}
