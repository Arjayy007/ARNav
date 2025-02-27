using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using TMPro; // Import TextMeshPro namespace
using UnityEngine.XR.ARFoundation;

public class NewIndoorNav : MonoBehaviour {
    [SerializeField] private Transform player; // Reference to the player (e.g., AR camera)
    [SerializeField] private ARTrackedImageManager m_TrackedImageManager;
    [SerializeField] private GameObject trackedImagePrefab;
    [SerializeField] private LineRenderer line; // Line Renderer component
    [SerializeField] private TMP_Dropdown dropdown; // TextMeshPro Dropdown for selecting targets

    private List<GameObject> navigationTargets = new List<GameObject>(); // All target cubes
    private NavMeshSurface navMeshSurface;
    private NavMeshPath navMeshPath;

    private GameObject navigationBase;

    private void Start() {
        navMeshPath = new NavMeshPath();

        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Find all target cubes in the scene
        navigationTargets = GameObject.FindGameObjectsWithTag("Target").ToList();

        if (navigationTargets.Count == 0) {
            Debug.LogWarning("No targets found with the 'Target' tag!");
        }

        // Populate the dropdown with the names of the target cubes
        PopulateDropdown();

        // Listen to dropdown selection changes
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void Update() {
        // Continuously update navigation when a target is selected
        if (navigationTargets.Count > 0 && dropdown.options.Count > 0) {
            GameObject selectedTarget = navigationTargets.FirstOrDefault(
                target => target.name == dropdown.options[dropdown.value].text
            );

            if (selectedTarget != null) {
                NavMesh.CalculatePath(player.position, selectedTarget.transform.position, NavMesh.AllAreas, navMeshPath);

                if (navMeshPath.status == NavMeshPathStatus.PathComplete) {
                    line.positionCount = navMeshPath.corners.Length;
                    line.SetPositions(navMeshPath.corners);
                } else {
                    line.positionCount = 0; // Clear the line if no valid path
                }
            }
        }
    }

    private void OnEnable() {
        if (m_TrackedImageManager != null)
            m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable() {
        if (m_TrackedImageManager != null)
            m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args) {
        foreach (var newImage in args.added) {
            navigationBase = Instantiate(trackedImagePrefab);
            navMeshSurface = navigationBase.GetComponentInChildren<NavMeshSurface>();
        }

        foreach (var updatedImage in args.updated) {
            if (navigationBase != null)
                navigationBase.transform.SetPositionAndRotation(
                    updatedImage.transform.position,
                    Quaternion.Euler(0, updatedImage.transform.rotation.eulerAngles.y, 0)
                );
        }
    }

    private void PopulateDropdown() {
        dropdown.options.Clear();

        // Add each target name to the dropdown
        foreach (var target in navigationTargets) {
            dropdown.options.Add(new TMP_Dropdown.OptionData(target.name));
        }

        // Refresh the dropdown to update its display
        dropdown.RefreshShownValue();

        // Default to the first target if available
        if (navigationTargets.Count > 0) {
            dropdown.value = 0;
            dropdown.captionText.text = dropdown.options[0].text;
            UpdateLineRenderer(); // Automatically draw the path to the first target
        }
    }

    private void OnDropdownValueChanged(int index) {
        // Triggered when a dropdown selection changes
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer() {
        // Find the selected target based on the dropdown value
        string selectedTargetName = dropdown.options[dropdown.value].text;
        GameObject selectedTarget = navigationTargets.FirstOrDefault(target => target.name == selectedTargetName);

        if (selectedTarget != null) {
            NavMesh.CalculatePath(player.position, selectedTarget.transform.position, NavMesh.AllAreas, navMeshPath);

            if (navMeshPath.status == NavMeshPathStatus.PathComplete) {
                line.positionCount = navMeshPath.corners.Length;
                line.SetPositions(navMeshPath.corners);
            } else {
                line.positionCount = 0; // Clear the line if no path exists
            }
        } else {
            Debug.LogWarning($"No valid target found with the name: {selectedTargetName}");
            line.positionCount = 0; // Clear the line if the target is not valid
        }
    }
}
