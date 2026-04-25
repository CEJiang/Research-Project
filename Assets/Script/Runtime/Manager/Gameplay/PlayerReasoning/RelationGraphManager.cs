using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using Microsoft.Unity.VisualStudio.Editor;
using Radishmouse;
using UnityEngine;

public class RelationGraphManager : Singleton<RelationGraphManager>
{
    Dictionary<string, RelationNode> evidenceInGraph = new();
    Dictionary<(string, string), RelationGraphEdge> edgesInGraph = new();
    Dictionary<string, GameObject> nodeToEvidenceListItem = new();

    List<RelationGraphEdge> leadToEdges = new();
    List<RelationGraphEdge> conflictEdges = new();
    List<RelationGraphEdge> coherentEdges = new();

    [Header("Dot Colors")]
    public Color leadToColor = new(0.65f, 0.90f, 0.65f, 1f);
    public Color conflictColor = new(0.80f, 0.35f, 0.35f, 1f);
    public Color coherentColor = new(0.45f, 0.85f, 0.95f, 1f);

    [Header("Edge Highlight Colors")]
    public Color leadToHighlightColor = new(0.90f, 1.00f, 0.90f, 1f);
    public Color conflictHighlightColor = new(1.00f, 0.55f, 0.55f, 1f);
    public Color coherentHighlightColor = new(0.75f, 0.95f, 1.00f, 1f);

    public RelationGraphType currentRelationGraphType = RelationGraphType.NONE;
    public RelationNode firstNode = null;
    public RelationGraphPreviewEdge relationGraphPreviewEdge;
    public RelationGraphEdge relationGraphHightlightEdge;
    public RelationGraphEdge currentSelectedEdge;
    public GameObject relationGraphHightlightNode;
    public RelationNode currentSelectedNode;

    public RelationGraphButtonController relationGraphButtonController;
    
    private bool isOnLeadTo = true;
    private bool isOnConflict = true;
    private bool isOnCoherent = true;

    void Start()
    {
        relationGraphHightlightEdge.gameObject.SetActive(false);
        relationGraphHightlightNode.SetActive(false);
    }

    void Update()
    {
        if (currentSelectedNode != null && relationGraphHightlightNode.activeSelf)
        {
            UpdateHightlightNodePosition();
        }
    }

    public void SetCurrentRelationGraphType(RelationGraphType type)
    {
        currentRelationGraphType = type;
        if (type == RelationGraphType.NONE)
        {
            firstNode = null;
            relationGraphPreviewEdge.EndPreview();
        }
        relationGraphButtonController.RefreshButtonVisual();
    }

