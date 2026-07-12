# Cards 原型卡牌系统说明

## 1. 文档目的

`Assets/Script/Cards` 保存当前战斗原型中的卡牌定义代码。

这套代码现阶段只负责以下内容：

- 描述一张卡牌属于什么类别和武器家族。
- 描述卡牌的名称、唯一 ID、卡图、说明文字和资源消耗。
- 描述攻击力、攻击次数、伤害类型和削韧等基础数值。
- 把中毒、眩晕、破甲、格挡等效果转换成标准的 `CardEffectCommand`。
- 通过严格继承保证同类卡牌共享相同的基础流程。
- 允许在 Unity 中通过 `Create > KiKs > Cards` 创建对应的卡牌资源。

这套代码当前不负责以下内容：

- 不控制玩家回合或敌人回合。
- 不检查当前是否允许出牌。
- 不扣除行动点或魔法点。
- 不直接修改玩家或敌人的生命、韧性、防御力。
- 不负责持续效果的回合计时。
- 不负责抽牌、弃牌、洗牌和牌库管理。
- 不负责目标选择、点击、拖拽和卡牌动画。
- 不负责胜负判断和敌人 AI。

这些职责以后应由战斗 Controller、回合 Controller、角色状态脚本和牌库脚本完成。当前卡牌层只提供数据和“请求执行什么效果”的命令。

---

## 2. 当前目录结构

```text
Cards
├── Base
│   ├── CardBase.cs
│   ├── CardEnums.cs
│   ├── CardEffectCommand.cs
│   ├── AttackCardBase.cs
│   ├── GunCardBase.cs
│   ├── MeleeWeaponCardBase.cs
│   ├── AxeCardBase.cs
│   ├── BladeCardBase.cs
│   ├── ChainWeaponCardBase.cs
│   ├── MagicCardBase.cs
│   └── DefenseCardBase.cs
├── Guns
│   ├── HandgunCard.cs
│   ├── SniperRifleCard.cs
│   ├── ShotgunCard.cs
│   ├── RocketLauncherCard.cs
│   └── GatlingGunCard.cs
├── ColdWeapon
│   ├── LongAxeCard.cs
│   ├── HandAxeCard.cs
│   ├── BattleAxeCard.cs
│   ├── KnightSwordCard.cs
│   ├── KatanaCard.cs
│   ├── DaggerCard.cs
│   ├── ScalpelCard.cs
│   ├── ShacklesCard.cs
│   ├── MeteorHammerCard.cs
│   └── ChainWhipCard.cs
├── Magic
│   ├── PoisonCard.cs
│   ├── FreezeCard.cs
│   ├── BleedCard.cs
│   ├── BurnCard.cs
│   ├── BlindCard.cs
│   └── ArmorBreakMagicCard.cs
├── Defense
│   ├── DodgeCard.cs
│   ├── BlockCard.cs
│   ├── StealthCard.cs
│   └── ImmunityCard.cs
└── README.md
```

命名空间统一为：

```csharp
KiKs.Cards
```

---

## 3. 继承层次

```text
ScriptableObject
└── CardBase
    ├── AttackCardBase
    │   ├── GunCardBase
    │   │   ├── HandgunCard
    │   │   ├── SniperRifleCard
    │   │   ├── ShotgunCard
    │   │   ├── RocketLauncherCard
    │   │   └── GatlingGunCard
    │   └── MeleeWeaponCardBase
    │       ├── AxeCardBase
    │       │   ├── LongAxeCard
    │       │   ├── HandAxeCard
    │       │   └── BattleAxeCard
    │       ├── BladeCardBase
    │       │   ├── KnightSwordCard
    │       │   ├── KatanaCard
    │       │   ├── DaggerCard
    │       │   └── ScalpelCard
    │       └── ChainWeaponCardBase
    │           ├── ShacklesCard
    │           ├── MeteorHammerCard
    │           └── ChainWhipCard
    ├── MagicCardBase
    │   ├── PoisonCard
    │   ├── FreezeCard
    │   ├── BleedCard
    │   ├── BurnCard
    │   ├── BlindCard
    │   └── ArmorBreakMagicCard
    └── DefenseCardBase
        ├── DodgeCard
        ├── BlockCard
        ├── StealthCard
        └── ImmunityCard
```

