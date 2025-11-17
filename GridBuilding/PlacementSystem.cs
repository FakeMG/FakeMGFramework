using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        private readonly Dictionary<string, ItemSO> _placedStructureItemSOs = new();

        public event Action OnPlaced;
        public event Action OnRemoved;

        public IReadOnlyDictionary<string, GameObject> GetPlacedStructureInstances()
        {
            return _placedStructureInstances;
        }

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

        public UniTask<bool> PlaceStructureIfEmptyAsync(ItemSO itemSO, Vector3 worldPosition)
        {
            UniTaskCompletionSource<bool> placementCompletionSource = new();

            _currentStructurePrefabHandle = Addressables.LoadAssetAsync<GameObject>(itemSO.PrefabAsset);
            _currentStructurePrefabHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject structurePrefab = handle.Result;
                    Vector3 gridWorldPosition = _gridManager.WorldToGridWorld(worldPosition);
                    GameObject structureInstance = Instantiate(structurePrefab, gridWorldPosition, Quaternion.identity);

                    if (!_gridManager.IsEmptyGridSpace(structureInstance, gridWorldPosition))
                    {
                        Echo.Warning("Cannot place structure: Grid space is occupied or outside bounds.", _enableLogging);
                        Destroy(structureInstance);
                        Addressables.Release(handle);
                        placementCompletionSource.TrySetResult(false);
                        return;
                    }

                    string instanceID = Guid.NewGuid().ToString();

                    _gridManager.RegisterStructure(structureInstance, instanceID, gridWorldPosition);

                    _placedStructureInstances[instanceID] = structureInstance;
                    _placedStructureHandles[instanceID] = handle;
                    _placedStructureItemSOs[instanceID] = itemSO;

                    OnPlaced?.Invoke();
                    placementCompletionSource.TrySetResult(true);
                }
                else
                {
                    Echo.Error("Failed to load structure prefab.", _enableLogging);
                    placementCompletionSource.TrySetResult(false);
                }
            };

            return placementCompletionSource.Task;
        }

        public ItemSO RemoveStructure(Vector3 worldPosition)
        {
            if (_gridManager.TryRemoveStructure(worldPosition, out string instanceID))
            {
                if (_placedStructureInstances.TryGetValue(instanceID, out GameObject structureInstance))
                {
                    Destroy(structureInstance);
                    _placedStructureInstances.Remove(instanceID);
                    OnRemoved?.Invoke();
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

                if (_placedStructureItemSOs.TryGetValue(instanceID, out ItemSO itemSO))
                {
                    _placedStructureItemSOs.Remove(instanceID);
                }

                return itemSO;
            }
            else
            {
                Echo.Log("No structure found at the specified position to remove.", _enableLogging);
            }

            return null;
        }

        public ItemSO GetItemSOAtPosition(Vector3 worldPosition)
        {
            Vector3Int cellPosition = _gridManager.WorldToCell(worldPosition);

            if (_gridManager.GridData.TryGetValue(cellPosition, out PlacementData placementData))
            {
                if (_placedStructureItemSOs.TryGetValue(placementData.InstanceID, out ItemSO itemSO))
                {
                    return itemSO;
                }
            }

            return null;
        }

        public Vector3 GetStructurePosition(Vector3 worldPosition)
        {
            Vector3Int cellPosition = _gridManager.WorldToCell(worldPosition);

            if (_gridManager.GridData.TryGetValue(cellPosition, out PlacementData placementData))
            {
                if (_placedStructureInstances.TryGetValue(placementData.InstanceID, out GameObject structureInstance))
                {
                    return structureInstance.transform.position;
                }
            }

            Echo.Error("No structure found at the specified position.", _enableLogging);

            return Vector3.zero;
        }
    }
}