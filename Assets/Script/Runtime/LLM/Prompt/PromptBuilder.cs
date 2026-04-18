public static class PromptBuilder
{
    public static string Build(PromptTemplate template)
    {
        return template.ToPromptText();
    }
}