using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsTextMesh;
    [SerializeField] private GameObject speedLeftArrGobjGameObject;
    [SerializeField] private GameObject speedRightArrGobjGameObject;
    [SerializeField] private GameObject speedUpArrGobjGameObject;
    [SerializeField] private GameObject speedDownArrGobjGameObject;
    [SerializeField] private Image fuelImage;

    private void Update()
    {
        UpdateStatsTextMesh();
    }

    private void UpdateStatsTextMesh()
    {
        speedLeftArrGobjGameObject.SetActive(Lander.Instance.GetSpeedX()>= 0);
        speedRightArrGobjGameObject.SetActive(Lander.Instance.GetSpeedX()< 0);
        speedUpArrGobjGameObject.SetActive(Lander.Instance.GetSpeedY()>= 0);
        speedDownArrGobjGameObject.SetActive(Lander.Instance.GetSpeedY()< 0);

        fuelImage.fillAmount = Lander.Instance.GetFuelNormalized();

        statsTextMesh.text =
            GameManager.Instance.GetLevelNumber() + "\n" +
            GameManager.Instance.GetScore() + "\n" +
            Mathf.Round(GameManager.Instance.GetTime()) + "\n" +
            Mathf.Abs(Mathf.Round(Lander.Instance.GetSpeedX() * 10f)) + "\n" +
            Mathf.Abs(Mathf.Round(Lander.Instance.GetSpeedY() * 10f)) + "\n";
    }
}

