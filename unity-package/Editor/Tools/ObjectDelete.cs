using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models; // For Response class

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles UnityEngine.Object deletion operations using dual state tree architecture with interactive confirmation.
    /// Supports GameObjects, assets, and other Unity objects.
    /// Target tree: IObjectSelector handles target location
    /// Action tree: 'confirm' parameter determines confirmation behavior:
    ///   - confirm=true: Always shows confirmation dialog before deletion
    ///   - confirm=false/unset: Asset deletion requires confirmation, scene object deletion is direct
    /// Uses coroutines with EditorUtility.DisplayDialog for interactive user confirmation.
    /// Corresponding method name: object_delete
    /// </summary>
    [ToolName("object_delete", "Object editing")]
    public class ObjectDelete : DualStateMethodBase
    {
        private IObjectSelector objectSelector;

        public ObjectDelete()
        {
            objectSelector = new ObjectSelector<UnityEngine.Object>();
        }

        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                // Target lookup parameter（Entrust toIObjectSelectorHandle）
                new MethodKey("path", "Object Hierarchy path", false),
                new MethodKey("instance_id", "Object InstanceID", true),
                new MethodKey("confirm", "Force confirmation dialog: true=always confirm, false/unset=smart confirmation (auto ≤3, dialog >3)", true),
            };
        }

        /// <summary>
        /// Create target location state tree（UseIObjectSelector）
        /// </summary>
        protected override StateTree CreateTargetTree()
        {
            return objectSelector.BuildStateTree();
        }

        /// <summary>
        /// Create operation execution state tree
        /// </summary>
        protected override StateTree CreateActionTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("confirm")
                    .Leaf("true", (Func<StateTreeContext, object>)HandleConfirmedDeleteAction) // Confirm deletion
                    .Leaf("false", (Func<StateTreeContext, object>)HandleUnconfirmedDeleteAction) // Unconfirmed deletion
                    .DefaultLeaf((Func<StateTreeContext, object>)HandleUnconfirmedDeleteAction) // Default as unconfirmed deletion
                .Build();
        }

        /// <summary>
        /// Asynchronously handle delete actions requiring user confirmation（For resource file deletions only）
        /// </summary>
        private IEnumerator HandleConfirmedDeleteActionAsync(StateTreeContext ctx)
        {
            UnityEngine.Object target = ExtractTargetFromContext(ctx);
            if (target == null)
            {
                yield return Response.Error("No target Object found for deletion.");
                yield break;
            }

            // Check if resource file is being deleted
            bool isAssetDeletion = IsAssetDeletion(ctx);
            if (!isAssetDeletion)
            {
                // Not a resource deletion，Delete directlyObject
                var result = DeleteSingleObject(target);
                yield return result;
                yield break;
            }

            // Resource delete requires confirmation dialog
            string confirmationMessage = $"Are you sure you want to delete the asset '{target.name}' ({target.GetType().Name})?\n\nThis action cannot be undone.";

            bool confirmed = EditorUtility.DisplayDialog(
                "Confirm Asset Deletion",
                confirmationMessage,
                "Delete Asset",
                "Cancel"
            );

            if (!confirmed)
            {
                LogInfo($"[ObjectDelete] User cancelled asset deletion for Object '{target.name}'");
                yield return Response.Success($"Asset deletion cancelled by user. Object '{target.name}' was not deleted.", new { cancelled = true, target_name = target.name });
                yield break;
            }

            LogInfo($"[ObjectDelete] User confirmed asset deletion for Object '{target.name}'");

            // Perform deletion after user confirmation
            var deleteResult = DeleteSingleObject(target);
            yield return deleteResult;
        }

        /// <summary>
        /// Handle delete actions requiring user confirmation
        /// </summary>
        private object HandleConfirmedDeleteAction(StateTreeContext ctx)
        {
            return ctx.AsyncReturn(HandleConfirmedDeleteActionAsync(ctx));
        }

        /// <summary>
        /// Asynchronously handle unconfirmed deletions，Check if resource deletion requires confirmation
        /// </summary>
        private IEnumerator HandleUnconfirmedDeleteActionAsync(StateTreeContext ctx)
        {
            UnityEngine.Object target = ExtractTargetFromContext(ctx);
            if (target == null)
            {
                yield return Response.Error("No target Object found for deletion.");
                yield break;
            }

            // Check prefab redirection（For onlyGameObjectApplicable）
            if (target is GameObject gameObject)
            {
                object redirectResult = CheckPrefabRedirection(gameObject);
                if (redirectResult != null)
                {
                    yield return redirectResult;
                    yield break;
                }
            }

            // Check if it is a resource deletion
            bool isAssetDeletion = IsAssetDeletion(ctx);
            if (!isAssetDeletion)
            {
                // SceneObjectDelete，Direct delete without confirmation
                LogInfo($"[ObjectDelete] Direct deletion of {target.GetType().Name} '{target.name}' without confirmation");
                var result = DeleteSingleObject(target);
                yield return result;
                yield break;
            }

            // Resource deletion requires user confirmation
            LogInfo($"[ObjectDelete] Asset deletion detected for '{target.name}', showing confirmation dialog");

            string confirmationMessage = $"You are about to delete the asset '{target.name}' ({target.GetType().Name}).\n\nThis action cannot be undone. Continue?";

            bool confirmed = EditorUtility.DisplayDialog(
                "Confirm Asset Deletion",
                confirmationMessage,
                "Delete Asset",
                "Cancel"
            );

            if (!confirmed)
            {
                LogInfo($"[ObjectDelete] User cancelled asset deletion for '{target.name}'");
                yield return Response.Success($"Asset deletion cancelled by user. Object '{target.name}' was not deleted.", new { cancelled = true, target_name = target.name });
                yield break;
            }

            LogInfo($"[ObjectDelete] User confirmed asset deletion for '{target.name}'");

            // Perform deletion after user confirmation
            var deleteResult = DeleteSingleObject(target);
            yield return deleteResult;
        }

        /// <summary>
        /// Handle unconfirmed delete actions
        /// </summary>
        private object HandleUnconfirmedDeleteAction(StateTreeContext ctx)
        {
            return ctx.AsyncReturn(HandleUnconfirmedDeleteActionAsync(ctx));
        }

        /// <summary>
        /// Extract unique target from execution contextUnityEngine.Object
        /// </summary>
        private UnityEngine.Object ExtractTargetFromContext(StateTreeContext context)
        {
            // First attempt fromObjectReferencesGet（Avoid serialization issues）
            if (context.TryGetObjectReference("_resolved_targets", out object targetsObj))
            {
                if (targetsObj is UnityEngine.Object singleObject)
                {
                    return singleObject;
                }
                else if (targetsObj is UnityEngine.Object[] objectArray && objectArray.Length > 0)
                {
                    return objectArray[0]; // Only take the first
                }
                else if (targetsObj is System.Collections.IList list && list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        if (item is UnityEngine.Object obj)
                            return obj; // Return the first foundUnityEngine.Object
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Check if it is a resource deletion operation
        /// </summary>
        private bool IsAssetDeletion(StateTreeContext context)
        {
            // ThroughpathDetermine if parameter is a resource path
            if (context.TryGetValue("path", out object pathObj) && pathObj != null)
            {
                string path = pathObj.ToString();
                // If path starts withAssets/Start，Then regarded as resource deletion
                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check prefab redirection logic
        /// </summary>
        private object CheckPrefabRedirection(GameObject target)
        {
            if (target == null)
                return null;

            // Check if it is a prefab instance，If it is the prefab asset itself，Should usemanage_assetCommand
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                // This is a prefab instance，Can delete normally
                return null;
            }

            return null; // Continue normal processing
        }

        /// <summary>
        /// Delete singleUnityEngine.Object
        /// </summary>
        private object DeleteSingleObject(UnityEngine.Object targetObject)
        {
            if (targetObject == null)
            {
                return Response.Error("Target Object is null.");
            }

            string objectName = targetObject.name;
            int objectId = targetObject.GetInstanceID();
            string objectType = targetObject.GetType().Name;

            try
            {
                // Determine if it is a resource file
                string assetPath = AssetDatabase.GetAssetPath(targetObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // Resource file deletion
                    bool success = AssetDatabase.DeleteAsset(assetPath);
                    if (success)
                    {
                        var deletedObject = new { name = objectName, instanceID = objectId, type = objectType, assetPath = assetPath };
                        return Response.Success($"{objectType} asset '{objectName}' deleted successfully.", deletedObject);
                    }
                    else
                    {
                        return Response.Error($"Failed to delete {objectType} asset '{objectName}' at path: {assetPath}");
                    }
                }
                else
                {
                    // Scene object deletion
                    Undo.DestroyObjectImmediate(targetObject);
                    var deletedObject = new { name = objectName, instanceID = objectId, type = objectType };
                    return Response.Success($"{objectType} '{objectName}' deleted successfully.", deletedObject);
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to delete {objectType} '{objectName}': {e.Message}");
            }
        }



    }
}