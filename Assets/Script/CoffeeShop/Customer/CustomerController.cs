using System.Collections.Generic;
using UnityEngine;
using KiKs.Combat;
using UnityEngine.UI;

public enum NPCState
{
    MovingToCounter,
    ArrivalDialogue,
    WaitingForCoffee,
    DepartureDialogue,
    Leaving
}

public class CustomerController : MonoBehaviour
{
    public static readonly Dictionary<string, CustomerController> ActiveCustomers = new();

    public NPCData NPCData { get; private set; }
    public CoffeeData CoffeeData { get; private set; }
    public NPCEntry Entry { get; private set; }
    public NPCState State { get; private set; }
    public bool AcceptAny { get; set; }
    public System.Action<CustomerController> OnLeftStore;
    public CustomerQueue Spawner { get; set; }

    public bool MatchesSpeaker(string speaker)
    {
        if (string.IsNullOrEmpty(speaker) || NPCData == null) return false;
        if (NPCData.npcName == speaker ||
            NPCData.npcName.Contains(speaker) ||
            speaker.Contains(NPCData.npcName))
            return true;

        if (NPCData.speakerAliases != null)
        {
            foreach (var alias in NPCData.speakerAliases)
            {
                if (string.IsNullOrEmpty(alias)) continue;
                if (alias == speaker || alias.Contains(speaker) || speaker.Contains(alias))
                    return true;
            }
        }
        return false;
    }

    [Header("Movement")]
    public float moveSpeed = 500f;
    public float arriveDistance = 1f;

    private Vector3 counterPosition;
    private Vector3 exitPosition;
    private Vector3 targetPosition;
    private bool _isCancelled;

    private void OnEnable()
    {
        GameEvent.On("OrderCompleted", OnOrderCompleted);
        GameEvent.On("DialogueEnded", OnDialogueEnded);
    }

    private void OnDisable()
    {
        GameEvent.Off("OrderCompleted", OnOrderCompleted);
        GameEvent.Off("DialogueEnded", OnDialogueEnded);
    }

    private void OnDestroy()
    {
        var npcName = NPCData != null ? NPCData.npcName : null;
        if (!string.IsNullOrEmpty(npcName) &&
            ActiveCustomers.TryGetValue(npcName, out var activeCustomer) &&
            activeCustomer == this)
        {
            ActiveCustomers.Remove(npcName);
        }
    }

    public void Initialize(
        NPCData npcData,
        NPCEntry entry,
        CoffeeData coffeeData,
        Vector3 counterPosition,
        Vector3 exitPosition
    )
    {
        NPCData = npcData;
        Entry = entry;
        CoffeeData = coffeeData;
        this.counterPosition = counterPosition;
        this.exitPosition = exitPosition;

        if (entry != null)
        {
            AcceptAny = entry.orderMode == OrderMode.AcceptAny;
        }

        if (!string.IsNullOrEmpty(npcData.npcName))
            ActiveCustomers[npcData.npcName] = this;

        transform.position = new Vector3(transform.position.x, counterPosition.y, transform.position.z);
        targetPosition = counterPosition;
        ChangeState(NPCState.MovingToCounter);
    }

    private void Update()
    {
        if (State == NPCState.MovingToCounter || State == NPCState.Leaving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
                Arrived();
        }
    }

    private void Arrived()
    {
        if (State == NPCState.MovingToCounter)
        {
            if (_pendingStartOfDayDialogue)
            {
                _pendingStartOfDayDialogue = false;
                ChangeState(NPCState.ArrivalDialogue);
                EmitDialogue(Entry?.GetDialogueId("startofday"), "startofday");
            }
            else if (_pendingEndOfDayDialogue)
            {
                _pendingEndOfDayDialogue = false;
                ChangeState(NPCState.ArrivalDialogue);
                EmitDialogue(Entry?.GetDialogueId("endofday"), "endofday");
            }
            else
            {
                StartArrivalDialogue();
            }
        }
        else if (State == NPCState.Leaving)
            LeaveFinished();
    }

