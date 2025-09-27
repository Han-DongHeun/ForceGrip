using UnityEngine;
using UnityEngine.Events;

public class TriggerButtonUI : MonoBehaviour
{
    public float requiredHoldTimeForTriggered = 0f;
    public Collider[] collidersToBeSensed;
    public string[] tagsToBeSensed;
    [ShowOnly] public int currentCollidersCount = 0;
    
    public UnityEvent OnButtonTriggered;
    public UnityEvent OnButtonStay;
    public UnityEvent OnButtonReleased;
    
    private bool _isButtonHeld = false;
    private float _buttonHoldTime = 0f;
    
    private void OnTriggerEnter(Collider other)
    {
        if (collidersToBeSensed.Contains(other) || (tagsToBeSensed.Length > 0 && tagsToBeSensed.Contains(other.tag)))
        {
            currentCollidersCount++;
            if (currentCollidersCount > collidersToBeSensed.Length)
                currentCollidersCount = collidersToBeSensed.Length;
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (collidersToBeSensed.Contains(other) || (tagsToBeSensed.Length > 0 && tagsToBeSensed.Contains(other.tag)))
        {
            OnButtonStay.Invoke();
            
            _buttonHoldTime += Time.fixedDeltaTime;
            if (_buttonHoldTime >= requiredHoldTimeForTriggered)
            {
                if (!_isButtonHeld)
                {
                    _isButtonHeld = true;
                    OnButtonTriggered.Invoke();
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (collidersToBeSensed.Contains(other) || (tagsToBeSensed.Length > 0 && tagsToBeSensed.Contains(other.tag)))
        {
            currentCollidersCount--;
            if (currentCollidersCount <= 0)
            {
                currentCollidersCount = 0;
                _isButtonHeld = false;
                _buttonHoldTime = 0f;
                OnButtonReleased.Invoke();
            }
        }
    }
}