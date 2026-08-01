# 战斗动画手动实现指南 — 从零开始

> 本指南以 PlayerPortrait（近战攻击）为例，手把手教你用 Unity Animation 系统替代 DOTween 代码动画。

---

## 第一部分：理解三个核心概念

### 1. Animation Clip（动画片段）
- 就是一段**录像**，记录了某个物体在一段时间内的属性变化
- 比如：0秒时在原位 → 0.07秒时冲到敌人面前 → 0.12秒时切换攻击图 → 0.25秒时回到原位
- 每个时间点的值叫**关键帧（Keyframe）**，Unity 会在关键帧之间自动插值（平滑过渡）
- 文件后缀 `.anim`，存在 Assets 目录里

### 2. Animator Controller（动画控制器）
- 就是**状态机**，决定"现在播哪个动画、什么时候切换到另一个"
- 比如默认状态是 Idle，收到 "Attack" 信号后切换到 MeleeAttack 动画
- 播完后自动回到 Idle
- 文件后缀 `.controller`，存在 Assets 目录里

### 3. Animation Event（动画事件）
- 在动画的**某一帧**上插一个标记，到达这帧时自动调用你 C# 脚本里的一个方法
- 比如在第 5 帧插一个事件，调用 `OnMeleeHitFrame()`，在这个方法里：
  - 触发伤害计算
  - 播放刀光特效
  - 播放音效
  - 震屏
- 这样**时机完全由动画决定**，不用在代码里硬写 `yield return new WaitForSeconds(0.12f)`

### 对比当前做法

```
当前（代码驱动）：
  代码 → DOTween.Sequence → 移动 → WaitForSeconds(0.12) → 切图 → 卡帧 → 回归
  缺点：时序写死在代码里，改时间要重新编译，无法预览

正规（动画驱动）：
  代码 → Animator.SetTrigger("Attack") → Animator 播放 Clip → 第X帧自动触发 Event → 代码处理逻辑
  优点：时序在 Animation 窗口里拖拽调整，所见即所得，不用改代码
```

---

## 第二部分：准备工作

### Step 1：给 PlayerPortrait 添加 Animator 组件

1. 在 Hierarchy 窗口选中 `PlayerPortrait`
2. Inspector 面板最下方点 `Add Component`
3. 搜索 `Animator`，添加它
4. 此时 Animator 的 `Controller` 字段是空的（None），先不管

### Step 2：打开 Animation 和 Animator 窗口

1. 顶部菜单 `Window` → `Animation` → `Animation`（快捷键 Ctrl+6）
2. 顶部菜单 `Window` → `Animation` → `Animator`
3. 把两个窗口拖到你觉得方便的位置（建议 Animation 放下面，Animator 放旁边）

### Step 3：创建存放动画资源的文件夹

1. 在 Project 窗口，Assets 目录下右键 → `Create` → `Folder`
2. 命名为 `Animation`
3. 在 Animation 文件夹里再创建一个子文件夹 `Player`

---

## 第三部分：创建近战攻击动画 Clip

### Step 4：创建第一个 Animation Clip

1. 在 Hierarchy 中选中 `PlayerPortrait`（必须选中带 Animator 的物体）
2. 在 Animation 窗口里，你会看到一个红色圆点按钮（录制键）和文字提示
3. 点击 `Create` 按钮
4. 弹出保存对话框，保存到 `Assets/Animation/Player/` 下，命名为 `PlayerMeleeAttack.anim`
5. 保存后，Animation 窗口变成时间轴编辑界面

> 此时 Animator 窗口会自动出现一个状态：`PlayerMeleeAttack`（橙色 = 默认状态）

### Step 5：理解 Animation 窗口布局

