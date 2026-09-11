using UnityEngine;
using System;
using System.IO;

public static class SAGATLogger
{
    public static void LogResponse(
        string sessionId,
        string scene,
        string hazard,
        int questionIndex,
        string questionText,
        int selectedAnswerId,
        string selectedAnswerText,
        int correctAnswerId,
        string correctAnswerText,
        bool isCorrect,
        float responseTime,
        float distanceFromCamera,
        float minDistanceDuringMovement,
        bool didCollide,
        bool nearCollision,
        int numCollisions,
        int numNearCollisions,
        Vector3 objectPos,
        Vector3 userPos)
    {
        string participantId = ParticipantContext.GetParticipantId();
        string folderPath = CSVLogger.BuildFolderPath(participantId, "SAGAT_Logs");
        string filePath = Path.Combine(folderPath, $"{scene}_SAGAT_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");

        if (!File.Exists(filePath))
        {
            File.WriteAllText(
                filePath,
                "SessionID,Scene,DateTimeLocal,UnscaledTime,Hazard,QuestionIndex,SALevel,QuestionText,SelectedAnswerID,SelectedAnswerText,CorrectAnswerID,CorrectAnswerText,Correct?,ResponseTime,DistanceFromCamera,MinDistanceDuringMovement,Collision,NearCollision,NumCollisionsSoFar,NumNearCollisionsSoFar,ObjectPosX,ObjectPosY,ObjectPosZ,UserPosX,UserPosY,UserPosZ" + Environment.NewLine
            );
        }

        string line =
            $"{sessionId},{scene},{CSVLogger.GetDateTimeLocal()},{CSVLogger.GetUnscaledTime():F6},{hazard},{questionIndex},1,{questionText},{selectedAnswerId},{selectedAnswerText},{correctAnswerId},{correctAnswerText},{isCorrect},{responseTime:F3},{distanceFromCamera:F3},{minDistanceDuringMovement:F3},{didCollide},{nearCollision},{numCollisions},{numNearCollisions},{objectPos.x:F3},{objectPos.y:F3},{objectPos.z:F3},{userPos.x:F3},{userPos.y:F3},{userPos.z:F3}";

        CSVLogger.AppendLine(filePath, line);
    }
}
