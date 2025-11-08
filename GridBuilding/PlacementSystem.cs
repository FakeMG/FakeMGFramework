using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.Framework.GridBuilding
{
    public class PlacementSystem : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LayerMask _placementLayerMask;
        [SerializeField] private bool _enableLogging = true;

        private Camera _mainCamera;

        private AsyncOperationHandle<GameObject> _currentStructurePrefabHandle;

        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _placedStructureHandles = new();
        private readonly Dictionary<string, GameObject> _placedStructureInstances = new();

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        public bool GetSelectedWorldPos(Vector2 screenPos, out Vector3 selectedWorldPos, out RaycastHit hitInfo)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out hitInfo, 100, _placementLayerMask))
            {
                // Offset slightly inside the structure to ensure we get the structure's actual position, not the surface
                selectedWorldPos = hitInfo.point - hitInfo.normal * 0.01f;

                return true;
            }

            selectedWorldPos = Vector3.zero;
            return false;
        }

        // TODO: Clamp to grid bounds

        public void PlaceStructureIfEmpty(ItemSO itemSO, Vector3 worldPosition)
        {
            _currentStructurePrefabHandle = Addressables.LoadAssetAsync<GameObject>(itemSO.PrefabAsset);
            _currentStructurePrefabHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject structurePrefab = handle.Result;
                    Vector3 gridWorldPosition = _gridManager.WorldToGridWorld(worldPosition);
                    GameObject structureInstance = Instantiate(structurePrefab, gridWorldPosition, Quaternion.identity);

                    if (!_gridManager.IsEmptyGridSpace(structureInstance))
                    {
                        Echo.Warning("Cannot place structure: Grid space is occupied or outside bounds.", _enableLogging);
                        Destroy(structureInstance);
                        Addressables.Release(handle);
                        return;
                    }

                    string instanceID = System.Guid.NewGuid().ToString();

                    _gridManager.RegisterStructure(gridWorldPosition, structureInstance, instanceID);

                    _placedStructureInstances[instanceID] = structureInstance;
                    _placedStructureHandles[instanceID] = handle;
                }
                else
                {
                    Echo.Error("Failed to load structure prefab.", _enableLogging);
                }
            };
        }

        public void RemoveStructure(Vector3 worldPosition)
        {
            if (_gridManager.TryRemoveStructure(worldPosition, out var instanceID))
            {
                if (_placedStructureInstances.TryGetValue(instanceID, out var structureInstance))
                {
                    Destroy(structureInstance);
                    _placedStructureInstances.Remove(instanceID);
                }
                else
                {
                    Echo.Log($"No structure instance found with ID: {instanceID}", _enableLogging);
                }

                if (_placedStructureHandles.TryGetValue(instanceID, out var handle))
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                    _placedStructureHandles.Remove(instanceID);
                }
            }
            else
            {
                Echo.Log("No structure found at the specified position to remove.", _enableLogging);
            }
        }

        public GameObject PickUpStructure(Vector3 worldPosition)
        {
            if (_gridManager.TryRemoveStructure(worldPosition, out string instanceID))
            {
                if (_placedStructureInstances.TryGetValue(instanceID, out GameObject structureInstance))
                {
                    _placedStructureInstances.Remove(instanceID);
                    return structureInstance;
                }
            }
            return null;
        }

        public void PlacePickedUpStructureIfEmpty(GameObject structureInstance, Vector3 worldPosition)
        {
            Vector3 gridWorldPosition = _gridManager.WorldToGridWorld(worldPosition);
            structureInstance.transform.position = gridWorldPosition;

            if (!_gridManager.IsEmptyGridSpace(structureInstance))
            {
                Echo.Warning("Cannot place structure: Grid space is occupied or outside bounds.", _enableLogging);
                return;
            }

            string instanceID = System.Guid.NewGuid().ToString();
            _gridManager.RegisterStructure(gridWorldPosition, structureInstance, instanceID);

            _placedStructureInstances[instanceID] = structureInstance;
        }
    }
}