```
┌─────────────────────────────────────────────────┐
│  [PlayerPortrait]  [▼ PlayerMeleeAttack]  [●录制] │  ← 左上角显示当前物体和clip
├─────────────────────────────────────────────────┤
│                                                   │
│  时间轴：0:00 ─── 0:05 ─── 0:10 ─── 0:15 ─── 0:20 │  ← 横轴是时间
│                                                   │
│  ◆ RectTransform.anchoredPosition  ←─── 属性轨道   │
│  ◆ RectTransform.localScale                          │
│  ◆ Image.sprite                                      │
│                                                   │
└─────────────────────────────────────────────────┘
```

- 左边是**属性列表**，显示这个 clip 记录了哪些属性的变化
- 右边是**时间轴**，横轴是时间（秒），每行轨道对应一个属性
- 菱形 ◆ 就是**关键帧**
- 红色圆点 ● 是**录制按钮**：按下后，你改物体属性的任何操作都会被记录成关键帧

### Step 6：录制近战攻击动画

> 先确认 PlayerPortrait 的初始位置和缩放，记下来（后面要回到这个状态）。
> 当前值：anchoredPosition = (-83, -173)，localScale = (5.42, 5.85, 0.91)

#### 录制步骤：

**准备：先添加默认帧（0:00）**

1. 确保时间轴在 `0:00`（点击最左边）
2. 点击红色 ● 录制按钮（变亮 = 正在录制）
3. 在 Inspector 中，**不要改任何值**，只需点击一下 PlayerPortrait 的 RectTransform
   - 或者：在 Animation 窗口，对 RectTransform.anchoredPosition 点击添加关键帧的按钮
   - 这样会在 0:00 生成一个关键帧，记录当前值
4. 同理，添加 RectTransform.localScale 的关键帧
5. 添加 Image.sprite 的关键帧（记录当前 sprite = 角色.png）

**录制冲刺（0:00 → 0:07）**

6. 把时间轴指针拖到 `0:07`（在横轴上点击 0:07 的位置，或直接输入）
7. 在 Inspector 中，把 PlayerPortrait 的 `anchoredPosition.x` 改成冲刺目标值
   - 当前是 -83，你想让他冲到哪？比如冲到敌人附近，可以试着改成 `-83 + 400 = 317` 或更合适的值
   - 暂时先填一个明显的值，后面可以微调
8. 这时 Animation 窗口的 anchoredPosition 轨道上会自动出现第二个 ◆

**录制切换攻击图（0:07）**

9. 时间轴仍在 `0:07`
10. 在 Inspector 中，把 Image 的 `Sprite` 字段从 `角色.png` 换成 `斩击.png`
    - 这时 Image.sprite 轨道上会出现关键帧
    - 画面上 PlayerPortrait 立刻变成斩击图
11. 如果斩击图大小不对，在 Inspector 中调整 RectTransform.localScale（比如改成 7.0, 7.0, 1.0）
    - 这会生成 localScale 的新关键帧

> 注意：切图后大小可能不对，因为不同 sprite 的分辨率不同。之前代码里用 SwapSprite 做面积等比缩放。
> 在动画里手动调 scale 即可。可以一边调一边看 Game 视图预览效果。

**录制攻击停留（0:07 → 0:12）**

12. 把时间轴指针拖到 `0:12`
13. 不需要改任何属性（保持斩击状态），只需添加一个关键帧标记
    - 对 anchoredPosition 和 localScale 各点一下添加关键帧按钮
    - 这样能保证从 0:07 到 0:12 这段保持不动（否则可能被插值算法影响）

**录制回到原图（0:12）**

14. 时间轴仍在 `0:12`
15. 把 Image 的 `Sprite` 换回 `角色.png`
16. 把 RectTransform.localScale 改回原始值 (5.42, 5.85, 0.91)
17. 这时 sprite 和 scale 轨道上会出现新的关键帧

**录制回到原位（0:12 → 0:25）**

18. 把时间轴指针拖到 `0:25`（即 0.25 秒）
19. 把 anchoredPosition 改回原始值 (-83, -173)

**完成录制**

