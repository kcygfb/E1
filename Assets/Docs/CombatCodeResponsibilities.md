# Combat 代码职责说明

## 总体结构

当前战斗代码分为四层：

1. JSON 数据层：读取并验证 43 张静态卡牌；
2. 纯规则层：保存战斗状态并执行回合、出牌、强化和伤害规则；
3. Unity 接入层：加载 StreamingAssets、跨场景传递牌组 ID、连接场景；
4. 测试层：验证牌堆、JSON、强化、魔法点和大招。

## 文件职责

### Core

- `CombatTypes.cs`：阶段、阵营、效果、资源、事件等枚举。
- `CardEffectSpec.cs`：一条 JSON 效果及其基础/强化数值。
- `CardSpec.cs`：从 JSON 得到的不可变卡牌定义。
- `CardInstance.cs`：本场战斗中的实体牌，保存唯一实例 ID 与 `IsUpgraded`。
- `DeckState.cs`：抽牌堆、手牌、弃牌堆、抽牌和洗牌。
- `ManaState.cs`：当前魔法点、每回合消耗、累计大招进度和魔法牌次数。
- `CombatantState.cs`：角色生命、韧性、行动点、眩晕和当前防御状态。
- `BattleState.cs`：整场战斗状态的聚合，包括玩家、敌人、牌堆和魔法点。
- `CombatRules.cs`：从 Inspector 复制出来的不可变规则快照。
- `CombatEvent.cs`：规则层输出给 UI/动画的事实事件。
- `CombatEngine.cs`：唯一允许推进规则的入口，负责出牌、强化、回合、处决、敌人攻击和大招。

### Data

- `SimpleJsonParser.cs`：项目内置的小型 JSON 解析器，不依赖 Newtonsoft.Json。
- `CardJsonRepository.cs`：解析 manifest 和分类文件，验证版本、数量、分类、重复 ID 与效果字段，并提供按 ID 查询。
- `CombatRulesConfig.cs`：可以通过 Create 菜单创建的全局战斗规则资源。
- `CombatantDefinition.cs`：可以通过 Create 菜单创建的玩家/敌人基础属性资源。

旧的 `CardDefinition.cs` 已移除，所以 Create 菜单中不再提供 `Card Definition`。卡牌本身只维护 JSON。

### Runtime

- `CardDatabaseService.cs`：从 `StreamingAssets/CardData` 异步加载牌库；单例可跨场景保留。
- `BattleSession.cs`：在选牌场景与战斗场景之间传递卡牌 ID 列表；不保存强化状态。
- `BattleController.cs`：从选中的 ID 自动创建卡牌实例，创建 `BattleState` 和 `CombatEngine`，向 UI 暴露操作接口。

### Tests

- `CardJsonRepositoryTests.cs`：用项目真实 JSON 验证 43 张牌、强化值和固定数值字段。
- `DeckStateTests.cs`：验证抽牌、手牌上限和中途洗牌。
- `CombatEngineTests.cs`：验证回合、行动点、处决、战斗内强化、每回合耗蓝限制和自动大招。

## 哪些脚本需要挂载

需要挂载：

- `CardDatabaseService`：挂在一个 `GameServices` 对象上；
- `BattleController`：挂在战斗场景的 `BattleRoot` 对象上；
- 你后续编写的 UI、动画和敌人 AI 脚本。

不需要挂载：

- 所有 `Core` 类；
- `CardJsonRepository`、`SimpleJsonParser`；
- `BattleSession`；
- 测试类。

需要创建资源但不挂载：

- `Combat Rules`；
- 玩家与敌人的 `Combatant Definition`。

## 对外常用接口

- 读取全部牌：`cardDatabase.Repository.Cards`
- 按 ID 取牌：`cardDatabase.Repository.GetRequiredCard(cardId)`
- 保存赛前牌组：`BattleSession.SetSelectedDeck(cardIds)`
- 打出牌：`battleController.PlayCard(instanceId, targetId)`
- 强化手牌：`battleController.UpgradeCard(instanceId, targetId)`
- 确认处决：`battleController.ConfirmExecution()`
- 结束玩家回合：`battleController.EndPlayerTurn()`
- 结算敌人攻击：`battleController.ResolveEnemyAttack(enemyId, damage)`
- 完成敌方回合：`battleController.CompleteEnemyTurn()`
- 刷新界面：读取 `battleController.State` 并监听 `CombatEventRaised`

UI 不应直接调用状态对象内部方法改变数值。
