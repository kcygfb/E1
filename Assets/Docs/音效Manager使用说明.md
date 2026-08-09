# 音效 Manager 完整使用说明

本文档从零说明项目里的短音效系统。阅读者不需要理解 `AudioManager` 的底层代码，只需要会在 Unity 的 Project 和 Inspector 窗口中创建资产、添加组件、拖入引用。

本文所说的“音效”主要是按钮、抽牌、出牌、刀剑命中、枪声、受击、破韧、治疗和胜负提示等短声音。当前咖啡厅与战斗场景中的长 BGM 仍由原有 BGM 脚本管理，不需要注册到这套短音效系统中。

## 1. 先理解五个对象分别是什么

### 1.1 AudioClip：原始音频文件

`AudioClip` 就是导入 Unity 的 `.wav`、`.mp3`、`.ogg` 等音频文件。例如：

- `sword_hit_01.wav`
- `sword_hit_02.wav`
- `card_magic.wav`
- `button_click.wav`

AudioClip 只代表“这一段录音”，它本身不知道自己是近战命中、按钮点击还是胜利音效，也不知道应该用多大音量、能同时播放多少次。

### 1.2 AudioCue：一个“逻辑声音”的完整定义

`AudioCue` 是本系统中最重要、也是音效注册人员最常操作的资产。

一个 AudioCue 表示一种有明确用途的逻辑声音，例如：

- `SFX_近战命中`
- `SFX_射击出牌`
- `SFX_玩家受击`
- `UI_按钮点击`

它除了保存 AudioClip，还保存音量、随机音高、冷却、最大并发和 3D 设置。因此业务组件只需要引用 AudioCue，不需要自己管理 AudioSource 或理解播放底层。

一个 AudioCue 可以放入多个 AudioClip。每次播放这个 Cue 时，Manager 会从所有非空 Clip 中等概率随机选择一个。

例如 `SFX_近战命中` 中放入：

```text
sword_hit_01.wav
sword_hit_02.wav
sword_hit_03.wav
```

第一次命中可能播放 02，第二次可能播放 01，第三次可能播放 03。开启 `Avoid Immediate Repeat` 后，只要存在两个以上有效 Clip，就不会连续两次选择同一个 Clip。这样连续打击不会像重复播放同一段录音那样机械。

如果 Cue 中只有一个 Clip，每次都会播放这个 Clip；随机音高仍然可以让它每次听起来略有变化。

### 1.3 BattleAudioBindings：战斗事件与 Cue 的映射表

`BattleAudioBindings` 不保存原始音频，也不负责播放。它只回答：

> 战斗中发生某个事件时，应该使用哪个 AudioCue？

例如：

```text
Melee Card Played  → SFX_近战出牌
Ranged Card Played → SFX_射击出牌
Player Hit         → SFX_玩家受击
Victory            → SFX_战斗胜利
```

一份 BattleAudioBindings 通常会引用很多 AudioCue。AudioCue 可以复用，例如同一个 `SFX_通用破韧` 可以同时用于多个战斗场景。

### 1.4 BattleAudioPresenter：监听战斗并发出播放请求

`BattleAudioPresenter` 是挂在战斗场景 GameObject 上的组件。它监听现有 `BattleController.CombatEventRaised`：

1. 战斗规则产生“成功出牌”“受到伤害”“胜利”等事件；
2. Presenter 在 BattleAudioBindings 中找到对应 Cue；
3. Presenter 请求 AudioManager 播放该 Cue。

它不会修改生命值、卡牌或战斗阶段，只负责把战斗事件翻译成音效请求。出牌失败时不会产生成功出牌音效。

### 1.5 AudioManager：真正执行播放的全局服务

`AudioManager` 负责：

- 创建和复用 AudioSource 对象池；
- 从 Cue 的 Clips 中选择音频；
- 执行冷却和并发限制；
- 应用 Cue 音量、总音量和分组音量；
- 在声音数量过多时按优先级决定是否抢占；
- 跨场景保留。

第一次播放 AudioCue 时，如果场景中没有 AudioManager，它会自动创建名为 `[AudioManager]` 的对象，所以普通注册人员不需要手动放置 Manager。

## 2. 一次声音从注册到播放的完整流程

以“玩家成功打出近战牌”为例：