20. 再次点击红色 ● 按钮，停止录制
21. 点 Animation 窗口左上角的播放按钮 ▶ 预览效果
22. 在 Game 视图中能看到：冲刺 → 切斩击图 → 停顿 → 切回原图 → 回原位

### Step 7：调整动画曲线（缓动）

默认关键帧之间是线性插值（匀速）。冲刺应该先快后慢（减速），回程应该有弹性。

1. 在 Animation 窗口，点击某个关键帧 ◆
2. 关键帧会高亮，底部出现曲线编辑器
3. 或者：选中两个关键帧之间的线段，右键 → 切线类型
   - `Ease In` = 开始慢后快
   - `Ease Out` = 开始快后慢（冲刺用这个）
   - `Ease In Out` = 两头慢中间快
4. 也可以在 Inspector 里直接调每个关键帧的 `In Tangent` / `Out Tangent`

> 更直观的方法：
> 1. 在 Animation 窗口底部，从 `Dopesheet` 模式切换到 `Curves` 模式
> 2. 会看到完整的曲线图，直接拖拽手柄调整曲线形状
> 3. 像在 Photoshop 里调贝塞尔曲线一样

### Step 8：添加 Animation Event（关键！）

这是连接动画和代码的桥梁。

1. 在 Animation 窗口，把时间轴指针移到你想要触发"命中判定"的那一帧
   - 对于近战，应该是斩击图刚切出来的那帧，比如 `0:08`
2. 在 Animation 窗口上方，找到 **"Add Event"** 按钮（一个白色小旗子图标）
   - 或者在时间轴上方右键 → `Add Animation Event`
3. 点击后，时间轴上会出现一个白色竖线标记（事件标记）
4. 点击这个事件标记，Inspector 会显示 Animation Event 设置
5. 在 `Function` 下拉菜单中选择你想调用的方法名（需要脚本中先定义对应方法）
6. 可以设置参数（int / float / string / object 等）

> 注意：Function 下拉菜单只显示**挂在同一个 GameObject 上**的脚本中的**公开方法**
> 所以我们需要在 PlayerAttackFeedback.cs 里添加新的公开方法（后面 Step 12 会做）

### Step 9：保存并预览

1. Ctrl+S 保存场景
2. 在 Animation 窗口点播放 ▶ 预览动画
3. 如果事件能正确触发（需要代码已修改），Console 里会打印日志

---

## 第四部分：配置 Animator Controller

### Step 10：创建 Animator Controller

> 如果你通过 Animation 窗口 Create 了 clip，Unity 可能已经自动创建了一个 Animator Controller 挂在 PlayerPortrait 上。检查一下：
> - 选中 PlayerPortrait → Inspector → Animator → Controller 字段
> - 如果是 None，手动创建：
>   1. Project 窗口右键 → `Create` → `Animator Controller`
>   2. 命名为 `PlayerBattleAnimator`，保存到 `Assets/Animation/Player/`
>   3. 把它拖到 PlayerPortrait 的 Animator.Controller 字段

### Step 11：设置状态和转换

打开 Animator 窗口，你会看到已有一个状态。我们需要设置：

#### 11.1 创建参数（Parameters）

1. 在 Animator 窗口左侧，找到 `Parameters` 面板
2. 点 `+` 按钮：
   - 添加一个 **Trigger** 类型，命名为 `Attack`
   - 添加一个 **Int** 类型，命名为 `AttackType`
     - 0 = 近战, 1 = 远程, 2 = 魔法

#### 11.2 创建状态

1. 在 Animator 窗口空白处右键 → `Create State` → `Empty`
2. 命名为 `Idle`（默认待机状态）
3. 把它的 Motion 设为 None（或者创建一个空的 Idle clip）
4. 你应该已经有 `PlayerMeleeAttack` 状态（从创建 clip 时自动来的）

> 如果没有自动创建：
> 1. 右键 → `Create State` → `From Animation Clip`
> 2. 选择 `PlayerMeleeAttack.anim`
> 3. 状态自动以 clip 名命名

