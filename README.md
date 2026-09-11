# HMD Data Saver

This repository contains Unity C# scripts for collecting HMD-based research data, including eye tracking, head tracking, collision events, memory tasks, object-search tasks, and SAGAT assessments.

## Project Structure

### hmdDatasavers/
Original Unity script files collected from the project.
- `CollisionCounter.cs` — logs hazard collision events to CSV
- `MemoryTask.cs` — handles memory task prompts, timing, and answer logging
- `ObjectManagement.cs` — tracks searched objects and final object counts
- `SAGATManager.cs` — manages SAGAT questionnaire flow and response logging
- `UnifiedTrackingManager.cs` — central tracking logic for the full study flow

### unified-scripts/
A simple copied set of the original scripts kept in one folder for easy upload and review.
- Useful when you want all script files grouped together in a single place.
- Keeps the same source logic as the original project files.

### modular-scripts/
A refactored version split by responsibility for cleaner maintenance and future extension.
- `Core/` — shared participant/session state and CSV utilities
- `Tracking/` — eye tracking, head tracking, and gaze-target logic
- `Tasks/` — collision, memory, object-search, and SAGAT task logic
- `Bootstrap/` — runtime wiring for tracking components

## Purpose
The project is designed to record participant behavior and performance in immersive or HMD-based experiments. Logged data can be used for analysis of attention, collisions, task accuracy, and user movement patterns.

## Notes
This repository is intended as a research and development codebase, with both the original scripts and a modular organization for easier maintenance and collaboration.
