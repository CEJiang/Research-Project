[System.Serializable]
public class Feature
{
    public string featureID;
    public string description;
    public Dimension dimension;

    public override string ToString()
    {
        return $"- Feature ID: {featureID}, Description: {description}";
    }
}
