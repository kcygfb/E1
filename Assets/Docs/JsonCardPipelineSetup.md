# JSON 卡牌管线与战斗场景配置

更新时间：2026-08-04

## 1. 数据目录

`Assets/StreamingAssets/CardDataV2`

包含：

- `manifest.json`
- `card-data.schema.json`
- `melee.json`
- `ranged.json`
- `magic.json`
- `defense.json`
- `enemy_dog.json`
- `enemy_little_girl.json`
- `enemy_big_eye.json`

当前数据：

| 项目 | 数量 |
|---|---:|
| 分类文件 | 7 |
| 卡牌定义 | 54 |
| copies 总数 | 76 |
| 玩家定义/copies | 42/64 |
| 敌人定义/copies | 12/12 |

## 2. Manifest 编码

Manifest 的中文来源字段现在使用 JSON Unicode 转义：

```json
{
  "sourceWorkbook": "Killkiss\u5168\u5957\u5361\u724c.xlsx",
  "sourceSheet": "\u5361\u724c\u5168\u5957\u5c0f\u6570\u503c"
}
```

文件为纯 ASCII，因此：

- Windows PowerShell 默认读取不会产生乱码；
- `ConvertFrom-Json` 可以直接解析；
- 项目的 `SimpleJsonParser` 会还原为正确中文；
- 不依赖 BOM 或系统代码页。

## 3. CardDatabaseService

场景中需要配置：

- Relative Directory：`CardDataV2`
- Manifest File Name：`manifest.json`
- Load On Awake：按场景启动方式决定

加载流程：

1. UnityWebRequest 读取 Manifest；
2. Repository 取得七个分类文件名；
3. 读取每个分类文件；
4. 验证 Schema、分类、数量、ID、费用、效果和 copies；
5. 转为不可变 `CardSpec`；
6. 只有全部成功才发布 Repository。

项目自己的 `CardJsonRepository` 已验证能加载 54 个定义、76 份 copies 和 12 张敌方牌。

## 4. 当前规则资源

- `Assets/Data/Combat/DefaultRules.asset`
- `Assets/Settings/PlayTime/CombatRules.asset`

关键值：

| 字段 | 值 |
|---|---:|
| Expected Initial Deck Size | 15 |
| Player Action Points Per Turn | 3 |
| Cards Drawn Per Player Turn | 4 |
| Maximum Hand Size | 10 |
| Starting/Maximum Mana | 3/3 |
| Maximum Mana Spend Per Turn | 1 |
| Ultimate Mana Threshold | 3 |
| Ultimate Damage | 30 |
| Execution Damage | 60 |

## 5. 统一出牌入口

玩家输入和 AI 都应提交：

```csharp
battleController.SubmitCardAction(new CombatActionIntent(
    actorId,
    cardInstanceId,
    targetId,
    CombatActionOrigin.PlayerInput)); // 或 EnemyAI
```

兼容方法仍可用：

```csharp
battleController.PlayCard(cardInstanceId, targetId);
battleController.PlayEnemyCard(enemyId, cardInstanceId);
battleController.PlayEnemySpecialCard(enemyId);
```

但这些方法内部仍会转为 `CombatActionIntent`，不会绕过统一流。

## 6. 目标方向

Repository 根据效果推断卡牌方向：

敌对目标包括：

- damage
- toughness_damage
- stun
- vulnerability
- bleed
- poison
- bleed_scaled_damage
- life_steal
- life_steal_max_health

其余自身增益默认作用于发起者。混合卡牌可以同时伤害对手并把减伤/格挡等增益施加给自己，具体由 `CombatFlowController` 按每个效果类型决定。

## 7. AI 与敌人牌组

敌人通过 `EnemyArchetype` 选择 Dog、LittleGirl 或 BigEye 数据。

统一流程：

1. AI 决定是否使用特殊牌；
2. AI 抽牌并选择普通牌；
3. AI 构造行动 Intent；
4. 引擎执行眩晕/跳过授权；
5. 引擎支付敌人行动点并执行出牌限制；
6. 中枢统一结算效果；
7. 事件驱动敌人卡牌表现；
8. AI 弃牌并结束回合。

外部直接调用敌方卡牌也会经过同一授权闸门。

## 8. 调试验收

1. Manifest 可被 PowerShell 默认读取和严格 UTF-8 读取。
2. Repository 加载 54 个定义和 76 份 copies。
3. BigEye 的 `enemy_big_eye_ten_thousand_hands` 被识别为特殊牌。
4. 赛前必须选择 15 张。
5. 玩家和敌人的相同效果产生相同防御/状态事件。
6. 敌人被眩晕时直接调用 `PlayEnemyCard` 仍失败。
7. 敌方眩晕玩家后，玩家下一回合会完整跳过。
8. 流血、中毒、格挡、减伤、无效和反伤都测试双向流向。
9. Unity Test Runner 中执行 Combat EditMode 测试。

## 9. 尚未实现

- 召唤伙伴；
- 易伤的专属数值逻辑；
- 免疫；
- 从弃牌堆选择卡牌；
- 战败 UI 与转场闭环。

这些效果以后只能在 `CombatFlowController` 中实现一次。
