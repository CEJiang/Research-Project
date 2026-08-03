using System;
using System.Collections.Generic;
using UnityEngine;

#region 1. Root Explicit Reasoning State

[Serializable]
public class ExplicitReasoningState
{
    [Header("Metadata")]
    public string caseID;
    public string graphVersion;
    public long timestamp;

    [Header("1. Global Reasoning Horizon")]
    [Tooltip("玩家目前透過整張推理圖所外顯出的宏觀事件理解。")]
    public GlobalReasoningHorizon globalHorizon =
        new GlobalReasoningHorizon();

    [Header("2. Situated Reasoning Configuration")]
    [Tooltip("玩家建立的情境化推理脈絡，以及脈絡之間的高階關係。")]
    public SituatedReasoningConfiguration reasoningConfiguration =
        new SituatedReasoningConfiguration();

    [Header("3. Expressed Reasoning Issues")]
    [Tooltip("玩家明確外顯，或由目前圖形結構直接呈現的推理問題。")]
    public List<ExpressedReasoningIssue> expressedIssues =
        new List<ExpressedReasoningIssue>();
}

#endregion

#region 2. Global Reasoning Horizon

[Serializable]
public class GlobalReasoningHorizon
{
    /// <summary>
    /// 玩家目前透過整張推理圖所外顯出的整體事件理解。
    ///
    /// 這是 Reasoning Threads 的濃縮摘要，
    /// 不可加入玩家圖中沒有支持的新事件、動機或最終結論。
    /// </summary>
    public string globalSummary;

    /// <summary>
    /// 共同構成目前主要事件理解的 Thread IDs。
    ///
    /// 一個主要事件理解可以由多條互補 Thread 組成，
    /// 因此不限制只能有一條。
    /// </summary>
    public List<string> dominantThreadIDs =
        new List<string>();

    /// <summary>
    /// 玩家目前仍保留的替代推理方向。
    /// </summary>
    public List<string> alternativeThreadIDs =
        new List<string>();

    /// <summary>
    /// Balanced、
    /// ModeratelyConcentrated、
    /// HighlyConcentrated、
    /// Fragmented。
    ///
    /// 由 C# 根據有效 Thread 的分布計算。
    /// </summary>
    public string concentrationState;

    /// <summary>
    /// 玩家目前外顯推理所涵蓋的主要空間與事件進程。
    ///
    /// 例如：
    /// 玩家目前的推理由臥室延伸至入口，
    /// 並涵蓋準備與活動中斷兩個階段。
    /// </summary>
    public string spatialTemporalHorizon;
}

#endregion

#region 3. Situated Reasoning Configuration

[Serializable]
public class SituatedReasoningConfiguration
{
    /// <summary>
    /// 玩家透過多項 Evidence 與多條 Edge，
    /// 聚合形成的情境化推理脈絡。
    /// </summary>
    public List<SituatedReasoningThread> reasoningThreads =
        new List<SituatedReasoningThread>();

    /// <summary>
    /// 不同 Reasoning Thread 之間的高階語意關係。
    /// </summary>
    public List<ReasoningThreadRelation> threadRelations =
        new List<ReasoningThreadRelation>();
}

[Serializable]
public class SituatedReasoningThread
{
    public string threadID;
    public string title;

    /// <summary>
    /// 多項 Evidence 與多條關係共同形成的完整推理命題。
    ///
    /// 不能只是單一 Edge 的自然語言改寫，也不能只是把
    /// 相同 Claim 或相同 Semantic Target 的 Evidence 集合起來。
    /// </summary>
    public string proposition;

    /// <summary>
    /// 此 Thread 主要正在回答的解釋問題。
    ///
    /// 例如：
    /// DeparturePreparedness、
    /// ActivityContinuity、
    /// EntryRoute、
    /// DisturbanceScale、
    /// EventTiming。
    /// </summary>
    public string primarySemanticTarget;

    /// <summary>
    /// 此 Thread 同時涉及，但不是主要焦點的解釋問題。
    /// </summary>
    public List<string> secondarySemanticTargets =
        new List<string>();

    /// <summary>
    /// 玩家目前如何在外顯推理中使用此 Thread。
    ///
    /// Adopted：
    /// 已被納入目前主要事件理解。
    ///
    /// Alternative：
    /// 被保留為替代事件方向。
    ///
    /// Conditional：
    /// 僅在特定條件下成立。
    ///
    /// Uncommitted：
    /// 已形成推理脈絡，但尚未呈現明確採用立場。
    /// </summary>
    public string commitmentRole;

