using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class DataLoggerImpl<T> where T : class
{
    protected List<T> dataBuffer = new();
    protected float lastLogTime;
    private void Awake()
    {
        lastLogTime = Time.time;
    }
    
    public void LogData(T data, string fileName)
    {
        DataToBuffer(data, fileName);
    }
    protected void DataToBuffer(T data, string fileName = "")
    {
        dataBuffer.Add(data);

        if (dataBuffer.Count >= Setup.MaxDataBuffers || Time.time - lastLogTime >= Setup.DataLoggingInterval)
        {
            FlushToDisk(fileName);
        }
    }
    
    public void FlushToDisk(string fileName = "")
    {
        string fullFileName = $"{Setup.LogFolder}/{Setup.DataFolderName}/{fileName}";
        string path = Path.Combine(Application.persistentDataPath, fullFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        List<string> jsonLines = new();
        foreach (var data in dataBuffer)
        {
            string json = JsonUtility.ToJson(data);
            jsonLines.Add(json);
        }

        File.AppendAllLines(path, jsonLines);

        dataBuffer.Clear();
        lastLogTime = Time.time;
    }
}

public static class DataLogger<T> where T : class
{
    static DataLoggerImpl<T> logger = new();
    private static string fileName;

    public static void LogData(T data, string fileName)
    {
        DataLogger<T>.fileName = fileName;
        logger.LogData(data, fileName);
    }
    public static void FlushToDisk()
    {
        logger.FlushToDisk(fileName);
    }
}