using UnityEngine;

public class CanObjectManager: MonoBehaviour
{
    public Mesh[] CanSqueezedStepMeshes;
    
    private MeshFilter _meshFilter;
    
    [ShowOnly] public CanSqueezeStep curStep;
    
    private void OnTriggerEnter(Collider other)
    {
        CanSqueezeTaskManager.Instance.CheckTrashCanInsertion(other);
    }
    
    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        SetCanSqueezedStep(CanSqueezeStep.Zero);
    }
    
    public void SetCanSqueezedStep(CanSqueezeStep step)
    {
        int stepIdx = (int) step;
        
        if (stepIdx < 0 || stepIdx >= CanSqueezedStepMeshes.Length)
        {
            Debug.LogError("Invalid step index: " + stepIdx);
            return;
        }
        
        if (curStep < step)
            EvaluationSoundController.Instance.PlayCanSqueeze(transform.position);
        
        curStep = step;
        _meshFilter.mesh = CanSqueezedStepMeshes[stepIdx];
    }
}