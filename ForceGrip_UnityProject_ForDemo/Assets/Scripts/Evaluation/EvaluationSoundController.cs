using UnityEngine;

public class EvaluationSoundController : MonoBehaviour
{
    public AudioClip rightSound;
    public AudioClip wrongSound;
    public AudioClip successSound;
    public AudioClip failedSound;
    public AudioClip beepSound;
    public AudioClip highBeepSound;
    public AudioClip canSqueezeSound;
    public AudioClip canTrashSound;
    
    public static EvaluationSoundController Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    
    public void PlayRight()
    {
        PlaySound(rightSound);
    }
    
    public void PlayWrong()
    {
        PlaySound(wrongSound);
    }
    
    public void PlaySuccess()
    {
        PlaySound(successSound);
    }
    
    public void PlayFailed()
    {
        PlaySound(failedSound);
    }

    public void PlayBeep()
    {
        PlaySound(beepSound);
    }

    public void PlayHighBeep()
    {
        PlaySound(highBeepSound);
    }
    
    public void PlayCanSqueeze(Vector3 position)
    {
        PlaySound(canSqueezeSound, position);
    }
    
    public void PlayCanTrash(Vector3 position)
    {
        PlaySound(canTrashSound, position);
    }
    
    private void PlaySound(AudioClip soundClip, Vector3 position = default)
    {
        if (soundClip == null)
        {
            Debug.LogWarning("Sound clip is null.");
            return;
        }
        
        if (position == default)
            AudioSource.PlayClipAtPoint(soundClip, Camera.main.transform.position);
        else
            AudioSource.PlayClipAtPoint(soundClip, position);
    }
}