继承层次的基本原则：

1. `CardBase` 只保存所有卡牌都具备的公共信息。
2. `AttackCardBase` 统一生成直接伤害命令。
3. `GunCardBase` 只允许枪械卡补充枪械专属命令。
4. `MeleeWeaponCardBase` 统一处理百分比削韧。
5. 斧、刀、锁链武器通过中间基类固定自己的武器家族。
6. `MagicCardBase` 统一魔法卡费用和目标类型。
7. `DefenseCardBase` 统一防御卡的自身目标。
8. 所有具体卡牌类使用 `sealed`，不再继续被继承。
9. 具体卡牌只填写数值和附加命令，不直接执行战斗逻辑。

---

## 4. CardBase

`CardBase` 是所有卡牌的根基类，并继承自 `ScriptableObject`。

### 4.1 公共身份属性

| 属性 | 类型 | 含义 |
| --- | --- | --- |
| `CardId` | `string` | 程序使用的唯一 ID，不应随显示名称变化 |
| `CardName` | `string` | UI 中显示的中文卡牌名称 |
| `Category` | `CardCategory` | 攻击、魔法或防御 |
| `Family` | `CardFamily` | 枪械、斧、刀、锁链、魔法或防御 |
| `TargetType` | `CardTargetType` | 当前目标是自己还是单个敌人 |

### 4.2 公共消耗属性

| 属性 | 默认值 | 含义 |
| --- | ---: | --- |
| `ActionPointCost` | `1` | 使用卡牌需要的行动点 |
| `ManaCost` | `0` | 使用卡牌需要的魔法点 |

`MagicCardBase` 会把消耗统一修改为：

- 行动点消耗：`0`
- 魔法点消耗：`1`

目前费用只是一项声明。卡牌不会自行扣费，未来 Controller 必须在执行命令前检查并扣除费用。

### 4.3 公共元数据

| 属性 | 含义 |
| --- | --- |
| `IsSpecial` | 是否属于图片中标记的“特殊”卡牌 |
| `IsUnique` | 是否属于“唯一”卡牌 |
| `Artwork` | 在 Inspector 中指定的卡图 Sprite |
| `Description` | 在 Inspector 中填写的卡牌说明文字 |

当前特殊卡牌：

- 火箭筒
- 战斧
- 流星锤
- 隐身

所有魔法卡由 `MagicCardBase` 统一标记为 `IsUnique = true`。

`IsSpecial` 和 `IsUnique` 暂时只是标签，不会自动限制牌库数量，也不会触发额外效果。

### 4.4 CreateEffectCommands

```csharp
IReadOnlyList<CardEffectCommand> CreateEffectCommands()
```

该方法创建并返回这张卡牌需要执行的全部效果命令。例如霰弹枪会返回：

1. 一条伤害命令：每次 10 点，共 3 次。
2. 一条固定削韧命令：每次 3 点，共 3 次。

返回命令不等于效果已经生效。未来 Controller 读取并执行这些命令后，角色数值才会变化。

---

## 5. CardEffectCommand

`CardEffectCommand` 是卡牌层与未来战斗逻辑层之间的临时协议。

它是一个不可变命令对象。创建之后只能读取，不能从外部修改，避免一张卡牌在执行过程中被其他脚本意外改变。

### 5.1 命令字段

| 字段 | 类型 | 使用方式 |
| --- | --- | --- |
| `EffectType` | `CardEffectType` | Controller 根据它决定执行哪一种效果 |
| `Amount` | `int` | 固定伤害、固定削韧、每回合伤害或防御降低值 |
| `HitCount` | `int` | 本命令执行多少次 |
| `DurationTurns` | `int` | 状态持续多少回合 |
| `TriggerCount` | `int` | 状态最多触发多少次 |
| `Percentage` | `float` | 百分比值，使用 `0.20f` 表示 20% |
| `Multiplier` | `float` | 倍率，使用 `2f` 表示乘以 2 |
| `DamageType` | `DamageType` | 普通伤害或真实伤害 |

