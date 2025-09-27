using UnityEngine;
using UnityEngine.UI;

public class CanSqueezeStepUI: MonoBehaviour
{
    public GameObject[] targetStepImages;
    public RawImage[] curStepImages;
    
    public Color stepInactiveColor;
    public Color stepActiveColor;
    public Color stepCorrectColor;
    public Color stepExceededColor;
    
    private void Awake()
    {
        if (targetStepImages.Length != curStepImages.Length)
        {
            Debug.LogError($"Different number of target steps({targetStepImages.Length})" +
                           $"and current steps({curStepImages.Length}).");
        }
    }
    
    /// <summary>
    /// Set the step level of the task.
    /// Steps >= 1.
    /// </summary>
    /// <param name="curStep"> Current step level. </param>
    /// <param name="targetStep"> Target step level. </param>
    public void SetStep(CanSqueezeStep curStep, CanSqueezeStep targetStep)
    {
        int curStepIdx = (int) curStep;
        int targetStepIdx = (int) targetStep;
        
        if (curStepIdx < 0 || curStepIdx >= curStepImages.Length)
        {
            Debug.LogError("Invalid step index: " + curStep);
            return;
        }
        if (targetStepIdx < 0 || targetStepIdx >= targetStepImages.Length)
        {
            Debug.LogError("Invalid step index: " + targetStep);
            return;
        }
        
        for (int i = 0; i < targetStepImages.Length; i++)
        {
            targetStepImages[i].SetActive(i == targetStepIdx);
            
            if (i <= curStepIdx)
            {
                if (i < targetStepIdx)
                    curStepImages[i].color = stepActiveColor;
                
                else if (i == targetStepIdx)
                    curStepImages[i].color = stepCorrectColor;
                
                else if (i > targetStepIdx)
                    curStepImages[i].color = stepExceededColor;
            }
            else
            {
                curStepImages[i].color = stepInactiveColor;
            }
        }
    }
    
    public void OffAllSteps()
    {
        foreach (GameObject targetStepImage in targetStepImages)
        {
            targetStepImage.SetActive(false);
        }
        
        foreach (RawImage curStepImage in curStepImages)
        {
            curStepImage.color = stepInactiveColor;
        }
    }
}