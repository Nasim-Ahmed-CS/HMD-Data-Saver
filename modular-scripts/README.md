# Modular Scripts

This folder contains a split version of the HMD data-collection logic into separate Unity scripts by responsibility.

Structure:
- Core/ParticipantContext.cs
- Core/CSVLogger.cs
- Tracking/EyeTrackingController.cs
- Tracking/HeadTrackingController.cs
- Tracking/GazeTargetTracker.cs
- Tasks/CollisionTracker.cs
- Tasks/MemoryTaskController.cs
- Tasks/MemoryTaskLogger.cs
- Tasks/ObjectSearchTracker.cs
- Tasks/ObjectSearchLogger.cs
- Tasks/SAGATQuestionManager.cs
- Tasks/SAGATLogger.cs
- Bootstrap/TrackingBootstrap.cs

These modules keep the same project intent as the original scripts but separate the logic for:
- participant/session information
- CSV file creation and writing
- eye tracking and head tracking
- collision and interaction logging
- memory, object search, and SAGAT tasks

This folder is intended as a modular, maintainable version for future extension and GitHub upload.
