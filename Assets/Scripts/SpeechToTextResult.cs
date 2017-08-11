using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpeechToTextResult
{
    public List<Result> results;
}

[System.Serializable]
public class Result
{
    public List<Alternative> alternatives;
}

[System.Serializable]
public class Alternative
{
    public string transcript;
    public float confidence;
}