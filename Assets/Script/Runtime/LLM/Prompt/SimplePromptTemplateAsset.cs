using UnityEngine;

[CreateAssetMenu(menuName = "LLM/Simple Prompt Template")]
public class SimplePromptTemplateAsset : ScriptableObject
{
    public string prompt;

    public void ApplyTo(SimplePromptTemplate template)
    {
        template.prompt = prompt;
    }
}
