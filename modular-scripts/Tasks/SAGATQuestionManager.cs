using UnityEngine;
using System.Collections.Generic;

public class SAGATQuestionManager : MonoBehaviour
{
    [Header("SAGAT Data")]
    public SAGATQuestions sagatData;

    private Dictionary<HazardName, QuestionSet> questionDict = new Dictionary<HazardName, QuestionSet>();

    private void Awake()
    {
        BuildQuestionDictionary();
    }

    private void BuildQuestionDictionary()
    {
        questionDict.Clear();
        foreach (var qs in sagatData.questionSet)
        {
            if (!questionDict.ContainsKey(qs.hazardName))
                questionDict.Add(qs.hazardName, qs);
        }
    }

    public QuestionSet GetQuestionSet(HazardName hazardName)
    {
        if (questionDict.TryGetValue(hazardName, out QuestionSet set))
            return set;

        return null;
    }
}
