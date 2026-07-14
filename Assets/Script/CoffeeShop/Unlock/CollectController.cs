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
    public ResourceData rewardItem;
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
        if (rewardItem == null) return;
        int amount = index == 1 ? rewardAmount1 : rewardAmount2;
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.Add(rewardItem.ResourceId, amount);
            if (index == 1) collected1++;
            else collected2++;
            Debug.Log($"[CollectScene] Collected {amount}x {rewardItem.DisplayName}");
        }
    }

    private void OnReturn()
    {
        TimeSystem.EndNightPhaseStatic();
    }
}