### 5.2 工厂方法

#### Damage

```csharp
CardEffectCommand.Damage(amount, hitCount, damageType)
```

用于直接伤害。

示例：手枪的 `amount = 3`、`hitCount = 8`、`damageType = Normal`。

#### FlatToughnessDamage

```csharp
CardEffectCommand.FlatToughnessDamage(amount, hitCount)
```

用于固定数值削韧。

示例：霰弹枪每次固定削韧 3 点，共执行 3 次。

#### ToughnessDamage

```csharp
CardEffectCommand.ToughnessDamage(percentage, hitCount)
```

用于百分比削韧。

示例：长斧的 `percentage = 0.20f`，表示削韧量为 20%。

目前建议未来 Controller 以“目标最大韧性”为百分比基准。若策划之后决定改成当前韧性或攻击伤害百分比，只修改 Controller 的结算公式，不需要修改每张卡牌的继承结构。

#### Status

```csharp
CardEffectCommand.Status(
    effectType,
    durationTurns,
    amount,
    triggerCount,
    percentage,
    multiplier,
    damageType)
```

用于中毒、冻结、流血、灼烧、致盲、破甲、眩晕、闪避、格挡、隐身和免疫等状态。

不同状态只使用自己需要的字段，其余字段保留默认值。

---

## 6. 枚举说明

### 6.1 CardCategory

| 值 | 含义 |
| --- | --- |
| `Attack` | 直接攻击类卡牌，包括枪械和近战武器 |
| `Magic` | 消耗魔法点的状态类卡牌 |
| `Defense` | 以玩家自己为目标的防御卡牌 |

### 6.2 CardFamily

| 值 | 含义 |
| --- | --- |
| `Gun` | 枪械 |
| `Axe` | 斧类近战武器 |
| `Blade` | 刀剑类近战武器 |
| `ChainWeapon` | 锁链类近战武器 |
| `Magic` | 魔法 |
| `Defense` | 防御 |

`Category` 是大的玩法分类，`Family` 是更具体的继承和武器分类。两者不要混用。

### 6.3 CardTargetType

| 值 | 含义 |
| --- | --- |
| `Self` | 目标为玩家自己 |
| `SingleEnemy` | 目标为一个敌人 |

目前原型只考虑单敌人。未来出现全体敌人、随机敌人或无目标卡牌时，再向该枚举添加新值。

### 6.4 DamageType

| 值 | 含义 |
| --- | --- |
| `Normal` | 普通伤害，会受到防御、减伤等规则影响 |
| `True` | 真实伤害，预期忽略防御和普通减伤 |

真实伤害的最终规则还没有实现，应由未来伤害结算脚本统一决定。

### 6.5 CardEffectType

| 值 | 预期作用 |
| --- | --- |
| `Damage` | 造成直接伤害 |
| `ToughnessDamageFlat` | 造成固定数值削韧 |
| `ToughnessDamagePercent` | 造成百分比削韧 |
| `Poison` | 每回合造成中毒伤害 |
| `Freeze` | 使敌人跳过回合 |
| `Bleed` | 每回合造成流血伤害 |
| `Burn` | 放大目标受到的伤害 |
| `Blind` | 令每回合第一次攻击伤害为 0 |
| `ArmorBreak` | 降低目标防御或附加破甲状态 |
| `Stun` | 使目标眩晕指定回合 |
| `Dodge` | 按概率令一次攻击无效 |
| `DamageReduction` | 按比例降低受到的伤害 |
| `Stealth` | 令敌人跳过一次攻击行为 |
| `Immunity` | 无视一次敌人伤害 |

---

## 7. 攻击卡基础类

### 7.1 AttackCardBase

攻击卡必须提供：

- `AttackPower`：每次攻击的伤害。
- `AttackCount`：攻击次数，默认值为 1。
- `AttackDamageType`：普通伤害或真实伤害，默认是普通伤害。

