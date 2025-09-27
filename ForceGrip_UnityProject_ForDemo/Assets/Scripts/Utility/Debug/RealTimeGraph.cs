using System.Collections.Generic;
using TrainingSequence;
using UnityEngine;

public class RealTimeGraph : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float xMaxPosition;
    public float yMax;
    public int maxPoints = 90;
    private List<float> dataPoints = new List<float>();

    void Start()
    {
        dataPoints = new List<float>(maxPoints);
        lineRenderer.positionCount = 0;
    }
    
    public void ResetGraph()
    {
        dataPoints.Clear();
        lineRenderer.positionCount = 0;
    }

    public void StackGraph(float newValue)
    {
        dataPoints.Add(newValue);
        if (dataPoints.Count > maxPoints)
            dataPoints.RemoveAt(0);
    }
    
    public void AdjustGraph()
    {
        lineRenderer.positionCount = dataPoints.Count;
        for (int i = 0; i < dataPoints.Count; i++)
        {
            float x = xMaxPosition * i / maxPoints;
            float y = dataPoints[i] * yMax;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}