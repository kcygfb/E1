using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] private float fallbackDisappearDuration = 0.35f;

        private static bool sceneLoadHooked;
        private readonly Dictionary<GameObject, Coroutine> _hideRoutines = new();
        private int _lastActionPointCount = -1;
        private int _lastManaPointCount = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSceneLoadHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            sceneLoadHooked = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToPoints()
        {
            EnsureSceneLoadHooked();
            AttachToPointsInScene(SceneManager.GetActiveScene());
        }

        private static void EnsureSceneLoadHooked()
        {
            if (sceneLoadHooked)
                return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneLoadHooked = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachToPointsInScene(scene);
        }

        private static void AttachToPointsInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var points = FindObjectInScene(scene, PointsObjectName);
            if (points != null && points.GetComponent<CombatPointIconUI>() == null)
                points.AddComponent<CombatPointIconUI>();
        }

        private static GameObject FindObjectInScene(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var match = FindChildRecursive(root.transform, objectName);
                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
                return parent;

            for (var index = 0; index < parent.childCount; index++)
            {
                var match = FindChildRecursive(parent.GetChild(index), objectName);
                if (match != null)
                    return match;
            }

            return null;
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

            foreach (var routine in _hideRoutines.Values)
            {
                if (routine != null)
                    StopCoroutine(routine);
            }
            _hideRoutines.Clear();
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
            SetVisibleCount(actionPointItems, state.Player.CurrentActionPoints, ref _lastActionPointCount);
            SetVisibleCount(manaPointItems, state.Mana.Current, ref _lastManaPointCount);
        }

        private void SetVisibleCount(GameObject[] items, int current, ref int previous)
        {
            if (items == null)
                return;

            current = Mathf.Clamp(current, 0, items.Length);
            if (previous < 0)
            {
                SetVisibleCountImmediate(items, current);
                previous = current;
                return;
            }

            for (var index = 0; index < items.Length; index++)
            {
                var item = items[index];
                if (item == null)
                    continue;

                if (index < current)
                {
                    ShowItem(item);
                }
                else if (_hideRoutines.ContainsKey(item))
                {
                    continue;
                }
                else if (current < previous && index >= current && index < previous)
                {
                    PlayDisappearThenHide(item);
                }
                else
                {
                    HideItemImmediate(item);
                }
            }

            previous = current;
        }

        private void SetVisibleCountImmediate(GameObject[] items, int current)
        {
            for (var index = 0; index < items.Length; index++)
            {
                var item = items[index];
                if (item == null)
                    continue;

                if (index < current)
                    ShowItem(item);
                else
                    HideItemImmediate(item);
            }
        }

        private void ShowItem(GameObject item)
        {
            CancelHideRoutine(item);

            if (!item.activeSelf)
                item.SetActive(true);

            ResetAnimatorToFirstFrame(FindAnimator(item));
        }

        private void HideItemImmediate(GameObject item)
        {
            CancelHideRoutine(item);
            item.SetActive(false);
        }

        private void PlayDisappearThenHide(GameObject item)
        {
            CancelHideRoutine(item);

            if (!item.activeSelf)
                item.SetActive(true);

            var animator = FindAnimator(item);
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                item.SetActive(false);
                return;
            }

            _hideRoutines[item] = StartCoroutine(PlayDisappearRoutine(item, animator));
        }

        private IEnumerator PlayDisappearRoutine(GameObject item, Animator animator)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);

            var duration = GetCurrentAnimationDuration(animator);
            if (duration > 0f)
                yield return new WaitForSeconds(duration);
            else
                yield return null;

            if (item != null)
            {
                if (animator != null)
                    animator.enabled = false;
                item.SetActive(false);
                _hideRoutines.Remove(item);
            }
        }

        private void CancelHideRoutine(GameObject item)
        {
            if (item == null)
                return;

            if (_hideRoutines.TryGetValue(item, out var routine))
            {
                if (routine != null)
                    StopCoroutine(routine);
                _hideRoutines.Remove(item);
            }
        }

        private void ResetAnimatorToFirstFrame(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }

        private float GetCurrentAnimationDuration(Animator animator)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length > 0f)
                return stateInfo.length;

            var clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips.Length > 0 && clips[0].clip != null)
                return clips[0].clip.length;

            return fallbackDisappearDuration;
        }

        private static Animator FindAnimator(GameObject item)
        {
            var animator = item.GetComponent<Animator>();
            return animator != null ? animator : item.GetComponentInChildren<Animator>(true);
        }
    }
}
