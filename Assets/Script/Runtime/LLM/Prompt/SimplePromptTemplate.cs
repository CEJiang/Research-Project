[System.Serializable]
public class SimplePromptTemplate : PromptTemplate
{
    public string prompt;

    public override string ToPromptText()
    {
        return prompt.Trim();
    }
}