    /// <summary>
    /// 此 Thread 目前的發展狀態。
    ///
    /// Forming：
    /// 正在形成，內容尚不完整。
    ///
    /// Coherent：
    /// 已形成相對連貫的解釋脈絡。
    ///
    /// Contested：
    /// 正受到其他 Thread 挑戰或限制。
    ///
    /// Integrated：
    /// 已與其他 Thread 整合成較完整的事件理解。
    ///
    /// Incomplete：
    /// 存在明確缺少的推理步驟。
    /// </summary>
    public string developmentState;

    /// <summary>
    /// 為何根據玩家目前的圖形配置，
    /// 判斷此 Thread 具有上述角色與狀態。
    /// </summary>
    public string statusBasis;

    /// <summary>
    /// 將案件空間、人物、事件階段與形成機制
    /// 綁定在同一份情境結構中。
    /// </summary>
    public SituatedReasoningScope scope =
        new SituatedReasoningScope();

    /// <summary>
    /// 回指完整推理圖中的來源 Node IDs。
    /// </summary>
    public List<string> sourceNodeIDs =
        new List<string>();

    /// <summary>
    /// 回指共同形成此 Thread 的來源 Edge IDs。
    ///
    /// 一條 Thread 通常由多條 Edge 形成；
    /// 單一 Edge 成為 Thread 應是具有完整解釋性的例外。
    /// </summary>
    public List<string> sourceEdgeIDs =
        new List<string>();
}

#endregion

#region 4. Situated Reasoning Scope

[Serializable]
public class SituatedReasoningScope
{
    /// <summary>
    /// LocalConfiguration：
    /// Evidence 集中於同一局部空間。
    ///
    /// CrossZoneProgression：
    /// Evidence 被玩家組織為跨區域事件進程。
    ///
    /// BoundaryPattern：
    /// Evidence 主要分布於門、窗、入口等空間邊界。
    ///
    /// DistributedPattern：
    /// Evidence 分布於不同區域，但共同構成同一事件脈絡。
    ///
    /// SpatialDiscontinuity：
    /// 空間之間存在尚未說明的推理斷裂。
    ///
    /// Unknown：
    /// 無法由目前圖形判定。
    /// </summary>
    public string configurationType;

    /// <summary>
    /// Ordered：
    /// 各 Phase 具有可辨識的先後順序。
    ///
    /// PartiallyOrdered：
    /// 部分 Phase 可排序，但不是完整線性過程。
    ///
    /// Unordered：
    /// 各 Phase 屬於平行或分布式配置。
    ///
    /// Unknown：
    /// 無法判定順序。
    /// </summary>
    public string sequenceMode;

    /// <summary>
    /// 對整體空間與事件配置的簡短說明。
    ///
    /// 例如：
    /// 玩家將臥室中的整理狀態與入口物品，
    /// 組織為由室內朝向出口發展的準備進程。
    /// </summary>
    public string scopeSummary;

    /// <summary>
    /// 將空間、人物、事件階段與機制綁定的情境階段。
    /// </summary>
    public List<SituatedReasoningPhase> phases =
        new List<SituatedReasoningPhase>();

    /// <summary>
    /// 整條 Thread 成立所依賴的共同條件。
    ///
    /// 例如：
    /// 這些物品屬於同一次離開準備活動。
    /// </summary>
    public List<string> conditions =
        new List<string>();
}

[Serializable]
public class SituatedReasoningPhase
{
    public string phaseID;

    /// <summary>
    /// 在 Ordered 或 PartiallyOrdered 模式下表示階段順序。
    /// Unordered 時只作穩定排序，不代表時間因果。
    /// </summary>
    public int sequenceIndex;

    /// <summary>
    /// 此階段的簡短語意說明。
    ///
    /// 例如：
    /// 玩家將臥室中的打開行李箱理解為整理離開物品的階段。
    /// </summary>
    public string phaseSummary;

    /// <summary>
    /// 此階段涉及的人物或角色。
    /// 資訊不足時使用 Unknown。
    /// </summary>
    public List<string> actors =
        new List<string>();

    /// <summary>
    /// 此階段涉及的案件場景區域。
    ///
    /// 例如：
    /// Bedroom、MainEntrance、DiningArea。
    /// </summary>
    public List<string> zoneIDs =
        new List<string>();