```text
三个 sword_hit AudioClip
        ↓ 放进 Clips
SFX_近战出牌 AudioCue
        ↓ 拖进 Melee Card Played 字段
BattleAudioBindings
        ↓ 拖进 Bindings 字段
BattleAudioPresenter
        ↓ 监听到 CardPlayed，且卡牌 Category 是 melee
AudioManager
        ↓ 检查冷却和并发，随机选择一个 Clip
内部 AudioSource 播放声音
```

这里的“槽位”就是 Inspector 中可以接收资产的对象引用字段，例如 `Clips`、`Melee Card Played`、`Bindings`、`Hover`、`Click` 和 `Cue`。可以把 Project 窗口中的对应资产拖到字段上，也可以点击字段右侧的小圆圈选择资产。

## 3. 第一步：创建 AudioCue

在 Project 窗口中选择适合保存配置的位置，例如：

```text
Assets/Audio/Cues/Combat
Assets/Audio/Cues/UI
```

然后：

1. 在空白处右键；
2. 选择 `Create → KiKs → Audio → Audio Cue`；
3. 把资产改成容易搜索的名字，例如 `SFX_近战命中`；
4. 选中资产，在 Inspector 中配置各个栏目。

## 4. AudioCue Inspector 每个栏目和字段的含义

### 4.1 Registration：这个 Cue 是什么声音

| 字段 | 作用 | 如何填写 |
| --- | --- | --- |
| `Display Name` | 用于阅读和诊断的显示名称。它不参与程序查找，也不要求全局唯一。 | 建议填写中文用途，例如“近战命中”。资产文件名仍建议使用统一命名。 |
| `Clips` | 这个逻辑声音可以播放的原始 AudioClip 列表。 | 设置 `Size` 后，把一个或多个音频文件拖入 Element。空 Element 会被跳过。 |

#### 为什么 Clips 可以放多个？

打击声、脚步声、发牌声如果始终重复同一段录音，很容易产生明显的机械感。把多个相似录音放入同一个 Cue 后，每次播放会随机选择一个变体。

选择规则如下：

- 只从非空 Clip 中随机；
- 每个有效 Clip 的基础概率相同；
- 一个 Clip 时固定播放它；
- 多个 Clip 且开启 `Avoid Immediate Repeat` 时，不连续重复上一次 Clip；
- Clip 的排列顺序不代表优先级。

例如 Clips 的 `Size` 为 3：

```text
Element 0 = hit_light_01.wav
Element 1 = hit_light_02.wav
Element 2 = hit_light_03.wav
```

每次调用 `SFX_轻击` 都会从这三份中随机选择。

### 4.2 Mix：声音如何进入混音与音量系统

| 字段 | 作用 | 推荐用法 |
| --- | --- | --- |
| `Bus` | 选择 Manager 内部的音量分组。当前有 `Sfx` 和 `UI`。 | 战斗、卡牌、环境短音效选 `Sfx`；按钮、标签页等界面声音选 `UI`。 |
| `Output` | 可选的 Unity `AudioMixerGroup` 输出目标。用于把声音送入 AudioMixer 的某个组，以使用 Mixer 音量、效果器、Snapshot 等功能。 | 项目没有配置 AudioMixer 时保持 `None` 即可；这不会导致没声音。 |
| `Volume` | 该 Cue 自身的基础音量，范围 0～1。 | 一般从 1 开始，过响再降低。不要靠把所有声音都调得很小来解决并发堆叠。 |
| `Pitch Range` | 每次播放时随机选择的音高范围。X 是最小值，Y 是最大值。 | 不随机填 `1 / 1`；普通打击可填 `0.95 / 1.05`；差异不宜过大。 |
| `Priority` | 当全局 AudioSource 池已经占满时决定抢占顺序。数值越小越重要，0 最高，256 最低。 | 关键命中或 UI 确认可用 64～96；普通声音保持 128；不重要环境声可更高。 |

#### Bus 与 Output 有什么区别？

`Bus` 是这套 Manager 自己的简单音量分类：

- `Sfx` 响应 `AudioManager.SetSfxVolume`；
- `UI` 响应 `AudioManager.SetUiVolume`。

`Output` 是 Unity 原生 AudioMixer 的路由。两者可以同时使用：Manager 先计算自身音量，然后声音再进入指定 MixerGroup。

