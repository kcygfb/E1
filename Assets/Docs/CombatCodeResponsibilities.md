# Combat 代码职责说明

更新时间：2026-08-04

## 1. 架构边界

战斗模块分为：

1. 输入/AI：只决定要执行的行动；
2. `CombatEngine`：验证权限、阶段、费用、牌组和回合；
3. `CombatFlowController`：统一处理效果、防御、状态、伤害、死亡和事件；
4. `BattleState`：保存战斗状态；
5. Runtime/Presenter：消费事件并播放 UI、VFX、音效和日志；
6. 测试：验证规则方向与边界。

完整设计见《统一战斗流架构》。

## 2. Core

目录：`Assets/Script/Combat/Core`

### CombatFlowController.cs

包含三个关键类型：

- `CombatActionIntent`：玩家输入与 AI 共用的出牌请求；
- `CombatFlowResult`：一次流式结算结果；
- `CombatFlowController`：唯一的效果与数值流中枢。

中枢负责：

- 卡牌效果；
- 普通/多段伤害；
- 韧性伤害；
- 流血、中毒和状态 tick；
- 眩晕、无效、减伤、格挡和反伤；
- 吸血、治疗、抽牌和资源效果；
- 固定攻击、枪械伤害、大招和处决；
- 死亡事件。

运行时代码不应在该文件之外直接修改上述战斗数值。

### CombatEngine.cs

唯一推进阶段和胜负的入口，负责：

- `SubmitCardAction(CombatActionIntent)`；
- 行动来源权限；
- 玩家/敌人回合授权闸门；
- 行动点、玩家魔法值和大招进度；
- 玩家牌组与敌人牌组编排；
- 敌人出牌数量和昂贵卡牌窗口；
- 特殊牌回合与使用次数；
- 阶段切换、胜负和事件派发。

兼容方法：

- `PlayCard`
- `PlayEnemyCard`
- `PlayEnemySpecialCard`

都只构造 Intent 并调用统一入口。

### BattleState.cs

保存：

- 玩家与敌人；
- 玩家牌组与按战斗单位 ID 注册的敌人牌组；
- 敌人特殊牌；
- 玩家魔法值；
- 阶段、结果和回合数。

通用查询：

- `FindCombatant`
- `FindFirstLivingOpponent`
- `GetDeck`
- `RegisterCombatantDeck`

### CombatantState.cs

保存单个战斗单位的：

- 生命、韧性和行动点；
- 眩晕、无效、减伤、跳过；
- 流血、中毒、格挡和反伤。

这些修改方法是中枢的底层原语，不是 UI 或 AI 的业务入口。

### 其他 Core

- `CombatTypes.cs`：阶段、结果、效果和事件枚举；
- `CombatRules.cs`：15 张牌组、60 点处决、30 点大招和敌人分级规则；
- `CardSpec.cs` / `CardEffectSpec.cs`：不可变卡牌规则；
- `CardInstance.cs`：单张实例与强化；
- `DeckState.cs`：抽牌、手牌、弃牌和洗牌；
- `ManaState.cs`：玩家魔法值和大招进度；
- `CombatEvent.cs`：表现层事件。

## 3. Data

目录：`Assets/Script/Combat/Data`

- `CardJsonRepository.cs`：Manifest/分类文件加载、验证和目标推断；
- `SimpleJsonParser.cs`：无外部依赖 JSON 解析器；
- `CombatRulesConfig.cs`：Inspector 规则转核心规则；
- `CombatantDefinition.cs`：单位定义、敌人类型和牌组配置。

目标推断已覆盖普通伤害、削韧、眩晕、易伤、流血、中毒、两种吸血等敌对效果。

## 4. AI

目录：`Assets/Script/Combat/AI`

- `EnemyAIStrategy.cs`：AI 策略基类；
- `SimpleCardAI.cs`：抽牌、选牌、特殊牌和兜底攻击决策。

AI 只能选择动作，不计算实际伤害，也不能直接修改目标状态。即使 AI 忘记调用前置检查，`SubmitCardAction` 仍会执行统一授权闸门。

## 5. Runtime

目录：`Assets/Script/Combat/Runtime`

- `BattleController.cs`：Unity 场景适配器，并暴露 `SubmitCardAction`；
- `CardDatabaseService.cs`：从 StreamingAssets 加载 JSON；
- `CardEnemyAI.cs` / `SimpleEnemyAI.cs`：驱动 AI 回合；
- Presenter/View/VFX：只消费 `CombatEvent`。

玩家 UI 和敌方 AI 最终都进入同一个 Runtime/Core 提交入口。

## 6. JSON 数据

目录：`Assets/StreamingAssets/CardDataV2`

Manifest 当前为 ASCII 安全 JSON，中文来源字段使用 `\uXXXX`：

- 7 个分类；
- 54 个定义；
- 76 份 copies；
- 42 个玩家定义/64 份；
- 12 个敌人定义/12 份。

项目自己的 Repository 已完整加载并验证这些数据。

## 7. Tests

目录：`Assets/Tests/EditMode/Combat`

测试覆盖：

- 牌组与回合；
- 玩家与敌人出牌；
- 双向伤害、格挡、减伤、流血、无效和反伤；
- 玩家/敌人眩晕与跳过闸门；
- 强化、魔法、大招和处决；
- Manifest、玩家牌和敌人牌。

当前纯战斗用例实际执行结果：28 通过、0 失败。

## 8. 扩展规则

新增效果时：

1. 增加 `CardEffectType` 和 JSON 映射；
2. 更新敌对/自身目标推断；
3. 只在 `CombatFlowController.ResolveEffect` 实现一次；
4. 增加玩家方向和敌人方向测试；
5. 禁止在 AI、UI、Presenter 或 `BattleController` 中复制结算代码。