    /// <summary>
    /// 此階段涉及的具體空間錨點。
    ///
    /// 例如：
    /// EntranceTable、DoorBoundary、DiningTable。
    /// </summary>
    public List<string> spatialAnchors =
        new List<string>();

    /// <summary>
    /// 此階段所對應的事件階段。
    ///
    /// 例如：
    /// BeforeDeparture、DuringDeparture、AfterDeparture、Unknown。
    /// </summary>
    public string eventStage;

    /// <summary>
    /// 此階段採用的事件形成機制。
    ///
    /// 例如：
    /// PlannedPreparation、
    /// InterruptedActivity、
    /// ForcedIntrusion、
    /// RoutineActivity、
    /// Unknown。
    /// </summary>
    public string mechanism;
}

#endregion

#region 5. Thread Relations

[Serializable]
public class ReasoningThreadRelation
{
    public string relationID;

    /// <summary>
    /// 關係來源 Thread IDs。
    /// Integrates 等關係可以具有多個來源。
    /// </summary>
    public List<string> sourceThreadIDs =
        new List<string>();

    /// <summary>
    /// 關係指向的 Thread IDs。
    /// 對稱關係也應完整列出參與雙方，不使用空集合代表另一端。
    /// </summary>
    public List<string> targetThreadIDs =
        new List<string>();

    /// <summary>
    /// DevelopsInto：
    /// 一條 Thread 發展為另一段事件脈絡。
    ///
    /// Supports：
    /// 補強另一條 Thread。
    ///
    /// Constrains：
    /// 限制另一條 Thread 的適用範圍或推論強度。
    ///
    /// Challenges：
    /// 對另一條 Thread 提出明確解釋挑戰。
    ///
    /// AlternativeTo：
    /// 針對相同問題形成替代解釋。
    ///
    /// Integrates：
    /// 將多條 Thread 整合成可以共同成立的事件理解。
    /// </summary>
    public string relationType;

    /// <summary>
    /// 為何這些 Thread 形成該高階關係。
    /// </summary>
    public string explanation;

    /// <summary>
    /// 支撐此高階關係的原始 Graph Edge IDs。
    /// </summary>
    public List<string> sourceEdgeIDs =
        new List<string>();
}

#endregion

#region 6. Expressed Reasoning Issues

[Serializable]
public class ExpressedReasoningIssue
{
    public string issueID;

    /// <summary>
    /// 此 Issue 所對應的主要解釋問題。
    /// </summary>
    public string semanticTarget;

    /// <summary>
    /// SurfacedConflict：
    /// 玩家明確建立了衝突或限制關係。
    ///
    /// MissingIntermediateStep：
    /// 推理中缺少必要中介過程。
    ///
    /// UnknownTiming：
    /// 事件順序或時間範圍尚未說明。
    ///
    /// UnspecifiedCondition：
    /// 條件式推理缺少明確成立條件。
    ///
    /// UnresolvedAlternative：
    /// 替代方向尚未被區分或整合。
    ///
    /// SpatialDiscontinuity：
    /// 跨區域推理缺少可理解的空間銜接。
    /// </summary>
    public string issueType;

    /// <summary>
    /// 問題的語意描述。
    /// </summary>
    public string description;

    /// <summary>
    /// ExplicitlySurfaced：
    /// 玩家透過 ConflictsWith 或其他明確操作表達此問題。
    ///
    /// StructurallyExposed：
    /// 問題直接呈現在玩家目前建立的圖形結構中，
    /// 但不能宣稱玩家主觀上已經意識到。
    ///
    /// 完全由系統額外推測的問題，不得進入 ERS。
    /// </summary>
    public string issueOrigin;

    /// <summary>
    /// Open：
    /// 問題目前尚未被處理。
    ///
    /// Acknowledged：
    /// 玩家已外顯問題存在，但尚未提供足夠解釋。
    ///
    /// Contained：
    /// 玩家已透過中介事件、條件、時間或空間區分，
    /// 使原本的問題得到容納。
    /// </summary>
    public string status;

    /// <summary>
    /// 玩家目前如何透過圖形回應此問題。
    /// Open 時可以為空字串。
    /// </summary>
    public string handlingExplanation;

    public List<string> relatedThreadIDs =
        new List<string>();

    public List<string> relatedNodeIDs =
        new List<string>();

    public List<string> relatedEdgeIDs =
        new List<string>();
}

#endregion