using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderUI : MonoBehaviour
{
    [Header("Order Panel")]
    [SerializeField] private Text npcText;
    [SerializeField] private Text coffeeText;
    [SerializeField] private Text stateText;
    [SerializeField] private GameObject orderPanel;

    [Header("Order Ticket Image")]
    [SerializeField] private GameObject orderTicketImage;
    [SerializeField] private Image ticketImage;
    [SerializeField] private TMP_Text ticketCoffeeLabel;
    [SerializeField] private TMP_Text ticketStepsText;
    [SerializeField] private TMP_Text ticketMaterialsText;
    [SerializeField] private TMP_Text ticketPriceText;

    private void OnEnable()
    {
        GameEvent.On("OrderCreated", HandleOrderCreated);
        GameEvent.On("OrderCompleted", HandleOrderCompleted);
    }

    private void OnDisable()
    {
        GameEvent.Off("OrderCreated", HandleOrderCreated);
        GameEvent.Off("OrderCompleted", HandleOrderCompleted);
    }

    private void Start()
    {
        if (orderPanel == null && npcText != null)
            orderPanel = npcText.transform.parent.gameObject;
        HidePanel();
        HideTicket();
    }

    private void HandleOrderCreated(object payload)
    {
        if (payload is not OrderTicket order) { HidePanel(); return; }

        ShowPanel();
        ShowTicket(order.TicketSprite);
        SetTicketLabel(order.CoffeeName);

        if (order.AcceptAnyCoffee)
            SetTicketDetailsAnyCoffee();
        else
            SetTicketDetails(order.CoffeeId);

        if (npcText != null) npcText.text = $"Customer: {order.NpcName}";
        if (coffeeText != null) coffeeText.text = $"Order: {order.CoffeeName}";
        if (stateText != null) stateText.text = "State: WaitingForCoffee";
    }

    private void HandleOrderCompleted(object payload)
    {
        HidePanel();
        HideTicket();
    }

    public void SetStateText(string state)
    {
        if (stateText != null) stateText.text = $"State: {state}";
    }

    private void ShowPanel() { if (orderPanel != null) orderPanel.SetActive(true); }
    private void HidePanel() { if (orderPanel != null) orderPanel.SetActive(false); }

    private void ShowTicket(Sprite ticket)
    {
        if (orderTicketImage != null) orderTicketImage.SetActive(true);
        if (ticketImage != null && ticket != null)
        {
            ticketImage.sprite = ticket;
            ticketImage.preserveAspect = true;
        }
    }

    private void SetTicketLabel(string coffeeName)
    {
        if (ticketCoffeeLabel != null) ticketCoffeeLabel.text = coffeeName;
    }

    private void SetTicketDetailsAnyCoffee()
    {
        if (ticketStepsText != null) ticketStepsText.text = "随意制作即可";
        if (ticketPriceText != null) ticketPriceText.text = "";
        if (ticketMaterialsText != null) ticketMaterialsText.text = "";
    }

    private void SetTicketDetails(string coffeeId)
    {
        if (CoffeeDataLoader.Instance == null) return;
        var coffee = CoffeeDataLoader.Instance.GetCoffee(coffeeId);
        if (coffee == null) return;

        // 制作流程
        if (ticketStepsText != null)
        {
            var sb = new StringBuilder();
            if (coffee.steps != null)
            {
                for (int i = 0; i < coffee.steps.Count; i++)
                {
                    var step = coffee.steps[i];
                    var name = !string.IsNullOrEmpty(step.displayName) ? step.displayName : step.id;
                    if (i > 0) sb.AppendLine();
                    sb.Append($"{i + 1}. {name}");
                }
            }
            ticketStepsText.text = sb.ToString();
        }

        // 价格
        if (ticketPriceText != null)
            ticketPriceText.text = $"{coffee.sellPrice} C";

        // 消耗材料
        if (ticketMaterialsText != null)
        {
            var sb = new StringBuilder();
            if (coffee.recipe != null)
            {
                for (int i = 0; i < coffee.recipe.Count; i++)
                {
                    var entry = coffee.recipe[i];
                    var displayName = entry.resourceId;
                    if (ResourceDataLoader.Instance != null)
                    {
                        var res = ResourceDataLoader.Instance.GetAllResources();
                        if (res != null)
                        {
                            foreach (var r in res)
                            {
                                if (r.id == entry.resourceId) { displayName = r.displayName; break; }
                            }
                        }
                    }
                    if (i > 0) sb.AppendLine();
                    sb.Append($"{displayName} ×{entry.amount}");
                }
            }
            ticketMaterialsText.text = sb.ToString();
        }
    }

    private void HideTicket()
    {
        if (orderTicketImage != null) orderTicketImage.SetActive(false);
    }
}
