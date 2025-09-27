using UnityEngine;

[AddComponentMenu("")]
public class PickAndPlaceObjectTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("PickAndPlaceTaskBoxTrigger"))
        {
            PickAndPlaceTaskManager.Instance.CheckBoxInsertion(other);
        }
    }
}