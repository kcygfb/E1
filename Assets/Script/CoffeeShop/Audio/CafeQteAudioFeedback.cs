using KiKs.Audio;
using UnityEngine;

/// <summary>
/// QTE 局部事件适配器。添加到每种 QTE Prefab 的根对象，并引用同一份 CafeAudioBindings。
/// </summary>
[RequireComponent(typeof(QTEBase))]
[AddComponentMenu("KiKs/Audio/Cafe QTE Audio Feedback")]
public sealed class CafeQteAudioFeedback : MonoBehaviour
{
    [SerializeField] private CafeAudioBindings bindings;

    private QTEBase _qte;

    private void Awake()
    {
        _qte = GetComponent<QTEBase>();
        _qte.OnQTEDone.AddListener(OnQteDone);
    }

    private void OnDestroy()
    {
        if (_qte != null) _qte.OnQTEDone.RemoveListener(OnQteDone);
    }

    private void OnQteDone(QTERating rating)
    {
        if (bindings != null)
            AudioManager.TryPlay(bindings.ResolveQte(rating));
    }
}
