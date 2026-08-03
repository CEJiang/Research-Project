using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region Context Builder Logic

/// <summary>
/// 將玩家目前的推理圖轉換為 ERS 語意分析輸入 X_t^ERS。
///
/// 此類別只負責：
/// 1. 擷取有效 Evidence Node。
/// 2. 擷取並過濾可供 ERS 使用的 Active Edge。
/// 3. 計算並建立圖形結構提示 (Connected Components, Directed Chains, Conflict Edges)。
/// 4. 組合場景空間拓撲 (Spatial Context)。
///
/// 不負責：
/// 1. 產生 Reasoning Thread。
/// 2. 產生正式 ERS。
/// 3. 判定案件真相或 Hypothesis。
/// </summary>
public sealed class ExplicitReasoningStateContextBuilder
{
    /// <summary>
    /// Edge Interpretation Confidence 的最低門檻。
    /// 目前暫時以 EdgeInterpretationResult.confidence 作為 ERS Active Edge 的品質依據。
    /// 後續接上正式 Edge Evaluation 後，應改用 q_P(e) 與正式 validity 判定。
    /// </summary>
    private readonly float activeEdgeThreshold;

    /// <summary>
    /// 開發測試期間，是否允許尚未完成 Interpretation Evaluation 的 Edge 暫時進入 X_t^ERS。
    /// 正式版本應設為 false。
    /// </summary>
    private readonly bool allowUnevaluatedEdgesForDebug;

    public ExplicitReasoningStateContextBuilder(
        float activeEdgeThreshold = 0.60f,
        bool allowUnevaluatedEdgesForDebug = false)
    {
        this.activeEdgeThreshold = Mathf.Clamp01(activeEdgeThreshold);
        this.allowUnevaluatedEdgesForDebug = allowUnevaluatedEdgesForDebug;
    }

    /// <summary>
    /// 建立當前推理圖的 ERS Analysis Input (X_t^ERS)。
    /// </summary>
    public ExplicitReasoningStateAnalysisInput Build(
        string caseID,
        string graphVersion)
    {
        if (string.IsNullOrWhiteSpace(caseID))
        {
            throw new ArgumentException("caseID cannot be null or empty.", nameof(caseID));
        }

        if (string.IsNullOrWhiteSpace(graphVersion))
        {
            throw new ArgumentException("graphVersion cannot be null or empty.", nameof(graphVersion));
        }

        List<ReasoningGraphNode> graphNodes = ReasoningGraphManager.Instance.GetReasoningNodesSnapshot();
        List<ReasoningGraphEdge> graphEdges = ReasoningGraphManager.Instance.GetReasoningEdgesSnapshot();

        List<ERSAnalysisNode> analysisNodes = BuildAnalysisNodes(graphNodes);
        List<ERSAnalysisEdge> activeEdges = BuildActiveEdges(graphEdges);

        ERSStructuralHints structuralHints = BuildStructuralHints(analysisNodes, activeEdges);

        return new ExplicitReasoningStateAnalysisInput
        {
            caseID = caseID.Trim(),
            graphVersion = graphVersion.Trim(),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

            nodes = analysisNodes,
            activeEdges = activeEdges,

            sceneSpatialContext = SceneSpatialContextDataLoader.Instance.SceneSpatialContext,
            structuralHints = structuralHints
        };
    }

    #region Node & Edge Processing

