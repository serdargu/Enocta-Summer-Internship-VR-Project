using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Settings
{
    public int total_scenes;
    public List<Scene> scenes;

    public static Settings CreateFromJSON()
    {
        TextAsset txtAssets = (TextAsset)Resources.Load("settings");
        return JsonUtility.FromJson<Settings>(txtAssets.text);
    }

}

[System.Serializable]
public class Scene
{
    public string name;
    public int total_questions;
    public int countdown_time;
    public string light_color;
    public List<Question> questions;
}


[System.Serializable]
public class Question
{
    public string description;
    public List<Answer> answers;
    public bool fb;
    public string fbType;
}

[System.Serializable]
public class Answer
{
    public string answer;
    public bool correct;
    public string feedback;
}
