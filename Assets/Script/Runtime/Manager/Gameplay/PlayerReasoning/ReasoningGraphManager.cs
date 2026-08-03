using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radishmouse;
using UnityEngine;

public class ReasoningGraphManager : Singleton<ReasoningGraphManager>
{
    Dictionary<string, ReasoningGraphNode> evidenceInGraph = new();
    Dictionary<(string, string), ReasoningGraphEdge> edgesInGraph = new();
    private Dictionary<string, EdgeInterpretationResult> edgeInterpretationResults = new();
    Dictionary<string, GameObject> nodeToEvidenceListItem = new();

    List<ReasoningGraphEdge> leadToEdges = new();
    List<ReasoningGraphEdge> conflictEdges = new();
    List<ReasoningGraphEdge> coherentEdges = new();

    [Header("Dot Colors")]
    public Color leadToColor = new(0.65f, 0.90f, 0.65f, 1f);
    public Color conflictColor = new(0.80f, 0.35f, 0.35f, 1f);
    public Color coherentColor = new(0.45f, 0.85f, 0.95f, 1f);

    [Header("Edge Highlight Colors")]
    public Color leadToHighlightColor = new(0.90f, 1.00f, 0.90f, 1f);
    public Color conflictHighlightColor = new(1.00f, 0.55f, 0.55f, 1f);
    public Color coherentHighlightColor = new(0.75f, 0.95f, 1.00f, 1f);

    public ReasoningGraphType currentReasoningGraphType = ReasoningGraphType.NONE;
    public ReasoningGraphNode firstNode = null;
    public ReasoningGraphPreviewEdge reasoningGraphPreviewEdge;
    public ReasoningGraphEdge reasoningGraphHightlightEdge;
    public ReasoningGraphEdge currentSelectedEdge;
    public GameObject reasoningGraphHightlightNode;
    public ReasoningGraphNode currentSelectedNode;

    public ReasoningGraphButtonController reasoningGraphButtonController;

    void Start()
    {
        reasoningGraphHightlightEdge.gameObject.SetActive(false);
        reasoningGraphHightlightNode.SetActive(false);
    }

    void Update()
    {
        if (currentSelectedNode != null && reasoningGraphHightlightNode.activeSelf)
        {
            UpdateHightlightNodePosition();
        }
    }

    public void SetCurrentReasoningGraphType(ReasoningGraphType type)
    {
        currentReasoningGraphType = type;
        if (type == ReasoningGraphType.NONE)
        {
            firstNode = null;
            reasoningGraphPreviewEdge.EndPreview();
        }
        reasoningGraphButtonController.RefreshButtonVisual();
    }

