public class JsonSchemaFormat
{
    public string formatName;
    public string jsonSchema;

    public JsonSchemaFormat(string formatName, string jsonSchema)
    {
        this.formatName = formatName;
        this.jsonSchema = jsonSchema;
    }
}