    private List<ERSAnalysisNode> BuildAnalysisNodes(IReadOnlyList<ReasoningGraphNode> graphNodes)
    {
        List<ERSAnalysisNode> results = new();

        if (graphNodes == null) return results;

        foreach (ReasoningGraphNode graphNode in graphNodes)
        {
            if (graphNode?.evidence == null || string.IsNullOrWhiteSpace(graphNode.evidence.evidenceID))
            {
                continue;
            }

            Evidence evidence = graphNode.evidence;

            results.Add(new ERSAnalysisNode
            {
                // 目前 ReasoningGraphNode 沒有獨立 Node ID，暫時直接使用 Evidence ID
                nodeID = evidence.evidenceID.Trim(),
                evidenceID = evidence.evidenceID.Trim(),
                evidenceName = evidence.displayNameEn?.Trim() ?? string.Empty,
                trueFacts = ExtractTrueFacts(evidence),
                zoneID = evidence.zoneAt.ToString(),
                spatialContext = evidence.spatialContext
            });
        }

        return results
            .OrderBy(node => node.nodeID, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<ERSAnalysisEdge> BuildActiveEdges(IReadOnlyList<ReasoningGraphEdge> graphEdges)
    {
        List<ERSAnalysisEdge> results = new();

        if (graphEdges == null) return results;

        foreach (ReasoningGraphEdge graphEdge in graphEdges)
        {
            if (!TryBuildAnalysisEdge(graphEdge, out ERSAnalysisEdge analysisEdge))
            {
                continue;
            }

            if (!analysisEdge.isActiveForERS)
            {
                continue;
            }

            results.Add(analysisEdge);
        }

        return results
            .OrderBy(edge => edge.edgeID, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool TryBuildAnalysisEdge(
        ReasoningGraphEdge graphEdge,
        out ERSAnalysisEdge analysisEdge)
    {
        analysisEdge = null;

        if (graphEdge?.fromNode?.evidence == null || graphEdge?.toNode?.evidence == null)
        {
            return false;
        }

        string sourceNodeID = graphEdge.fromNode.evidence.evidenceID;
        string targetNodeID = graphEdge.toNode.evidence.evidenceID;

        if (string.IsNullOrWhiteSpace(sourceNodeID) ||
            string.IsNullOrWhiteSpace(targetNodeID) ||
            string.Equals(sourceNodeID, targetNodeID, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string edgeID = graphEdge.edgeID;
        if (string.IsNullOrWhiteSpace(edgeID))
        {
            return false;
        }

        bool hasEvaluation = ReasoningGraphManager.Instance.TryGetEdgeInterpretationResult(
            edgeID, 
            out EdgeInterpretationResult evaluation);

        float qualityScore = hasEvaluation ? Mathf.Clamp01(evaluation.confidence) : 0f;

        bool isActiveForERS = hasEvaluation
            ? qualityScore >= activeEdgeThreshold
            : allowUnevaluatedEdgesForDebug;

        analysisEdge = new ERSAnalysisEdge
        {
            edgeID = edgeID.Trim(),
            sourceNodeID = sourceNodeID.Trim(),
            targetNodeID = targetNodeID.Trim(),
            relationType = GetRelationTypeName(graphEdge.reasoningGraphType),
            directed = graphEdge.reasoningGraphType == ReasoningGraphType.LEADTO,
            playerAssertion = BuildPlayerAssertion(graphEdge),
            evaluatedProposition = (hasEvaluation && !string.IsNullOrWhiteSpace(evaluation.interpretation))
                ? evaluation.interpretation.Trim()
                : string.Empty,
            qualityScore = qualityScore,
            evaluationState = hasEvaluation ? "Evaluated" : "Unevaluated",
            isActiveForERS = isActiveForERS
        };

        return true;
    }

    private List<string> ExtractTrueFacts(Evidence evidence)
    {
        List<string> facts = new();
        if (evidence == null) return facts;

        string factsText = evidence.GetFactsAsStringForLLM();
        if (string.IsNullOrWhiteSpace(factsText)) return facts;

        string[] lines = factsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawLine in lines)
        {
            string fact = rawLine.Trim();

            if (fact.StartsWith("-"))
            {
                fact = fact.Substring(1).Trim();
            }

            if (string.IsNullOrWhiteSpace(fact) ||
                string.Equals(fact, "None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            facts.Add(fact);
        }

        return facts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    #endregion

    #region Structural Graph Algorithms (BFS / DFS Hints)

    private ERSStructuralHints BuildStructuralHints(
        IReadOnlyList<ERSAnalysisNode> nodes,
        IReadOnlyList<ERSAnalysisEdge> activeEdges)
    {
        return new ERSStructuralHints
        {
            connectedComponents = BuildConnectedComponents(nodes, activeEdges),
            directedChains = BuildDirectedChains(activeEdges),
            explicitConflictEdgeIDs = activeEdges
                .Where(edge => edge.relationType == "ConflictsWith")
                .Select(edge => edge.edgeID)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(edgeID => edgeID, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private List<ERSConnectedComponentHint> BuildConnectedComponents(
        IReadOnlyList<ERSAnalysisNode> nodes,
        IReadOnlyList<ERSAnalysisEdge> activeEdges)
    {
        List<ERSConnectedComponentHint> results = new();
        Dictionary<string, HashSet<string>> adjacency = new(StringComparer.OrdinalIgnoreCase);

        foreach (ERSAnalysisNode node in nodes)
        {
            adjacency[node.nodeID] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (ERSAnalysisEdge edge in activeEdges)
        {
            if (!adjacency.ContainsKey(edge.sourceNodeID))
                adjacency[edge.sourceNodeID] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!adjacency.ContainsKey(edge.targetNodeID))
                adjacency[edge.targetNodeID] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            adjacency[edge.sourceNodeID].Add(edge.targetNodeID);
            adjacency[edge.targetNodeID].Add(edge.sourceNodeID);
        }

        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        int componentIndex = 1;

        foreach (string startNodeID in adjacency.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            // 沒有 Active Edge 的孤立 Node 不建立 Component 提示
            if (adjacency[startNodeID].Count == 0 || visited.Contains(startNodeID))
            {
                continue;
            }

            Queue<string> queue = new();
            HashSet<string> componentNodeIDs = new(StringComparer.OrdinalIgnoreCase);

            queue.Enqueue(startNodeID);
            visited.Add(startNodeID);

            while (queue.Count > 0)
            {
                string currentNodeID = queue.Dequeue();
                componentNodeIDs.Add(currentNodeID);

                foreach (string nextNodeID in adjacency[currentNodeID])
                {
                    if (visited.Add(nextNodeID))
                    {
                        queue.Enqueue(nextNodeID);
                    }
                }
            }

            List<string> componentEdgeIDs = activeEdges
                .Where(edge => componentNodeIDs.Contains(edge.sourceNodeID) && 
                               componentNodeIDs.Contains(edge.targetNodeID))
                .Select(edge => edge.edgeID)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            results.Add(new ERSConnectedComponentHint
            {
                componentID = $"COMPONENT_{componentIndex:D2}",
                nodeIDs = componentNodeIDs.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList(),
                edgeIDs = componentEdgeIDs
            });

            componentIndex++;
        }

        return results;
    }

    private List<ERSDirectedChainHint> BuildDirectedChains(IReadOnlyList<ERSAnalysisEdge> activeEdges)
    {
        List<ERSAnalysisEdge> directedEdges = activeEdges
            .Where(edge => edge.directed && edge.relationType == "LeadsTo")
            .ToList();

        List<ERSDirectedChainHint> results = new();
        if (directedEdges.Count == 0) return results;

        Dictionary<string, List<ERSAnalysisEdge>> adjacency = directedEdges
            .GroupBy(edge => edge.sourceNodeID, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(edge => edge.edgeID, StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        Dictionary<string, int> indegree = new(StringComparer.OrdinalIgnoreCase);

        foreach (ERSAnalysisEdge edge in directedEdges)
        {
            if (!indegree.ContainsKey(edge.sourceNodeID)) indegree[edge.sourceNodeID] = 0;
            if (!indegree.ContainsKey(edge.targetNodeID)) indegree[edge.targetNodeID] = 0;

            indegree[edge.targetNodeID]++;
        }

        List<string> startNodeIDs = adjacency.Keys
            .Where(nodeID => !indegree.TryGetValue(nodeID, out int val) || val == 0)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 若沒有 indegree = 0，代表可能存在內部 Cycle
        if (startNodeIDs.Count == 0)
        {
            startNodeIDs = adjacency.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        }

        HashSet<string> chainSignatures = new(StringComparer.OrdinalIgnoreCase);

        foreach (string startNodeID in startNodeIDs)
        {
            TraverseDirectedChain(
                currentNodeID: startNodeID,
                adjacency: adjacency,
                pathNodeIDs: new List<string>(),
                pathEdgeIDs: new List<string>(),
                pathNodeSet: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                chainSignatures: chainSignatures,
                results: results);
        }

        for (int index = 0; index < results.Count; index++)
        {
            results[index].chainID = $"CHAIN_{index + 1:D2}";
        }

        return results;
    }

    private void TraverseDirectedChain(
        string currentNodeID,
        IReadOnlyDictionary<string, List<ERSAnalysisEdge>> adjacency,
        List<string> pathNodeIDs,
        List<string> pathEdgeIDs,
        HashSet<string> pathNodeSet,
        HashSet<string> chainSignatures,
        List<ERSDirectedChainHint> results)
    {
        pathNodeIDs.Add(currentNodeID);
        pathNodeSet.Add(currentNodeID);

        bool hasOutgoingEdges = adjacency.TryGetValue(currentNodeID, out List<ERSAnalysisEdge> outgoingEdges) && outgoingEdges.Count > 0;
        bool extended = false;

        if (hasOutgoingEdges)
        {
            foreach (ERSAnalysisEdge edge in outgoingEdges)
            {
                if (pathNodeSet.Contains(edge.targetNodeID))
                {
                    List<string> cycleNodeIDs = new(pathNodeIDs) { edge.targetNodeID };
                    List<string> cycleEdgeIDs = new(pathEdgeIDs) { edge.edgeID };

                    AddDirectedChain(cycleNodeIDs, cycleEdgeIDs, containsCycle: true, chainSignatures, results);
                    continue;
                }

                extended = true;
                pathEdgeIDs.Add(edge.edgeID);

                TraverseDirectedChain(
                    edge.targetNodeID,
                    adjacency,
                    pathNodeIDs,
                    pathEdgeIDs,
                    pathNodeSet,
                    chainSignatures,
                    results);

                pathEdgeIDs.RemoveAt(pathEdgeIDs.Count - 1);
            }
        }

        if (!extended && pathEdgeIDs.Count > 0)
        {
            AddDirectedChain(pathNodeIDs, pathEdgeIDs, containsCycle: false, chainSignatures, results);
        }

        pathNodeSet.Remove(currentNodeID);
        pathNodeIDs.RemoveAt(pathNodeIDs.Count - 1);
    }

    private void AddDirectedChain(
        IReadOnlyList<string> nodeIDs,
        IReadOnlyList<string> edgeIDs,
        bool containsCycle,
        HashSet<string> chainSignatures,
        List<ERSDirectedChainHint> results)
    {
        string signature = string.Join(">", nodeIDs) + "|" + string.Join(">", edgeIDs) + "|" + containsCycle;

        if (!chainSignatures.Add(signature))
        {
            return;
        }

        results.Add(new ERSDirectedChainHint
        {
            nodeIDs = new List<string>(nodeIDs),
            edgeIDs = new List<string>(edgeIDs),
            containsCycle = containsCycle
        });
    }

    #endregion

    #region Helper String Formatter Methods

    private string BuildPlayerAssertion(ReasoningGraphEdge edge)
    {
        string sourceName = edge.fromNode.evidence.displayNameEn;
        string targetName = edge.toNode.evidence.displayNameEn;

        return edge.reasoningGraphType switch
        {
            ReasoningGraphType.LEADTO => $"{sourceName} leads to {targetName}.",
            ReasoningGraphType.CONFLICT => $"{sourceName} conflicts with {targetName}.",
            ReasoningGraphType.COHERENT => $"{sourceName} is consistent with {targetName}.",
            _ => $"{sourceName} has an unspecified relation with {targetName}."
        };
    }

    private string GetRelationTypeName(ReasoningGraphType reasoningGraphType)
    {
        return reasoningGraphType switch
        {
            ReasoningGraphType.LEADTO => "LeadsTo",
            ReasoningGraphType.CONFLICT => "ConflictsWith",
            ReasoningGraphType.COHERENT => "ConsistentWith",
            _ => "Unknown"
        };
    }

    #endregion
}

#endregion

#region Data Models - Analysis Input (X_t^ERS)

/// <summary>
/// 提供給 ERS 語意分析 LLM 的結構化輸入。
/// 此資料不是正式 ERS，而是從目前推理圖提取出的分析材料 X_t^ERS。
/// </summary>
[Serializable]
public class ExplicitReasoningStateAnalysisInput
{
    public string caseID;
    public string graphVersion;
    public long timestamp;

    /// <summary>
    /// 玩家目前已放入推理圖中的 Evidence Nodes。
    /// </summary>
    public List<ERSAnalysisNode> nodes = new();

    /// <summary>
    /// 經結構檢查與 Interpretation Quality 過濾後，可供 ERS 使用的 Active Edges。
    /// </summary>
    public List<ERSAnalysisEdge> activeEdges = new();
    public SceneSpatialContext sceneSpatialContext = new();

    /// <summary>
    /// 由 C# 預先計算的圖形結構提示（僅輔助 LLM，不等於最終 Thread）。
    /// </summary>
    public ERSStructuralHints structuralHints = new();
}

[Serializable]
public class ERSAnalysisNode
{
    /// <summary>
    /// 目前暫時與 evidenceID 相同。
    /// </summary>
    public string nodeID;

    public string evidenceID;
    public string evidenceName;
    public List<string> trueFacts = new();

    /// <summary>
    /// Evidence 所在案件場景區域。
    /// </summary>
    public string zoneID;

    /// <summary>
    /// Evidence 在案件空間中的局部位置描述。
    /// </summary>
    public string spatialContext;
}

[Serializable]
public class ERSAnalysisEdge
{
    public string edgeID;

    public string sourceNodeID;
    public string targetNodeID;

    /// <summary>
    /// LeadsTo、ConflictsWith、ConsistentWith。
    /// </summary>
    public string relationType;

    public bool directed;

    /// <summary>
    /// 玩家透過建立此 Edge 所明確外顯的局部關係。
    /// </summary>
    public string playerAssertion;

    /// <summary>
    /// Edge Evaluation 對完整 Edge 所形成的語意解讀。
    /// </summary>
    public string evaluatedProposition;

    /// <summary>
    /// 目前暫時使用 EdgeInterpretationResult.confidence。
    /// 後續應改成正式的 q_P(e)。
    /// </summary>
    public float qualityScore;

    /// <summary>
    /// Evaluated、Unevaluated。
    /// </summary>
    public string evaluationState;

    /// <summary>
    /// 此 Edge 是否通過 ERS 使用門檻。
    /// </summary>
    public bool isActiveForERS;
}

#endregion



#region Data Models - Structural Hints

[Serializable]
public class ERSStructuralHints
{
    /// <summary>
    /// 將 Active Edges 視為無向結構後形成的連通群組。
    /// </summary>
    public List<ERSConnectedComponentHint> connectedComponents = new();

    /// <summary>
    /// 由 Active LeadsTo Edges 構成的有向路徑。
    /// </summary>
    public List<ERSDirectedChainHint> directedChains = new();

    /// <summary>
    /// 玩家明確建立且通過 Active Edge 門檻的 ConflictsWith Edge IDs。
    /// </summary>
    public List<string> explicitConflictEdgeIDs = new();
}

[Serializable]
public class ERSConnectedComponentHint
{
    public string componentID;

    public List<string> nodeIDs = new();
    public List<string> edgeIDs = new();
}

[Serializable]
public class ERSDirectedChainHint
{
    public string chainID;

    /// <summary>
    /// 依 LeadsTo 方向排列。
    /// </summary>
    public List<string> nodeIDs = new();

    /// <summary>
    /// 與 nodeIDs 順序對應的 Edge IDs。
    /// </summary>
    public List<string> edgeIDs = new();

    public bool containsCycle;
}

#endregion

