using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// Keeps the Points panel in sync with the player's current action and magic points.
    /// Direct children named ACT* and MGC* are shown from left to right while points remain.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatPointIconUI : MonoBehaviour
    {
        private const string PointsObjectName = "Points";
        private const string ActionPointPrefix = "ACT";
        private const string ManaPointPrefix = "MGC";

        [SerializeField] private BattleController battleController;
        [SerializeField] private GameObject[] actionPointItems;
        [SerializeField] private GameObject[] manaPointItems;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToPoints()
        {
            var points = GameObject.Find(PointsObjectName);
            if (points != null && points.GetComponent<CombatPointIconUI>() == null)
                points.AddComponent<CombatPointIconUI>();
        }

        private IEnumerator Start()
        {
            CachePointItems();

            if (battleController == null)
            {
                var controllerObject = GameObject.Find("BattleController");
                if (controllerObject != null)
                    battleController = controllerObject.GetComponent<BattleController>();
            }
            if (battleController == null)
                yield break;

            battleController.CombatEventRaised += OnCombatEvent;
            while (!battleController.IsInitialized)
                yield return null;

            RefreshIcons();
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Type == CombatEventType.ActionPointsChanged ||
                combatEvent.Type == CombatEventType.ManaChanged)
            {
                RefreshIcons();
            }
        }

        private void CachePointItems()
        {
            if (actionPointItems == null || actionPointItems.Length == 0)
                actionPointItems = FindDirectChildren(ActionPointPrefix);
            if (manaPointItems == null || manaPointItems.Length == 0)
                manaPointItems = FindDirectChildren(ManaPointPrefix);
        }

        private GameObject[] FindDirectChildren(string prefix)
        {
            var items = new List<GameObject>();
            for (var index = 0; index < transform.childCount; index++)
            {
                var child = transform.GetChild(index);
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                    items.Add(child.gameObject);
            }

            items.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return items.ToArray();
        }

        private void RefreshIcons()
        {
            if (battleController == null || !battleController.IsInitialized)
                return;

            var state = battleController.State;
            SetVisibleCount(actionPointItems, state.Player.CurrentActionPoints);
            SetVisibleCount(manaPointItems, state.Mana.Current);
        }

        private static void SetVisibleCount(GameObject[] items, int current)
        {
            if (items == null)
                return;

            for (var index = 0; index < items.Length; index++)
            {
                if (items[index] != null)
                    items[index].SetActive(index < current);
            }
        }
    }
}