    public void AddReasoningGraphNode(Evidence evidence, Vector2 localPosition = default(Vector2), GameObject evidenceListItem = null)
    {
        if (!evidenceInGraph.ContainsKey(evidence.evidenceID))
        {
            ReasoningGraphNode node = UIManager.Instance.playerReasoningUI.reasoningGraphUI.AddReasoningGraphNode(evidence, localPosition);
            evidenceInGraph[evidence.evidenceID] = node;

            if (evidenceListItem != null)
            {
                nodeToEvidenceListItem[evidence.evidenceID] = evidenceListItem;

                // Change the evidence list item font color to white
                var text = evidenceListItem.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)                
                {
                    text.color = Color.white;
                }
            }
        }
    }
    
    public void OnReasoningGraphEdgeClicked(ReasoningGraphEdge edge)
    {
        if (currentSelectedNode != null)
        {
            currentSelectedNode = null;
            reasoningGraphHightlightNode.SetActive(false);
        }

        if (currentSelectedEdge == edge)
        {
            // If the same edge is clicked again, we can consider it as unselecting the edge
            currentSelectedEdge = null;
            reasoningGraphHightlightEdge.gameObject.SetActive(false);
            return;
        }

        currentSelectedEdge = edge;
        // Highlight the clicked edge and show details in the UI
        reasoningGraphHightlightEdge.gameObject.SetActive(true);
        reasoningGraphHightlightEdge.fromNode = edge.fromNode;
        reasoningGraphHightlightEdge.toNode = edge.toNode;

        switch (edge.reasoningGraphType)
        {
            case ReasoningGraphType.LEADTO:
                reasoningGraphHightlightEdge.GetComponent<UILineRenderer>().color = leadToHighlightColor;
                reasoningGraphHightlightEdge.GetComponent<ReasoningGraphEdge>().reasoningGraphType = ReasoningGraphType.LEADTO;
                break;
            case ReasoningGraphType.CONFLICT:
                reasoningGraphHightlightEdge.GetComponent<UILineRenderer>().color = conflictHighlightColor;
                reasoningGraphHightlightEdge.GetComponent<ReasoningGraphEdge>().reasoningGraphType = ReasoningGraphType.CONFLICT;    
                break;
            case ReasoningGraphType.COHERENT:
                reasoningGraphHightlightEdge.GetComponent<UILineRenderer>().color = coherentHighlightColor;
                reasoningGraphHightlightEdge.GetComponent<ReasoningGraphEdge>().reasoningGraphType = ReasoningGraphType.COHERENT;
                break;
        }
    }

    public void OnReasoningGraphNodeClicked(ReasoningGraphNode node)
    {
        if (currentReasoningGraphType == ReasoningGraphType.NONE) 
            HightlightNode(node);
        else 
            HandleReasoningNodeClicked(node);
    }

    public void HightlightNode(ReasoningGraphNode node)
    {
        if (currentSelectedEdge != null)
        {
            currentSelectedEdge = null;
            reasoningGraphHightlightEdge.gameObject.SetActive(false);
        }

        if (currentSelectedNode == node)
        {
            // If the same node is clicked again, we can consider it as unselecting the node
            currentSelectedNode = null;
            reasoningGraphHightlightNode.SetActive(false);
            return;
        }
        
        currentSelectedNode = node;
        reasoningGraphHightlightNode.SetActive(true);
        reasoningGraphHightlightNode.GetComponent<RectTransform>().anchoredPosition = node.GetComponent<RectTransform>().anchoredPosition;
        reasoningGraphHightlightNode.GetComponent<RectTransform>().sizeDelta = node.GetComponent<RectTransform>().sizeDelta + new Vector2(10, 10); // Add some padding for highlight
    }

    public void HandleReasoningNodeClicked(ReasoningGraphNode node)
    {
        if (firstNode == null)
        {
            firstNode = node;
            reasoningGraphPreviewEdge.BeginPreview(node, currentReasoningGraphType);
        }
        else
        {
            string firstEvidenceID = firstNode.evidence.evidenceID;
            string secondEvidenceID = node.evidence.evidenceID;
            var edgeKey = BuildEdgeKey(firstEvidenceID, secondEvidenceID);

            if (firstNode != node && !edgesInGraph.ContainsKey(edgeKey))
            {
                ReasoningGraphEdge edge = UIManager.Instance.playerReasoningUI.reasoningGraphUI.AddReasoningGraphEdge(firstNode, node, currentReasoningGraphType);

                // Cause we want to toggle the reasoning graph type in the reasoning graph, so we need to clarify the reasoning graph type when adding edge
                // If I just want to see the lead to reasoning, we can temporarily hide the other two types of edges in the UI, but the reasoning graph type is still there,
                // which means when I switch to see the conflict reasoning, the conflict edge will show up immediately without needing to add edge again
                switch (currentReasoningGraphType)
                {
                    case ReasoningGraphType.LEADTO:
                        leadToEdges.Add(edge);
                        break;
                    case ReasoningGraphType.CONFLICT:
                        conflictEdges.Add(edge);
                        break;
                    case ReasoningGraphType.COHERENT:
                        coherentEdges.Add(edge);
                        break;
                }

                edgesInGraph[edgeKey] = edge;
                edge.edgeID = BuildEdgeID(edge.fromNode.evidence.evidenceID, edge.reasoningGraphType, edge.toNode.evidence.evidenceID);
            }

            firstNode = null;
            reasoningGraphPreviewEdge.EndPreview();
        }
    }
    
    private (string, string) BuildEdgeKey(
        string evidenceAID,
        string evidenceBID)
    {
        return string.CompareOrdinal(evidenceAID, evidenceBID) <= 0
            ? (evidenceAID, evidenceBID)
            : (evidenceBID, evidenceAID);
    }

    public void UpdateHightlightNodePosition()
    {
        reasoningGraphHightlightNode.GetComponent<RectTransform>().anchoredPosition = currentSelectedNode.GetComponent<RectTransform>().anchoredPosition;
    }

    public void DeleteSelectedNode()
    {
        if (currentSelectedNode != null)
        {
            // Remove from graph data structure
            string removedEvidenceID = currentSelectedNode.evidence.evidenceID;
            evidenceInGraph.Remove(removedEvidenceID);

            // Remove the corresponding node in the UI and all edges connected to it
            UIManager.Instance.playerReasoningUI.reasoningGraphUI.RemoveReasoningGraphNode(currentSelectedNode);

            // Also need to remove all edges connected to this node
            var edgesToRemove = new List<ReasoningGraphEdge>();
            foreach (var edge in edgesInGraph.Values)
            {
                if (edge.fromNode == currentSelectedNode || edge.toNode == currentSelectedNode)
                {
                    edgesToRemove.Add(edge);
                }
            }
            foreach (var edge in edgesToRemove)
            {
                RemoveReasoningGraphEdge(edge);
            }

            // Change the evidence list item font color to gray
            if (nodeToEvidenceListItem.TryGetValue(currentSelectedNode.evidence.evidenceID, out GameObject evidenceListItem))
            {
                var text = evidenceListItem.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    text.color = Color.black;
                }
            }

            currentSelectedNode = null;
            reasoningGraphHightlightNode.SetActive(false);
        }
    }
    public void DeleteSelectedEdge()
    {
        if (currentSelectedEdge != null)
        {
            // Remove from graph data structure
            edgesInGraph.Remove(BuildEdgeKey(currentSelectedEdge.fromNode.evidence.evidenceID, currentSelectedEdge.toNode.evidence.evidenceID));

            // Remove from edge type list
            switch (currentSelectedEdge.reasoningGraphType)
            {
                case ReasoningGraphType.LEADTO:
                    leadToEdges.Remove(currentSelectedEdge);
                    break;
                case ReasoningGraphType.CONFLICT:
                    conflictEdges.Remove(currentSelectedEdge);
                    break;
                case ReasoningGraphType.COHERENT:
                    coherentEdges.Remove(currentSelectedEdge);
                    break;
            }

            // Remove the corresponding edge in the UI
            UIManager.Instance.playerReasoningUI.reasoningGraphUI.RemoveReasoningGraphEdge(currentSelectedEdge);
            currentSelectedEdge = null;
            reasoningGraphHightlightEdge.gameObject.SetActive(false);
        }
    }

    public void RemoveReasoningGraphEdge(ReasoningGraphEdge edge)
    {
        // Remove from graph data structure
        edgesInGraph.Remove(BuildEdgeKey(edge.fromNode.evidence.evidenceID, edge.toNode.evidence.evidenceID));

        // Remove from edge type list
        switch (edge.reasoningGraphType)
        {
            case ReasoningGraphType.LEADTO:
                leadToEdges.Remove(edge);
                break;
            case ReasoningGraphType.CONFLICT:
                conflictEdges.Remove(edge);
                break;
            case ReasoningGraphType.COHERENT:
                coherentEdges.Remove(edge);
                break;
        }

        // Remove the corresponding edge in the UI
        UIManager.Instance.playerReasoningUI.reasoningGraphUI.RemoveReasoningGraphEdge(edge);
    }

    public string GetReasoningGraphDataForLLM()
    {
        StringBuilder sb = new();

        List<ReasoningGraphNode> validNodes = evidenceInGraph.Values
                                            .Where(node =>
                                                node != null &&
                                                node.evidence != null &&
                                                !string.IsNullOrWhiteSpace(
                                                    node.evidence.evidenceID
                                                )
                                            )
                                            .OrderBy(
                                                node => node.evidence.evidenceID,
                                                StringComparer.OrdinalIgnoreCase
                                            )
                                            .ToList();

        List<ReasoningGraphEdge> uniqueEdges =
            GetUniqueEdges();

        sb.AppendLine("NODES:");

        if (validNodes.Count == 0)
        {
            sb.AppendLine("- None");
        }

        foreach (ReasoningGraphNode node in validNodes)
        {
            AppendNodeForLLM(sb, node);
            sb.AppendLine();
        }

        sb.AppendLine("PLAYER-CREATED EDGES:");

        if (uniqueEdges.Count == 0)
        {
            sb.AppendLine("- None");
        }

        for (int index = 0; index < uniqueEdges.Count; index++)
        {
            AppendEdgeForLLM(
                sb,
                uniqueEdges[index],
                index + 1
            );

            sb.AppendLine();
        }

        sb.AppendLine("PLAYER-DEFINED LEAD-TO CHAINS:");
        sb.AppendLine(BuildReasoningGraphLogicChains());

        return sb.ToString().TrimEnd();
    }

    private void AppendNodeForLLM(
        StringBuilder sb,
        ReasoningGraphNode node
    )
    {
        Evidence evidence = node.evidence;

        sb.AppendLine($"NodeID: {evidence.evidenceID}");
        sb.AppendLine($"EvidenceName: {evidence.displayNameEn}");
        sb.AppendLine($"Zone: {evidence.zoneAt}");
        sb.AppendLine($"SpatialContext: {evidence.spatialContext}");

        AppendEvidenceFacts(sb, evidence);
        AppendEvidenceClaims(sb, evidence);
    }

    private void AppendEdgeForLLM(
        StringBuilder sb,
        ReasoningGraphEdge edge,
        int index
    )
    {
        if (edge?.fromNode?.evidence == null ||
            edge.toNode?.evidence == null)
        {
            return;
        }
        string edgeID = edge.edgeID;
        string sourceID =
            edge.fromNode.evidence.evidenceID;
        string targetID =
            edge.toNode.evidence.evidenceID;

        bool isDirected =
            edge.reasoningGraphType ==
            ReasoningGraphType.LEADTO;
        sb.AppendLine($"EdgeID: {edgeID}");
        sb.AppendLine($"SourceEvidenceID: {sourceID}");
        sb.AppendLine($"TargetEvidenceID: {targetID}");

        sb.AppendLine(
            $"PlayerReasoningType: " +
            $"{GetReasoningGraphTypeForLLM(edge.reasoningGraphType)}"
        );

        sb.AppendLine(
            $"Directed: " +
            $"{isDirected.ToString().ToLowerInvariant()}"
        );

        sb.AppendLine(
            $"PlayerReasoningAssertion: " +
            $"{BuildPlayerAssertion(edge)}"
        );
    }
    
    private string BuildPlayerAssertion(
        ReasoningGraphEdge edge
    )
    {
        string sourceName =
            edge.fromNode.evidence.displayNameEn;

        string targetName =
            edge.toNode.evidence.displayNameEn;

        return edge.reasoningGraphType switch
        {
            ReasoningGraphType.LEADTO =>
                $"{sourceName} leads to {targetName}.",

            ReasoningGraphType.CONFLICT =>
                $"{sourceName} conflicts with {targetName}.",

            ReasoningGraphType.COHERENT =>
                $"{sourceName} is consistent with {targetName}.",

            _ =>
                $"{sourceName} has an unspecified reasoning with {targetName}."
        };
    }

    private void AppendEvidenceFacts(
        StringBuilder sb,
        Evidence evidence)
    {
        sb.AppendLine("TrueFacts:");

        if (evidence == null ||
            evidence.facts == null ||
            evidence.facts.Count == 0)
        {
            sb.AppendLine("- None");
            return;
        }

        string factsText = evidence.GetFactsAsStringForLLM();

        if (string.IsNullOrWhiteSpace(factsText))
        {
            sb.AppendLine("- None");
            return;
        }

        sb.AppendLine(factsText.Trim());
    }

    private void AppendEvidenceClaims(
        StringBuilder sb,
        Evidence evidence)
    {
        sb.AppendLine("PossibleLocalInterpretations:");

        IReadOnlyList<ClaimSelectionResult> claimResults = evidence.claimResults;

        if (claimResults == null || claimResults.Count == 0)
        {
            sb.AppendLine("- None");
            return;
        }

        Dictionary<string, Claim> claimDefinitions =
            BuildClaimDefinitionDictionary();

        foreach (ClaimSelectionResult result in claimResults)
        {
            if (result == null ||
                string.IsNullOrWhiteSpace(result.claimID))
            {
                continue;
            }

            string claimId = result.claimID.Trim();
            string description = string.Empty;

            if (claimDefinitions.TryGetValue(
                    claimId,
                    out Claim claim))
            {
                description =
                    claim.description?.Trim() ?? string.Empty;
            }

            sb.AppendLine($"- ClaimId: {claimId}");
            sb.AppendLine(
                $"  Confidence: {Mathf.Clamp01(result.confidence):F2}");

            if (!string.IsNullOrWhiteSpace(description))
            {
                sb.AppendLine($"  Description: {description}");
            }

            if (result.basedFeatureIDs != null &&
                result.basedFeatureIDs.Count > 0)
            {
                IEnumerable<string> featureIds =
                    result.basedFeatureIDs
                        .Where(featureId =>
                            !string.IsNullOrWhiteSpace(featureId))
                        .Select(featureId => featureId.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                sb.AppendLine(
                    $"  BasedFeatureIds: {string.Join(", ", featureIds)}");
            }

            if (!string.IsNullOrWhiteSpace(result.reason))
            {
                sb.AppendLine(
                    $"  SelectionReason: {result.reason.Trim()}");
            }
        }
    }

    private Dictionary<string, Claim>
        BuildClaimDefinitionDictionary()
    {
        if (ClaimManager.Instance == null ||
            ClaimManager.Instance.claims == null)
        {
            return new Dictionary<string, Claim>(
                StringComparer.OrdinalIgnoreCase);
        }

        return ClaimManager.Instance.claims
            .Where(claim =>
                claim != null &&
                !string.IsNullOrWhiteSpace(claim.claimID))
            .GroupBy(
                claim => claim.claimID.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private List<ReasoningGraphEdge> GetUniqueEdges()
    {
        return edgesInGraph.Values
            .Where(edge =>
                edge != null &&
                edge.fromNode != null &&
                edge.toNode != null &&
                edge.fromNode.evidence != null &&
                edge.toNode.evidence != null)
            .Distinct()
            .OrderBy(
                edge => edge.fromNode.evidence.evidenceID,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                edge => edge.toNode.evidence.evidenceID,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.reasoningGraphType)
            .ToList();
    }

    private string BuildEdgeID(
    string sourceID,
    ReasoningGraphType reasoningGraphType,
    string targetID)
    {
        switch (reasoningGraphType)
        {
            case ReasoningGraphType.LEADTO:
                return $"EDGE_{sourceID}_{reasoningGraphType}_{targetID}";

            case ReasoningGraphType.CONFLICT:
            case ReasoningGraphType.COHERENT:
            {
                var edgeKey = BuildEdgeKey(sourceID, targetID);

                return $"EDGE_{edgeKey.Item1}_{reasoningGraphType}_{edgeKey.Item2}";
            }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(reasoningGraphType),
                    reasoningGraphType,
                    "Unsupported reasoning graph type.");
        }
    }

    private string GetReasoningGraphTypeForLLM(
        ReasoningGraphType reasoningGraphType)
    {
        return reasoningGraphType switch
        {
            ReasoningGraphType.LEADTO => "LeadsTo",
            ReasoningGraphType.CONFLICT => "ConflictsWith",
            ReasoningGraphType.COHERENT => "ConsistentWith",
            _ => "Unknown"
        };
    }

    public string BuildReasoningGraphLogicChains()
    {
        StringBuilder sb = new();

        Dictionary<ReasoningGraphNode, List<ReasoningGraphNode>> adjacency =
            BuildLeadToAdjacency();

        if (adjacency.Count == 0)
        {
            return "- No lead-to chain exists.";
        }

        Dictionary<ReasoningGraphNode, int> indegree =
            BuildLeadToIndegree(adjacency);

        List<ReasoningGraphNode> startNodes =
            adjacency.Keys
                .Where(node =>
                    indegree.GetValueOrDefault(node, 0) == 0
                )
                .ToList();

        if (startNodes.Count == 0)
        {
            return
                "- No clear starting node found in the lead-to graph. Possible cycle or closed loop.";
        }

        foreach (ReasoningGraphNode start in startNodes)
        {
            List<ReasoningGraphNode> path = new();
            HashSet<ReasoningGraphNode> pathSet = new();

            DFSBuildChains(
                start,
                adjacency,
                path,
                pathSet,
                sb
            );
        }

        if (sb.Length == 0)
        {
            return "- No multi-step lead-to chain exists.";
        }

        return sb.ToString().TrimEnd();
    }

    private Dictionary<ReasoningGraphNode, List<ReasoningGraphNode>> BuildLeadToAdjacency()
    {
        var adjacency = new Dictionary<ReasoningGraphNode, List<ReasoningGraphNode>>();

        foreach (var edge in leadToEdges)
        {
            if (edge == null || edge.fromNode == null || edge.toNode == null)
                continue;

            if (!adjacency.ContainsKey(edge.fromNode))
                adjacency[edge.fromNode] = new List<ReasoningGraphNode>();

            if (!adjacency.ContainsKey(edge.toNode))
                adjacency[edge.toNode] = new List<ReasoningGraphNode>();

            adjacency[edge.fromNode].Add(edge.toNode);
        }

        return adjacency;
    }

    private Dictionary<ReasoningGraphNode, int> BuildLeadToIndegree(Dictionary<ReasoningGraphNode, List<ReasoningGraphNode>> adjacency)
    {
        var indegree = new Dictionary<ReasoningGraphNode, int>();

        foreach (var node in adjacency.Keys)
            indegree[node] = 0;

        foreach (var kvp in adjacency)
        {
            foreach (var next in kvp.Value)
            {
                indegree[next]++;
            }
        }

        return indegree;
    }

    private void DFSBuildChains(
    ReasoningGraphNode node,
    Dictionary<ReasoningGraphNode, List<ReasoningGraphNode>> adjacency,
    List<ReasoningGraphNode> path,
    HashSet<ReasoningGraphNode> pathSet,
    StringBuilder sb)
    {
        path.Add(node);
        pathSet.Add(node);

        List<ReasoningGraphNode> nextNodes = adjacency.GetValueOrDefault(node, new List<ReasoningGraphNode>());
        bool extended = false;

        foreach (var next in nextNodes)
        {
            if (pathSet.Contains(next))
            {
                sb.AppendLine($"- Cycle detected: {FormatPath(path)} -> {next.evidence.displayNameEn}");
                continue;
            }

            extended = true;
            DFSBuildChains(next, adjacency, path, pathSet, sb);
        }

        if (!extended && path.Count > 1)
        {
            sb.AppendLine($"- {FormatPath(path)}");
        }

        pathSet.Remove(node);
        path.RemoveAt(path.Count - 1);
    }

    private string FormatPath(List<ReasoningGraphNode> path)
    {
        return string.Join(" leads to ", path.ConvertAll(n => n.evidence.displayNameEn));
    }

    public bool IsGraphEmpty()
    {
        return evidenceInGraph.Count == 0 ||
               GetUniqueEdges().Count == 0;
    }
    public bool IsEvidenceInGraph(string evidenceID)
    {
        return evidenceInGraph.ContainsKey(evidenceID);
    }
    public bool HasConnectedEdgeInGraph(string EvidenceID)
    {
        foreach (var edge in edgesInGraph.Values)
        {
            if (edge.fromNode.evidence.evidenceID == EvidenceID || edge.toNode.evidence.evidenceID == EvidenceID)
            {
                return true;
            }
        }
        return false;
    }

    public int GetEdgeCount()
    {
        return edgesInGraph.Count;
    }
    
    /// <summary>
    /// 取得目前推理圖 Node 的唯讀快照。
    /// 外部修改回傳 List 不會影響 Manager 內部 Dictionary。
    /// </summary>
    public List<ReasoningGraphNode>
        GetReasoningNodesSnapshot()
    {
        return evidenceInGraph.Values
            .Where(node =>
                node != null &&
                node.evidence != null &&
                !string.IsNullOrWhiteSpace(
                    node.evidence.evidenceID))
            .OrderBy(
                node => node.evidence.evidenceID,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// 取得目前推理圖 Edge 的唯讀快照。
    /// </summary>
    public List<ReasoningGraphEdge>
        GetReasoningEdgesSnapshot()
    {
        return GetUniqueEdges();
    }

    /// <summary>
    /// 依 Edge ID 取得 Interpretation Evaluation。
    /// </summary>
    public bool TryGetEdgeInterpretationResult(
        string edgeID,
        out EdgeInterpretationResult result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(edgeID))
        {
            return false;
        }

        return edgeInterpretationResults.TryGetValue(
            edgeID.Trim(),
            out result);
    }

    /// <summary>
    /// 儲存或更新 Edge Interpretation Evaluation。
    /// </summary>
    public void SetEdgeInterpretationResult(
        EdgeInterpretationResult result)
    {
        if (result == null ||
            string.IsNullOrWhiteSpace(result.edgeID))
        {
            return;
        }

        edgeInterpretationResults[result.edgeID.Trim()] =
            result;
    }
}

[System.Serializable]
public class EdgeInterpretationResult
{
    public string edgeID;
    public string interpretation;
    public float confidence;
}