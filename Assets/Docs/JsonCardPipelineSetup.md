# JSON 卡牌战斗配置指南

这份指南按“先直接在战斗场景调通，再接选牌场景”的顺序配置。

## 一、你现在不需要做的事

不需要为 43 张牌分别创建 `Card Definition`。

Create 菜单现在只保留：

- `Create → KiKs → Combat → Combat Rules`
- `Create → KiKs → Combat → Combatant Definition`

卡牌数据在 `Assets/StreamingAssets/CardData` 中维护。程序启动时根据 `manifest.json` 自动加载全部分类文件。

## 二、创建三个基础配置资源

建议在 `Assets/Settings/Combat` 下创建。

### 1. Combat Rules

在 Project 窗口空白处右键：

`Create → KiKs → Combat → Combat Rules`

命名为 `DefaultCombatRules`。

推荐先填写：

- Base Action Points：3
- Cards Drawn Per Turn：4
- Hand Limit：10
- Expected Initial Deck Size：15
- Starting Mana：5
- Maximum Mana：5
- Maximum Mana Spend Per Turn：1
- Card Upgrade Mana Cost：1
- Magic Cards Per Turn：1
- Ultimate Mana Threshold：3
- Ultimate Damage：0（数值确定后再改）
- Ultimate Stun Turns：1
- Ultimate Mana Refund：3

精英、Boss 处决和韧性恢复数值目前保持默认即可。

### 2. Player Definition

右键：

`Create → KiKs → Combat → Combatant Definition`

命名为 `PlayerDefinition`，设置：

- Combatant Id：`player`
- Display Name：玩家
- Side：Player
- Enemy Rank：None
- Max Health：按当前设计填写
- Max Toughness：0（玩家没有韧性时）

### 3. Enemy Definition

再创建一个 `Combatant Definition`，例如 `TestEnemyDefinition`：

- Combatant Id：`enemy_test`
- Display Name：测试敌人
- Side：Enemy
- Enemy Rank：Minion
- Max Health：例如 100
- Max Toughness：例如 100

同一场战斗中每个角色 ID 必须唯一。

## 三、创建 JSON 牌库服务

在最早会进入的场景（目前没有选牌场景时也可以直接放在战斗场景）：

1. Hierarchy 右键创建空物体；
2. 命名为 `GameServices`；
3. Add Component，添加 `Card Database Service`；
4. Relative Directory 保持 `CardData`；
5. 勾选 Auto Load On Awake；
6. 勾选 Persist Across Scenes。

它会加载：

`Assets/StreamingAssets/CardData/manifest.json`

加载成功时 Console 会显示：

`Loaded 43 cards from JSON.`

这个服务是单例。以后选牌场景中的 `GameServices` 会跨场景保留；如果下一个场景意外又有一份，重复对象会自动销毁。

## 四、先用调试 ID 直接启动战斗

在战斗场景：

1. 创建空物体 `BattleRoot`；
2. 添加 `Battle Controller`；
3. 把 `GameServices` 上的 Card Database Service 拖到 Card Database；
4. 把 `DefaultCombatRules` 拖到 Rules Config；
5. 把 `PlayerDefinition` 拖到 Player Definition；
6. 展开 Enemy Definitions，Size 设为 1；
7. 把 `TestEnemyDefinition` 拖到 Element 0；
8. 展开 Debug Starting Card Ids，Size 设为 15；
9. 填入 JSON 中存在的卡牌 ID；
10. 勾选 Auto Start Battle。

调试牌组允许重复，例如可以暂时多次填写 `blade_dagger`。也可以从以下文件复制 ID：

- `guns.json`
- `axes.json`
- `blades.json`
- `flexible-weapons.json`
- `hidden-weapons.json`
- `defense.json`
- `magic.json`

如果 `BattleSession` 中已经有赛前选择，控制器会优先使用选择结果，忽略调试列表。

跨场景时，Battle Controller 可以不手动拖 Card Database；它会在 Start 中自动寻找跨场景保留的单例。为了直接从战斗场景运行时更直观，仍建议在调试阶段手动关联同场景服务。

