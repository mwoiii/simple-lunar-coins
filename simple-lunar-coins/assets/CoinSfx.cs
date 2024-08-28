using UnityEngine;


// this script is attached to the lunar coin effects, but for some god forsaken reason, more than half the time it isn't since SotS and idk what to do about it
public class CoinSfx : MonoBehaviour
{
    public void Awake()
    {
        AkSoundEngine.PostEvent(4043138392, base.gameObject);
    }
}