如果项目目前没有 AudioMixer：

- `Output` 留为 `None`；
- 仍然可以正常播放；
- `Bus`、Cue Volume 和 Master Volume 仍然生效。

如果以后需要 AudioMixer：

1. 在 Project 右键创建 `Audio Mixer`；
2. 在 Mixer 中建立 `SFX`、`UI` 等 Group；
3. 将对应 Group 从 AudioMixer 窗口拖到 Cue 的 `Output`；
4. 之后可以在 Mixer 上使用压缩器、低通、混响或 Snapshot。

#### Volume 最终是怎么计算的？

普通播放的最终音量大致为：

```text
Cue Volume × 调用处 Volume Scale × Master Volume × Bus Volume
```

例如：

```text
0.8 × 1.0 × 0.9 × 0.7 = 0.504
```

如果还设置了 Output，声音进入 AudioMixer 后还会受到 MixerGroup 音量影响。

### 4.3 Repeated-play protection：处理连击与重复触发

| 字段 | 作用 | 例子 |
| --- | --- | --- |
| `Cooldown` | 同一个 AudioCue 两次成功播放之间的最短间隔，单位为秒。使用不受 `Time.timeScale` 影响的时间。 | 设为 0.05 时，第一次播放后 0.05 秒内再次请求会被忽略。0 表示无冷却。 |
| `Max Simultaneous` | 同一个 Cue 最多允许同时存在多少个正在播放的声音。限制针对整个 Cue，不是每个 Clip。 | 设为 3 时，即使 Cue 有 6 个 Clip，也最多同时播放 3 份。 |
| `Overflow Mode` | 已达到该 Cue 的最大并发时如何处理新请求。 | `Ignore New` 忽略新声音；`Replace Oldest` 停掉最早的一份，播放新声音。 |
| `Avoid Immediate Repeat` | 多 Clip 随机时避免连续两次选择相同 Clip。 | 打击、脚步建议开启；只有一个有效 Clip 时此项没有区别。 |

#### Cooldown 与 Max Simultaneous 的区别

- `Cooldown` 控制“触发有多密”；
- `Max Simultaneous` 控制“当前最多叠几层”。

例如一个 1 秒长的命中声：

- Cooldown 为 0.05 秒：每 0.05 秒最多接受一次新播放；
- Max Simultaneous 为 3：即使前三份都没播完，也不会出现第四份同时叠加。

高频 DOT、连击、群体伤害尤其应该配置这两个字段，否则同一帧大量事件会叠得很响。

### 4.4 Optional 3D sound：可选的空间音效

| 字段 | 作用 | 推荐用法 |
| --- | --- | --- |
| `Spatial Blend` | 0 表示完全 2D，1 表示完全 3D，中间值为混合。 | UI 和卡牌通常为 0；世界中的敌人、机关或环境声可以接近 1。 |
| `Min Distance` | 3D 声音在该距离内基本保持最大响度。 | 根据场景世界单位设置，例如 1。 |
| `Max Distance` | 超过该距离后声音衰减到最低。 | 例如 20～30。必须不小于 Min Distance。 |
| `Rolloff Mode` | 从 Min Distance 到 Max Distance 的音量衰减方式。 | `Logarithmic` 最接近常见自然衰减；`Linear` 是线性；`Custom` 需要自定义曲线。 |
| `Ignore Listener Pause` | 是否忽略 `AudioListener.pause`。 | 暂停菜单仍要响的 UI Cue 可以开启；普通战斗音效通常关闭。 |

重要：只有通过 `TryPlayAtPosition`，或者 `AudioCuePlayer` 开启 `Play At Transform` 时，Cue 的 Spatial Blend 才会按配置使用。普通 `TryPlay` 会按 2D 声音播放，以免 UI 或战斗界面声音意外受空间距离影响。

## 5. 第二步：创建 BattleAudioBindings

如果要注册战斗、抽牌和出牌声音：

1. 在 Project 窗口右键；
2. 选择 `Create → KiKs → Audio → Battle Audio Bindings`；
3. 命名为 `BattleAudioBindings_Default` 或按场景命名；
4. 选中该资产；
5. 将之前创建的 AudioCue 拖入下面的字段。

BattleAudioBindings 中的字段可以留空。字段为空时，对应事件保持静音，不会报空引用错误。

### 5.1 Card Movement：卡牌移动

