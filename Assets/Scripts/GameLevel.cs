using UnityEngine;

public class GameLevel : MonoBehaviour
{
    
    [SerializeField] private int levelNumber;
    [SerializeField] private Transform landerStartPositionTransform;
    [SerializeField] private Transform cameraStartTagetTransform;
    [SerializeField] private float zoomedOutOrthographicSize;

    private void Awake()
    {

    }
    
    public int GetLevelNumber()
    {
        return levelNumber;
    }

    public Vector3 GetLanderStartPosition()
    {
        return landerStartPositionTransform.position;
    }

    public Transform getCameraStartTargetTransform()
    {
        return cameraStartTagetTransform;
    }

    public float GetZoomedOutOrthographicSize()
    {
        return zoomedOutOrthographicSize;
    }

}
