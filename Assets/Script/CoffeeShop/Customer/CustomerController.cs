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
    /// <summary>按 npcName 注册的活跃顾客，供 DialoguePlayer.AnimateSpeaker 快速查找。</summary>
    public static readonly Dictionary<string, CustomerController> ActiveCustomers = new();

    public NPCData NPCData { get; private set; }
    public CoffeeData CoffeeData { get; private set; }
    public NPCState State { get; private set; }
    public System.Action<CustomerController> OnLeftStore;
    public CustomerQueue Spawner { get; set; }

    [Header("Movement")]
    public float moveSpeed = 500f;
    public float arriveDistance = 1f;

    private Vector3 counterPosition;
    private Vector3 exitPosition;
    private Vector3 targetPosition;

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

    public void Initialize(
        NPCData npcData,
        CoffeeData coffeeData,
        Vector3 counterPosition,
        Vector3 exitPosition
    )
    {
        NPCData = npcData;
        CoffeeData = coffeeData;
        this.counterPosition = counterPosition;
        this.exitPosition = exitPosition;

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
                EmitDialogue(NPCData.startOfDayDialogueId, "startofday");
            }
            else if (!string.IsNullOrEmpty(NPCData.endOfDayDialogueId) && _pendingEndOfDayDialogue)
            {
                _pendingEndOfDayDialogue = false;
                StartEndOfDayDialogue(NPCData.endOfDayDialogueId);
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

    /// <summary>标记 NPC 到达柜台后播放收尾对话（而非正常到达对话）。由 CustomerQueue.TrySpawnNextNpc 调用。</summary>
    public void MarkEndOfDayDialogue()
    {
        _pendingEndOfDayDialogue = true;
    }

    /// <summary>标记 NPC 到达柜台后播放经营前对话。由 CustomerQueue.TrySpawnStartOfDayNpc 调用。</summary>
    public void MarkStartOfDayDialogue()
    {
        _pendingStartOfDayDialogue = true;
    }

    /// <summary>收尾对话：到达柜台后直接播放，不下单。对话结束以 'endofday' context 离开。</summary>
    public void StartEndOfDayDialogue(string dialogueId)
    {
        ChangeState(NPCState.ArrivalDialogue);
        EmitDialogue(dialogueId, "endofday");
    }

    private void StartArrivalDialogue()
    {
        ChangeState(NPCState.ArrivalDialogue);
        GameEvent.Emit("CustomerArrived", NPCData);

        // 鍥炶妫�鏌?
        if (!string.IsNullOrEmpty(NPCData.desiredCoffeeId) && Spawner != null && Spawner.HasPendingReturnVisit(NPCData))
        {
            bool unlocked = CoffeeData != null && (UnlockManager.Instance == null || UnlockManager.Instance.IsUnlocked(CoffeeData));
            if (unlocked)
            {
                GiveReturnReward();
                Spawner.ClearReturnVisit(NPCData);
                EmitDialogue(NPCData.returnFoundDialogueId, "arrival");
            }
            else
            {
                EmitDialogue(NPCData.returnNotFoundDialogueId, "locked_departure");
            }
            return;
        }

        // 棣栨鍒拌锛氱壒娈奛PC鎸囧畾鍜栧暋鏈В閿?
        if (!string.IsNullOrEmpty(NPCData.desiredCoffeeId) && CoffeeData != null && UnlockManager.Instance != null && !UnlockManager.Instance.IsUnlocked(CoffeeData))
        {
            if (Spawner != null) Spawner.MarkReturnVisit(NPCData);
            EmitDialogue(NPCData.lockedDialogueId, "locked_departure");
            return;
        }

        var tokens = new Dictionary<string, string>
        {
            { "coffee", CoffeeData != null ? CoffeeData.coffeeName : "鍜栧暋" }
        };
        EmitDialogue(NPCData.arrivalDialogueId, "arrival", tokens);
    }

    private void EmitDialogue(string dialogueId, string context, Dictionary<string, string> tokens = null)
    {
        GameEvent.Emit("DialogueRequested", new DialogueRequest(dialogueId, context, tokens, NPCData.npcName, NPCData.speakerColor));
    }

    private void OnDialogueEnded(object payload)
    {
        if (payload is not string context) return;

        switch (context)
        {
            case "arrival":
                if (NPCData.willOrder)
                {
                    MakeOrder();
                    ChangeState(NPCState.WaitingForCoffee);
                }
                else
                {
                    StartDepartureDialogue();
                }
                break;

            case "locked_departure":
                StartDepartureDialogue(true);
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

    private void GiveReturnReward()
    {
        if (NPCData.returnReward <= 0) return;
        RuntimeGameRepository.AddGold(NPCData.returnReward);
        Debug.Log($"[CustomerController] {NPCData.npcName} return reward: +{NPCData.returnReward} gold");
    }

    private void MakeOrder()
    {
        GameEvent.Emit("CustomerReadyToOrder", new OrderRequest(this, NPCData, CoffeeData));
    }

    private void OnOrderCompleted(object payload)
    {
        if (payload is not OrderTicket order) return;
        if (order.Owner != this) return;
        StartDepartureDialogue();
    }

    private void StartDepartureDialogue(bool locked = false)
    {
        ChangeState(NPCState.DepartureDialogue);
        string dialogueId = locked && !string.IsNullOrEmpty(NPCData.lockedDepartureDialogueId)
            ? NPCData.lockedDepartureDialogueId
            : NPCData.departureDialogueId;
        EmitDialogue(dialogueId, "departure");
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