| 字段 | 何时播放 | 注意事项 |
| --- | --- | --- |
| `Card Draw` | 每产生一张成功抽牌事件时播放一次。 | 一次抽多张牌会触发多次，建议使用短音频并设置 Cooldown，避免挤成一团。 |
| `Card Discard` | 卡牌进入弃牌堆时播放，包括使用后的弃牌和回合结束弃牌。 | 如果一次弃掉整手牌可能连续触发；不需要弃牌声时留空。 |

### 5.2 Successful Card Play By Category：成功出牌分类

| 字段 | 对应卡牌 Category | 说明 |
| --- | --- | --- |
| `Melee Card Played` | `melee` | 成功打出近战牌时播放。 |
| `Ranged Card Played` | `ranged` 或 `guns` | 成功打出射击或枪械牌时播放。 |
| `Magic Card Played` | `magic` | 成功打出魔法牌时播放。 |
| `Defense Card Played` | `defense` | 成功打出防御牌时播放。 |
| `Fallback Card Played` | 其他、未知或未识别类别 | 某个分类字段为空时也会尝试使用它作为兜底。敌人特殊牌通常也可使用此项。 |

如果 `Melee Card Played` 没有填写，但 `Fallback Card Played` 填了通用出牌 Cue，近战牌会使用通用 Cue。如果两者都为空，则近战出牌保持静音。

这里播放的是“成功出牌”的逻辑声音，例如卡片挥出、能量确认或纸牌声。真正需要与刀光、枪口火焰严格同步的命中声，应该通过动画帧上的 AudioCuePlayer 播放，参见第 8 节。

### 5.3 Combat Results：战斗结果

| 字段 | 何时播放 | 当前实现细节 |
| --- | --- | --- |
| `Player Hit` | 玩家收到实际伤害，或玩家受到有正数伤害的状态 Tick 时。 | 只检查目标是不是玩家；敌人被玩家命中的声音不从这里播放。 |
| `Toughness Broken` | 任意角色发生破韧事件时。 | 当前玩家和敌人共用一个 Cue；需要区分时可以后续拆成两个字段。 |
| `Healing` | 产生大于 0 的实际治疗事件时。 | 满血且实际治疗为 0 时不会播放。 |
| `Status Applied` | 成功施加流血、中毒等状态时。 | 当前所有状态共用一个 Cue；需要按状态区分时可扩展映射表。 |

### 5.4 Battle Outcome：战斗结果

| 字段 | 何时播放 |
| --- | --- |
| `Victory` | 战斗规则产生胜利事件时。 |
| `Defeat` | 战斗规则产生失败事件时。 |

## 6. 第三步：在战斗场景放置 BattleAudioPresenter

完成 Cue 和 Bindings 资产后，还需要让场景监听战斗事件：

1. 打开战斗场景 `Assets/Scenes/Card.unity`；
2. 新建空对象，建议命名为 `BattleAudio`；
3. 在 Inspector 点击 `Add Component`；
4. 搜索并添加 `Battle Audio Presenter`；
5. 把 `BattleAudioBindings` 资产拖入 `Bindings` 字段；
6. `Battle Controller` 可以留空，也可以显式拖入场景中的 BattleController。

### BattleAudioPresenter 的全部字段

| 字段 | 是否必填 | 作用 |
| --- | --- | --- |
| `Battle Controller` | 否 | 指定要监听的战斗控制器。留空时启动后自动寻找场景中的第一个 BattleController。为了让注册关系更直观，也可以手动拖入。 |
| `Bindings` | 是 | 指定本场战斗使用的 BattleAudioBindings 映射表。为空时 Presenter 会输出警告并保持静音。 |

每个战斗场景通常只需要一个 BattleAudioPresenter。不要在同一场战斗中挂多个并指向同一套 Bindings，否则每个事件可能播放多次。

## 7. 按钮音效：AudioButtonFeedback

给需要声音的 Button 对象添加 `Audio Button Feedback` 组件。

| 字段 | 何时播放 |
| --- | --- |
| `Hover` | 鼠标指针进入按钮范围时播放。 |
| `Click` | 指针点击按钮时播放。 |

把 `UI_按钮悬浮` 和 `UI_按钮点击` 等 AudioCue 直接拖入对应字段。Cue 的 `Bus` 建议选择 `UI`。

