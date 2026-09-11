# HMD Data Saver - Modular Scripts

This repository contains a modular Unity script structure for collecting and logging head-mounted display (HMD) research data, including eye tracking, head tracking, collision logging, memory tasks, object-search tracking, and SAGAT assessments.

## Folder Structure

### Core
- `ParticipantContext.cs` — gets the participant ID and active scene name
- `CSVLogger.cs` — shared helper for creating folders and writing CSV log files

### Tracking
- `EyeTrackingController.cs` — handles eye-gaze sampling and gaze-based tracking
- `HeadTrackingController.cs` — tracks head movement and orientation
- `GazeTargetTracker.cs` — detects the object currently looked at by the user

### Tasks
- `CollisionTracker.cs` — logs hazard collisions and trigger events
- `MemoryTaskController.cs` — controls the memory task UI and answer validation
- `MemoryTaskLogger.cs` — writes memory-task CSV results
- `ObjectSearchTracker.cs` — tracks object-search progress and final counts
- `ObjectSearchLogger.cs` — saves object-search log entries
- `SAGATQuestionManager.cs` — manages SAGAT question sets
- `SAGATLogger.cs` — logs SAGAT responses with timing and scene data

### Bootstrap
- `TrackingBootstrap.cs` — connects the main tracking components at runtime

## Purpose
The scripts are split by responsibility so the project is easier to maintain, extend, and debug. This modular setup keeps the same research workflow while separating data collection, tracking behavior, and task logic into clearer components.

## Notes
This is a structured, refactored version intended for cleaner project organization and future development, while preserving the original research logic and logging workflow.