`AttackCardBase` 已经封装直接伤害命令，所以具体攻击卡不应重复添加普通伤害命令。

它的固定流程是：

```text
读取 AttackPower、AttackCount、AttackDamageType
→ 创建 Damage 命令
→ 调用 BuildAdditionalCommands
```

### 7.2 GunCardBase

`GunCardBase` 固定：

- `Category = Attack`
- `Family = Gun`
- `TargetType = SingleEnemy`

具体枪械如有额外效果，只能通过 `BuildGunCommands` 添加。例如霰弹枪在这里添加固定削韧命令。

### 7.3 MeleeWeaponCardBase

近战卡必须提供 `ToughnessDamagePercent`。

固定流程为：

```text
创建直接伤害命令
→ 如果削韧百分比大于 0，则创建百分比削韧命令
→ 调用 BuildMeleeCommands 添加眩晕、流血、破甲等附加效果
```

`AxeCardBase`、`BladeCardBase`、`ChainWeaponCardBase` 只负责固定武器家族，不重复实现攻击流程。

---

## 8. 枪械卡数据

所有枪械卡默认消耗 1 行动点，以单个敌人为目标。

| 卡牌 | CardId | 攻击力 | 次数 | 伤害类型 | 附加效果 | 标签 |
| --- | --- | ---: | ---: | --- | --- | --- |
| 手枪 | `gun_handgun` | 3 | 8 | 普通 | 无 | 普通 |
| 狙击枪 | `gun_sniper_rifle` | 10 | 1 | 真实 | 无 | 普通 |
| 霰弹枪 | `gun_shotgun` | 10 | 3 | 普通 | 每次固定削韧 3 点，共 3 次 | 普通 |
| 火箭筒 | `gun_rocket_launcher` | 30 | 1 | 普通 | 暂无额外命令 | 特殊 |
| 加特林 | `gun_gatling` | 2 | 100 | 普通 | 无 | 普通 |

注意：攻击次数表示 Controller 应当执行多少次独立攻击。未来若需要每一击分别触发暴击、护盾、受击动画或死亡中止，应该由 Controller 在循环中处理，而不是简单把攻击力乘以次数。

---

## 9. 近战武器卡数据

所有近战武器卡默认消耗 1 行动点，以单个敌人为目标。

| 卡牌 | CardId | 家族 | 攻击力 | 削韧 | 附加效果 | 标签 |
| --- | --- | --- | ---: | ---: | --- | --- |
| 长斧 | `axe_long` | Axe | 10 | 20% | 眩晕 1 回合 | 普通 |
| 手斧 | `axe_hand` | Axe | 15 | 10% | 无 | 普通 |
| 战斧 | `axe_battle` | Axe | 35 | 35% | 无 | 特殊 |
| 骑士剑 | `blade_knight_sword` | Blade | 15 | 25% | 无 | 普通 |
| 太刀 | `blade_katana` | Blade | 25 | 5% | 破甲 3 回合 | 普通 |
| 匕首 | `blade_dagger` | Blade | 10 | 0% | 真实伤害 | 普通 |
| 手术刀 | `blade_scalpel` | Blade | 10 | 0% | 流血 10 回合，单回合数值待定 | 普通 |
| 绊脚锁 | `chain_shackles` | ChainWeapon | 1 | 10% | 眩晕 1 回合 | 普通 |
| 流星锤 | `chain_meteor_hammer` | ChainWeapon | 30 | 20% | 破甲 1 回合 | 特殊 |
| 锁镰 | `chain_whip` | ChainWeapon | 15 | 10% | 流血 6 回合，单回合数值待定 | 普通 |

削韧百分比统一使用小数保存：

- 5% 写作 `0.05f`
- 10% 写作 `0.10f`
- 20% 写作 `0.20f`
- 35% 写作 `0.35f`

---

## 10. 魔法卡数据

魔法卡当前统一：

- 消耗 0 行动点。
- 消耗 1 魔法点。
- 目标为单个敌人。
- `IsUnique = true`。