这个组件不需要额外 AudioSource。字段留空时对应行为静音。旧场景中的 `ButtonHoverSound` 仍然可以工作，新按钮建议优先使用 AudioButtonFeedback，再逐步迁移旧按钮，避免一次性破坏现有场景引用。

## 8. 动画命中帧和任意触发：AudioCuePlayer

`AudioCuePlayer` 是最通用的无代码播放器，可以用于：

- Animation Event 命中帧；
- Timeline Signal 或 UnityEvent；
- 对象启用时播放；
- 机关、敌人和世界物体的 3D 声音；
- 临时测试某个 Cue。

### AudioCuePlayer 的全部字段

| 字段 | 作用 |
| --- | --- |
| `Cue` | 要播放的 AudioCue。这个对象引用字段就是该组件的音效“槽位”。 |
| `Volume Scale` | 仅对此调用额外乘一次音量，范围 0～2。1 表示保持 Cue 原音量，0.5 表示减半。 |
| `Play On Enable` | 每次组件或 GameObject 从禁用变为启用时自动调用 Play。对象池对象反复启用时也会反复播放。 |
| `Play At Transform` | 开启时使用该 GameObject 的世界坐标播放，并应用 Cue 的 Spatial Blend、距离和衰减设置；关闭时按 2D 播放。 |

### AudioCuePlayer 的公开方法

| 方法 | 作用 |
| --- | --- |
| `Play()` | 播放当前 Cue，可由 Button UnityEvent、Animation Event 或其他组件调用。 |
| `Stop()` | 停止当前正在播放的所有同一 AudioCue 声音，不只停止这个组件发出的那一份。 |

### 让打击声准确卡在动画命中帧

1. 把 `AudioCuePlayer` 添加到接收 Animation Event 的动画 GameObject；
2. 把 `SFX_近战命中` Cue 拖入 `Cue`；
3. 打开 Animation 窗口；
4. 在刀光接触敌人的那一帧添加 Animation Event；
5. 事件函数选择 `Play`；
6. 播放测试，确认声音与闪白、震屏或受击动作同步。

Animation Event 通常只能调用动画对象上组件的方法。如果函数列表中看不到 `Play`，先确认 AudioCuePlayer 是否挂在 Animator 所在对象或实际接收事件的对象上。

不要同时让 BattleAudioBindings 和 Animation Event 播放同一个命中 Cue，否则可能听到双重声音。推荐职责是：

- BattleAudioBindings 的 `Melee Card Played`：卡牌成功打出的声音；
- Animation Event 的 `AudioCuePlayer.Play()`：武器真正命中的声音。

## 9. AudioManager 是否需要手动放进场景

通常不需要。第一次调用任何 AudioCue 时，Manager 会自动创建并执行 `DontDestroyOnLoad`。

如果需要在 Inspector 中调整对象池和初始音量，可以在游戏最先进入的场景中：

1. 新建空对象，命名为 `AudioManager`；
2. 添加 `Audio Manager` 组件；
3. 调整下面的字段。

场景里只应保留一个 AudioManager。即使误放多个，运行时也会销毁重复实例。

### AudioManager 的全部 Inspector 字段

#### Pool

| 字段 | 作用 | 默认值 |
| --- | --- | ---: |
| `Initial Voices` | Manager 启动时预先创建多少个内部 AudioSource。预创建可以避免第一次密集战斗时临时创建对象。 | 12 |
| `Max Voices` | 全局最多允许存在多少个内部 AudioSource。全部占满时会根据 Cue Priority 决定是否抢占。 | 32 |

`Voice` 只是 Manager 内部对一个 AudioSource 播放通道的称呼，注册人员不需要手动创建它。Manager 会创建名为 `SFX Voice 1`、`SFX Voice 2` 的子对象并循环复用。

#### Volume

| 字段 | 作用 | 默认值 |
| --- | --- | ---: |
| `Default Master Volume` | 所有 Manager 短音效的总音量默认值。 | 1 |
| `Default Sfx Volume` | Bus 为 Sfx 的默认分组音量。 | 1 |
| `Default Ui Volume` | Bus 为 UI 的默认分组音量。 | 1 |

