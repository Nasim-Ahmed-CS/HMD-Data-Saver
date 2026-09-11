using System;
using UnityEngine;

public static class ParticipantContext
{
    public static string GetParticipantId()
    {
        if (ParticipantManager.Instance == null ||
            string.IsNullOrWhiteSpace(ParticipantManager.Instance.ParticipantId))
        {
            return "MissingParticipantID_" +
                   DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        return ParticipantManager.Instance.ParticipantId;
    }

    public static string GetSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
}