## 五、选牌界面怎么接

选牌界面不实例化战斗卡牌，也不保存强化状态。

加载牌库后，可以从：

```csharp
cardDatabase.Repository.Cards
```

读取每张 `CardSpec`，使用以下字段生成按钮或卡面：

- `Id`
- `DisplayName`
- `Category`
- `CostResource`
- `CostAmount`
- `Effects`
- `CanUpgrade`

赛前选牌界面直接读取 `Combat Rules` 的 `Expected Initial Deck Size`。当前规则为 15 张：

- 未满 15 张时 `Begin` 不可用；
- 第 16 个旧牌槽会在运行时隐藏；
- 点击 `Begin` 后按钮会锁定，防止重复提交；
- 牌组写入 `BattleSession` 后异步加载 `Card` 场景。

```csharp
BattleSession.SetSelectedDeck(selectedCardIds);
SceneManager.LoadSceneAsync("Card");
```

`selectedCardIds` 是字符串列表，允许重复。不要传 `CardSpec`、`CardInstance` 或“已强化”标记。

`Cafe`、`PreBattle` 和 `Card` 必须加入 Unity 6 当前 Build Profile 的共享场景列表，否则按下
`Begin` 时 Unity 会拒绝加载场景。

战斗场景加载后，Battle Controller 会自动把每个 ID 转成独立实例。

## 六、战斗 UI 怎么调用

每张手牌 UI 必须保存 `CardInstance.InstanceId`，而不只是卡牌 ID。因为同名牌可能有多张，并且强化状态不同。

### 普通出牌

```csharp
battleController.PlayCard(cardInstance.InstanceId, selectedEnemyId);
```

防御牌等自身目标牌可以传 `null` 作为目标 ID；规则层会使用玩家自身。

### 战斗内强化

强化按钮调用：

```csharp
battleController.UpgradeCard(cardInstance.InstanceId, selectedEnemyId);
```

第二个参数只用于“本次消耗正好触发自动大招”时选择目标，可以省略。

成功后：

- 当前魔法点减少 1；
- 当前实例的 `IsUpgraded` 变为 true；
- 牌仍在手中；
- 行动点不变；
- 不会自动出牌；
- UI 应显示强化后的每个效果数值。

如果本回合已经消耗过 1 点魔法点，强化会失败。使用魔法牌也占用同一个额度。

### 魔法牌

魔法牌仍通过 `PlayCard` 使用。引擎会根据 JSON 的 `cost.resource = "mana"` 自动：

- 检查本回合魔法牌次数；
- 检查魔法点和每回合消耗上限；
- 消耗魔法点；
- 不消耗行动点；
- 检查是否累计消耗 3 点并自动释放大招。

### 处决

收到 `ExecutionConfirmationRequired` 事件后显示确认 UI。确认按钮调用：

```csharp
battleController.ConfirmExecution();
```

处于该阶段时不能继续出牌或结束回合。

### 回合按钮

结束玩家回合：

```csharp
battleController.EndPlayerTurn();
```

敌人 AI 依次调用：

```csharp
battleController.ResolveEnemyAttack(enemyId, damage);
```

敌方全部行动完成后：

```csharp
battleController.CompleteEnemyTurn();
```

## 七、界面刷新方式

推荐监听：

```csharp
battleController.CombatEventRaised += OnCombatEvent;
```

重要事件包括：

- `CardDrawn`
- `CardPlayed`
- `CardDiscarded`
- `CardUpgraded`
- `ActionPointsChanged`
- `ManaChanged`
- `UltimateTriggered`
- `DamageApplied`
- `ToughnessChanged`
- `ExecutionConfirmationRequired`
- `Victory`
- `Defeat`

动画可以并行播放，但数值显示应最终读取 `battleController.State`。不要让 UI 直接修改 State。

## 八、修改 JSON 时的规则

