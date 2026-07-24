

## Codely Structured Memories

### User

### Feedback
- [2026-07-22 20:22:53] User had issues with CardInteraction GlowBorder alignment — the root cause was sprite pivot offset and GlowBorder being a sibling vs child object. Final solution: GlowBorder as sibling + LateUpdate position sync. This took many iterations; if touching CardInteraction again, be very careful with GlowBorder positioning logic.
- [2026-07-24 00:26:30] Optimization lessons: (1) NEVER delete scene objects during code cleanup — git reset restores code but scene .unity may have stale references. (2) git checkout/restore of .unity file requires Unity scene reload to take effect — Unity caches scene in memory. (3) BGM DecompressOnLoad + preloadAudioData=False causes main thread block on first PlayMode after Library cache clear — must set preloadAudioData=True in .meta. (4) NEVER push without explicit user request. **Why:** multiple optimization attempts broke things, git resets caused scene reference loss and stale Unity cache. **How to apply:** only change .meta files for audio import; force reimport after git reset; always reload scene after git checkout of .unity files.

### Project
- [2026-07-22 20:22:39] Unity project E1 (github.com/kcygfb/E1.git) — a card battle + coffee shop game. Three feature branches: feature/Card-Battle (combat), feature/CoffeeShop (cafe sim), dev/maomao (teammate's branch). Master now merges all three as of 2026-07-21.
- [2026-07-22 20:22:43] CoffeeShop architecture (as of 2026-07-18): 8 functional modules under Assets/Script/CoffeeShop/ — CafeFlow, Customer, Dialogue, Order, CoffeeCraft, Inventory, Unlock, Reward. Uses GameEvent (string channel + object payload) for inter-module communication. Dialogue/Coffee/Resource data loaded from JSON in StreamingAssets/.
- [2026-07-22 20:22:46] Card-Battle visual system: CardView/CardDealAnimator/CardDragBridge/BattleCardBridge/BattleEventPresenter under Assets/Script/Combat/Runtime/. Old CombatTemp system fully removed. Card prefab at Assets/Prefabs/Card_Battle.prefab. BattleController needs rulesConfig/playerDefinition/enemyDefinitions/cardDatabase configured in Inspector.
- [2026-07-23 12:49:59] Scene Card.unity has TWO BattleController components: root "BattleController" (the real one, initializes correctly) and "Canvas/PanelSeparator/BattleRoot/HandCardPanel" (duplicate, never initializes). When wiring BattleController references in Inspector, always target the root "BattleController" object, NOT the HandCardPanel one. This caused PlayerAttackFeedback to miss all combat events until fixed.
- [2026-07-23 22:52:14] Battle VFX system (feature/Battle-VFX branch): VFX scripts under Assets/Script/Combat/Runtime/VFX/ — PlayerAttackFeedback (melee state machine: Dashing→Slashing→Returning→Idle, with hitstop/sprite swap/screen shake/radial impact pulse/multi-layer glow slash/DamagePopup/SFX; ranged state machine: Shooting→Idle with muzzle flash/gunshot SFX; PlayRangedSingleShot for multi-shot VFX; pose state machine: SwitchToRangedPose/SwitchToMagicPose/SwitchToMeleePose on hover), EnemyHitFeedbackNew (called by PlayerAttackFeedback.PlayHit(), has squash&stretch + white→red flash), BattleVFXManager (event-driven particle spawner on VFXRoot), SlashVFX (color gradient white→red, direction/flip, multi-layer bloom glow). Magic hand: GameObject is "Magichand" at Canvas/PlayerArea/Magichand (NOT "PlayerPanel" — ConfigureMagicHandUpgradeBridge finds "Magichand" first, falls back to "PlayerPanel"). MagicHandUpgradeBridge has IPointerEnter/ExitHandler for magic pose. MagicHandInteraction (Assets/Script/UI/, namespace KiKs.UI) is independent from CardInteraction — handles hover scale/lift + glow as sibling node (SetAsFirstSibling) + LateUpdate sync. CardInteraction.cs is UNCHANGED from original (do not modify for magic hand). Magichand Image alpha was 0.6, set to 1.0. Multi-shot gun system: CombatEngine.PlaySingleShot/PlayRemainingShots/CancelShooting/IsShooting, CardDealAnimator.OnCardShot event, CardView shows [remaining/total] counter (>=1 shots shown), click=one shot at a time, drag=full play all at once, EndTurn cancels ongoing shooting. Bug fixes: ResolveAttackType falls back to card ID prefix; SwapSprite takes scaleMultiplier param + resets sizeDelta to natural size (originScale non-uniform 5.42/5.85). Inspector fields: meleeSpriteScaleMultiplier, rangedSpriteScaleMultiplier, magicSpriteScaleMultiplier, meleeSpriteOffset, rangedSpriteOffset, magicSpriteOffset. BattleBGM = いらない サカナクション REMIX on BattleBGM GameObject (loop, volume 0.5). SFX: Assets/Audio/Combat/slash_clean.wav + dash.wav + gunshot.wav. VFXRoot child of Canvas (last sibling). Canvas ScreenSpaceOverlay → uGUI Image-based VFX only. 









### Reference

