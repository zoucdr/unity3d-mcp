using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;

namespace UnityMcp.Tools
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
            return new[]
            {
                // 目标查找参数
                new MethodKey("instance_id", "GameObject的实例ID", true),
                new MethodKey("path", "GameObject的层次结构路径", true),
                
                // 操作类型
                new MethodKey("action", "操作类型：init_component, get_properties, set_properties, play, pause, stop, clear, simulate, restart", false),
                
                // 主模块属性
                new MethodKey("duration", "粒子系统持续时间", true),
                new MethodKey("looping", "是否循环播放", true),
                new MethodKey("prewarm", "是否预热", true),
                new MethodKey("start_delay", "开始延迟", true),
                new MethodKey("start_lifetime", "粒子生命周期", true),
                new MethodKey("start_speed", "粒子初始速度", true),
                new MethodKey("start_size", "粒子初始大小", true),
                new MethodKey("start_rotation", "粒子初始旋转", true),
                new MethodKey("start_color", "粒子初始颜色 [r,g,b,a]", true),
                new MethodKey("gravity_modifier", "重力修正系数", true),
                new MethodKey("simulation_space", "模拟空间：Local, World, Custom", true),
                new MethodKey("simulation_speed", "模拟速度", true),
                new MethodKey("scaling_mode", "缩放模式：Hierarchy, Local, Shape", true),
                new MethodKey("play_on_awake", "唤醒时播放", true),
                new MethodKey("max_particles", "最大粒子数", true),
                
                // 发射模块
                new MethodKey("emission_enabled", "是否启用发射", true),
                new MethodKey("emission_rate_over_time", "每秒发射率", true),
                new MethodKey("emission_rate_over_distance", "每单位距离发射率", true),
                
                // 形状模块
                new MethodKey("shape_enabled", "是否启用形状模块", true),
                new MethodKey("shape_type", "形状类型：Sphere, Hemisphere, Cone, Box, Circle, Edge, Rectangle", true),
                new MethodKey("shape_angle", "锥体角度", true),
                new MethodKey("shape_radius", "半径", true),
                new MethodKey("shape_box_thickness", "盒子厚度 [x,y,z]", true),
                new MethodKey("shape_arc", "圆弧角度", true),
                new MethodKey("shape_random_direction", "随机方向", true),
                
                // 速度模块
                new MethodKey("velocity_over_lifetime_enabled", "是否启用生命周期速度", true),
                new MethodKey("velocity_linear", "线性速度 [x,y,z]", true),
                new MethodKey("velocity_orbital", "轨道速度 [x,y,z]", true),
                
                // 限制速度模块
                new MethodKey("limit_velocity_enabled", "是否启用速度限制", true),
                new MethodKey("limit_velocity_dampen", "速度衰减", true),
                
                // 力场模块
                new MethodKey("force_over_lifetime_enabled", "是否启用生命周期力", true),
                new MethodKey("force_x", "X轴力", true),
                new MethodKey("force_y", "Y轴力", true),
                new MethodKey("force_z", "Z轴力", true),
                
                // 颜色模块
                new MethodKey("color_over_lifetime_enabled", "是否启用生命周期颜色", true),
                new MethodKey("color_gradient", "颜色渐变配置", true),
                
                // 大小模块
                new MethodKey("size_over_lifetime_enabled", "是否启用生命周期大小", true),
                new MethodKey("size_curve", "大小曲线配置", true),
                
                // 旋转模块
                new MethodKey("rotation_over_lifetime_enabled", "是否启用生命周期旋转", true),
                new MethodKey("rotation_angular_velocity", "角速度", true),
                
                // 噪声模块
                new MethodKey("noise_enabled", "是否启用噪声", true),
                new MethodKey("noise_strength", "噪声强度", true),
                new MethodKey("noise_frequency", "噪声频率", true),
                
                // 碰撞模块
                new MethodKey("collision_enabled", "是否启用碰撞", true),
                new MethodKey("collision_type", "碰撞类型：Planes, World", true),
                new MethodKey("collision_dampen", "碰撞衰减", true),
                new MethodKey("collision_bounce", "碰撞反弹", true),
                
                // 渲染模块
                new MethodKey("render_mode", "渲染模式：Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh", true),
                new MethodKey("material", "材质路径", true),
                new MethodKey("trail_material", "拖尾材质路径", true),
                new MethodKey("sorting_layer", "排序层", true),
                new MethodKey("sorting_order", "排序顺序", true),
                
                // 纹理表动画模块
                new MethodKey("texture_sheet_animation_enabled", "是否启用纹理表动画", true),
                new MethodKey("texture_sheet_tiles", "纹理表分块 [x,y]", true),
                new MethodKey("texture_sheet_animation_type", "动画类型：WholeSheet, SingleRow", true),
                new MethodKey("texture_sheet_fps", "动画帧率", true),
                
                // 子发射器模块
                new MethodKey("sub_emitters_enabled", "是否启用子发射器", true),
                
                // 灯光模块
                new MethodKey("lights_enabled", "是否启用灯光", true),
                new MethodKey("lights_ratio", "灯光比率", true),
                
                // 拖尾模块
                new MethodKey("trails_enabled", "是否启用拖尾", true),
                new MethodKey("trails_ratio", "拖尾比率", true),
                new MethodKey("trails_lifetime", "拖尾生命周期", true),
                
                // 播放控制
                new MethodKey("simulate_time", "模拟时间（秒）", true),
                new MethodKey("with_children", "是否包含子粒子系统", true),
                new MethodKey("restart_mode", "重启模式：Default, Fast", true),
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

            if (context.TryGetJsonValue("_resolved_targets", out JToken targetToken))
            {
                if (targetToken is JArray arr && arr.Count > 0)
                {
                    int instanceId = arr[0].Value<int>();
                    return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                }
                else if (targetToken.Type == JTokenType.Integer)
                {
                    int instanceId = targetToken.Value<int>();
                    return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                }
            }

            return null;
        }

        // --- 操作方法 ---

        private object HandleDefaultAction(StateTreeContext context)
        {
            JObject args = context.JsonData;
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
                    LogInfo($"[EditParticleSystem] Added ParticleSystem component to '{target.name}'");
                }
                else
                {
                    Undo.RecordObject(ps, "Initialize ParticleSystem");
                    LogInfo($"[EditParticleSystem] Found existing ParticleSystem on '{target.name}', initializing properties");
                }
                
                // 应用初始属性
                JObject args = context.JsonData;
                if (args.HasValues)
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
                JObject args = context.JsonData;
                ApplyParticleSystemProperties(ps, args);
                
                LogInfo($"[EditParticleSystem] Set properties on '{target.name}'");
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

            JObject args = context.JsonData;
            bool withChildren = args["with_children"]?.ToObject<bool>() ?? true;

            ps.Play(withChildren);
            LogInfo($"[EditParticleSystem] Playing ParticleSystem on '{target.name}'");
            
            return Response.Success($"ParticleSystem playing on '{target.name}'.", new JObject
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

            JObject args = context.JsonData;
            bool withChildren = args["with_children"]?.ToObject<bool>() ?? true;

            ps.Pause(withChildren);
            LogInfo($"[EditParticleSystem] Paused ParticleSystem on '{target.name}'");
            
            return Response.Success($"ParticleSystem paused on '{target.name}'.", new JObject
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

            JObject args = context.JsonData;
            bool withChildren = args["with_children"]?.ToObject<bool>() ?? true;

            ps.Stop(withChildren);
            LogInfo($"[EditParticleSystem] Stopped ParticleSystem on '{target.name}'");
            
            return Response.Success($"ParticleSystem stopped on '{target.name}'.", new JObject
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

            JObject args = context.JsonData;
            bool withChildren = args["with_children"]?.ToObject<bool>() ?? true;

            ps.Clear(withChildren);
            LogInfo($"[EditParticleSystem] Cleared ParticleSystem on '{target.name}'");
            
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

            JObject args = context.JsonData;
            float time = args["simulate_time"]?.ToObject<float>() ?? 1.0f;
            bool withChildren = args["with_children"]?.ToObject<bool>() ?? true;

            ps.Simulate(time, withChildren, true);
            LogInfo($"[EditParticleSystem] Simulated {time}s on '{target.name}'");
            
            return Response.Success($"ParticleSystem simulated {time}s on '{target.name}'.", new JObject
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

            JObject args = context.JsonData;
            bool withChildren = args["with_children"]?.ToObject<bool>() ?? true;

            ps.Stop(withChildren);
            ps.Clear(withChildren);
            ps.Play(withChildren);
            
            LogInfo($"[EditParticleSystem] Restarted ParticleSystem on '{target.name}'");
            return Response.Success($"ParticleSystem restarted on '{target.name}'.");
        }

        // --- 属性应用和获取方法 ---

        private void ApplyParticleSystemProperties(ParticleSystem ps, JObject args)
        {
            var main = ps.main;
            
            // 主模块属性
            if (args.TryGetValue("duration", out JToken durationToken))
                main.duration = durationToken.ToObject<float>();
            
            if (args.TryGetValue("looping", out JToken loopingToken))
                main.loop = loopingToken.ToObject<bool>();
            
            if (args.TryGetValue("prewarm", out JToken prewarmToken))
                main.prewarm = prewarmToken.ToObject<bool>();
            
            if (args.TryGetValue("start_delay", out JToken delayToken))
                main.startDelay = delayToken.ToObject<float>();
            
            if (args.TryGetValue("start_lifetime", out JToken lifetimeToken))
                main.startLifetime = lifetimeToken.ToObject<float>();
            
            if (args.TryGetValue("start_speed", out JToken speedToken))
                main.startSpeed = speedToken.ToObject<float>();
            
            if (args.TryGetValue("start_size", out JToken sizeToken))
                main.startSize = sizeToken.ToObject<float>();
            
            if (args.TryGetValue("start_rotation", out JToken rotationToken))
                main.startRotation = rotationToken.ToObject<float>() * Mathf.Deg2Rad;
            
            if (args.TryGetValue("start_color", out JToken colorToken))
            {
                var colorArray = colorToken.ToObject<float[]>();
                if (colorArray != null && colorArray.Length >= 3)
                {
                    main.startColor = new Color(
                        colorArray[0],
                        colorArray[1],
                        colorArray[2],
                        colorArray.Length > 3 ? colorArray[3] : 1.0f
                    );
                }
            }
            
            if (args.TryGetValue("gravity_modifier", out JToken gravityToken))
                main.gravityModifier = gravityToken.ToObject<float>();
            
            if (args.TryGetValue("simulation_space", out JToken simSpaceToken))
            {
                if (Enum.TryParse(simSpaceToken.ToString(), out ParticleSystemSimulationSpace simSpace))
                    main.simulationSpace = simSpace;
            }
            
            if (args.TryGetValue("simulation_speed", out JToken simSpeedToken))
                main.simulationSpeed = simSpeedToken.ToObject<float>();
            
            if (args.TryGetValue("scaling_mode", out JToken scalingToken))
            {
                if (Enum.TryParse(scalingToken.ToString(), out ParticleSystemScalingMode scalingMode))
                    main.scalingMode = scalingMode;
            }
            
            if (args.TryGetValue("play_on_awake", out JToken playOnAwakeToken))
                main.playOnAwake = playOnAwakeToken.ToObject<bool>();
            
            if (args.TryGetValue("max_particles", out JToken maxParticlesToken))
                main.maxParticles = maxParticlesToken.ToObject<int>();
            
            // 发射模块
            if (args.ContainsKey("emission_enabled") || args.ContainsKey("emission_rate_over_time") || 
                args.ContainsKey("emission_rate_over_distance"))
            {
                var emission = ps.emission;
                
                if (args.TryGetValue("emission_enabled", out JToken emissionEnabledToken))
                    emission.enabled = emissionEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("emission_rate_over_time", out JToken rateTimeToken))
                    emission.rateOverTime = rateTimeToken.ToObject<float>();
                
                if (args.TryGetValue("emission_rate_over_distance", out JToken rateDistToken))
                    emission.rateOverDistance = rateDistToken.ToObject<float>();
            }
            
            // 形状模块
            if (args.ContainsKey("shape_enabled") || args.ContainsKey("shape_type") || 
                args.ContainsKey("shape_radius") || args.ContainsKey("shape_angle"))
            {
                var shape = ps.shape;
                
                if (args.TryGetValue("shape_enabled", out JToken shapeEnabledToken))
                    shape.enabled = shapeEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("shape_type", out JToken shapeTypeToken))
                {
                    if (Enum.TryParse(shapeTypeToken.ToString(), out ParticleSystemShapeType shapeType))
                        shape.shapeType = shapeType;
                }
                
                if (args.TryGetValue("shape_angle", out JToken angleToken))
                    shape.angle = angleToken.ToObject<float>();
                
                if (args.TryGetValue("shape_radius", out JToken radiusToken))
                    shape.radius = radiusToken.ToObject<float>();
                
                if (args.TryGetValue("shape_arc", out JToken arcToken))
                    shape.arc = arcToken.ToObject<float>();
                
                if (args.TryGetValue("shape_random_direction", out JToken randomDirToken))
                    shape.randomDirectionAmount = randomDirToken.ToObject<float>();
            }
            
            // 速度模块
            if (args.ContainsKey("velocity_over_lifetime_enabled") || args.ContainsKey("velocity_linear"))
            {
                var velocity = ps.velocityOverLifetime;
                
                if (args.TryGetValue("velocity_over_lifetime_enabled", out JToken velEnabledToken))
                    velocity.enabled = velEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("velocity_linear", out JToken linearToken))
                {
                    var linear = linearToken.ToObject<float[]>();
                    if (linear != null && linear.Length >= 3)
                    {
                        velocity.x = linear[0];
                        velocity.y = linear[1];
                        velocity.z = linear[2];
                    }
                }
                
                if (args.TryGetValue("velocity_orbital", out JToken orbitalToken))
                {
                    var orbital = orbitalToken.ToObject<float[]>();
                    if (orbital != null && orbital.Length >= 3)
                    {
                        velocity.orbitalX = orbital[0];
                        velocity.orbitalY = orbital[1];
                        velocity.orbitalZ = orbital[2];
                    }
                }
            }
            
            // 颜色模块
            if (args.ContainsKey("color_over_lifetime_enabled"))
            {
                var colorOverLifetime = ps.colorOverLifetime;
                
                if (args.TryGetValue("color_over_lifetime_enabled", out JToken colorEnabledToken))
                    colorOverLifetime.enabled = colorEnabledToken.ToObject<bool>();
            }
            
            // 大小模块
            if (args.ContainsKey("size_over_lifetime_enabled"))
            {
                var sizeOverLifetime = ps.sizeOverLifetime;
                
                if (args.TryGetValue("size_over_lifetime_enabled", out JToken sizeEnabledToken))
                    sizeOverLifetime.enabled = sizeEnabledToken.ToObject<bool>();
            }
            
            // 旋转模块
            if (args.ContainsKey("rotation_over_lifetime_enabled") || args.ContainsKey("rotation_angular_velocity"))
            {
                var rotationOverLifetime = ps.rotationOverLifetime;
                
                if (args.TryGetValue("rotation_over_lifetime_enabled", out JToken rotEnabledToken))
                    rotationOverLifetime.enabled = rotEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("rotation_angular_velocity", out JToken angVelToken))
                    rotationOverLifetime.z = angVelToken.ToObject<float>() * Mathf.Deg2Rad;
            }
            
            // 碰撞模块
            if (args.ContainsKey("collision_enabled") || args.ContainsKey("collision_type"))
            {
                var collision = ps.collision;
                
                if (args.TryGetValue("collision_enabled", out JToken collisionEnabledToken))
                    collision.enabled = collisionEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("collision_type", out JToken collisionTypeToken))
                {
                    if (Enum.TryParse(collisionTypeToken.ToString(), out ParticleSystemCollisionType collisionType))
                        collision.type = collisionType;
                }
                
                if (args.TryGetValue("collision_dampen", out JToken dampenToken))
                    collision.dampen = dampenToken.ToObject<float>();
                
                if (args.TryGetValue("collision_bounce", out JToken bounceToken))
                    collision.bounce = bounceToken.ToObject<float>();
            }
            
            // 噪声模块
            if (args.ContainsKey("noise_enabled") || args.ContainsKey("noise_strength"))
            {
                var noise = ps.noise;
                
                if (args.TryGetValue("noise_enabled", out JToken noiseEnabledToken))
                    noise.enabled = noiseEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("noise_strength", out JToken strengthToken))
                    noise.strength = strengthToken.ToObject<float>();
                
                if (args.TryGetValue("noise_frequency", out JToken freqToken))
                    noise.frequency = freqToken.ToObject<float>();
            }
            
            // 渲染器属性
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                if (args.TryGetValue("render_mode", out JToken renderModeToken))
                {
                    if (Enum.TryParse(renderModeToken.ToString(), out ParticleSystemRenderMode renderMode))
                        renderer.renderMode = renderMode;
                }
                
                if (args.TryGetValue("material", out JToken materialToken))
                {
                    string materialPath = materialToken.ToString();
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (mat != null)
                        renderer.material = mat;
                }
                
                if (args.TryGetValue("sorting_layer", out JToken sortingLayerToken))
                    renderer.sortingLayerName = sortingLayerToken.ToString();
                
                if (args.TryGetValue("sorting_order", out JToken sortingOrderToken))
                    renderer.sortingOrder = sortingOrderToken.ToObject<int>();
            }
            
            // 纹理表动画
            if (args.ContainsKey("texture_sheet_animation_enabled") || args.ContainsKey("texture_sheet_tiles"))
            {
                var textureSheet = ps.textureSheetAnimation;
                
                if (args.TryGetValue("texture_sheet_animation_enabled", out JToken texSheetEnabledToken))
                    textureSheet.enabled = texSheetEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("texture_sheet_tiles", out JToken tilesToken))
                {
                    var tiles = tilesToken.ToObject<int[]>();
                    if (tiles != null && tiles.Length >= 2)
                    {
                        textureSheet.numTilesX = tiles[0];
                        textureSheet.numTilesY = tiles[1];
                    }
                }
                
                if (args.TryGetValue("texture_sheet_fps", out JToken fpsToken))
                    textureSheet.fps = fpsToken.ToObject<float>();
            }
            
            // 拖尾模块
            if (args.ContainsKey("trails_enabled") || args.ContainsKey("trails_ratio"))
            {
                var trails = ps.trails;
                
                if (args.TryGetValue("trails_enabled", out JToken trailsEnabledToken))
                    trails.enabled = trailsEnabledToken.ToObject<bool>();
                
                if (args.TryGetValue("trails_ratio", out JToken ratioToken))
                    trails.ratio = ratioToken.ToObject<float>();
                
                if (args.TryGetValue("trails_lifetime", out JToken trailLifetimeToken))
                    trails.lifetime = trailLifetimeToken.ToObject<float>();
            }
        }

        private JObject GetParticleSystemData(ParticleSystem ps)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            
            var data = new JObject
            {
                ["name"] = ps.name,
                ["isPlaying"] = ps.isPlaying,
                ["isPaused"] = ps.isPaused,
                ["isStopped"] = ps.isStopped,
                ["time"] = ps.time,
                ["particleCount"] = ps.particleCount,
                
                // 主模块
                ["main"] = new JObject
                {
                    ["duration"] = main.duration,
                    ["looping"] = main.loop,
                    ["prewarm"] = main.prewarm,
                    ["startDelay"] = main.startDelay.constant,
                    ["startLifetime"] = main.startLifetime.constant,
                    ["startSpeed"] = main.startSpeed.constant,
                    ["startSize"] = main.startSize.constant,
                    ["startRotation"] = main.startRotation.constant * Mathf.Rad2Deg,
                    ["startColor"] = new JArray 
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
                ["emission"] = new JObject
                {
                    ["enabled"] = emission.enabled,
                    ["rateOverTime"] = emission.rateOverTime.constant,
                    ["rateOverDistance"] = emission.rateOverDistance.constant
                },
                
                // 形状模块
                ["shape"] = new JObject
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
                data["renderer"] = new JObject
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