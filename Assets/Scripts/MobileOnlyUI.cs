using UnityEngine;

public class MobileOnlyUI : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(Application.isEditor || Application.isMobilePlatform);
    }
}