#### 11.3 设置默认状态

1. 右键点击 `Idle` 状态 → `Set as Layer Default State`
2. Idle 会变成橙色（表示这是默认进入的状态）

#### 11.4 创建状态转换

**Idle → MeleeAttack：**
1. 右键 `Idle` → `Make Transition`
2. 拖到 `PlayerMeleeAttack` 状态上
3. 点击这个 Transition（箭头），在 Inspector 里：
   - 取消勾选 `Has Exit Time`（不等待动画播完才切，立即响应）
   - 添加条件：`+` → 选 `Attack`（Trigger）
   - 再添加条件：`+` → 选 `AttackType` → `Equals` → `0`
   - 设置 `Transition Duration` = 0（立即切换，不要混合）

**MeleeAttack → Idle（播完自动回）：**
1. 右键 `PlayerMeleeAttack` → `Make Transition`
2. 拖到 `Idle`
3. 点击这个 Transition：
   - **勾选** `Has Exit Time`（等动画播完才切回 Idle）
   - `Exit Time` = 1.0（即播放到 100% 时切回）
   - `Transition Duration` = 0.1（短暂混合，看起来自然）

#### 11.5 最终状态图

```
  ┌──Attack(Trigger) + AttackType==0──→ ┐
Idle                                     PlayerMeleeAttack
  └←──Has Exit Time (1.0)──────────────┘
```

之后加入远程和魔法后：

```
  ┌──Attack + AttackType==0──→ PlayerMeleeAttack ──→ Idle
  │
Idle──Attack + AttackType==1──→ PlayerRangedAttack ──→ Idle
  │
  └──Attack + AttackType==2──→ PlayerMagicAttack  ──→ Idle
```

---

## 第五部分：修改代码

### Step 12：给 PlayerAttackFeedback.cs 添加 Animation Event 方法

在 `PlayerAttackFeedback.cs` 里，**保留现有的 DOTween 逻辑作为 fallback**，新增以下公开方法供 Animation Event 调用：

```csharp
// ======== Animation Event 方法（在 Animation 窗口里选择这些方法名）========

/// <summary>近战动画到达命中帧时调用（Animation Event）</summary>
public void OnMeleeHitFrame()
{
    // 在这里做：
    // 1. 触发敌人受击特效
    TriggerEnemyHit();
    
    // 2. 播放刀光
    SpawnSlash();
    
    // 3. 播放命中音效
    PlaySfx(hitSfx);
    
    // 4. 屏震
    ShakeCanvas();
    
    // 5. 如果是强化攻击，闪屏
    if (_currentAttackUpgraded) FlashScreen();
}

/// <summary>远程动画到达射击帧时调用（Animation Event）</summary>
public void OnRangedShootFrame()
{
    // 1. 枪口闪光
    SpawnMuzzleFlash();
    
    // 2. 敌人受击
    TriggerEnemyHit();
    
    // 3. 音效
    PlaySfx(rangedSfx);
    
    // 4. 屏震
    ShakeCanvas();
    
    // 5. 强化闪屏
    if (_currentAttackUpgraded) FlashScreen();
}

/// <summary>魔法动画到达释放帧时调用（Animation Event）</summary>
public void OnMagicCastFrame()
{
    // 1. 魔法火特效
    SpawnMagicFire();
    
    // 2. 敌人受击
    TriggerEnemyHit();
    
    // 3. 屏震
    ShakeCanvas();
}

/// <summary>攻击动画结束时调用（Animation Event，放在最后一帧）</summary>
public void OnAttackAnimationEnd()
{
    _rect.anchoredPosition = _originPos;
    _rect.localScale = _originScale;
    _state = AttackState.Idle;
    _attackRoutine = null;
}
```

### Step 13：修改 PlayAttack 方法