这些是“第一次运行或没有保存值时”的默认音量。游戏运行后通过 `SetMasterVolume`、`SetSfxVolume`、`SetUiVolume` 修改并保存过音量，PlayerPrefs 中的保存值会优先于 Inspector 默认值。

AudioManager 当前管理短音效和 UI 音效，不管理现有咖啡厅/战斗 BGM。

## 10. 推荐配置示例

### 10.1 普通近战命中

```text
Display Name          = 近战命中
Clips                 = 3～6 个相似打击变体
Bus                   = Sfx
Output                = None（没有 AudioMixer 时）
Volume                = 0.8～1
Pitch Range           = 0.95 / 1.05
Priority              = 80
Cooldown              = 0.03～0.06
Max Simultaneous      = 3～5
Overflow Mode         = Replace Oldest
Avoid Immediate Repeat = 开启
Spatial Blend         = 0（UI 战斗界面）
```

### 10.2 高频 DOT 或群体伤害

```text
Clips                 = 2～4 个短变体
Pitch Range           = 0.97 / 1.03
Cooldown              = 0.05～0.1
Max Simultaneous      = 2～3
Overflow Mode         = Ignore New 或 Replace Oldest
```

不要把轻击、重击和持续伤害全部放进同一个 Cue，因为它们通常需要不同的冷却、并发和音量设置。

### 10.3 按钮点击

```text
Clips                 = 1～2 个
Bus                   = UI
Volume                = 0.6～0.9
Pitch Range           = 1 / 1
Priority              = 64～96
Cooldown              = 0.03
Max Simultaneous      = 1～2
Overflow Mode         = Replace Oldest
Ignore Listener Pause = 开启（暂停菜单仍需响应时）
```

### 10.4 3D 世界机关

```text
Bus              = Sfx
Spatial Blend    = 1
Min Distance     = 1
Max Distance     = 20
Rolloff Mode     = Logarithmic
Play At Transform = 开启
```

## 11. 从零配置一套战斗声音的实际步骤

### 步骤 A：准备文件夹

建议创建：

```text
Assets/Audio/Cues/Combat
Assets/Audio/Cues/UI
Assets/Audio/Bindings
```

### 步骤 B：创建 Cue

至少可以先创建：

```text
SFX_抽牌
SFX_近战出牌
SFX_射击出牌
SFX_魔法出牌
SFX_防御出牌
SFX_玩家受击
SFX_破韧
SFX_胜利
SFX_失败
```

逐个选中 Cue，把对应 AudioClip 放入 Clips，并按用途配置参数。

### 步骤 C：创建并填写 Bindings

1. 创建 `Battle Audio Bindings`；
2. 命名为 `BattleAudioBindings_Default`；
3. 选中它；
4. 将各 Cue 拖入名字相同的字段；
5. 暂时没有声音的字段可以留空。

### 步骤 D：场景接入

1. 打开 `Card` 战斗场景；
2. 创建 `BattleAudio` GameObject；
3. 添加 `Battle Audio Presenter`；
4. 把 `BattleAudioBindings_Default` 拖入 `Bindings`；
5. 保存场景。

### 步骤 E：命中帧

1. 在攻击动画对象添加 `Audio Cue Player`；
2. 拖入命中 Cue；
3. 在真实命中帧添加 `Play` Animation Event。

### 步骤 F：测试

进入 Play Mode，依次检查：

- 抽一张牌是否只有一次抽牌声；
- 成功打出不同类别卡牌是否使用正确 Cue；
- 出牌失败是否保持静音；
- 连击是否出现过响叠加；
- 命中声是否与画面帧同步；
- 玩家受击、破韧、胜利和失败是否正确触发；
- 暂停状态下 UI Cue 是否按预期播放。

## 12. 程序接口说明

普通音效注册人员不需要写代码。以下内容仅供编写业务脚本的人使用。

```csharp
using KiKs.Audio;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] private AudioCue hit;

    public void Play2D()
    {
        AudioManager.TryPlay(hit);
    }

    public void Play3D()
    {
        AudioManager.TryPlayAtPosition(hit, transform.position);
    }
}
```

