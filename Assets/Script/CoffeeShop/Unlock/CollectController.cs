using UnityEngine;
using UnityEngine.UI;

public class CollectController : MonoBehaviour
{
    [Header("Collect Buttons")]
    public Button collectButton1;
    public Button collectButton2;

    [Header("Return")]
    public Button returnButton;

    [Header("Collect Rewards")]
    [Tooltip("采集获得的资源 resourceId")]
    public string rewardResourceId = "CocoaPowder";
    public int rewardAmount1 = 1;
    public int rewardAmount2 = 1;

    private int collected1 = 0;
    private int collected2 = 0;

    private void Start()
    {
        if (collectButton1 != null)
            collectButton1.onClick.AddListener(() => OnCollect(1));
        if (collectButton2 != null)
            collectButton2.onClick.AddListener(() => OnCollect(2));
        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturn);
    }

    private void OnCollect(int index)
    {
        int amount = index == 1 ? rewardAmount1 : rewardAmount2;
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.Add(rewardResourceId, amount);
            if (index == 1) collected1++;
            else collected2++;
            Debug.Log($"[CollectScene] Collected {amount}x {rewardResourceId}");
        }
    }

    private void OnReturn()
    {
        TimeSystem.EndNightPhaseStatic();
    }
}