    public void AddRelationNode(Evidence evidence, Vector2 localPosition = default(Vector2), GameObject evidenceListItem = null)
    {
        if (!evidenceInGraph.ContainsKey(evidence.evidenceId))
        {
            RelationNode node = UIManager.Instance.playerReasoningUI.relationGraphUI.AddRelationNode(evidence, localPosition);
            evidenceInGraph[evidence.evidenceId] = node;

            if (evidenceListItem != null)
            {
                nodeToEvidenceListItem[evidence.evidenceId] = evidenceListItem;

                // Change the evidence list item font color to white
                var text = evidenceListItem.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)                
                {
                    text.color = Color.white;
                }
            }
        }
    }

    public void toggleLeadToEdges()
    {
        isOnLeadTo = !isOnLeadTo;
        foreach (var edge in leadToEdges)
        {
            edge.gameObject.SetActive(isOnLeadTo);
        }
    }

    public void toggleConflictEdges()
    {
        isOnConflict = !isOnConflict;
        foreach (var edge in conflictEdges)
        {
            edge.gameObject.SetActive(isOnConflict);
        }
    }

    public void toggleCoherentEdges()
    {
        isOnCoherent = !isOnCoherent;
        foreach (var edge in coherentEdges)
        {
            edge.gameObject.SetActive(isOnCoherent);
        }
    }

    public void OnRelationEdgeClicked(RelationGraphEdge edge)
    {
        if (currentSelectedNode != null)
        {
            currentSelectedNode = null;
            relationGraphHightlightNode.SetActive(false);
        }

        if (currentSelectedEdge == edge)
        {
            // If the same edge is clicked again, we can consider it as unselecting the edge
            currentSelectedEdge = null;
            relationGraphHightlightEdge.gameObject.SetActive(false);
            return;
        }

        currentSelectedEdge = edge;
        // Highlight the clicked edge and show details in the UI
        relationGraphHightlightEdge.gameObject.SetActive(true);
        relationGraphHightlightEdge.fromNode = edge.fromNode;
        relationGraphHightlightEdge.toNode = edge.toNode;

        switch (edge.relationType)
        {
            case RelationGraphType.LEADTO:
                relationGraphHightlightEdge.GetComponent<UILineRenderer>().color = leadToHighlightColor;
                relationGraphHightlightEdge.GetComponent<RelationGraphEdge>().relationType = RelationGraphType.LEADTO;
                break;
            case RelationGraphType.CONFLICT:
                relationGraphHightlightEdge.GetComponent<UILineRenderer>().color = conflictHighlightColor;
                relationGraphHightlightEdge.GetComponent<RelationGraphEdge>().relationType = RelationGraphType.CONFLICT;    
                break;
            case RelationGraphType.COHERENT:
                relationGraphHightlightEdge.GetComponent<UILineRenderer>().color = coherentHighlightColor;
                relationGraphHightlightEdge.GetComponent<RelationGraphEdge>().relationType = RelationGraphType.COHERENT;
                break;
        }
    }

    public void OnRelationNodeClicked(RelationNode node)
    {
        if (currentRelationGraphType == RelationGraphType.NONE) 
            HightlightNode(node);
        else 
            HandleRelationNodeClicked(node);
    }

    public void HightlightNode(RelationNode node)
    {
        if (currentSelectedEdge != null)
        {
            currentSelectedEdge = null;
            relationGraphHightlightEdge.gameObject.SetActive(false);
        }

        if (currentSelectedNode == node)
        {
            // If the same node is clicked again, we can consider it as unselecting the node
            currentSelectedNode = null;
            relationGraphHightlightNode.SetActive(false);
            return;
        }
        
        currentSelectedNode = node;
        relationGraphHightlightNode.SetActive(true);
        relationGraphHightlightNode.GetComponent<RectTransform>().anchoredPosition = node.GetComponent<RectTransform>().anchoredPosition;
        relationGraphHightlightNode.GetComponent<RectTransform>().sizeDelta = node.GetComponent<RectTransform>().sizeDelta + new Vector2(10, 10); // Add some padding for highlight
    }

    public void HandleRelationNodeClicked(RelationNode node)
    {
        if (firstNode == null)
        {
            firstNode = node;
            relationGraphPreviewEdge.BeginPreview(node, currentRelationGraphType);
        }
        else
        {
            if (firstNode != node && !edgesInGraph.ContainsKey((firstNode.evidence.evidenceId, node.evidence.evidenceId)))
            {
                RelationGraphEdge edge = UIManager.Instance.playerReasoningUI.relationGraphUI.AddRelationEdge(firstNode, node, currentRelationGraphType);
                relationGraphPreviewEdge.EndPreview();

                // Cause we want to toggle the relation type in the relation graph, so we need to clarify the relation type when adding edge
                // If I just want to see the lead to relation, we can temporarily hide the other two types of edges in the UI, but the relation type is still there, 
                // which means when I switch to see the conflict relation, the conflict edge will show up immediately without needing to add edge again
                switch (currentRelationGraphType)
                {
                    case RelationGraphType.LEADTO:
                        leadToEdges.Add(edge);
                        break;
                    case RelationGraphType.CONFLICT:
                        conflictEdges.Add(edge);
                        break;
                    case RelationGraphType.COHERENT:
                        coherentEdges.Add(edge);
                        break;
                }

                edgesInGraph[(firstNode.evidence.evidenceId, node.evidence.evidenceId)] = edge;
                edgesInGraph[(node.evidence.evidenceId, firstNode.evidence.evidenceId)] = edge; // for undirected graph
            }

            firstNode = null;
        }
    }

    public void UpdateHightlightNodePosition()
    {
        relationGraphHightlightNode.GetComponent<RectTransform>().anchoredPosition = currentSelectedNode.GetComponent<RectTransform>().anchoredPosition;
    }

    public void DeleteSelectedNode()
    {
        if (currentSelectedNode != null)
        {
            // Remove from graph data structure
            evidenceInGraph.Remove(currentSelectedNode.evidence.evidenceId);

            // Remove the corresponding node in the UI and all edges connected to it
            UIManager.Instance.playerReasoningUI.relationGraphUI.RemoveRelationNode(currentSelectedNode);

            // Also need to remove all edges connected to this node
            var edgesToRemove = new List<RelationGraphEdge>();
            foreach (var edge in edgesInGraph.Values)
            {
                if (edge.fromNode == currentSelectedNode || edge.toNode == currentSelectedNode)
                {
                    edgesToRemove.Add(edge);
                }
            }
            foreach (var edge in edgesToRemove)
            {
                RemoveRelationEdge(edge);
            }

            // Change the evidence list item font color to gray
            if (nodeToEvidenceListItem.TryGetValue(currentSelectedNode.evidence.evidenceId, out GameObject evidenceListItem))
            {
                var text = evidenceListItem.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    text.color = Color.black;
                }
            }

            currentSelectedNode = null;
            relationGraphHightlightNode.SetActive(false);
        }
    }
    public void DeleteSelectedEdge()
    {
        if (currentSelectedEdge != null)
        {
            // Remove from graph data structure
            edgesInGraph.Remove((currentSelectedEdge.fromNode.evidence.evidenceId, currentSelectedEdge.toNode.evidence.evidenceId));
            edgesInGraph.Remove((currentSelectedEdge.toNode.evidence.evidenceId, currentSelectedEdge.fromNode.evidence.evidenceId));

            // Remove from edge type list
            switch (currentSelectedEdge.relationType)
            {
                case RelationGraphType.LEADTO:
                    leadToEdges.Remove(currentSelectedEdge);
                    break;
                case RelationGraphType.CONFLICT:
                    conflictEdges.Remove(currentSelectedEdge);
                    break;
                case RelationGraphType.COHERENT:
                    coherentEdges.Remove(currentSelectedEdge);
                    break;
            }

            // Remove the corresponding edge in the UI
            UIManager.Instance.playerReasoningUI.relationGraphUI.RemoveRelationEdge(currentSelectedEdge);
            currentSelectedEdge = null;
            relationGraphHightlightEdge.gameObject.SetActive(false);
        }
    }

    public void RemoveRelationEdge(RelationGraphEdge edge)
    {
        // Remove from graph data structure
        edgesInGraph.Remove((edge.fromNode.evidence.evidenceId, edge.toNode.evidence.evidenceId));
        edgesInGraph.Remove((edge.toNode.evidence.evidenceId, edge.fromNode.evidence.evidenceId));

        // Remove from edge type list
        switch (edge.relationType)
        {
            case RelationGraphType.LEADTO:
                leadToEdges.Remove(edge);
                break;
            case RelationGraphType.CONFLICT:
                conflictEdges.Remove(edge);
                break;
            case RelationGraphType.COHERENT:
                coherentEdges.Remove(edge);
                break;
        }

        // Remove the corresponding edge in the UI
        UIManager.Instance.playerReasoningUI.relationGraphUI.RemoveRelationEdge(edge);
    }

    public string GetRelationGraphDataForLLM()
    {
        StringBuilder sb = new();
        HashSet<string> uniqueEdgeKeys = new();

        sb.AppendLine("EVIDENCE NODES:");
        foreach (var node in evidenceInGraph.Values)
        {
            if (node == null || node.evidence == null) continue;

            sb.AppendLine($"Evidence: {node.evidence.displayNameEn}");
            sb.AppendLine($"EvidenceId: {node.evidence.evidenceId}");

            if (node.evidence.facts != null && node.evidence.facts.Count > 0)
            {
                sb.AppendLine("Facts: ");
                sb.AppendLine(node.evidence.GetEvidenceFactsAsStringForLLM());
            }

            sb.AppendLine();
        }

        sb.AppendLine("RELATIONS:");
        foreach (var edge in edgesInGraph.Values)
        {
            if (edge == null || edge.fromNode == null || edge.toNode == null) continue;
            if (edge.fromNode.evidence == null || edge.toNode.evidence == null) continue;

            string fromId = edge.fromNode.evidence.evidenceId;
            string toId = edge.toNode.evidence.evidenceId;
            string relation = edge.relationType.ToString();

            string edgeKey;
            if (edge.relationType == RelationGraphType.LEADTO)
            {
                edgeKey = $"{fromId}->{toId}:{relation}";
            }
            else
            {
                string a = string.CompareOrdinal(fromId, toId) < 0 ? fromId : toId;
                string b = string.CompareOrdinal(fromId, toId) < 0 ? toId : fromId;
                edgeKey = $"{a}<->{b}:{relation}";
            }

            if (!uniqueEdgeKeys.Add(edgeKey))
                continue;

            switch (edge.relationType)
            {
                case RelationGraphType.LEADTO:
                    sb.AppendLine($"- {edge.fromNode.evidence.displayNameEn} leads to {edge.toNode.evidence.displayNameEn}");
                    break;
                case RelationGraphType.CONFLICT:
                    sb.AppendLine($"- {edge.fromNode.evidence.displayNameEn} conflicts with {edge.toNode.evidence.displayNameEn}");
                    break;
                case RelationGraphType.COHERENT:
                    sb.AppendLine($"- {edge.fromNode.evidence.displayNameEn} is consistent with {edge.toNode.evidence.displayNameEn}");
                    break;
            }
        }

        sb.AppendLine("LOGICAL CHAINS:\n");
        sb.AppendLine(BuildRelationGraphLogicChains());

        return sb.ToString();
    }

    public string BuildRelationGraphLogicChains()
    {
        StringBuilder sb = new();

        var adjacency = BuildLeadToAdjacency();
        var indegree = BuildLeadToIndegree(adjacency);

        List<RelationNode> startNodes = new();
        foreach (var node in adjacency.Keys)
        {
            if (indegree.GetValueOrDefault(node, 0) == 0)
                startNodes.Add(node);
        }

        // 如果整張圖沒有 indegree 0，可能代表有 cycle
        if (startNodes.Count == 0)
        {
            sb.AppendLine("- No clear starting node found in lead-to graph. Possible cycle or closed loop.");
            return sb.ToString();
        }

        foreach (var start in startNodes)
        {
            var path = new List<RelationNode>();
            var pathSet = new HashSet<RelationNode>();
            DFSBuildChains(start, adjacency, path, pathSet, sb);
        }

        return sb.ToString();
    }

    private Dictionary<RelationNode, List<RelationNode>> BuildLeadToAdjacency()
    {
        var adjacency = new Dictionary<RelationNode, List<RelationNode>>();

        foreach (var edge in leadToEdges)
        {
            if (edge == null || edge.fromNode == null || edge.toNode == null)
                continue;

            if (!adjacency.ContainsKey(edge.fromNode))
                adjacency[edge.fromNode] = new List<RelationNode>();

            if (!adjacency.ContainsKey(edge.toNode))
                adjacency[edge.toNode] = new List<RelationNode>();

            adjacency[edge.fromNode].Add(edge.toNode);
        }

        return adjacency;
    }

    private Dictionary<RelationNode, int> BuildLeadToIndegree(Dictionary<RelationNode, List<RelationNode>> adjacency)
    {
        var indegree = new Dictionary<RelationNode, int>();

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
    RelationNode node,
    Dictionary<RelationNode, List<RelationNode>> adjacency,
    List<RelationNode> path,
    HashSet<RelationNode> pathSet,
    StringBuilder sb)
    {
        path.Add(node);
        pathSet.Add(node);

        List<RelationNode> nextNodes = adjacency.GetValueOrDefault(node, new List<RelationNode>());
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

    private string FormatPath(List<RelationNode> path)
    {
        return string.Join(" leads to ", path.ConvertAll(n => n.evidence.displayNameEn));
    }

    public bool IsGraphEmpty()
    {
        return evidenceInGraph.Count == 0 || edgesInGraph.Count == 0;
    }
}