| 接口 | 作用 |
| --- | --- |
| `TryPlay(cue, volumeScale)` | 以 2D 方式请求播放；被冷却或并发规则拒绝时返回 false。 |
| `TryPlayAtPosition(cue, position, volumeScale)` | 在世界坐标播放，并应用 Cue 的 3D 设置。 |
| `SetMasterVolume(value, save)` | 设置总音量；save 默认为 true，会写入 PlayerPrefs。 |
| `SetSfxVolume(value, save)` | 设置 Sfx Bus 音量。 |
| `SetUiVolume(value, save)` | 设置 UI Bus 音量。 |
| `Stop(cue)` | 停止所有正在播放的同一 Cue。 |
| `StopAll()` | 停止 Manager 管理的全部短音效。 |

## 13. 常见问题排查

### Inspector 中看不到创建菜单或组件

1. 执行 `Assets → Refresh`；
2. 等待右下角脚本编译完成；
3. 查看 Console 是否存在任何红色编译错误；
4. 编译错误会阻止新组件出现在 Add Component 菜单中。

### Cue 播放时完全没声音

依次检查：

1. Cue 的 Clips 是否至少有一个非空 AudioClip；
2. 目标组件的 Cue/Bindings 字段是否真的拖入资产；
3. Cue Volume 是否大于 0；
4. Master、Sfx 或 UI 保存音量是否为 0；
5. Unity Game 视图的 `Mute Audio` 是否开启；
6. 系统输出设备是否正确；
7. 如果设置了 Output，AudioMixerGroup 是否被静音或音量过低。

Console 出现 `AudioCue has no valid clip`，说明该 Cue 的 Clips 为空，或者所有 Element 都是 None。

### 放了多个 Clip，却好像总听到同一个

- 先确认 Clips 中确实是不同音频文件；
- 确认 `Avoid Immediate Repeat` 已开启；
- 三个以上 Clip 仍然是随机，不保证固定轮流顺序；
- 音频差异太小时，人耳可能不容易区分；
- Pitch Range 可以加入少量变化，但不要用过大的范围掩盖素材问题。

### 连击时少了一部分声音

这通常是正常保护行为。检查：

- Cooldown 是否过大；
- Max Simultaneous 是否过小；
- Overflow Mode 是否为 Ignore New；
- 全局 Max Voices 是否已经占满；
- Cue Priority 是否过低。

### 连击时声音叠得太响

按下面顺序调整：

1. 降低 Max Simultaneous；
2. 增加一点 Cooldown；
3. 选择合适的 Overflow Mode；
4. 最后再降低 Cue Volume。

单纯降低 Volume 不能阻止几十份声音同时叠加。

### 出牌失败却播放了声音

不要在“开始拖卡”或“松开鼠标”时直接播放出牌 Cue。使用 BattleAudioPresenter，它只消费战斗规则产生的成功 CardPlayed 事件。

### 命中声音和刀光不同步

规则事件发生时间不一定就是动画真正命中的帧。把命中 Cue 放到攻击对象上的 AudioCuePlayer，并从 Animation Event 调用 Play。

### 一次事件播放了两遍

检查：

- 场景是否有两个 BattleAudioPresenter；
- 是否同时从 Bindings 和 Animation Event 播放了同一个声音；
- 按钮上是否同时存在旧 ButtonHoverSound 和新 AudioButtonFeedback；
- UnityEvent 是否重复绑定 Play。

## 14. 最终职责划分

| 对象 | 谁负责 | 做什么 |
| --- | --- | --- |
| `AudioClip` | 音效素材人员 | 提供原始音频文件。 |
| `AudioCue` | 音效注册人员 | 把一个或多个 Clip 组织成逻辑声音，并配置音量、随机、冷却、并发和空间参数。 |
| `BattleAudioBindings` | 战斗音效注册人员 | 明确指定每个战斗事件使用哪个 Cue。 |
| `BattleAudioPresenter` | 场景接入人员 | 放进战斗场景并引用 Bindings。 |
| `AudioCuePlayer` | 动画或场景人员 | 在动画帧、UnityEvent 或对象启用时触发 Cue。 |
| `AudioButtonFeedback` | UI 人员 | 为按钮显式注册 Hover 和 Click Cue。 |
| `AudioManager` | Manager 维护者 | 维护对象池、音量、并发、优先级和播放生命周期。普通注册人员无需修改。 |

最重要的规则是：

> AudioCue 决定“一个声音应该怎么播放”；BattleAudioBindings 和各个组件决定“什么时候播放哪个 Cue”；AudioManager 负责“真正把它安全、高效地播放出来”。
