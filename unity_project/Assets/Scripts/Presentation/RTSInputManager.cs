using System.Collections.Generic;
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    /// <summary>
    /// Professional RTS input management based on Age of Empires 4 control scheme
    /// Handles all keyboard shortcuts, mouse controls, and hotkeys for RTS gameplay
    /// </summary>
    public class RTSInputManager : MonoBehaviour {
        [Header("Core Systems")]
        public SelectionManager SelectionManager;
        public SimBootstrap GameController;
        
        [Header("Camera Controls")]
        public Transform CameraRig;
        public float CameraRotationSpeed = 45f;
        public float CameraPanSpeed = 10f;
        public float CameraZoomSpeed = 5f;
        
        [Header("Control Groups")]
        private Dictionary<int, HashSet<int>> _controlGroups = new Dictionary<int, HashSet<int>>();
        
        [Header("Building Placement")]
        private bool _placingBuilding = false;
        private int _placeBuildingIndex = 0;
        private GameObject _buildingGhost;
        
        [Header("Game State")]
        private Simulator _sim;
        private Camera _camera;
        
        void Start() {
            _sim = GameController?.GetSimulator();
            _camera = Camera.main;
            
            // Initialize control groups
            for (int i = 0; i < 10; i++) {
                _controlGroups[i] = new HashSet<int>();
            }
            
            Debug.Log("=== RTS INPUT MANAGER INITIALIZED ===");
            Debug.Log("Age of Empires 4 controls are now active!");
            Debug.Log("Press F1 to see the new control scheme");
        }
        
        void Update() {
            if (_sim == null) return;
            
            HandleMouseControls();
            HandleUnitSelection();
            HandleCameraControls();
            HandleUnitManagement();
            HandleBuildingControls();
            HandleGameControls();
            HandleCommunication();
        }
        
        #region Mouse Controls
        private void HandleMouseControls() {
            // Left Click - handled by SelectionManager
            // Right Click - contextual orders (move/attack)
            if (Input.GetMouseButtonDown(1)) {
                IssueContextualOrder();
            }
            
            // Right Click + Drag - facing move order
            if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(1)) {
                // TODO: Implement facing move with drag
            }
        }
        
        private void IssueContextualOrder() {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f)) {
                // Convert world position to simulation coordinates
                int tx = (int)(hit.point.x * SimConstants.PositionScale);
                int ty = (int)(hit.point.z * SimConstants.PositionScale);
                
                // Issue move order to selected units
                var selectedUnits = SelectionManager.GetSelectedUnits();
                foreach (var unitId in selectedUnits) {
                    GameController.IssueMove(unitId, tx, ty);
                }
            }
        }
        #endregion
        
        #region Unit Selection & Management
        private void HandleUnitSelection() {
            // Control + A - Select all units on screen
            if (Input.GetKeyDown(KeyCode.A) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
                SelectAllUnitsOnScreen();
            }
            
            // Control + Shift + A - Select all units
            if (Input.GetKeyDown(KeyCode.A) && 
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                SelectAllUnits();
            }
            
            // ESC - Cancel/Deselect (handled by SelectionManager but enforced here)
            if (Input.GetKeyDown(KeyCode.Escape)) {
                CancelCurrentAction();
            }
        }
        
        private void HandleUnitManagement() {
            // Control Groups (0-9)
            for (int i = 0; i <= 9; i++) {
                KeyCode key = KeyCode.Alpha0 + i;
                
                // Set Control Group (Ctrl + Number)
                if (Input.GetKeyDown(key) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
                    SetControlGroup(i);
                }
                // Select Control Group (Number)
                else if (Input.GetKeyDown(key)) {
                    SelectControlGroup(i);
                }
                // Add to Control Group (Shift + Number)
                else if (Input.GetKeyDown(key) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                    AddToControlGroup(i);
                }
            }
            
            // F1 - Select all Military Production Buildings
            if (Input.GetKeyDown(KeyCode.F1)) {
                SelectBuildingsByType("military_production");
            }
            
            // F2 - Select all Economy Buildings
            if (Input.GetKeyDown(KeyCode.F2)) {
                SelectBuildingsByType("economy");
            }
            
            // F3 - Select all Research Buildings
            if (Input.GetKeyDown(KeyCode.F3)) {
                SelectBuildingsByType("research");
            }
            
            // F4 - Select all Landmarks, Wonders, and Capital Town Centers
            if (Input.GetKeyDown(KeyCode.F4)) {
                SelectBuildingsByType("landmarks");
            }
            
            // H - Cycle through Town Centers
            if (Input.GetKeyDown(KeyCode.H)) {
                CycleTownCenters();
            }
            
            // Control + H - Focus on Capital Town Center
            if (Input.GetKeyDown(KeyCode.H) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
                FocusCapitalTownCenter();
            }
            
            // Period (.) - Cycle through Idle Economy
            if (Input.GetKeyDown(KeyCode.Period)) {
                if (_placingBuilding) {
                    // In building mode, cycle building types
                    CycleBuildingType(1);
                } else {
                    CycleIdleWorkers();
                }
            }
            
            // Comma (,) - Cycle through idle Military units or previous building type
            if (Input.GetKeyDown(KeyCode.Comma)) {
                if (_placingBuilding) {
                    CycleBuildingType(-1);
                } else {
                    CycleIdleMilitary();
                }
            }
            
            // Control + Period - Select all idle Villagers/Workers
            if (Input.GetKeyDown(KeyCode.Period) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
                SelectAllIdleWorkers();
            }
            
            // Control + Comma - Select all idle Military units
            if (Input.GetKeyDown(KeyCode.Comma) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
                SelectAllIdleMilitary();
            }
            
            // Delete - Delete selected units/buildings
            if (Input.GetKeyDown(KeyCode.Delete)) {
                DeleteSelectedEntities();
            }
        }
        #endregion
        
        #region Camera Controls
        private void HandleCameraControls() {
            // Alt + Mouse - Rotate camera (handled by mouse look if needed)
            
            // [ - Rotate camera 45 degrees counter-clockwise
            if (Input.GetKeyDown(KeyCode.LeftBracket)) {
                RotateCamera(-45f);
            }
            
            // ] - Rotate camera 45 degrees clockwise  
            if (Input.GetKeyDown(KeyCode.RightBracket)) {
                RotateCamera(45f);
            }
            
            // Backspace - Reset camera
            if (Input.GetKeyDown(KeyCode.Backspace)) {
                ResetCamera();
            }
            
            // F5 - Focus on selected units
            if (Input.GetKeyDown(KeyCode.F5)) {
                FocusOnSelectedUnits();
            }
            
            // Home - Follow selected unit
            if (Input.GetKeyDown(KeyCode.Home)) {
                FollowSelectedUnit();
            }
            
            // Arrow keys for camera panning
            HandleCameraPanning();
        }
        
        private void HandleCameraPanning() {
            Vector3 panDirection = Vector3.zero;
            
            if (Input.GetKey(KeyCode.LeftArrow)) panDirection += Vector3.left;
            if (Input.GetKey(KeyCode.RightArrow)) panDirection += Vector3.right;
            if (Input.GetKey(KeyCode.UpArrow)) panDirection += Vector3.forward;
            if (Input.GetKey(KeyCode.DownArrow)) panDirection += Vector3.back;
            
            if (panDirection != Vector3.zero && CameraRig) {
                CameraRig.Translate(panDirection * CameraPanSpeed * Time.deltaTime, Space.World);
            }
        }
        #endregion
        
        #region Building Controls
        private void HandleBuildingControls() {
            // B - Enter/Exit building placement mode
            if (Input.GetKeyDown(KeyCode.B)) {
                ToggleBuildingMode();
            }
            
            // T - Train units (quick train hotkey)
            if (Input.GetKeyDown(KeyCode.T)) {
                QuickTrainUnit();
            }
            
            // Y - Access secondary UI panel / Multiple unit training
            if (Input.GetKeyDown(KeyCode.Y)) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    // Shift+Y: Stress test multiple training
                    StressTestTraining();
                } else {
                    // Y: Access secondary UI
                    ToggleSecondaryUI();
                }
            }
            
            // P - Set rally point
            if (Input.GetKeyDown(KeyCode.P)) {
                SetRallyPoint();
            }
            
            // O - Clear rally point
            if (Input.GetKeyDown(KeyCode.O)) {
                ClearRallyPoint();
            }
        }
        #endregion
        
        #region Game Controls
        private void HandleGameControls() {
            // F10 - Game Menu
            if (Input.GetKeyDown(KeyCode.F10)) {
                ToggleGameMenu();
            }
            
            // F11 - Toggle game time display
            if (Input.GetKeyDown(KeyCode.F11)) {
                ToggleGameTimeDisplay();
            }
            
            // Pause - Pause simulation (single player)
            if (Input.GetKeyDown(KeyCode.Pause)) {
                TogglePause();
            }
            
            // F8 - Quick Save
            if (Input.GetKeyDown(KeyCode.F8)) {
                QuickSave();
            }
            
            // F9 - Quick Load
            if (Input.GetKeyDown(KeyCode.F9)) {
                QuickLoad();
            }
            
            // Insert - Toggle team colors
            if (Input.GetKeyDown(KeyCode.Insert)) {
                ToggleTeamColors();
            }
            
            // Tab - Cycle through selected units/unit types
            if (Input.GetKeyDown(KeyCode.Tab)) {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                    CycleSelectedUnitsReverse();
                } else {
                    CycleSelectedUnits();
                }
            }
        }
        #endregion
        
        #region Communication
        private void HandleCommunication() {
            // Enter - Team chat
            if (Input.GetKeyDown(KeyCode.Return)) {
                OpenTeamChat();
            }
            
            // Shift + Enter - All chat
            if (Input.GetKeyDown(KeyCode.Return) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                OpenAllChat();
            }
            
            // Spacebar - Focus on last event
            if (Input.GetKeyDown(KeyCode.Space)) {
                FocusOnLastEvent();
            }
            
            // F6 - Toggle Players and Tribute panel
            if (Input.GetKeyDown(KeyCode.F6)) {
                TogglePlayersPanel();
            }
        }
        #endregion
        
        #region Implementation Methods
        private void SelectAllUnitsOnScreen() {
            Debug.Log("Select all units on screen");
            // TODO: Implement screen-based unit selection
        }
        
        private void SelectAllUnits() {
            Debug.Log("Select all units");
            // TODO: Implement all unit selection
        }
        
        private void CancelCurrentAction() {
            if (_placingBuilding) {
                _placingBuilding = false;
                if (_buildingGhost) {
                    Destroy(_buildingGhost);
                    _buildingGhost = null;
                }
            }
            SelectionManager.ClearSelection();
        }
        
        private void SetControlGroup(int groupIndex) {
            var selectedUnits = SelectionManager.GetSelectedUnits();
            _controlGroups[groupIndex].Clear();
            foreach (var unitId in selectedUnits) {
                _controlGroups[groupIndex].Add(unitId);
            }
            Debug.Log($"Set control group {groupIndex} with {selectedUnits.Count} units");
        }
        
        private void SelectControlGroup(int groupIndex) {
            if (_controlGroups[groupIndex].Count > 0) {
                SelectionManager.SetSelection(_controlGroups[groupIndex]);
                Debug.Log($"Selected control group {groupIndex}");
            }
        }
        
        private void AddToControlGroup(int groupIndex) {
            var selectedUnits = SelectionManager.GetSelectedUnits();
            foreach (var unitId in selectedUnits) {
                _controlGroups[groupIndex].Add(unitId);
            }
            Debug.Log($"Added {selectedUnits.Count} units to control group {groupIndex}");
        }
        
        private void SelectBuildingsByType(string buildingType) {
            Debug.Log($"Select buildings of type: {buildingType}");
            // TODO: Implement building type selection
        }
        
        private void CycleTownCenters() {
            Debug.Log("Cycle town centers");
            // TODO: Implement town center cycling
        }
        
        private void FocusCapitalTownCenter() {
            Debug.Log("Focus on capital town center");
            // TODO: Implement capital focus
        }
        
        private void CycleIdleWorkers() {
            Debug.Log("Cycle idle workers");
            // TODO: Implement idle worker cycling
        }
        
        private void CycleIdleMilitary() {
            Debug.Log("Cycle idle military");
            // TODO: Implement idle military cycling
        }
        
        private void SelectAllIdleWorkers() {
            Debug.Log("Select all idle workers");
            // TODO: Implement idle worker selection
        }
        
        private void SelectAllIdleMilitary() {
            Debug.Log("Select all idle military");
            // TODO: Implement idle military selection
        }
        
        private void DeleteSelectedEntities() {
            var selectedEntities = SelectionManager.GetSelectedEntities();
            foreach (var entityId in selectedEntities) {
                // Check if it's a building
                for (int i = 0; i < _sim.State.BuildingCount; i++) {
                    if (_sim.State.Buildings[i].Id == entityId) {
                        _sim.DestroyBuilding(entityId);
                        break;
                    }
                }
                // TODO: Handle unit deletion
            }
            SelectionManager.ClearSelection();
            Debug.Log($"Deleted {selectedEntities.Count} entities");
        }
        
        private void RotateCamera(float degrees) {
            if (CameraRig) {
                CameraRig.Rotate(0, degrees, 0);
                Debug.Log($"Rotated camera {degrees} degrees");
            }
        }
        
        private void ResetCamera() {
            if (CameraRig) {
                CameraRig.rotation = Quaternion.identity;
                // TODO: Reset zoom and position
                Debug.Log("Camera reset");
            }
        }
        
        private void FocusOnSelectedUnits() {
            var selectedUnits = SelectionManager.GetSelectedUnits();
            if (selectedUnits.Count > 0) {
                // TODO: Focus camera on selected units
                Debug.Log("Focus on selected units");
            }
        }
        
        private void FollowSelectedUnit() {
            var selectedUnits = SelectionManager.GetSelectedUnits();
            if (selectedUnits.Count > 0) {
                // TODO: Follow selected unit
                Debug.Log("Follow selected unit");
            }
        }
        
        private void ToggleBuildingMode() {
            _placingBuilding = !_placingBuilding;
            if (_placingBuilding) {
                if (GameController.BuildingGhostPrefab) {
                    _buildingGhost = Instantiate(GameController.BuildingGhostPrefab);
                }
                Debug.Log("Entered building placement mode");
            } else {
                if (_buildingGhost) {
                    Destroy(_buildingGhost);
                    _buildingGhost = null;
                }
                Debug.Log("Exited building placement mode");
            }
        }
        
        private void CycleBuildingType(int direction) {
            if (!_placingBuilding) return;
            
            int buildingCount = Mathf.Max(1, FrontierAges.Sim.DataRegistry.Buildings?.Length ?? 1);
            _placeBuildingIndex = (_placeBuildingIndex + direction + buildingCount) % buildingCount;
            Debug.Log($"Building type: {_placeBuildingIndex}");
        }
        
        private void QuickTrainUnit() {
            if (_sim.State.BuildingCount > 0) {
                var buildingId = _sim.State.Buildings[0].Id;
                _sim.EnqueueTrain(buildingId, 0, 5000); // 5s train time
                Debug.Log("Quick train unit queued");
            }
        }
        
        private void StressTestTraining() {
            if (_sim.State.BuildingCount > 0) {
                var buildingId = _sim.State.Buildings[0].Id;
                for (int i = 0; i < 3; i++) {
                    _sim.EnqueueTrain(buildingId, 0, 5000);
                }
                Debug.Log("Stress test training queued (3 units)");
            }
        }
        
        private void ToggleSecondaryUI() {
            Debug.Log("Toggle secondary UI panel");
            // TODO: Implement secondary UI toggle
        }
        
        private void SetRallyPoint() {
            if (_sim.State.BuildingCount > 0) {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f)) {
                    int wx = (int)(hit.point.x * SimConstants.PositionScale);
                    int wy = (int)(hit.point.z * SimConstants.PositionScale);
                    _sim.SetRallyPoint(_sim.State.Buildings[0].Id, wx, wy);
                    Debug.Log($"Rally point set at ({wx}, {wy})");
                }
            }
        }
        
        private void ClearRallyPoint() {
            if (_sim.State.BuildingCount > 0) {
                _sim.ClearRallyPoint(_sim.State.Buildings[0].Id);
                Debug.Log("Rally point cleared");
            }
        }
        
        private void ToggleGameMenu() {
            Debug.Log("Toggle game menu");
            // TODO: Implement game menu toggle
        }
        
        private void ToggleGameTimeDisplay() {
            Debug.Log("Toggle game time display");
            // TODO: Implement game time display toggle
        }
        
        private void TogglePause() {
            Debug.Log("Toggle pause");
            // TODO: Implement pause toggle
        }
        
        private void QuickSave() {
            Debug.Log("Quick save");
            // TODO: Implement quick save
        }
        
        private void QuickLoad() {
            Debug.Log("Quick load");
            // TODO: Implement quick load
        }
        
        private void ToggleTeamColors() {
            Debug.Log("Toggle team colors");
            // TODO: Implement team color toggle
        }
        
        private void CycleSelectedUnits() {
            Debug.Log("Cycle selected units forward");
            // TODO: Implement unit cycling
        }
        
        private void CycleSelectedUnitsReverse() {
            Debug.Log("Cycle selected units reverse");
            // TODO: Implement reverse unit cycling
        }
        
        private void OpenTeamChat() {
            Debug.Log("Open team chat");
            // TODO: Implement team chat
        }
        
        private void OpenAllChat() {
            Debug.Log("Open all chat");
            // TODO: Implement all chat
        }
        
        private void FocusOnLastEvent() {
            Debug.Log("Focus on last event");
            // TODO: Implement last event focus
        }
        
        private void TogglePlayersPanel() {
            Debug.Log("Toggle players panel");
            // TODO: Implement players panel toggle
        }
        #endregion
        
        /// <summary>
        /// Display current control scheme in debug overlay
        /// </summary>
        public void ShowControlHelp() {
            Debug.Log(@"
=== Age of Empires 4 Control Scheme ===
SELECTION:
- Left Click: Select unit
- Double Click: Select all visible units of same type
- Ctrl+Click: Add/remove from selection
- Drag: Box select
- Ctrl+A: Select all units on screen
- ESC: Deselect all

CONTROL GROUPS:
- 0-9: Select control group
- Ctrl+0-9: Set control group
- Shift+0-9: Add to control group

UNIT MANAGEMENT:
- F1: Military buildings
- F2: Economy buildings  
- F3: Research buildings
- F4: Landmarks/Town Centers
- H: Cycle town centers
- Period(.): Cycle idle workers
- Comma(,): Cycle idle military
- Delete: Delete selected

CAMERA:
- [: Rotate left 45°
- ]: Rotate right 45°
- Backspace: Reset camera
- F5: Focus on selection
- Arrow Keys: Pan camera

BUILDING:
- B: Toggle building mode
- Period/Comma: Cycle building types
- P: Set rally point
- O: Clear rally point

TRAINING:
- T: Train unit
- Y: Secondary UI / Multiple training

GAME:
- F10: Game menu
- F11: Toggle time display
- Space: Focus last event
- F6: Players panel
            ");
        }
    }
}
