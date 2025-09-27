using System.Collections;
using UnityEngine;

public class EvaluationUIManager : MonoBehaviour
{
    public GameObject correctPrefab;
    public GameObject wrongPrefab;
    
    public static EvaluationUIManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    
    public void PlotCorrect(Vector3 position)
    {
        StartCoroutine(PlotOImage(position));
        EvaluationSoundController.Instance.PlayRight();
    }
    
    IEnumerator PlotOImage(Vector3 position)
    {
        GameObject Oobject = Instantiate(correctPrefab, position, Quaternion.identity);
        
        yield return new WaitForSeconds(2f);
        
        Destroy(Oobject);
    }
    
    public void PlotWrong(Vector3 position)
    {
        StartCoroutine(PlotXImage(position));
        EvaluationSoundController.Instance.PlayWrong();
    }
    
    IEnumerator PlotXImage(Vector3 position)
    {
        GameObject Xobject = Instantiate(wrongPrefab, position, Quaternion.identity);
        
        yield return new WaitForSeconds(2f);
        
        Destroy(Xobject);
    }
}