    private bool _pendingEndOfDayDialogue;
    private bool _pendingStartOfDayDialogue;

    public void MarkEndOfDayDialogue()
    {
        _pendingEndOfDayDialogue = true;
    }

    public void MarkStartOfDayDialogue()
    {
        _pendingStartOfDayDialogue = true;
    }

    private void StartArrivalDialogue()
    {
        ChangeState(NPCState.ArrivalDialogue);
        GameEvent.Emit("CustomerArrived", NPCData);

        var tokens = new Dictionary<string, string>
        {
            { "coffee", CoffeeData != null ? CoffeeData.coffeeName : (AcceptAny ? "任意咖啡" : "咖啡") }
        };
        EmitDialogue(Entry?.GetDialogueId("arrival"), "arrival", tokens);
    }

    private void EmitDialogue(string dialogueId, string context, Dictionary<string, string> tokens = null)
    {
        GameEvent.Emit("DialogueRequested", new DialogueRequest(dialogueId, context, tokens, NPCData.npcName, NPCData.speakerColor));
    }

    private void OnDialogueEnded(object payload)
    {
        if (_isCancelled) return;
        if (payload is not string context) return;

        switch (context)
        {
            case "arrival":
                // 有 orderMode 就下单（SpecificCoffee/RandomUnlocked/AcceptAny 都下单）
                if (Entry != null && Entry.orderMode != OrderMode.AcceptAny || Entry == null)
                {
                    if (CoffeeData != null || AcceptAny)
                    {
                        MakeOrder();
                        ChangeState(NPCState.WaitingForCoffee);
                    }
                    else
                    {
                        StartDepartureDialogue();
                    }
                }
                else if (Entry != null && Entry.orderMode == OrderMode.AcceptAny)
                {
                    // AcceptAny 也下单
                    MakeOrder();
                    ChangeState(NPCState.WaitingForCoffee);
                }
                else
                {
                    StartDepartureDialogue();
                }
                break;

            case "locked_departure":
                StartDepartureDialogue();
                break;

            case "departure":
                ChangeState(NPCState.Leaving);
                targetPosition = exitPosition;
                break;

            case "endofday":
                ChangeState(NPCState.Leaving);
                targetPosition = exitPosition;
                break;

            case "startofday":
                ChangeState(NPCState.Leaving);
                targetPosition = exitPosition;
                break;
        }
    }

    private void MakeOrder()
    {
        GameEvent.Emit("CustomerReadyToOrder", new OrderRequest(this, NPCData, CoffeeData));
    }

    private void OnOrderCompleted(object payload)
    {
        if (_isCancelled) return;
        if (payload is not OrderTicket order) return;
        if (order.Owner != this) return;
        StartDepartureDialogue();
    }

    private void StartDepartureDialogue()
    {
        ChangeState(NPCState.DepartureDialogue);
        EmitDialogue(Entry?.GetDialogueId("departure"), "departure");
    }

    /// <summary>Stops this customer's flow and leaves through the normal queue callback.</summary>
    public void CancelAndLeave()
    {
        if (_isCancelled || State == NPCState.Leaving) return;
        _isCancelled = true;
        _pendingStartOfDayDialogue = false;
        _pendingEndOfDayDialogue = false;
        ChangeState(NPCState.Leaving);
        targetPosition = exitPosition;
    }

    private void LeaveFinished()
    {
        Debug.Log(NPCData.npcName + " left store");
        if (NPCData != null && !string.IsNullOrEmpty(NPCData.npcName))
            ActiveCustomers.Remove(NPCData.npcName);
        OnLeftStore?.Invoke(this);
        Destroy(gameObject);
    }

    private void ChangeState(NPCState newState)
    {
        State = newState;
        Debug.Log($"[Customer] {NPCData.npcName} -> {State}");
    }
}
