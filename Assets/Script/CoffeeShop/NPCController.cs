using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum NPCState
{
    MovingToCounter,
    ArrivalDialogue,
    WaitingForCoffee,
    DepartureDialogue,
    Leaving
}


public class NPCController : MonoBehaviour
{
    public NPCData NPCData { get; private set; }

    public CoffeeData CoffeeData { get; private set; }

    public NPCState State { get; private set; }

    public Action<NPCController> OnLeftStore;

    public NPCSpawner Spawner { get; set; }

    [Header("Movement")]
    public float moveSpeed = 500f;

    public float arriveDistance = 1f;

    private OrderSystem orderSystem;

    private DialogueManager dialogueManager;

    private Vector3 counterPosition;

    private Vector3 exitPosition;

    private Vector3 targetPosition;


    //==========================
    // Head UI
    //==========================

    private Text statusText;


    //==========================
    // Init
    //==========================

    public void Initialize(
        NPCData npcData,
        CoffeeData coffeeData,
        OrderSystem orderSystem,
        DialogueManager dialogueManager,
        Vector3 counterPosition,
        Vector3 exitPosition
    )
    {
        NPCData = npcData;

        CoffeeData = coffeeData;

        this.orderSystem = orderSystem;

        this.dialogueManager = dialogueManager;

        this.counterPosition = counterPosition;

        this.exitPosition = exitPosition;

        CreateHeadText();

        transform.position =
            new Vector3(
                transform.position.x,
                counterPosition.y,
                transform.position.z
            );

        targetPosition = counterPosition;

        ChangeState(NPCState.MovingToCounter);
    }


    private void OnEnable()
    {
        EventBus.OrderCompleted += OnOrderCompleted;
    }

    private void OnDisable()
    {
        EventBus.OrderCompleted -= OnOrderCompleted;
    }


    private void Update()
    {
        if (
            State == NPCState.MovingToCounter ||
            State == NPCState.Leaving
        )
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

            if (
                Vector3.Distance(
                    transform.position,
                    targetPosition
                )
                <= arriveDistance
            )
            {
                Arrived();
            }
        }
    }


    //==========================
    // Arrived
    //==========================

    private void Arrived()
    {
        if (State == NPCState.MovingToCounter)
        {
            StartArrivalDialogue();
        }

        else if (State == NPCState.Leaving)
        {
            LeaveFinished();
        }
    }


    //==========================
    // Arrival Dialogue
    //==========================

    private void StartArrivalDialogue()
    {
        ChangeState(NPCState.ArrivalDialogue);

        // 回访检查：之前来过且咖啡是锁的
        if (NPCData.desiredCoffee != null &&
            Spawner != null &&
            Spawner.HasPendingReturnVisit(NPCData))
        {
            bool unlocked = CoffeeUnlockManager.Instance != null &&
                            CoffeeUnlockManager.Instance.IsUnlocked(NPCData.desiredCoffee);

            if (unlocked)
            {
                GiveReturnReward();
                Spawner.ClearReturnVisit(NPCData);

                dialogueManager.StartDialogue(
                    NPCData.returnFoundDialogue,
                    onComplete: () =>
                    {
                        if (NPCData.willOrder)
                        {
                            MakeOrder();
                            ChangeState(NPCState.WaitingForCoffee);
                        }
                        else
                        {
                            StartDepartureDialogue();
                        }
                    },
                    null,
                    NPCData.npcName
                );
            }
            else
            {
                dialogueManager.StartDialogue(
                    NPCData.returnNotFoundDialogue,
                    onComplete: () => StartDepartureDialogue(true),
                    null,
                    NPCData.npcName
                );
            }
            return;
        }

        // 首次到访：特殊NPC指定咖啡未解锁 → 走锁定对话 + 标记回访
        if (NPCData.desiredCoffee != null &&
            CoffeeUnlockManager.Instance != null &&
            !CoffeeUnlockManager.Instance.IsUnlocked(NPCData.desiredCoffee))
        {
            if (Spawner != null)
                Spawner.MarkReturnVisit(NPCData);

            dialogueManager.StartDialogue(
                NPCData.lockedDialogue,
                onComplete: () => StartDepartureDialogue(true),
                null,
                NPCData.npcName
            );
            return;
        }

        var tokens = new Dictionary<string, string>
        {
            { "coffee", CoffeeData != null ? CoffeeData.coffeeName : "咖啡" }
        };

        dialogueManager.StartDialogue(
            NPCData.arrivalDialogue,
            onComplete: () =>
            {
                if (NPCData.willOrder)
                {
                    MakeOrder();
                    ChangeState(NPCState.WaitingForCoffee);
                }
                else
                {
                    StartDepartureDialogue();
                }
            },
            tokens,
            NPCData.npcName
        );
    }

    private void GiveReturnReward()
    {
        if (NPCData.returnReward <= 0) return;

        var inv = KiKs.Core.InventorySystem.Instance;
        if (inv != null)
        {
            inv.Add("gold", NPCData.returnReward);
            Debug.Log($"[NPCController] {NPCData.npcName} return reward: +{NPCData.returnReward} gold");
        }
    }


    //==========================
    // Order (called after arrival dialogue)
    //==========================

    private void MakeOrder()
    {
        orderSystem.CreateOrder(
            this,
            NPCData,
            CoffeeData
        );
    }


    //==========================
    // Order Completed
    //==========================

    private void OnOrderCompleted(OrderRuntime order)
    {
        if (order == null)
            return;

        if (order.Owner != this)
            return;

        StartDepartureDialogue();
    }


    //==========================
    // Departure Dialogue
    //==========================

    private void StartDepartureDialogue(bool locked = false)
    {
        ChangeState(NPCState.DepartureDialogue);

        DialogueData dialogue = locked && NPCData.lockedDepartureDialogue != null
            ? NPCData.lockedDepartureDialogue
            : NPCData.departureDialogue;

        dialogueManager.StartDialogue(
            dialogue,
            onComplete: () =>
            {
                ChangeState(NPCState.Leaving);

                targetPosition = exitPosition;
            },
            null,
            NPCData.npcName
        );
    }


    //==========================
    // Leave Finished
    //==========================

    private void LeaveFinished()
    {
        Debug.Log(
            NPCData.npcName + " left store"
        );

        OnLeftStore?.Invoke(this);

        Destroy(gameObject);
    }


    //==========================
    // State Change
    //==========================

    private void ChangeState(NPCState newState)
    {
        State = newState;

        UpdateHeadText();

        Debug.Log(
            $"[NPC] {NPCData.npcName} -> {State}"
        );
    }


    //==========================
    // Head Text
    //==========================

    [Header("Head Text Settings")]
    public float headTextOffsetY = 120f;

    public int headTextFontSize = 24;

    private void CreateHeadText()
    {
        // Head text disabled
    }

    public void RefreshHeadText()
    {
        // Head text disabled
    }


    private void UpdateHeadText()
    {
        // Head text disabled
    }
}