| 卡牌 | CardId | 效果命令 |
| --- | --- | --- |
| 中毒 | `magic_poison` | 每回合造成 2 点真实伤害，持续 3 回合 |
| 冻结 | `magic_freeze` | 敌人跳过回合，持续 1 回合 |
| 流血 | `magic_bleed` | 每回合造成 1 点伤害，持续 10 回合 |
| 灼烧 | `magic_burn` | 受到的伤害乘以 2，持续 2 回合 |
| 致盲 | `magic_blind` | 每回合第一次攻击伤害乘以 0，持续 3 回合 |
| 破甲 | `magic_armor_break` | 防御降低 5 点，持续 2 回合 |

魔法卡不会自行注册状态，也不会自行减少持续回合。Controller 应把命令转换为目标身上的运行时状态。

---

## 11. 防御卡数据

防御卡当前统一：

- 默认消耗 1 行动点。
- 目标为玩家自己。

| 卡牌 | CardId | 效果命令 | 当前参数解释 |
| --- | --- | --- | --- |
| 闪避 | `defense_dodge` | `Dodge` | 50% 概率令一次攻击无效，按当前回合处理 |
| 格挡 | `defense_block` | `DamageReduction` | 当前回合受到的伤害降低 50% |
| 隐身 | `defense_stealth` | `Stealth` | 敌人跳过一次攻击行为，特殊卡 |
| 免疫 | `defense_immunity` | `Immunity` | 无视一次敌人造成的伤害 |

`DurationTurns = 0` 且 `TriggerCount = 1` 的命令表示它不依靠固定回合数结束，而是在成功触发一次之后消耗。

---

## 12. 未来 Controller 的调用约定

未来战斗 Controller 使用一张卡牌时，推荐遵循以下顺序：

```text
1. 检查当前是否为玩家可操作阶段
2. 检查卡牌是否在手牌中
3. 检查目标是否合法
4. 检查行动点和魔法点是否足够
5. 扣除费用
6. 调用 CardBase.CreateEffectCommands()
7. 按顺序执行每一条 CardEffectCommand
8. 处理死亡、破韧、眩晕或其他触发结果
9. 将使用后的卡牌移入弃牌堆
10. 通知 UI 和动画系统刷新
```

伪代码示例：

```csharp
CardBase card = selectedCard;

// 以下方法均由未来 Controller 提供，目前不存在。
ValidateTurn(card);
ValidateTarget(card, target);
PayCost(card.ActionPointCost, card.ManaCost);

foreach (CardEffectCommand command in card.CreateEffectCommands())
{
    ExecuteCommand(command, player, target);
}

MoveCardToDiscardPile(card);
```

这段代码只是接口约定，不是当前项目中已经存在的实现。

---

## 13. 持续状态的建议约定

目前没有回合 Controller，因此持续状态只保留参数。以后实现时建议统一以下规则，避免每张卡各自计时：

### 13.1 状态保存

运行时状态至少应保存：

- `EffectType`
- 来源卡牌 ID
- 剩余回合数
- 剩余触发次数
- 数值、百分比和倍率
- 伤害类型

### 13.2 状态生效时机

建议约定：

- 中毒和流血：目标回合开始或结束时结算一次，二选一后全项目统一。
- 冻结和眩晕：目标即将开始行动时检查。
- 灼烧和破甲：伤害计算前检查。
- 致盲：敌人每回合第一次攻击结算前检查。
- 闪避、隐身和免疫：敌人攻击命中前检查，成功后减少触发次数。
- 格挡：玩家受到伤害时计算，玩家回合开始或敌人回合结束时移除。

### 13.3 回合数减少

不要让卡牌类自己减少 `DurationTurns`。建议由统一状态管理脚本在固定时机减少剩余回合数并移除过期状态。

---

## 14. 在 Unity 中创建卡牌资源

每个具体卡牌类都有 `CreateAssetMenu`。

操作步骤：

