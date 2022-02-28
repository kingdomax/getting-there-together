using System.IO;
using UnityEngine;

/* Derive from this class and add it as a component under NetworkSetup 
 * to define custom config behavior executed prior to connecting entering a Photon room. */
public class SystemConfig : MonoBehaviour
{
    public string systemConfigPath = ""; 

    public string content { get; protected set; }

    public static SystemConfig Instance { get; private set; }

    private void Start()
    {
        Instance = this;
        if(systemConfigPath.Length > 0)
            Read();
    }

    // override this in derived classes for custom read behavior, e.g. Json Parsing
    public virtual void Read()
    {
        Debug.Log("Read Application Config: " + systemConfigPath);
        if (!File.Exists(systemConfigPath))
        {
            throw new FileNotFoundException(systemConfigPath);
        }

        StreamReader reader = new StreamReader(systemConfigPath);
        content = reader.ReadToEnd();
        reader.Close();

        Debug.Log("content: " + content);
    }

    public void Save(string configString)
    {
        string file = string.Format(systemConfigPath);
        string configJson = JsonUtility.ToJson(configString, true); // prettyPrint = true
        using (StreamWriter sw = File.CreateText(file))
        {
            sw.Write(configJson);
            sw.Close();
        }
        Debug.Log("Save Application Config: " + systemConfigPath);
    }  
}
