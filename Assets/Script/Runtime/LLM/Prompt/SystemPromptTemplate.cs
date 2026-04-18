using System.Collections.Generic;

[System.Serializable]
public class SystemPromptTemplate : PromptTemplate
{
    public string instructions;
    public string task;
    public string role;

    public override string ToPromptText()
    {
        List<string> segments = new();

        if (!string.IsNullOrEmpty(role))
            segments.Add($"Your role: {role}");

        if (!string.IsNullOrEmpty(task))
            segments.Add($"Your task: {task}");

        if (!string.IsNullOrEmpty(instructions))
            segments.Add($"Instructions: {instructions}");

        return string.Join("\n", segments).Trim();
    }
}