1. 等待 Unity 完成脚本编译。
2. 在 Project 窗口中进入希望保存卡牌资源的目录。
3. 建议建立 `Assets/ScriptableObjects/Cards`，并按类别继续分目录。
4. 在空白处右键。
5. 选择 `Create > KiKs > Cards`。
6. 继续选择枪械、近战武器、魔法或防御目录下的具体卡牌。
7. 创建 `.asset` 后，在 Inspector 中指定 `Artwork` 和 `Description`。

推荐资源目录：

```text
Assets/ScriptableObjects/Cards
├── Guns
├── ColdWeapon
├── Magic
└── Defense
```

代码目录保存“卡牌类型”，`ScriptableObjects` 目录保存“可以被场景和牌库引用的卡牌资源”。不要把 `.asset` 文件放进 `Assets/Script/Cards`。

当前攻击力、攻击次数、削韧和效果参数由具体类固定，不会显示成可编辑 Inspector 字段。这样做是为了让本次继承原型保持明确。以后进入频繁调数值阶段，可以再把这些属性改成序列化配置。

---

## 15. 如何新增卡牌

### 15.1 新增普通枪械卡

1. 在 `Cards/Guns` 新建一个与类名相同的 `.cs` 文件。
2. 继承 `GunCardBase`。
3. 类必须使用 `sealed`。
4. 添加 `CreateAssetMenu`。
5. 提供唯一 `CardId`、显示名称、攻击力和攻击次数。
6. 有附加效果时重写 `BuildGunCommands`。

```csharp
public sealed class ExampleGunCard : GunCardBase
{
    public override string CardId => "gun_example";
    public override string CardName => "示例枪械";
    public override int AttackPower => 5;
    public override int AttackCount => 2;
}
```

不要在具体枪械中再次添加普通 `Damage` 命令，因为 `AttackCardBase` 已经自动创建。

### 15.2 新增近战卡

根据武器类型继承：

- 斧：`AxeCardBase`
- 刀剑：`BladeCardBase`
- 锁链：`ChainWeaponCardBase`

必须填写攻击力和削韧百分比。有附加状态时重写 `BuildMeleeCommands`。

### 15.3 新增魔法卡

继承 `MagicCardBase`，然后在 `BuildMagicCommands` 中添加状态命令。

魔法卡不要自行声明攻击卡的属性，也不要直接调用敌人的 `TakeDamage`。

### 15.4 新增防御卡

继承 `DefenseCardBase`，然后在 `BuildDefenseCommands` 中添加作用于玩家自己的命令。

---

## 16. CardId 规范

`CardId` 是程序标识，不是显示文字。

当前前缀：

| 前缀 | 类型 |
| --- | --- |
| `gun_` | 枪械 |
| `axe_` | 斧 |
| `blade_` | 刀剑 |
| `chain_` | 锁链武器 |
| `magic_` | 魔法 |
| `defense_` | 防御 |

规则：

- 全部使用小写英文。
- 单词之间使用下划线。
- 一个 ID 只能对应一张卡牌。
- 卡牌改中文名时不要随意改 ID。
- 已经进入存档的数据不要直接修改 ID，否则旧存档会找不到卡牌。

---

## 17. 与 CombatTemp 和 UI 的关系

`Cards` 与 `CombatTemp` 目前是两套并存代码。

- `CombatTemp/CardData` 仍然挂在旧卡牌 GameObject 上。
- `ClickAttackCard` 和 `DragAttackCard` 仍然直接调用旧的 `EnemyStats`。
- `Cards` 下的新类不会自动接管旧卡牌按钮。
- `CardInteraction`、`CardSkew`、`Draggable` 仍然只负责 UI 表现和拖拽。

未来接入时建议逐步迁移：

```text
旧卡牌 GameObject
→ 引用一个 CardBase 类型的 .asset
→ 点击或拖拽只提交“尝试使用该卡牌”的请求
→ 战斗 Controller 检查费用和目标
→ Controller 执行 CardEffectCommand
```

在新流程可以完整运行之前，不要删除 `CombatTemp`，否则当前场景里的旧引用可能丢失。

---

## 18. 当前临时假设和未确定项

