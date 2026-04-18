using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.IO;

public class EvidenceLogManager : Singleton<EvidenceLogManager>
{
    private List<string> logEntries = new();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            SaveLogToFile();
            Debug.Log(GetFullLog());
        }
    }

    /// <summary>
    /// 記錄一個 evidence 的完整推理過程
    /// </summary>
    public void LogEvidence(EvidenceMetadata metadata, List<String> hypothesisInterpretations)
    {
        StringBuilder sb = new();
        // Claims
        sb.AppendLine("Metadata:");
        sb.AppendLine(metadata.ToString());

        // Interpretations
        sb.AppendLine("Interpretations:");
        if (hypothesisInterpretations != null && hypothesisInterpretations.Count > 0)
        {
            for (int i = 0; i < hypothesisInterpretations.Count; i++)
            {
                sb.AppendLine($" -[{i + 1}] {hypothesisInterpretations[i]}");
            }
        }
        else
        {
            sb.AppendLine(" - No interpretations recorded");
        }

        sb.AppendLine("==========================");

        string entry = sb.ToString();
        logEntries.Add(entry);

        Debug.Log(entry);
    }

    /// <summary>
    /// 取得完整 Log
    /// </summary>
    public string GetFullLog()
    {
        return string.Join("\n", logEntries);
    }

    public void SaveLogToFile()
    {
        string logContent = GetFullLog();

        // Unity 專案的 Resources 資料夾路徑
        string folderPath = Path.Combine(Application.dataPath, "Resources", "Jiang");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, $"evidence_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        File.WriteAllText(filePath, logContent);

        Debug.Log($"Evidence log saved to: {filePath}");
    }

    /// <summary>
    /// 清除 Log
    /// </summary>
    public void ClearLog()
    {
        logEntries.Clear();
    }
}