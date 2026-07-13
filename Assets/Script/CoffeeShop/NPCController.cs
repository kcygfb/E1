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
    // 初始化
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
    // 到达目标
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
    // 到达对话
    //==========================

    private void StartArrivalDialogue()
    {
        ChangeState(NPCState.ArrivalDialogue);

        DialogueData dialogue =
            NPCData.arrivalDialogue ?? BuildFallbackDialogue(true);

        var tokens = new Dictionary<string, string>
        {
            { "coffee", CoffeeData != null ? CoffeeData.coffeeName : "咖啡" }
        };

        dialogueManager.StartDialogue(
            dialogue,
            onTrigger: (trigger) =>
            {
                if (trigger == DialogueTrigger.CreateOrder)
                    MakeOrder();
            },
            onComplete: () =>
            {
                ChangeState(NPCState.WaitingForCoffee);
            },
            tokens,
            NPCData.npcName
        );
    }


    //==========================
    // 点单（由对话 trigger 调用）
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
    // 收到订单完成事件
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
    // 离开对话
    //==========================

    private void StartDepartureDialogue()
    {
        ChangeState(NPCState.DepartureDialogue);

        DialogueData dialogue =
            NPCData.departureDialogue ?? BuildFallbackDialogue(false);

        dialogueManager.StartDialogue(
            dialogue,
            onTrigger: null,
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
    // 兜底对话（无 DialogueData 时）
    //==========================

    private DialogueData BuildFallbackDialogue(bool withOrderTrigger)
    {
        var data = ScriptableObject.CreateInstance<DialogueData>();

        data.lines = new DialogueLine[]
        {
            new DialogueLine
            {
                speaker = NPCData.npcName,

                text = "1",

                trigger = withOrderTrigger
                    ? DialogueTrigger.CreateOrder
                    : DialogueTrigger.None
            }
        };

        return data;
    }


    //==========================
    // 离开完成
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
    // 状态改变
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
    // 创建头顶文字
    //==========================

    [Header("Head Text Settings")]
    public float headTextOffsetY = 120f;

    public int headTextFontSize = 24;

    private void CreateHeadText()
    {
        GameObject textObject =
            new GameObject(
                "NPC_Status_Text",
                typeof(RectTransform),
                typeof(Text)
            );

        textObject.transform.SetParent(
            transform,
            false
        );

        statusText =
            textObject.GetComponent<Text>();

        RefreshHeadText();
    }

    public void RefreshHeadText()
    {
        if (statusText == null)
            return;

        var rt = statusText.GetComponent<RectTransform>();
        rt.anchoredPosition =
            new Vector2(0, headTextOffsetY);

        rt.sizeDelta = new Vector2(300, 100);

        statusText.fontSize = headTextFontSize;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;

        if (statusText.font == null)
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        UpdateHeadText();
    }


    //==========================
    // 更新文字
    //==========================

    private void UpdateHeadText()
    {
        if (statusText == null)
            return;

        if (NPCData == null)
            return;

        statusText.text = State.ToString();
    }
}