- 卡牌 ID 必须全局唯一；
- 分类文件的 `category` 必须与 manifest 一致；
- 每个分类和总卡牌数量必须与 manifest 一致；
- `schemaVersion` 当前必须为 1；
- 消耗资源只能是 `action_point` 或 `mana`；
- 效果类型必须是解析器支持的类型；
- 强化字段使用 `{"base": 数值, "upgraded": 数值或 null}`；
- 固定不强化的字段也可以直接写一个数值；
- 修改 JSON 后重新进入 Play Mode，让服务重新加载。

加载失败时，Console 会指出文件名和原因，战斗不会使用半加载数据启动。

## 九、目前能测什么

进入 Play Mode 后至少检查：

1. Console 显示加载 43 张牌；
2. 第一回合行动点为 3、手牌为 4、魔法点为 3；
3. 点击强化后魔法点变 2、行动点仍为 3、牌仍在手中；
4. 使用强化牌后按 upgraded 数值结算；
5. 同回合不能再强化或使用另一张魔法牌；
6. 下一回合又能消耗 1 点魔法点；
7. 第三次累计耗蓝出现 `UltimateTriggered`，目标眩晕并返还 3 点；
8. 回合结束时未使用手牌全部弃掉；
9. 抽牌堆不足时会把弃牌堆洗回继续抽。

## 十、当前限制

流血、中毒、易伤、免疫、召唤伙伴和“从弃牌堆由玩家选择”已经能从 JSON 读取，但专属结算还未完成；使用相关牌时会产生 `EffectNotImplemented` 事件。其余已实现与待确认规则见《回合对战设计文档》。


## 十一、CombatTemp 旧场景脚本如何配置

`CombatTemp` 已经改成当前规则层的表现适配器，原有脚本 GUID 保持不变，因此 Card 场景中的组件不会变成 Missing Script。

### CardData

`CardData` 已删除以下字段：

- Card Type
- Attack
- Toughness Reduction

它现在自动绑定当前手牌实例。Hand Index 为 -1 时，会按照同一父物体下所有 `CardData` 的兄弟顺序绑定：

- 第一张场景卡牌绑定 `State.Deck.Hand[0]`
- 第二张绑定 `State.Deck.Hand[1]`
- 依此类推

Card 场景目前只有 Card1 和 Card2 两个卡牌视图，而规则每回合抽 4 张。若要同时操作 4 张手牌，需要再创建两个卡牌视图，或后续制作动态手牌 Prefab 生成器。

卡牌视图可以调用：

```csharp
cardData.TryPlay(enemyStats);
cardData.TryUpgrade(enemyStats);
```

两者最终都会进入 Battle Controller，不会直接改数值。

### 拖拽出牌

一张可拖拽卡牌需要：

- `CardData`
- `DragAttackCard`
- 项目原有的 `KiKs.UI.Draggable`
- 一个能接收 UI 射线的 Graphic
- 场景中存在 EventSystem

建议把 Draggable 的 Return On End 勾选，让出牌失败或结算后卡面回到原位。

敌人头像或其父物体需要挂 `EnemyStats`，并且至少有一个 Graphic 的 Raycast Target 被勾选。拖拽松手时只负责识别敌人，实际伤害、削韧、费用、强化值和处决全部由 JSON 与 Combat Engine 计算。

### 点击出牌

点击出牌对象需要：

- `CardData`
- `ClickAttackCard`
- `Button`

单敌人战斗会自动查找目标。多敌人战斗应通过 `SetTarget` 指定当前目标。

### EnemyStats

`EnemyStats` 不再拥有 Max HP、Current HP、Max Toughness 或 Current Toughness 的 Inspector 数值。

它从 Battle Controller 的 `BattleState` 读取对应敌人：

- 单敌人战斗：Enemy Id 可以留空；
- 多敌人战斗：每个敌人视图必须填写唯一 Enemy Id，与 Combatant Definition 一致。

保留的 `OnHPChanged`、`OnToughnessChanged` 和 `OnDeath` 只用于 UI 与动画。

### 状态条与受击反馈

`StatBarUI` 继续挂在血条或韧性条上：

- Track HP 勾选：显示生命；
- Track HP 不勾选：显示韧性。

`EnemyHitFeedback` 继续挂在敌人头像上。它只在读取到生命下降后播放位移、震动和闪色，不参与伤害计算。
