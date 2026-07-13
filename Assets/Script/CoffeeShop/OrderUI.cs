using TMPro;
using UnityEngine;

public class OrderUI : MonoBehaviour
{
    [Header("Order Panel")]
    [SerializeField] private TMP_Text npcText;
    [SerializeField] private TMP_Text coffeeText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private GameObject orderPanel;

    [Header("Order Ticket Image")]
    [SerializeField] private GameObject orderTicketImage;
    [SerializeField] private UnityEngine.UI.Image ticketImage;
    [SerializeField] private TMP_Text ticketCoffeeLabel;

    private void Awake()
    {
        EventBus.OrderCreated += HandleOrderCreated;
        EventBus.OrderCompleted += HandleOrderCompleted;
    }

    private void OnDestroy()
    {
        EventBus.OrderCreated -= HandleOrderCreated;
        EventBus.OrderCompleted -= HandleOrderCompleted;
    }

    private void Start()
    {
        if (orderPanel == null && npcText != null)
            orderPanel = npcText.transform.parent.gameObject;

        HidePanel();
        HideTicket();
    }

    private void HandleOrderCreated(OrderRuntime order)
    {
        if (order == null)
        {
            HidePanel();
            return;
        }

        ShowPanel();
        ShowTicket(order.orderTicket);
        SetTicketLabel(order.coffeeName);

        if (npcText != null)
            npcText.text = $"Customer: {order.npcName}";

        if (coffeeText != null)
            coffeeText.text = $"Order: {order.coffeeName}";

        if (stateText != null)
            stateText.text = "State: WaitingForCoffee";
    }

    private void HandleOrderCompleted(OrderRuntime order)
    {
        HidePanel();
        HideTicket();
    }

    public void SetStateText(string state)
    {
        if (stateText != null)
            stateText.text = $"State: {state}";
    }

    private void ShowPanel()
    {
        if (orderPanel != null)
            orderPanel.SetActive(true);
    }

    private void HidePanel()
    {
        if (orderPanel != null)
            orderPanel.SetActive(false);
    }

    private void ShowTicket(Sprite ticket)
    {
        if (orderTicketImage != null)
            orderTicketImage.SetActive(true);

        if (ticketImage != null && ticket != null)
        {
            ticketImage.sprite = ticket;
            ticketImage.preserveAspect = true;
        }
    }

    private void SetTicketLabel(string coffeeName)
    {
        if (ticketCoffeeLabel != null)
            ticketCoffeeLabel.text = coffeeName;
    }

    private void HideTicket()
    {
        if (orderTicketImage != null)
            orderTicketImage.SetActive(false);
    }
}
