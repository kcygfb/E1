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

    private void HideTicket()
    {
        if (orderTicketImage != null) orderTicketImage.SetActive(false);
    }
}
