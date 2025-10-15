using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models; // For Response class

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles RectTransform modification operations using dual state tree architecture.
    /// First tree: Target location (using GameObjectSelector)
    /// Second tree: Layout operations based on action type
    /// 
    /// Usage：
    /// - do_layout: Perform integrated layout modification (Can set multiple properties at once，Does not contain anchor preset)
    /// - get_layout: GetRectTransformAttribute (Get all attribute information)
    /// - tattoo: Set anchor preset (Specifically handletattoo_preset、tattoo_self、preserve_visual_position)
    /// 
    /// Special parameter：
    /// - tattoo_self: When istrueWhen，Anchor presets will be based on the element's current position instead of the parent's preset position
    ///   * stretch_all + tattoo_self = tattooFunction（Equivalent toUGUIUtil.AnchorsToCorners）
    ///   * top_center + tattoo_self = Set the anchor to the element's own top-center position
    ///   * Other presets + tattoo_self = Set the anchor to the corresponding position of the element itself
    /// 
    /// For example：
    /// action="do_layout", anchored_pos=[100, -50], size_delta=[200, 100]  // Do not use anchor preset
    /// action="tattoo", tattoo_preset="stretch_all", tattoo_self=true  // tattooEffect
    /// action="tattoo", tattoo_preset="top_center", tattoo_self=true   // Pin to the top center of the element
    /// action="get_layout"
    /// 
    /// Note：do_layoutOperation not supportedtattoo_presetParameter，Use to set anchor presets if neededtattooOperation。
    /// 
    /// Note：GameWindow resolution setting feature has been moved to game_view Tool
    /// 
    /// Corresponding method name: ugui_layout
    /// </summary>
    [ToolName("ugui_layout", "UIManage")]
    public class UGUILayout : DualStateMethodBase
    {
        /// <summary>
        /// Create a list of parameter keys supported by the current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                // Target search parameter
                new MethodKey("instance_id", "Object InstanceID", true),
                new MethodKey("path", "Object Hierarchy path", false),
                
                // Operation parameter
                new MethodKey("action", "Operation type: do_layout(Integrated layout,Does not contain anchor preset), get_layout(Get attribute), tattoo(Set anchor preset)", true),
                
                // RectTransformBasic property
                new MethodKey("anchored_pos", "Anchor position [x, y]", true),
                new MethodKey("size_delta", "Size delta [width, height]", true),
                new MethodKey("anchor_min", "Minimum anchor [x, y]", true),
                new MethodKey("anchor_max", "Maximum anchor [x, y]", true),
                      // Preset anchor type
                new MethodKey("tattoo_preset", "Anchor preset: top_left, top_center, top_right, middle_left, middle_center, middle_right, bottom_left, bottom_center, bottom_right, stretch_horizontal, stretch_vertical, stretch_all", true),
                new MethodKey("tattoo_self", "When true, anchor preset will be based on element's current position rather than parent's preset position (default: false)", true),
                new MethodKey("preserve_visual_position", "Whether to preserve visual position when changing anchor preset (default: true)", true),
                new MethodKey("pivot", "Pivot point [x, y]", true),
                
                // Hierarchy control
                new MethodKey("sibling_index", "Sibling index in parent hierarchy", true)
            };
        }

        /// <summary>
        /// Create target positioning status tree（UseGameObjectSelector）
        /// </summary>
        protected override StateTree CreateTargetTree()
        {
            return new ObjectSelector<GameObject>().BuildStateTree();
        }

        /// <summary>
        /// Create operation execution status tree
        /// </summary>
        protected override StateTree CreateActionTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("do_layout", (Func<StateTreeContext, object>)HandleDoLayoutAction)
                    .Leaf("get_layout", (Func<StateTreeContext, object>)HandleGetLayoutAction)
                    .Leaf("tattoo", (Func<StateTreeContext, object>)HandleSetAnchorPresetAction)
                    .DefaultLeaf((Func<StateTreeContext, object>)HandleDefaultAction)
                .Build();
        }

        /// <summary>
        /// Handle layout operation（ExecuteRectTransformModify）
        /// </summary>
        private object HandleDoLayoutAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            // Otherwise execute according to the incoming attribute parameters RectTransform Modify
            if (targets.Length == 1)
            {
                return ApplyRectTransformModifications(targets[0], args);
            }
            else
            {
                return ApplyRectTransformModificationsToMultiple(targets, args);
            }
        }

        /// <summary>
        /// Handle get layout info operation
        /// </summary>
        private object HandleGetLayoutAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }
            // Otherwise get all RectTransform Attribute information
            if (targets.Length == 1)
            {
                return GetAllRectTransformProperties(targets[0]);
            }
            else
            {
                return GetAllRectTransformPropertiesFromMultiple(targets);
            }
        }

        /// <summary>
        /// Handle anchor preset operation
        /// </summary>
        private object HandleSetAnchorPresetAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            // Specially handle anchor presets
            if (targets.Length == 1)
            {
                return ApplyAnchorPresetToSingle(targets[0], args);
            }
            else
            {
                return ApplyAnchorPresetToMultiple(targets, args);
            }
        }

        /// <summary>
        /// Default operation handling（Not specified action Default when do_layout）
        /// </summary>
        private object HandleDefaultAction(StateTreeContext args)
        {
            LogInfo("[UGUILayout] No action specified, using default do_layout action");
            return HandleDoLayoutAction(args);
        }



        #region Core modification method

        /// <summary>
        /// ApplyRectTransformModify to singleGameObject
        /// </summary>
        private object ApplyRectTransformModifications(GameObject targetGo, StateTreeContext args)
        {
            RectTransform rectTransform = targetGo.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return Response.Error($"GameObject '{targetGo.name}' does not have a RectTransform component.");
            }

            Undo.RecordObject(rectTransform, "Modify RectTransform");

            bool modified = false;

            // ApplyRectTransformSpecific property（Does not contain anchor preset）
            modified |= ApplyAnchoredPosition(rectTransform, args);
            modified |= ApplySizeDelta(rectTransform, args);
            modified |= ApplyAnchorMin(rectTransform, args);
            modified |= ApplyAnchorMax(rectTransform, args);
            modified |= ApplyPivot(rectTransform, args);

            // Apply hierarchy control
            modified |= ApplySetSiblingIndex(rectTransform, args);
            if (!modified)
            {
                return Response.Success(
                    $"No modifications applied to RectTransform on '{targetGo.name}'.",
                    GetRectTransformData(rectTransform)
                );
            }

            EditorUtility.SetDirty(rectTransform);
            return Response.Success(
                $"RectTransform on '{targetGo.name}' modified successfully.",
                GetRectTransformData(rectTransform)
            );
        }

        /// <summary>
        /// ApplyRectTransformModify to multipleGameObject
        /// </summary>
        private object ApplyRectTransformModificationsToMultiple(GameObject[] targets, StateTreeContext args)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject targetGo in targets)
            {
                if (targetGo == null) continue;

                try
                {
                    var result = ApplyRectTransformModifications(targetGo, args);

                    if (IsSuccessResponse(result, out object data, out string responseMessage))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                        else
                        {
                            var rectTransform = targetGo.GetComponent<RectTransform>();
                            if (rectTransform != null)
                            {
                                results.Add(GetRectTransformData(rectTransform));
                            }
                        }
                    }
                    else
                    {
                        errors.Add($"[{targetGo.name}] {responseMessage ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{targetGo.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("modify RectTransform", successCount, targets.Length, results, errors);
        }

        #endregion

        #region Method dedicated to anchor presets

        /// <summary>
        /// Apply anchor preset to singleGameObject
        /// </summary>
        private object ApplyAnchorPresetToSingle(GameObject targetGo, StateTreeContext args)
        {
            RectTransform rectTransform = targetGo.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return Response.Error($"GameObject '{targetGo.name}' does not have a RectTransform component.");
            }

            Undo.RecordObject(rectTransform, "Set Anchor Preset");

            // Only process anchor preset related parameters
            bool modified = ApplyAnchorPreset(rectTransform, args);

            if (!modified)
            {
                return Response.Success(
                    $"No anchor preset modifications applied to RectTransform on '{targetGo.name}'.",
                    GetRectTransformData(rectTransform)
                );
            }

            EditorUtility.SetDirty(rectTransform);
            return Response.Success(
                $"Anchor preset applied successfully to RectTransform on '{targetGo.name}'.",
                GetRectTransformData(rectTransform)
            );
        }

        /// <summary>
        /// Apply anchor preset to multipleGameObject
        /// </summary>
        private object ApplyAnchorPresetToMultiple(GameObject[] targets, StateTreeContext args)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject targetGo in targets)
            {
                if (targetGo == null) continue;

                try
                {
                    var result = ApplyAnchorPresetToSingle(targetGo, args);

                    if (IsSuccessResponse(result, out object data, out string responseMessage))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                        else
                        {
                            var rectTransform = targetGo.GetComponent<RectTransform>();
                            if (rectTransform != null)
                            {
                                results.Add(GetRectTransformData(rectTransform));
                            }
                        }
                    }
                    else
                    {
                        errors.Add($"[{targetGo.name}] {responseMessage ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{targetGo.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("set anchor preset", successCount, targets.Length, results, errors);
        }

        #endregion

        #region RectTransformAttribute apply method

        /// <summary>
        /// Apply anchor preset（Keep visual position unchanged）
        /// </summary>
        private bool ApplyAnchorPreset(RectTransform rectTransform, StateTreeContext args)
        {
            if (args.TryGetValue("tattoo_preset", out object presetObj) && presetObj != null)
            {
                string preset = presetObj.ToString().ToLower();
                Vector2 targetAnchorMin, targetAnchorMax, targetPivot;

                switch (preset)
                {
                    case "top_left":
                        targetAnchorMin = new Vector2(0, 1);
                        targetAnchorMax = new Vector2(0, 1);
                        targetPivot = new Vector2(0, 1);
                        break;
                    case "top_center":
                        targetAnchorMin = new Vector2(0.5f, 1);
                        targetAnchorMax = new Vector2(0.5f, 1);
                        targetPivot = new Vector2(0.5f, 1);
                        break;
                    case "top_right":
                        targetAnchorMin = new Vector2(1, 1);
                        targetAnchorMax = new Vector2(1, 1);
                        targetPivot = new Vector2(1, 1);
                        break;
                    case "middle_left":
                        targetAnchorMin = new Vector2(0, 0.5f);
                        targetAnchorMax = new Vector2(0, 0.5f);
                        targetPivot = new Vector2(0, 0.5f);
                        break;
                    case "middle_center":
                        targetAnchorMin = new Vector2(0.5f, 0.5f);
                        targetAnchorMax = new Vector2(0.5f, 0.5f);
                        targetPivot = new Vector2(0.5f, 0.5f);
                        break;
                    case "middle_right":
                        targetAnchorMin = new Vector2(1, 0.5f);
                        targetAnchorMax = new Vector2(1, 0.5f);
                        targetPivot = new Vector2(1, 0.5f);
                        break;
                    case "bottom_left":
                        targetAnchorMin = new Vector2(0, 0);
                        targetAnchorMax = new Vector2(0, 0);
                        targetPivot = new Vector2(0, 0);
                        break;
                    case "bottom_center":
                        targetAnchorMin = new Vector2(0.5f, 0);
                        targetAnchorMax = new Vector2(0.5f, 0);
                        targetPivot = new Vector2(0.5f, 0);
                        break;
                    case "bottom_right":
                        targetAnchorMin = new Vector2(1, 0);
                        targetAnchorMax = new Vector2(1, 0);
                        targetPivot = new Vector2(1, 0);
                        break;
                    case "stretch_horizontal":
                        targetAnchorMin = new Vector2(0, 0.5f);
                        targetAnchorMax = new Vector2(1, 0.5f);
                        targetPivot = new Vector2(0.5f, 0.5f);
                        break;
                    case "stretch_vertical":
                        targetAnchorMin = new Vector2(0.5f, 0);
                        targetAnchorMax = new Vector2(0.5f, 1);
                        targetPivot = new Vector2(0.5f, 0.5f);
                        break;
                    case "stretch_all":
                        targetAnchorMin = new Vector2(0, 0);
                        targetAnchorMax = new Vector2(1, 1);
                        targetPivot = new Vector2(0.5f, 0.5f);
                        break;
                    default:
                        return false;
                }

                // Check whether to usetattoo_selfPattern
                bool anchorSelf = false;
                if (args.TryGetValue("tattoo_self", out object anchorSelfObj))
                {
                    if (anchorSelfObj is bool anchorSelfBool)
                        anchorSelf = anchorSelfBool;
                    else if (bool.TryParse(anchorSelfObj?.ToString(), out bool parsedAnchorSelf))
                        anchorSelf = parsedAnchorSelf;
                }

                // If usetattoo_selfPattern，Recalculate anchor based on the element's current position
                if (anchorSelf)
                {
                    return ApplyAnchorSelfPreset(rectTransform, preset, args);
                }

                // Check if modification is needed
                if (rectTransform.anchorMin == targetAnchorMin &&
                    rectTransform.anchorMax == targetAnchorMax &&
                    rectTransform.pivot == targetPivot)
                {
                    return false; // Already at target anchor，No modification required
                }

                // Check whether to keep the visual position
                bool preserveVisualPosition = true; // Keep visual position by default
                if (args.TryGetValue("preserve_visual_position", out object preserveObj))
                {
                    if (preserveObj is bool preserveBool)
                        preserveVisualPosition = preserveBool;
                    else if (bool.TryParse(preserveObj?.ToString(), out bool parsedPreserve))
                        preserveVisualPosition = parsedPreserve;
                }

                // Apply anchor preset
                if (preserveVisualPosition)
                {
                    return ApplyAnchorPresetWithVisualPositionPreserved(rectTransform, targetAnchorMin, targetAnchorMax, targetPivot);
                }
                else
                {
                    // Set anchor directly，Do not keep visual position
                    rectTransform.anchorMin = targetAnchorMin;
                    rectTransform.anchorMax = targetAnchorMax;
                    rectTransform.pivot = targetPivot;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Apply anchor preset and keep visual position unchanged（ReferenceUGUIUtil.AnchorsToCornersImplement）
        /// </summary>
        private bool ApplyAnchorPresetWithVisualPositionPreserved(RectTransform rectTransform, Vector2 targetAnchorMin, Vector2 targetAnchorMax, Vector2 targetPivot)
        {
            // Get parent container'sRectTransform
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
            {
                // If there is no parentRectTransform，Set anchor directly
                rectTransform.anchorMin = targetAnchorMin;
                rectTransform.anchorMax = targetAnchorMax;
                rectTransform.pivot = targetPivot;
                return true;
            }

            // Save the current world position and size
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);
            Vector2 worldSize = new Vector2(
                Vector3.Distance(worldCorners[0], worldCorners[3]),
                Vector3.Distance(worldCorners[0], worldCorners[1])
            );

            // Calculate the current relative position in the parent container（ReferenceUGUIUtil.AnchorsToCornersComputation method of）
            Vector2 currentOffsetMin = rectTransform.offsetMin;
            Vector2 currentOffsetMax = rectTransform.offsetMax;
            Vector2 currentAnchorMin = rectTransform.anchorMin;
            Vector2 currentAnchorMax = rectTransform.anchorMax;

            // Calculate actual anchor position currently（ContainoffsetEffect of）
            Vector2 actualAnchorMin = new Vector2(
                currentAnchorMin.x + currentOffsetMin.x / parentRect.rect.width,
                currentAnchorMin.y + currentOffsetMin.y / parentRect.rect.height
            );
            Vector2 actualAnchorMax = new Vector2(
                currentAnchorMax.x + currentOffsetMax.x / parentRect.rect.width,
                currentAnchorMax.y + currentOffsetMax.y / parentRect.rect.height
            );

            // Set new anchor and pivot
            rectTransform.anchorMin = targetAnchorMin;
            rectTransform.anchorMax = targetAnchorMax;
            rectTransform.pivot = targetPivot;

            // Calculate the required under the new anchoroffsetTo keep the same visual position
            Vector2 newOffsetMin = new Vector2(
                (actualAnchorMin.x - targetAnchorMin.x) * parentRect.rect.width,
                (actualAnchorMin.y - targetAnchorMin.y) * parentRect.rect.height
            );
            Vector2 newOffsetMax = new Vector2(
                (actualAnchorMax.x - targetAnchorMax.x) * parentRect.rect.width,
                (actualAnchorMax.y - targetAnchorMax.y) * parentRect.rect.height
            );

            // Apply newoffset
            rectTransform.offsetMin = newOffsetMin;
            rectTransform.offsetMax = newOffsetMax;

            return true;
        }

        /// <summary>
        /// Apply anchor preset based on its own position（tattoo_self=trueCall when）
        /// </summary>
        private bool ApplyAnchorSelfPreset(RectTransform rectTransform, string preset, StateTreeContext args)
        {
            // Get parent container'sRectTransform
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
            {
                Debug.LogWarning("[UGUILayout] Anchor self preset requires a parent RectTransform, skipping.");
                return false;
            }

            // Get the element's current world position in the parent container
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            Vector3[] parentWorldCorners = new Vector3[4];
            parentRect.GetWorldCorners(parentWorldCorners);

            // Calculate the element's relative position in the parent container（0-1Range）
            Vector3 elementBottomLeft = worldCorners[0];
            Vector3 elementTopRight = worldCorners[2];
            Vector3 elementCenter = (elementBottomLeft + elementTopRight) * 0.5f;

            Vector3 parentBottomLeft = parentWorldCorners[0];
            Vector3 parentTopRight = parentWorldCorners[2];

            Vector2 elementCenterRel = new Vector2(
                (elementCenter.x - parentBottomLeft.x) / (parentTopRight.x - parentBottomLeft.x),
                (elementCenter.y - parentBottomLeft.y) / (parentTopRight.y - parentBottomLeft.y)
            );

            Vector2 elementBottomLeftRel = new Vector2(
                (elementBottomLeft.x - parentBottomLeft.x) / (parentTopRight.x - parentBottomLeft.x),
                (elementBottomLeft.y - parentBottomLeft.y) / (parentTopRight.y - parentBottomLeft.y)
            );

            Vector2 elementTopRightRel = new Vector2(
                (elementTopRight.x - parentBottomLeft.x) / (parentTopRight.x - parentBottomLeft.x),
                (elementTopRight.y - parentBottomLeft.y) / (parentTopRight.y - parentBottomLeft.y)
            );

            // Not perform0-1Range clamping，Avoid being pushed back into the parent when exceeding the parent

            Vector2 newAnchorMin, newAnchorMax, newPivot;

            // Calculate anchor based on the element's own position according to the preset type
            switch (preset)
            {
                case "top_left":
                    newAnchorMin = new Vector2(elementBottomLeftRel.x, elementTopRightRel.y);
                    newAnchorMax = new Vector2(elementBottomLeftRel.x, elementTopRightRel.y);
                    newPivot = new Vector2(0, 1);
                    break;
                case "top_center":
                    newAnchorMin = new Vector2(elementCenterRel.x, elementTopRightRel.y);
                    newAnchorMax = new Vector2(elementCenterRel.x, elementTopRightRel.y);
                    newPivot = new Vector2(0.5f, 1);
                    break;
                case "top_right":
                    newAnchorMin = new Vector2(elementTopRightRel.x, elementTopRightRel.y);
                    newAnchorMax = new Vector2(elementTopRightRel.x, elementTopRightRel.y);
                    newPivot = new Vector2(1, 1);
                    break;
                case "middle_left":
                    newAnchorMin = new Vector2(elementBottomLeftRel.x, elementCenterRel.y);
                    newAnchorMax = new Vector2(elementBottomLeftRel.x, elementCenterRel.y);
                    newPivot = new Vector2(0, 0.5f);
                    break;
                case "middle_center":
                    newAnchorMin = elementCenterRel;
                    newAnchorMax = elementCenterRel;
                    newPivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middle_right":
                    newAnchorMin = new Vector2(elementTopRightRel.x, elementCenterRel.y);
                    newAnchorMax = new Vector2(elementTopRightRel.x, elementCenterRel.y);
                    newPivot = new Vector2(1, 0.5f);
                    break;
                case "bottom_left":
                    newAnchorMin = elementBottomLeftRel;
                    newAnchorMax = elementBottomLeftRel;
                    newPivot = new Vector2(0, 0);
                    break;
                case "bottom_center":
                    newAnchorMin = new Vector2(elementCenterRel.x, elementBottomLeftRel.y);
                    newAnchorMax = new Vector2(elementCenterRel.x, elementBottomLeftRel.y);
                    newPivot = new Vector2(0.5f, 0);
                    break;
                case "bottom_right":
                    newAnchorMin = new Vector2(elementTopRightRel.x, elementBottomLeftRel.y);
                    newAnchorMax = new Vector2(elementTopRightRel.x, elementBottomLeftRel.y);
                    newPivot = new Vector2(1, 0);
                    break;
                case "stretch_horizontal":
                    newAnchorMin = new Vector2(elementBottomLeftRel.x, elementCenterRel.y);
                    newAnchorMax = new Vector2(elementTopRightRel.x, elementCenterRel.y);
                    newPivot = new Vector2(0.5f, 0.5f);
                    break;
                case "stretch_vertical":
                    newAnchorMin = new Vector2(elementCenterRel.x, elementBottomLeftRel.y);
                    newAnchorMax = new Vector2(elementCenterRel.x, elementTopRightRel.y);
                    newPivot = new Vector2(0.5f, 0.5f);
                    break;
                case "stretch_all":
                    // stretch_all + tattoo_self = tattooFunction（AnchorsToCorners）
                    newAnchorMin = new Vector2(
                        rectTransform.anchorMin.x + rectTransform.offsetMin.x / parentRect.rect.width,
                        rectTransform.anchorMin.y + rectTransform.offsetMin.y / parentRect.rect.height
                    );
                    newAnchorMax = new Vector2(
                        rectTransform.anchorMax.x + rectTransform.offsetMax.x / parentRect.rect.width,
                        rectTransform.anchorMax.y + rectTransform.offsetMax.y / parentRect.rect.height
                    );
                    newPivot = new Vector2(0.5f, 0.5f);
                    break;
                default:
                    return false;
            }

            // Check if modification is needed（Avoid unnecessary updates）
            if (Vector2.Distance(rectTransform.anchorMin, newAnchorMin) < 0.001f &&
                Vector2.Distance(rectTransform.anchorMax, newAnchorMax) < 0.001f &&
                Vector2.Distance(rectTransform.pivot, newPivot) < 0.001f)
            {
                return false; // Already in target state，No modification required
            }

            // Apply new anchor
            rectTransform.anchorMin = newAnchorMin;
            rectTransform.anchorMax = newAnchorMax;
            rectTransform.pivot = newPivot;

            // ReadUnityAnchor actually applied（AvoidUnityInternal clamping causes errors）
            Vector2 appliedAnchorMin = rectTransform.anchorMin;
            Vector2 appliedAnchorMax = rectTransform.anchorMax;

            // Calculate new based on the element's four-corner relative coordinatesoffset，Keep the visual position and size unchanged
            float pw = parentRect.rect.width;
            float ph = parentRect.rect.height;
            Vector2 newOffsetMin = new Vector2(
                (elementBottomLeftRel.x - appliedAnchorMin.x) * pw,
                (elementBottomLeftRel.y - appliedAnchorMin.y) * ph
            );
            Vector2 newOffsetMax = new Vector2(
                (elementTopRightRel.x - appliedAnchorMax.x) * pw,
                (elementTopRightRel.y - appliedAnchorMax.y) * ph
            );
            rectTransform.offsetMin = newOffsetMin;
            rectTransform.offsetMax = newOffsetMax;

            Debug.Log($"[UGUILayout] Applied tattoo_self preset '{preset}' to '{rectTransform.name}': anchors [{newAnchorMin.x:F3},{newAnchorMin.y:F3}] to [{newAnchorMax.x:F3},{newAnchorMax.y:F3}]");
            return true;
        }

        /// <summary>
        /// Apply anchor position modification
        /// </summary>
        private bool ApplyAnchoredPosition(RectTransform rectTransform, StateTreeContext args)
        {
            if (args.TryGetValue("anchored_pos", out object positionObj) || args.TryGetValue("anchored_position", out positionObj))
            {
                Vector2? position = ParseVector2(positionObj);
                if (position.HasValue && rectTransform.anchoredPosition != position.Value)
                {
                    rectTransform.anchoredPosition = position.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Apply size delta modification
        /// </summary>
        private bool ApplySizeDelta(RectTransform rectTransform, StateTreeContext args)
        {
            if (args.TryGetValue("size_delta", out object sizeObj))
            {
                Vector2? size = ParseVector2(sizeObj);
                if (size.HasValue && rectTransform.sizeDelta != size.Value)
                {
                    rectTransform.sizeDelta = size.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Apply minimum anchor modification
        /// </summary>
        private bool ApplyAnchorMin(RectTransform rectTransform, StateTreeContext args)
        {
            if (args.TryGetValue("anchor_min", out object anchorObj))
            {
                Vector2? anchor = ParseVector2(anchorObj);
                if (anchor.HasValue && rectTransform.anchorMin != anchor.Value)
                {
                    rectTransform.anchorMin = anchor.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Apply maximum anchor modification
        /// </summary>
        private bool ApplyAnchorMax(RectTransform rectTransform, StateTreeContext args)
        {
            if (args.TryGetValue("anchor_max", out object anchorObj))
            {
                Vector2? anchor = ParseVector2(anchorObj);
                if (anchor.HasValue && rectTransform.anchorMax != anchor.Value)
                {
                    rectTransform.anchorMax = anchor.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Apply pivot modification
        /// </summary>
        private bool ApplyPivot(RectTransform rectTransform, StateTreeContext args)
        {
            if (args.TryGetValue("pivot", out object pivotObj))
            {
                Vector2? pivot = ParseVector2(pivotObj);
                if (pivot.HasValue && rectTransform.pivot != pivot.Value)
                {
                    rectTransform.pivot = pivot.Value;
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// ApplySiblingIndexModify
        /// </summary>
        private bool ApplySetSiblingIndex(RectTransform rectTransform, StateTreeContext args)
        {
            if (args.TryGetValue("sibling_index", out object indexObj))
            {
                if (int.TryParse(indexObj?.ToString(), out int siblingIndex))
                {
                    int currentIndex = rectTransform.GetSiblingIndex();
                    if (currentIndex != siblingIndex)
                    {
                        rectTransform.SetSiblingIndex(siblingIndex);
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion




        #region Attribute operation method

        /// <summary>
        /// Set properties on a single target
        /// </summary>
        private object SetPropertyOnSingleTarget(GameObject targetGo, string propertyName, object valueObj)
        {
            RectTransform rectTransform = targetGo.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return Response.Error($"GameObject '{targetGo.name}' does not have a RectTransform component.");
            }

            try
            {
                Undo.RecordObject(rectTransform, $"Set RectTransform Property {propertyName}");

                if (valueObj is JsonNode valueToken)
                {
                    SetPropertyValue(rectTransform, propertyName, valueToken);
                }
                else
                {
                    JsonNode convertedToken = Json.FromObject(valueObj);
                    SetPropertyValue(rectTransform, propertyName, convertedToken);
                }

                EditorUtility.SetDirty(rectTransform);

                LogInfo($"[EditRectTransform] Set property '{propertyName}' on {targetGo.name}");

                return Response.Success(
                    $"RectTransform property '{propertyName}' set successfully on {targetGo.name}.",
                    new Dictionary<string, object>
                    {
                        { "target", targetGo.name },
                        { "property", propertyName },
                        { "value", valueObj?.ToString() }
                    }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set RectTransform property '{propertyName}': {e.Message}");
            }
        }

        /// <summary>
        /// Get properties from a single target
        /// </summary>
        private object GetPropertyFromSingleTarget(GameObject targetGo, string propertyName)
        {
            RectTransform rectTransform = targetGo.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return Response.Error($"GameObject '{targetGo.name}' does not have a RectTransform component.");
            }

            try
            {
                var value = GetPropertyValue(rectTransform, propertyName);
                LogInfo($"[EditRectTransform] Got property '{propertyName}' from {targetGo.name}: {value}");

                return Response.Success(
                    $"RectTransform property '{propertyName}' retrieved successfully from {targetGo.name}.",
                    new Dictionary<string, object>
                    {
                        { "target", targetGo.name },
                        { "property", propertyName },
                        { "value", value }
                    }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get RectTransform property '{propertyName}': {e.Message}");
            }
        }

        /// <summary>
        /// Get all of a single targetRectTransformAttribute
        /// </summary>
        private object GetAllRectTransformProperties(GameObject targetGo)
        {
            RectTransform rectTransform = targetGo.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return Response.Error($"GameObject '{targetGo.name}' does not have a RectTransform component.");
            }

            return Response.Success(
                $"RectTransform properties retrieved successfully from '{targetGo.name}'.",
                GetRectTransformData(rectTransform)
            );
        }

        /// <summary>
        /// Get all of multiple targetsRectTransformAttribute
        /// </summary>
        private object GetAllRectTransformPropertiesFromMultiple(GameObject[] targets)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject target in targets)
            {
                if (target == null) continue;

                try
                {
                    var result = GetAllRectTransformProperties(target);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                    }
                    else
                    {
                        errors.Add($"[{target.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{target.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("get all properties", successCount, targets.Length, results, errors);
        }

        #endregion

        #region Batch operation method

        /// <summary>
        /// Set properties on multiple targets
        /// </summary>
        private object SetPropertyOnMultipleTargets(GameObject[] targets, string propertyName, object valueObj)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject target in targets)
            {
                if (target == null) continue;

                try
                {
                    var result = SetPropertyOnSingleTarget(target, propertyName, valueObj);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                    }
                    else
                    {
                        errors.Add($"[{target.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{target.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse($"set property '{propertyName}'", successCount, targets.Length, results, errors);
        }

        /// <summary>
        /// Get properties from multiple targets
        /// </summary>
        private object GetPropertyFromMultipleTargets(GameObject[] targets, string propertyName)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject target in targets)
            {
                if (target == null) continue;

                try
                {
                    var result = GetPropertyFromSingleTarget(target, propertyName);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                    }
                    else
                    {
                        errors.Add($"[{target.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{target.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse($"get property '{propertyName}'", successCount, targets.Length, results, errors);
        }

        #endregion

        #region Helper method

        /// <summary>
        /// Extract target from execution contextGameObjectArray
        /// </summary>
        private GameObject[] ExtractTargetsFromContext(StateTreeContext context)
        {
            // Try from firstObjectReferencesGet（Avoid serialization issues）
            if (context.TryGetObjectReference("_resolved_targets", out object targetsObj))
            {
                if (targetsObj is GameObject[] gameObjectArray)
                {
                    return gameObjectArray;
                }
                else if (targetsObj is GameObject singleGameObject)
                {
                    return new GameObject[] { singleGameObject };
                }
                else if (targetsObj is System.Collections.IList list)
                {
                    var gameObjects = new List<GameObject>();
                    foreach (var item in list)
                    {
                        if (item is GameObject go)
                            gameObjects.Add(go);
                    }
                    return gameObjects.ToArray();
                }
            }

            // IfObjectReferencesNot in，Try fromJsonDataGet（Backward compatible）
            if (context.TryGetJsonValue("_resolved_targets", out JsonNode targetToken))
            {
                if (targetToken is JsonArray targetArray)
                {
                    // JsonArray Cannot convert directly to GameObject[]，Need to parse one by one
                    // This feature may need to be implemented by other means
                    return new GameObject[0];
                }
                else
                {
                    // Single object case - JsonNode Cannot directly convert to GameObject
                    // Need to obtain via selector or other methods
                    return new GameObject[0];
                }
            }

            return new GameObject[0];
        }

        /// <summary>
        /// Check whether a batch operation should be performed
        /// </summary>
        private bool ShouldSelectMany(StateTreeContext context)
        {
            if (context.TryGetValue("select_many", out object selectManyObj))
            {
                if (selectManyObj is bool selectMany)
                    return selectMany;
                if (bool.TryParse(selectManyObj?.ToString(), out bool parsedSelectMany))
                    return parsedSelectMany;
            }
            return false; // Default isfalse
        }

        /// <summary>
        /// According toselect_manyParameter to get target object（Single or multiple）
        /// </summary>
        private GameObject[] GetTargetsBasedOnSelectMany(StateTreeContext context)
        {
            GameObject[] targets = ExtractTargetsFromContext(context);

            if (ShouldSelectMany(context))
            {
                return targets; // Return all matching objects
            }
            else
            {
                // Only return the first object（If exists）
                return targets.Length > 0 ? new GameObject[] { targets[0] } : new GameObject[0];
            }
        }

        /// <summary>
        /// ParseVector2，Support multiple formats：JsonArray、Vector2、String
        /// Supported format: [1, 2], ["1", "2"], "[1, 2]", "(1, 2)", "1, 2"
        /// </summary>
        private Vector2? ParseVector2(object obj)
        {
            if (obj == null) return null;

            // Process JsonArray Type
            if (obj is JsonArray JsonArray && JsonArray.Count >= 2)
            {
                try
                {
                    // Try directly asfloatParse
                    float x = ParseFloatValue(JsonArray[0]);
                    float y = ParseFloatValue(JsonArray[1]);
                    return new Vector2(x, y);
                }
                catch
                {
                    return null;
                }
            }

            // Process Vector2 Type
            if (obj is Vector2 vector2)
            {
                return vector2;
            }

            // Handle string format
            if (obj is string str)
            {
                float[] values = ParseNumberArrayFromString(str, 2);
                if (values != null && values.Length == 2)
                {
                    return new Vector2(values[0], values[1]);
                }
            }

            // Process JsonNode Type
            if (obj is JsonNode node && node.type == JsonNodeType.String)
            {
                float[] values = ParseNumberArrayFromString(node.Value, 2);
                if (values != null && values.Length == 2)
                {
                    return new Vector2(values[0], values[1]);
                }
            }

            return null;
        }

        /// <summary>
        /// ParseVector3，Support multiple formats：JsonArray、Vector3、String
        /// Supported format: [1, 2, 3], ["1", "2", "3"], "[1, 2, 3]", "(1, 2, 3)", "1, 2, 3"
        /// </summary>
        private Vector3? ParseVector3(object obj)
        {
            if (obj == null) return null;

            // Process JsonArray Type
            if (obj is JsonArray JsonArray && JsonArray.Count >= 3)
            {
                try
                {
                    // Try directly asfloatParse
                    float x = ParseFloatValue(JsonArray[0]);
                    float y = ParseFloatValue(JsonArray[1]);
                    float z = ParseFloatValue(JsonArray[2]);
                    return new Vector3(x, y, z);
                }
                catch
                {
                    return null;
                }
            }

            // Process Vector3 Type
            if (obj is Vector3 vector3)
            {
                return vector3;
            }

            // Handle string format
            if (obj is string str)
            {
                float[] values = ParseNumberArrayFromString(str, 3);
                if (values != null && values.Length == 3)
                {
                    return new Vector3(values[0], values[1], values[2]);
                }
            }

            // Process JsonNode Type
            if (obj is JsonNode node && node.type == JsonNodeType.String)
            {
                float[] values = ParseNumberArrayFromString(node.Value, 3);
                if (values != null && values.Length == 3)
                {
                    return new Vector3(values[0], values[1], values[2]);
                }
            }

            return null;
        }

        /// <summary>
        /// Get attribute value
        /// </summary>
        private object GetPropertyValue(object target, string propertyName)
        {
            Type type = target.GetType();
            PropertyInfo propInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propInfo != null && propInfo.CanRead)
            {
                return propInfo.GetValue(target);
            }

            FieldInfo fieldInfo = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(target);
            }

            throw new ArgumentException($"Property or field '{propertyName}' not found on type '{type.Name}'");
        }

        /// <summary>
        /// Set attribute value
        /// </summary>
        private void SetPropertyValue(object target, string propertyName, JsonNode value)
        {
            Type type = target.GetType();
            PropertyInfo propInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propInfo != null && propInfo.CanWrite)
            {
                object convertedValue = ConvertValue(value, propInfo.PropertyType);
                propInfo.SetValue(target, convertedValue);
                return;
            }

            FieldInfo fieldInfo = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                object convertedValue = ConvertValue(value, fieldInfo.FieldType);
                fieldInfo.SetValue(target, convertedValue);
                return;
            }

            throw new ArgumentException($"Property or field '{propertyName}' not found or is read-only on type '{type.Name}'");
        }

        /// <summary>
        /// ConvertJTokenValue to specified type
        /// </summary>
        private object ConvertValue(JsonNode token, Type targetType)
        {
            if (token == null || token.IsNull())
            {
                throw new ArgumentNullException(nameof(token), $"Unable to convert null Convert to type {targetType.Name}");
            }

            // Base type
            if (targetType == typeof(string))
                return token.Value;
            if (targetType == typeof(int))
                return token.AsInt;
            if (targetType == typeof(long))
                return (long)token.AsInt;
            if (targetType == typeof(short))
                return (short)token.AsInt;
            if (targetType == typeof(byte))
                return (byte)token.AsInt;
            if (targetType == typeof(float))
                return token.AsFloat;
            if (targetType == typeof(double))
                return token.AsDouble;
            if (targetType == typeof(bool))
                return token.AsBool;

            // Unity Vector type
            if (targetType == typeof(Vector2))
            {
                if (token is JsonArray arr2 && arr2.Count >= 2)
                    return new Vector2(arr2[0].AsFloat, arr2[1].AsFloat);
                throw new InvalidCastException($"Unable to convert JsonNode Convert to Vector2，Must contain at least2Array of elements");
            }
            if (targetType == typeof(Vector3))
            {
                if (token is JsonArray arr3 && arr3.Count >= 3)
                    return new Vector3(arr3[0].AsFloat, arr3[1].AsFloat, arr3[2].AsFloat);
                throw new InvalidCastException($"Unable to convert JsonNode Convert to Vector3，Must contain at least3Array of elements");
            }
            if (targetType == typeof(Vector4))
            {
                if (token is JsonArray arr4 && arr4.Count >= 4)
                    return new Vector4(arr4[0].AsFloat, arr4[1].AsFloat, arr4[2].AsFloat, arr4[3].AsFloat);
                throw new InvalidCastException($"Unable to convert JsonNode Convert to Vector4，Must contain at least4Array of elements");
            }
            if (targetType == typeof(Color))
            {
                var color = token.ToColor();
                if (color.HasValue)
                    return color.Value;
                throw new InvalidCastException($"Unable to convert JsonNode Convert to Color，Need to contain r,g,b,a Object or at least3Array of elements");
            }

            // Enum type
            if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, token.Value, true);
                }
                catch
                {
                    throw new InvalidCastException($"Cannot convert value '{token.Value}' Convert to enum type {targetType.Name}");
                }
            }

            // For other types，Throw exception
            throw new NotSupportedException($"Does not support converting JsonNode Convert to type {targetType.Name}。Current value：{token}");
        }

        /// <summary>
        /// CheckResponseWhether the object indicates success
        /// </summary>
        private bool IsSuccessResponse(object response, out object data, out string message)
        {
            data = null;
            message = null;

            var resultType = response.GetType();
            var successProperty = resultType.GetProperty("success");
            var dataProperty = resultType.GetProperty("data");
            var messageProperty = resultType.GetProperty("message");
            var errorProperty = resultType.GetProperty("error");

            bool isSuccess = successProperty != null && (bool)successProperty.GetValue(response);
            data = dataProperty?.GetValue(response);
            message = isSuccess ?
                messageProperty?.GetValue(response)?.ToString() :
                (errorProperty?.GetValue(response)?.ToString() ?? messageProperty?.GetValue(response)?.ToString());

            return isSuccess;
        }

        /// <summary>
        /// Create batch operation response
        /// </summary>
        private object CreateBatchOperationResponse(string operation, int successCount, int totalCount,
            List<Dictionary<string, object>> results, List<string> errors)
        {
            string message;
            if (successCount == totalCount)
            {
                message = $"Successfully completed {operation} on {successCount} RectTransform(s).";
            }
            else if (successCount > 0)
            {
                message = $"Completed {operation} on {successCount} of {totalCount} RectTransform(s). {errors.Count} failed.";
            }
            else
            {
                message = $"Failed to complete {operation} on any of the {totalCount} RectTransform(s).";
            }

            var responseData = new Dictionary<string, object>
            {
                { "operation", operation },
                { "success_count", successCount },
                { "total_count", totalCount },
                { "success_rate", (double)successCount / totalCount },
                { "affected_objects", results }
            };

            if (errors.Count > 0)
            {
                responseData["errors"] = errors;
            }

            if (successCount > 0)
            {
                return Response.Success(message, responseData);
            }
            else
            {
                return Response.Error(message, responseData);
            }
        }

        /// <summary>
        /// GetRectTransformData representation of（UseYAMLFormat savingtoken）
        /// </summary>
        private Dictionary<string, object> GetRectTransformData(RectTransform rectTransform)
        {
            if (rectTransform == null) return null;

            // UseYAMLCompact representation of the format
            var yamlData = GetRectTransformDataYaml(rectTransform);
            return new Dictionary<string, object>
            {
                { "yaml", yamlData }
            };
        }

        /// <summary>
        /// CreateRectTransformOfYAMLFormat data representation（Savetoken）
        /// </summary>
        private string GetRectTransformDataYaml(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return "null";

            var yaml = $@"name: {rectTransform.name}
id: {rectTransform.GetInstanceID()}
anchoredPos: [{rectTransform.anchoredPosition.x:F1}, {rectTransform.anchoredPosition.y:F1}]
sizeDelta: [{rectTransform.sizeDelta.x:F1}, {rectTransform.sizeDelta.y:F1}]
anchorMin: [{rectTransform.anchorMin.x:F3}, {rectTransform.anchorMin.y:F3}]
anchorMax: [{rectTransform.anchorMax.x:F3}, {rectTransform.anchorMax.y:F3}]
pivot: [{rectTransform.pivot.x:F3}, {rectTransform.pivot.y:F3}]
offsetMin: [{rectTransform.offsetMin.x:F1}, {rectTransform.offsetMin.y:F1}]
offsetMax: [{rectTransform.offsetMax.x:F1}, {rectTransform.offsetMax.y:F1}]
localPos: [{rectTransform.localPosition.x:F1}, {rectTransform.localPosition.y:F1}, {rectTransform.localPosition.z:F1}]
localScale: [{rectTransform.localScale.x:F2}, {rectTransform.localScale.y:F2}, {rectTransform.localScale.z:F2}]
rect: [x:{rectTransform.rect.x:F1}, y:{rectTransform.rect.y:F1}, w:{rectTransform.rect.width:F1}, h:{rectTransform.rect.height:F1}]";

            return yaml;
        }

        /// <summary>
        /// From JsonNode Parse float Value，Support numbers and strings
        /// </summary>
        private float ParseFloatValue(JsonNode node)
        {
            if (node == null)
                return 0f;

            // Handle numeric type
            if (node.type == JsonNodeType.Integer || node.type == JsonNodeType.Float)
            {
                return node.AsFloat;
            }

            // Handle string type
            if (node.type == JsonNodeType.String)
            {
                if (float.TryParse(node.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
            }

            return 0f;
        }

        /// <summary>
        /// Parse numeric array from string，Support multiple formats：
        /// "[0.1, 0.2, 0.3]", "(0.1, 0.2, 0.3)", "0.1, 0.2, 0.3"
        /// </summary>
        /// <param name="str">Input string</param>
        /// <param name="expectedCount">Expected number of digits</param>
        /// <returns>Parsed float Array，Return on failure null</returns>
        private float[] ParseNumberArrayFromString(string str, int expectedCount)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;

            try
            {
                // Trim leading and trailing spaces
                str = str.Trim();

                // Remove outer parentheses（Supports square and round brackets）
                if ((str.StartsWith("[") && str.EndsWith("]")) ||
                    (str.StartsWith("(") && str.EndsWith(")")))
                {
                    str = str.Substring(1, str.Length - 2);
                }

                // Split by comma
                string[] parts = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Check quantity
                if (parts.Length != expectedCount)
                {
                    Debug.LogWarning($"[ParseNumberArrayFromString] Expected {expectedCount} values, but got {parts.Length} in string: '{str}'");
                    return null;
                }

                // Parse each number
                float[] result = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result[i]))
                    {
                        Debug.LogWarning($"[ParseNumberArrayFromString] Failed to parse '{parts[i].Trim()}' as float in string: '{str}'");
                        return null;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ParseNumberArrayFromString] Failed to parse string '{str}': {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}