以下规则是为了让原型结构可以先建立，并不代表最终策划结论：

1. 所有攻击卡和防御卡默认消耗 1 行动点。
2. 所有魔法卡默认消耗 1 魔法点且不消耗行动点。
3. 魔法卡都被标记为唯一，但尚未限制牌库中的数量。
4. 闪避和格挡暂按当前回合有效。
5. 隐身和免疫使用 `TriggerCount = 1`，触发后消耗。
6. 手术刀流血 10 回合，但图片没有给出每回合伤害，所以 `Amount` 暂时为 0。
7. 锁镰流血 6 回合，但图片没有给出每回合伤害，所以 `Amount` 暂时为 0。
8. 太刀和流星锤的破甲只有持续回合，没有给出具体降防数值，所以 `Amount` 暂时为 0。
9. 魔法破甲明确降低 5 点防御，因此命令中保存 `Amount = 5`。Controller 应将它解释为减少 5，而不是增加 5。
10. 近战百分比削韧暂定以目标最大韧性为基准。
11. 真实伤害是否无视所有减伤、格挡和免疫，尚未最终确定。
12. 火箭筒、战斧、流星锤的“特殊”目前只有标签，没有特殊规则。
13. 攻击次数是否逐次播放动画、逐次触发状态和逐次检查死亡，交给未来 Controller 决定。
14. 当前只支持玩家自己和单个敌人两种目标。

修改这些规则时，应优先修改统一 Controller 或状态结算规则，不要把回合逻辑分散到每个具体卡牌类中。

---

## 19. 禁止事项

为了保持当前继承层次清晰，`Cards` 目录中的代码暂时遵守以下限制：

- 不使用 `FindFirstObjectByType` 查找敌人或玩家。
- 不直接引用 `EnemyStats`。
- 不直接调用 `TakeDamage`。
- 不直接修改 UI 文本、血条或动画。
- 不在具体卡牌中判断当前回合。
- 不在具体卡牌中扣行动点、魔法点或移动牌堆。
- 不在具体卡牌中自己启动协程计算持续回合。
- 不根据点击或拖拽方式区分卡牌类别。
- 不让具体卡牌继承另一张具体卡牌。
- 不在一个 `.cs` 文件中放多张具体卡牌类。

---

## 20. 后续最小接入顺序

当准备让这些卡牌真正参与战斗时，推荐按以下顺序继续：

1. 新增玩家和敌人共用的角色状态接口或类。
2. 新增一个最小战斗 Controller，只处理出牌请求和命令执行。
3. 先实现 `Damage`、固定削韧、百分比削韧三个命令。
4. 用手枪、霰弹枪、长斧测试直接伤害和削韧。
5. 实现行动点和魔法点检查。
6. 实现持续状态容器和回合递减。
7. 接入魔法卡与防御卡。
8. 最后接入抽牌、弃牌、洗牌和完整回合流程。

这样可以始终保持一个可运行的小闭环，同时保留当前已经存在的 UI 和临时战斗代码。

---

## 21. 检查清单

新增或修改卡牌后检查：

- [ ] 文件是否位于正确目录。
- [ ] 类名是否与文件名一致。
- [ ] 是否继承正确的中间基类。
- [ ] 具体类是否为 `sealed`。
- [ ] 是否具有 `CreateAssetMenu`。
- [ ] `CardId` 是否唯一且符合前缀规范。
- [ ] 攻击力和攻击次数是否与策划数据一致。
- [ ] 固定削韧和百分比削韧是否区分正确。
- [ ] 百分比是否使用 0 到 1 的小数形式。
- [ ] 真实伤害是否正确声明 `DamageType.True`。
- [ ] 持续回合是否只写入命令，没有在卡牌类中自行计时。
- [ ] 触发次数效果是否正确设置 `TriggerCount`。
- [ ] 是否没有直接依赖敌人、UI、回合或牌库脚本。
- [ ] Unity Console 是否没有 C# 编译错误。
- [ ] 新建的 `.asset` 是否填写卡图和说明文字。

