using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// 专门的粒子系统编辑工具，提供粒子系统的创建、修改、控制等操作
    /// 对应方法名: edit_particle_system
    /// </summary>
    [ToolName("edit_particle_system", "资源管理")]
    public class EditParticleSystem : DualStateMethodBase
    {
        private IObjectSelector objectSelector;

        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 目标查找参数
                new MethodKey("instance_id", "GameObject的实例ID", false),
                new MethodKey("path", "GameObject的层次结构路径", false),
                
                // 操作类型
                new MethodKey("action", "操作类型：init_component, get_properties, set_properties, play, pause, stop, clear, simulate, restart", false),
                
                // 主模块属性
                new MethodKey("duration", "粒子系统持续时间"),
                new MethodKey("looping", "是否循环播放"),
                new MethodKey("prewarm", "是否预热"),
                new MethodKey("start_delay", "开始延迟"),
                new MethodKey("start_lifetime", "粒子生命周期"),
                new MethodKey("start_speed", "粒子初始速度"),
                new MethodKey("start_size", "粒子初始大小"),
                new MethodKey("start_rotation", "粒子初始旋转"),
                new MethodKey("start_color", "粒子初始颜色 [r,g,b,a]"),
                new MethodKey("gravity_modifier", "重力修正系数"),
                new MethodKey("simulation_space", "模拟空间：Local, World, Custom"),
                new MethodKey("simulation_speed", "模拟速度"),
                new MethodKey("scaling_mode", "缩放模式：Hierarchy, Local, Shape"),
                new MethodKey("play_on_awake", "唤醒时播放"),
                new MethodKey("max_particles", "最大粒子数"),
                
                // 发射模块
                new MethodKey("emission_enabled", "是否启用发射"),
                new MethodKey("emission_rate_over_time", "每秒发射率"),
                new MethodKey("emission_rate_over_distance", "每单位距离发射率"),
                
                // 形状模块
                new MethodKey("shape_enabled", "是否启用形状模块"),
                new MethodKey("shape_type", "形状类型：Sphere, Hemisphere, Cone, Box, Circle, Edge, Rectangle"),
                new MethodKey("shape_angle", "锥体角度"),
                new MethodKey("shape_radius", "半径"),
                new MethodKey("shape_box_thickness", "盒子厚度 [x,y,z]"),
                new MethodKey("shape_arc", "圆弧角度"),
                new MethodKey("shape_random_direction", "随机方向"),
                
                // 速度模块
                new MethodKey("velocity_over_lifetime_enabled", "是否启用生命周期速度"),
                new MethodKey("velocity_linear", "线性速度 [x,y,z]"),
                new MethodKey("velocity_orbital", "轨道速度 [x,y,z]"),
                
                // 限制速度模块
                new MethodKey("limit_velocity_enabled", "是否启用速度限制"),
                new MethodKey("limit_velocity_dampen", "速度衰减"),
                
                // 力场模块
                new MethodKey("force_over_lifetime_enabled", "是否启用生命周期力"),
                new MethodKey("force_x", "X轴力"),
                new MethodKey("force_y", "Y轴力"),
                new MethodKey("force_z", "Z轴力"),
                
                // 颜色模块
                new MethodKey("color_over_lifetime_enabled", "是否启用生命周期颜色"),
                new MethodKey("color_gradient", "颜色渐变配置"),
                
                // 大小模块
                new MethodKey("size_over_lifetime_enabled", "是否启用生命周期大小"),
                new MethodKey("size_curve", "大小曲线配置"),
                
                // 旋转模块
                new MethodKey("rotation_over_lifetime_enabled", "是否启用生命周期旋转"),
                new MethodKey("rotation_angular_velocity", "角速度"),
                
                // 噪声模块
                new MethodKey("noise_enabled", "是否启用噪声"),
                new MethodKey("noise_strength", "噪声强度"),
                new MethodKey("noise_frequency", "噪声频率"),
                
                // 碰撞模块
                new MethodKey("collision_enabled", "是否启用碰撞"),
                new MethodKey("collision_type", "碰撞类型：Planes, World"),
                new MethodKey("collision_dampen", "碰撞衰减"),
                new MethodKey("collision_bounce", "碰撞反弹"),
                
                // 渲染模块
                new MethodKey("render_mode", "渲染模式：Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh"),
                new MethodKey("material", "材质路径"),
                new MethodKey("trail_material", "拖尾材质路径"),
                new MethodKey("sorting_layer", "排序层"),
                new MethodKey("sorting_order", "排序顺序"),
                
                // 纹理表动画模块
                new MethodKey("texture_sheet_animation_enabled", "是否启用纹理表动画"),
                new MethodKey("texture_sheet_tiles", "纹理表分块 [x,y]"),
                new MethodKey("texture_sheet_animation_type", "动画类型：WholeSheet, SingleRow"),
                new MethodKey("texture_sheet_fps", "动画帧率"),
                
                // 子发射器模块
                new MethodKey("sub_emitters_enabled", "是否启用子发射器"),
                
                // 灯光模块
                new MethodKey("lights_enabled", "是否启用灯光"),
                new MethodKey("lights_ratio", "灯光比率"),
                
                // 拖尾模块
                new MethodKey("trails_enabled", "是否启用拖尾"),
                new MethodKey("trails_ratio", "拖尾比率"),
                new MethodKey("trails_lifetime", "拖尾生命周期"),
                
                // 播放控制
                new MethodKey("simulate_time", "模拟时间（秒）"),
                new MethodKey("with_children", "是否包含子粒子系统"),
                new MethodKey("restart_mode", "重启模式：Default, Fast")
            };
        }

        /// <summary>
        /// 创建目标定位状态树
        /// </summary>
        protected override StateTree CreateTargetTree()
        {
            objectSelector = objectSelector ?? new HierarchySelector<GameObject>();
            return objectSelector.BuildStateTree();
        }

        /// <summary>
        /// 创建粒子系统操作执行状态树
        /// </summary>
        protected override StateTree CreateActionTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("init_component", (Func<StateTreeContext, object>)HandleInitComponentAction)
                    .Leaf("get_properties", (Func<StateTreeContext, object>)HandleGetPropertiesAction)
                    .Leaf("set_properties", (Func<StateTreeContext, object>)HandleSetPropertiesAction)
                    .Leaf("play", (Func<StateTreeContext, object>)HandlePlayAction)
                    .Leaf("pause", (Func<StateTreeContext, object>)HandlePauseAction)
                    .Leaf("stop", (Func<StateTreeContext, object>)HandleStopAction)
                    .Leaf("clear", (Func<StateTreeContext, object>)HandleClearAction)
                    .Leaf("simulate", (Func<StateTreeContext, object>)HandleSimulateAction)
                    .Leaf("restart", (Func<StateTreeContext, object>)HandleRestartAction)
                    .DefaultLeaf((Func<StateTreeContext, object>)HandleDefaultAction)
                .Build();
        }

        // --- 辅助方法 ---

        private GameObject ExtractTargetFromContext(StateTreeContext context)
        {
            if (context.TryGetObjectReference("_resolved_targets", out object targetsObj))
            {
                if (targetsObj is GameObject singleGameObject)
                {
                    return singleGameObject;
                }
                else if (targetsObj is GameObject[] gameObjectArray && gameObjectArray.Length > 0)
                {
                    return gameObjectArray[0];
                }
                else if (targetsObj is System.Collections.IList list && list.Count > 0)
                {
                    if (list[0] is GameObject go)
                        return go;
                }
            }

            if (context.TryGetJsonValue("_resolved_targets", out JsonNode targetToken))
            {
                if (targetToken is JsonArray arr && arr.Count > 0)
                {
                    int instanceId = arr[0].AsInt;
                    return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                }
                else if (targetToken.type == JsonNodeType.Integer)
                {
                    int instanceId = targetToken.AsInt;
                    return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                }
            }

            return null;
        }

        // --- 操作方法 ---

        private object HandleDefaultAction(StateTreeContext context)
        {
            JsonClass args = context.JsonData;
            if (args.ContainsKey("duration") || args.ContainsKey("start_lifetime") ||
                args.ContainsKey("emission_rate_over_time") || args.ContainsKey("start_color"))
            {
                return HandleSetPropertiesAction(context);
            }
            return Response.Error("Action is required. Valid actions: init_component, get_properties, set_properties, play, pause, stop, clear, simulate, restart");
        }

        private object HandleInitComponentAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            try
            {
                ParticleSystem ps = target.GetComponent<ParticleSystem>();
                bool isNewComponent = false;

                // 如果不存在粒子系统组件，则添加
                if (ps == null)
                {
                    ps = Undo.AddComponent<ParticleSystem>(target);
                    isNewComponent = true;
                    McpLogger.Log($"[EditParticleSystem] Added ParticleSystem component to '{target.name}'");
                }
                else
                {
                    Undo.RecordObject(ps, "Initialize ParticleSystem");
                    McpLogger.Log($"[EditParticleSystem] Found existing ParticleSystem on '{target.name}', initializing properties");
                }

                // 应用初始属性
                JsonClass args = context.JsonData;
                if (args.Count > 0)
                {
                    ApplyParticleSystemProperties(ps, args);
                }

                string message = isNewComponent
                    ? $"ParticleSystem added and initialized on '{target.name}'."
                    : $"ParticleSystem initialized on '{target.name}'.";

                return Response.Success(message, GetParticleSystemData(ps));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to initialize ParticleSystem: {e.Message}");
            }
        }

        private object HandleGetPropertiesAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            return Response.Success($"ParticleSystem properties retrieved from '{target.name}'.", GetParticleSystemData(ps));
        }

        private object HandleSetPropertiesAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            try
            {
                Undo.RecordObject(ps, "Set ParticleSystem Properties");
                JsonClass args = context.JsonData;
                ApplyParticleSystemProperties(ps, args);

                McpLogger.Log($"[EditParticleSystem] Set properties on '{target.name}'");
                return Response.Success($"ParticleSystem properties updated on '{target.name}'.", GetParticleSystemData(ps));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set ParticleSystem properties: {e.Message}");
            }
        }

        private object HandlePlayAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            JsonClass args = context.JsonData;
            bool withChildren = args["with_children"].AsBoolDefault(true);

            ps.Play(withChildren);
            McpLogger.Log($"[EditParticleSystem] Playing ParticleSystem on '{target.name}'");

            return Response.Success($"ParticleSystem playing on '{target.name}'.", new JsonClass
            {
                ["isPlaying"] = ps.isPlaying,
                ["isPaused"] = ps.isPaused,
                ["isStopped"] = ps.isStopped
            });
        }

        private object HandlePauseAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            JsonClass args = context.JsonData;
            bool withChildren = args["with_children"].AsBoolDefault(true);

            ps.Pause(withChildren);
            McpLogger.Log($"[EditParticleSystem] Paused ParticleSystem on '{target.name}'");

            return Response.Success($"ParticleSystem paused on '{target.name}'.", new JsonClass
            {
                ["isPlaying"] = ps.isPlaying,
                ["isPaused"] = ps.isPaused
            });
        }

        private object HandleStopAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            JsonClass args = context.JsonData;
            bool withChildren = args["with_children"].AsBoolDefault(true);

            ps.Stop(withChildren);
            McpLogger.Log($"[EditParticleSystem] Stopped ParticleSystem on '{target.name}'");

            return Response.Success($"ParticleSystem stopped on '{target.name}'.", new JsonClass
            {
                ["isStopped"] = ps.isStopped
            });
        }

        private object HandleClearAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            JsonClass args = context.JsonData;
            bool withChildren = args["with_children"].AsBoolDefault(true);

            ps.Clear(withChildren);
            McpLogger.Log($"[EditParticleSystem] Cleared ParticleSystem on '{target.name}'");

            return Response.Success($"ParticleSystem cleared on '{target.name}'.");
        }

        private object HandleSimulateAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            JsonClass args = context.JsonData;
            float time = args["simulate_time"].AsFloatDefault(1.0f);
            bool withChildren = args["with_children"].AsBoolDefault(true);

            ps.Simulate(time, withChildren, true);
            McpLogger.Log($"[EditParticleSystem] Simulated {time}s on '{target.name}'");

            return Response.Success($"ParticleSystem simulated {time}s on '{target.name}'.", new JsonClass
            {
                ["time"] = ps.time,
                ["particleCount"] = ps.particleCount
            });
        }

        private object HandleRestartAction(StateTreeContext context)
        {
            GameObject target = ExtractTargetFromContext(context);
            if (target == null)
                return Response.Error("Target GameObject not found.");

            ParticleSystem ps = target.GetComponent<ParticleSystem>();
            if (ps == null)
                return Response.Error($"No ParticleSystem found on '{target.name}'.");

            JsonClass args = context.JsonData;
            bool withChildren = args["with_children"].AsBoolDefault(true);

            ps.Stop(withChildren);
            ps.Clear(withChildren);
            ps.Play(withChildren);

            McpLogger.Log($"[EditParticleSystem] Restarted ParticleSystem on '{target.name}'");
            return Response.Success($"ParticleSystem restarted on '{target.name}'.");
        }

        // --- 属性应用和获取方法 ---

        private void ApplyParticleSystemProperties(ParticleSystem ps, JsonClass args)
        {
            var main = ps.main;

            // 主模块属性
            if (args.TryGetValue("duration", out JsonNode durationToken))
                main.duration = durationToken.AsFloat;

            if (args.TryGetValue("looping", out JsonNode loopingToken))
                main.loop = loopingToken.AsBool;

            if (args.TryGetValue("prewarm", out JsonNode prewarmToken))
                main.prewarm = prewarmToken.AsBool;

            if (args.TryGetValue("start_delay", out JsonNode delayToken))
                main.startDelay = delayToken.AsFloat;

            if (args.TryGetValue("start_lifetime", out JsonNode lifetimeToken))
                main.startLifetime = lifetimeToken.AsFloat;

            if (args.TryGetValue("start_speed", out JsonNode speedToken))
                main.startSpeed = speedToken.AsFloat;

            if (args.TryGetValue("start_size", out JsonNode sizeToken))
                main.startSize = sizeToken.AsFloat;

            if (args.TryGetValue("start_rotation", out JsonNode rotationToken))
                main.startRotation = rotationToken.AsFloat * Mathf.Deg2Rad;

            if (args.TryGetValue("start_color", out JsonNode colorToken))
            {
                JsonArray colorJsonArray = colorToken as JsonArray;
                if (colorJsonArray != null && colorJsonArray.Count >= 3)
                {
                    main.startColor = new Color(
                        colorJsonArray[0].AsFloat,
                        colorJsonArray[1].AsFloat,
                        colorJsonArray[2].AsFloat,
                        colorJsonArray.Count > 3 ? colorJsonArray[3].AsFloat : 1.0f
                    );
                }
            }

            if (args.TryGetValue("gravity_modifier", out JsonNode gravityToken))
                main.gravityModifier = gravityToken.AsFloat;

            if (args.TryGetValue("simulation_space", out JsonNode simSpaceToken))
            {
                if (Enum.TryParse(simSpaceToken.Value, out ParticleSystemSimulationSpace simSpace))
                    main.simulationSpace = simSpace;
            }

            if (args.TryGetValue("simulation_speed", out JsonNode simSpeedToken))
                main.simulationSpeed = simSpeedToken.AsFloat;

            if (args.TryGetValue("scaling_mode", out JsonNode scalingToken))
            {
                if (Enum.TryParse(scalingToken.Value, out ParticleSystemScalingMode scalingMode))
                    main.scalingMode = scalingMode;
            }

            if (args.TryGetValue("play_on_awake", out JsonNode playOnAwakeToken))
                main.playOnAwake = playOnAwakeToken.AsBool;

            if (args.TryGetValue("max_particles", out JsonNode maxParticlesToken))
                main.maxParticles = maxParticlesToken.AsInt;

            // 发射模块
            if (args.ContainsKey("emission_enabled") || args.ContainsKey("emission_rate_over_time") ||
                args.ContainsKey("emission_rate_over_distance"))
            {
                var emission = ps.emission;

                if (args.TryGetValue("emission_enabled", out JsonNode emissionEnabledToken))
                    emission.enabled = emissionEnabledToken.AsBool;

                if (args.TryGetValue("emission_rate_over_time", out JsonNode rateTimeToken))
                    emission.rateOverTime = rateTimeToken.AsFloat;

                if (args.TryGetValue("emission_rate_over_distance", out JsonNode rateDistToken))
                    emission.rateOverDistance = rateDistToken.AsFloat;
            }

            // 形状模块
            if (args.ContainsKey("shape_enabled") || args.ContainsKey("shape_type") ||
                args.ContainsKey("shape_radius") || args.ContainsKey("shape_angle"))
            {
                var shape = ps.shape;

                if (args.TryGetValue("shape_enabled", out JsonNode shapeEnabledToken))
                    shape.enabled = shapeEnabledToken.AsBool;

                if (args.TryGetValue("shape_type", out JsonNode shapeTypeToken))
                {
                    if (Enum.TryParse(shapeTypeToken.Value, out ParticleSystemShapeType shapeType))
                        shape.shapeType = shapeType;
                }

                if (args.TryGetValue("shape_angle", out JsonNode angleToken))
                    shape.angle = angleToken.AsFloat;

                if (args.TryGetValue("shape_radius", out JsonNode radiusToken))
                    shape.radius = radiusToken.AsFloat;

                if (args.TryGetValue("shape_arc", out JsonNode arcToken))
                    shape.arc = arcToken.AsFloat;

                if (args.TryGetValue("shape_random_direction", out JsonNode randomDirToken))
                    shape.randomDirectionAmount = randomDirToken.AsFloat;
            }

            // 速度模块
            if (args.ContainsKey("velocity_over_lifetime_enabled") || args.ContainsKey("velocity_linear"))
            {
                var velocity = ps.velocityOverLifetime;

                if (args.TryGetValue("velocity_over_lifetime_enabled", out JsonNode velEnabledToken))
                    velocity.enabled = velEnabledToken.AsBool;

                if (args.TryGetValue("velocity_linear", out JsonNode linearToken))
                {
                    JsonArray linearArray = linearToken as JsonArray;
                    if (linearArray != null && linearArray.Count >= 3)
                    {
                        velocity.x = linearArray[0].AsFloat;
                        velocity.y = linearArray[1].AsFloat;
                        velocity.z = linearArray[2].AsFloat;
                    }
                }

                if (args.TryGetValue("velocity_orbital", out JsonNode orbitalToken))
                {
                    JsonArray orbitalArray = orbitalToken as JsonArray;
                    if (orbitalArray != null && orbitalArray.Count >= 3)
                    {
                        velocity.orbitalX = orbitalArray[0].AsFloat;
                        velocity.orbitalY = orbitalArray[1].AsFloat;
                        velocity.orbitalZ = orbitalArray[2].AsFloat;
                    }
                }
            }

            // 颜色模块
            if (args.ContainsKey("color_over_lifetime_enabled"))
            {
                var colorOverLifetime = ps.colorOverLifetime;

                if (args.TryGetValue("color_over_lifetime_enabled", out JsonNode colorEnabledToken))
                    colorOverLifetime.enabled = colorEnabledToken.AsBool;
            }

            // 大小模块
            if (args.ContainsKey("size_over_lifetime_enabled"))
            {
                var sizeOverLifetime = ps.sizeOverLifetime;

                if (args.TryGetValue("size_over_lifetime_enabled", out JsonNode sizeEnabledToken))
                    sizeOverLifetime.enabled = sizeEnabledToken.AsBool;
            }

            // 旋转模块
            if (args.ContainsKey("rotation_over_lifetime_enabled") || args.ContainsKey("rotation_angular_velocity"))
            {
                var rotationOverLifetime = ps.rotationOverLifetime;

                if (args.TryGetValue("rotation_over_lifetime_enabled", out JsonNode rotEnabledToken))
                    rotationOverLifetime.enabled = rotEnabledToken.AsBool;

                if (args.TryGetValue("rotation_angular_velocity", out JsonNode angVelToken))
                    rotationOverLifetime.z = angVelToken.AsFloat * Mathf.Deg2Rad;
            }

            // 碰撞模块
            if (args.ContainsKey("collision_enabled") || args.ContainsKey("collision_type"))
            {
                var collision = ps.collision;

                if (args.TryGetValue("collision_enabled", out JsonNode collisionEnabledToken))
                    collision.enabled = collisionEnabledToken.AsBool;

                if (args.TryGetValue("collision_type", out JsonNode collisionTypeToken))
                {
                    if (Enum.TryParse(collisionTypeToken.Value, out ParticleSystemCollisionType collisionType))
                        collision.type = collisionType;
                }

                if (args.TryGetValue("collision_dampen", out JsonNode dampenToken))
                    collision.dampen = dampenToken.AsFloat;

                if (args.TryGetValue("collision_bounce", out JsonNode bounceToken))
                    collision.bounce = bounceToken.AsFloat;
            }

            // 噪声模块
            if (args.ContainsKey("noise_enabled") || args.ContainsKey("noise_strength"))
            {
                var noise = ps.noise;

                if (args.TryGetValue("noise_enabled", out JsonNode noiseEnabledToken))
                    noise.enabled = noiseEnabledToken.AsBool;

                if (args.TryGetValue("noise_strength", out JsonNode strengthToken))
                    noise.strength = strengthToken.AsFloat;

                if (args.TryGetValue("noise_frequency", out JsonNode freqToken))
                    noise.frequency = freqToken.AsFloat;
            }

            // 渲染器属性
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                if (args.TryGetValue("render_mode", out JsonNode renderModeToken))
                {
                    if (Enum.TryParse(renderModeToken.Value, out ParticleSystemRenderMode renderMode))
                        renderer.renderMode = renderMode;
                }

                if (args.TryGetValue("material", out JsonNode materialToken))
                {
                    string materialPath = materialToken.Value;
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (mat != null)
                        renderer.material = mat;
                }

                if (args.TryGetValue("sorting_layer", out JsonNode sortingLayerToken))
                    renderer.sortingLayerName = sortingLayerToken.Value;

                if (args.TryGetValue("sorting_order", out JsonNode sortingOrderToken))
                    renderer.sortingOrder = sortingOrderToken.AsInt;
            }

            // 纹理表动画
            if (args.ContainsKey("texture_sheet_animation_enabled") || args.ContainsKey("texture_sheet_tiles"))
            {
                var textureSheet = ps.textureSheetAnimation;

                if (args.TryGetValue("texture_sheet_animation_enabled", out JsonNode texSheetEnabledToken))
                    textureSheet.enabled = texSheetEnabledToken.AsBool;

                if (args.TryGetValue("texture_sheet_tiles", out JsonNode tilesToken))
                {
                    JsonArray tilesArray = tilesToken as JsonArray;
                    if (tilesArray != null && tilesArray.Count >= 2)
                    {
                        textureSheet.numTilesX = tilesArray[0].AsInt;
                        textureSheet.numTilesY = tilesArray[1].AsInt;
                    }
                }

                if (args.TryGetValue("texture_sheet_fps", out JsonNode fpsToken))
                    textureSheet.fps = fpsToken.AsFloat;
            }

            // 拖尾模块
            if (args.ContainsKey("trails_enabled") || args.ContainsKey("trails_ratio"))
            {
                var trails = ps.trails;

                if (args.TryGetValue("trails_enabled", out JsonNode trailsEnabledToken))
                    trails.enabled = trailsEnabledToken.AsBool;

                if (args.TryGetValue("trails_ratio", out JsonNode ratioToken))
                    trails.ratio = ratioToken.AsFloat;

                if (args.TryGetValue("trails_lifetime", out JsonNode trailLifetimeToken))
                    trails.lifetime = trailLifetimeToken.AsFloat;
            }
        }

        private JsonClass GetParticleSystemData(ParticleSystem ps)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();

            var data = new JsonClass
            {
                ["name"] = ps.name,
                ["isPlaying"] = ps.isPlaying,
                ["isPaused"] = ps.isPaused,
                ["isStopped"] = ps.isStopped,
                ["time"] = ps.time,
                ["particleCount"] = ps.particleCount,

                // 主模块
                ["main"] = new JsonClass
                {
                    ["duration"] = main.duration,
                    ["looping"] = main.loop,
                    ["prewarm"] = main.prewarm,
                    ["startDelay"] = main.startDelay.constant,
                    ["startLifetime"] = main.startLifetime.constant,
                    ["startSpeed"] = main.startSpeed.constant,
                    ["startSize"] = main.startSize.constant,
                    ["startRotation"] = main.startRotation.constant * Mathf.Rad2Deg,
                    ["startColor"] = new JsonArray
                    {
                        main.startColor.color.r,
                        main.startColor.color.g,
                        main.startColor.color.b,
                        main.startColor.color.a
                    },
                    ["gravityModifier"] = main.gravityModifier.constant,
                    ["simulationSpace"] = main.simulationSpace.ToString(),
                    ["simulationSpeed"] = main.simulationSpeed,
                    ["scalingMode"] = main.scalingMode.ToString(),
                    ["playOnAwake"] = main.playOnAwake,
                    ["maxParticles"] = main.maxParticles
                },

                // 发射模块
                ["emission"] = new JsonClass
                {
                    ["enabled"] = emission.enabled,
                    ["rateOverTime"] = emission.rateOverTime.constant,
                    ["rateOverDistance"] = emission.rateOverDistance.constant
                },

                // 形状模块
                ["shape"] = new JsonClass
                {
                    ["enabled"] = shape.enabled,
                    ["shapeType"] = shape.shapeType.ToString(),
                    ["radius"] = shape.radius,
                    ["angle"] = shape.angle,
                    ["arc"] = shape.arc
                }
            };

            if (renderer != null)
            {
                data["renderer"] = new JsonClass
                {
                    ["renderMode"] = renderer.renderMode.ToString(),
                    ["materialName"] = renderer.sharedMaterial?.name,
                    ["sortingLayer"] = renderer.sortingLayerName,
                    ["sortingOrder"] = renderer.sortingOrder
                };
            }

            return data;
        }

        // --- 工具方法 ---

        private bool AssetExists(string path)
        {
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path));
        }

        private string SanitizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            path = path.Replace("\\", "/");
            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            return path;
        }
    }
}