```csharp
public void PlayAttack(int attackType = 0, bool isUpgraded = false)
{
    _currentAttackUpgraded = isUpgraded;

    // === Animator 路径 ===
    if (_useAnimator)
    {
        if (!string.IsNullOrEmpty(attackTypeParam))
            animator.SetInteger(attackTypeParam, attackType);
        animator.SetTrigger(attackTrigger);
        return;  // ← 走 Animator，不再走 DOTween
    }

    // === DOTween fallback（和原来一样，Animator 没配置时用）===
    if (attackType == 0 && meleeDashTarget != null)
    {
        StopAttack();
        _attackRoutine = StartCoroutine(MeleeAttackRoutine());
        return;
    }
    // ... 远程、魔法 fallback 不变
}
```

> 这样设计的好处：
> - Animator 配好了 → 自动走 Animation，所见即所得
> - Animator 没配 → 退回原来的 DOTween 代码，不影响现有功能
> - 你可以**一个攻击类型一个攻击类型地切换**，近战先用 Animation，远程还用 DOTween

---

## 第六部分：完整操作流程（回顾）

### 你在 Unity Editor 里要做的事（按顺序）

```
1. 选中 PlayerPortrait → Add Component → Animator
2. Window > Animation > Animation（打开 Animation 窗口）
3. Window > Animation > Animator（打开 Animator 窗口）
4. 在 Animation 窗口 Create → 保存 PlayerMeleeAttack.anim
5. 录制关键帧（位置、缩放、Sprite 切换）
6. 调整动画曲线（缓动效果）
7. 在命中帧添加 Animation Event
8. 在 Animator 窗口设置 Idle 状态、参数、转换条件
9. 在代码里添加 Event 方法
10. 把 Animator Controller 拖到 PlayerPortrait 的 Animator.Controller
11. 选中 PlayerPortrait → Inspector → PlayerAttackFeedback → Animator 字段拖入 Animator 组件
12. 进入 PlayMode 测试
```

### 之后的扩展

近战做好后，远程和魔法重复 Step 4-9 的流程：

```
- PlayerRangedAttack.anim → 录制射击姿势 + 枪口位置 → Event: OnRangedShootFrame
- PlayerMagicAttack.anim → 录制魔法预备 + 释放 → Event: OnMagicCastFrame
```

Animator 窗口里各加一个状态和两组转换，参数都是 AttackType == 1 / 2。

---

## 常见问题

### Q: 为什么我的 Animation Event 下拉菜单里看不到方法？
A: 方法必须满足：
1. 是 `public` 的
2. 在**挂在同一个 GameObject 上**的脚本里
3. 返回 `void`
4. 参数只能是：无参数 / int / float / string / GameObject / Transform / AnimationEvent / Object

### Q: 动画播放后位置/缩放没恢复？
A: 在动画最后一帧加一个 Animation Event 调用 `OnAttackAnimationEnd()`，或者确保最后一帧的关键帧值 = 初始值。

### Q: Sprite 切换后大小变了？
A: 每个 sprite 分辨率不同。在关键帧里手动调整 localScale。建议用 Animation 窗口的 Curves 模式直观调。

### Q: Hitstop（卡帧）怎么实现？
A: 在 Animation Event 方法里加：
```csharp
public void OnMeleeHitFrame()
{
    StartCoroutine(HitstopRoutine(0.12f));
    // ... 其他逻辑
}

private IEnumerator HitstopRoutine(float duration)
{
    animator.speed = 0f;  // 只暂停动画，不暂停全局
    yield return new WaitForSecondsRealtime(duration);
    animator.speed = 1f;
}
```
这样只冻这个物体的动画，不影响其他系统。比 `Time.timeScale = 0` 安全得多。

### Q: 能不能不录动画，全部用代码触发 Animator 参数？
A: 可以。如果你有现成的角色动画素材（比如 Spine 导出的），直接用 Animator 控制播放即可。Animation 窗口录制适合"没有现成动画素材，用 sprite 拼出效果"的情况。
