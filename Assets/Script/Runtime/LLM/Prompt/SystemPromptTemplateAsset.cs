
using UnityEngine;

[CreateAssetMenu(menuName = "LLM/System Prompt Template")]
public class SystemPromptTemplateAsset : ScriptableObject
{
    public string instructions;
    public string task;
    public string role;

    public void ApplyTo(SystemPromptTemplate template)
    {
        template.instructions = instructions;
        template.task = task;
        template.role = role;
    }
}
