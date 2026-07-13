using System;
using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    public int dayCount = 1;
    public float dayDuration = 10f;

    [Header("Optional")]
    public NPCSpawner npcSpawner;
    public bool waitUntilAllCustomersFinished = true;

    private float timer;
    private bool dayEndedWaiting;

    public static event Action<int> OnDayEnded;

    private void Start()
    {
        Debug.Log("[TimeSystem] Start()");
        StartDay();
    }

    private void Update()
    {
        if (dayEndedWaiting)
            return;

        timer += Time.deltaTime;

        if (timer >= dayDuration)
        {
            if (waitUntilAllCustomersFinished && npcSpawner != null && !npcSpawner.CanEndDay)
            {
                return;
            }

            EndDay();
        }
    }

    public void StartDay()
    {
        dayEndedWaiting = false;
        timer = 0f;
        Debug.Log($"[TimeSystem] StartDay -> Day {dayCount}");
        EventBus.PublishDayStarted(dayCount);
    }

    public void EndDay()
    {
        dayEndedWaiting = true;
        Debug.Log($"[TimeSystem] EndDay -> Day {dayCount}");
        EventBus.PublishDayEnded(dayCount);
        OnDayEnded?.Invoke(dayCount);

        dayCount++;
    }
}