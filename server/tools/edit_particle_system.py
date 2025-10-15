"""
粒子系统编辑工具
专门用于Unity粒子系统的创建、配置、播放控制等操作
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_particle_system_tools(mcp: FastMCP):
    @mcp.tool("edit_particle_system")
    def edit_particle_system(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="操作类型: init_component(初始化组件), get_properties(获取属性), set_properties(设置属性), play(播放), pause(暂停), stop(停止), clear(清除), simulate(模拟), restart(重启)",
            examples=["init_component", "get_properties", "set_properties", "play", "pause", "stop", "clear", "simulate", "restart"]
        ),
        instance_id: Optional[str] = Field(
            None,
            title="实例ID",
            description="GameObject的实例ID",
            examples=["12345", "67890"]
        ),
        path: Optional[str] = Field(
            None,
            title="对象路径",
            description="GameObject的层次结构路径",
            examples=["Particle", "Effects/Smoke", "Player/FireEffect"]
        ),
        
        # 主模块属性
        duration: Optional[float] = Field(
            None,
            title="持续时间",
            description="粒子系统持续时间（秒）",
            examples=[5.0, 10.0, 2.5]
        ),
        looping: Optional[bool] = Field(
            None,
            title="循环播放",
            description="是否循环播放"
        ),
        prewarm: Optional[bool] = Field(
            None,
            title="预热",
            description="是否预热（仅循环模式有效）"
        ),
        start_delay: Optional[float] = Field(
            None,
            title="开始延迟",
            description="开始延迟时间（秒）",
            examples=[0.0, 0.5, 1.0]
        ),
        start_lifetime: Optional[float] = Field(
            None,
            title="粒子生命周期",
            description="粒子生命周期（秒）",
            examples=[1.0, 2.0, 5.0]
        ),
        start_speed: Optional[float] = Field(
            None,
            title="初始速度",
            description="粒子初始速度",
            examples=[5.0, 10.0, 15.0]
        ),
        start_size: Optional[float] = Field(
            None,
            title="初始大小",
            description="粒子初始大小",
            examples=[0.1, 0.5, 1.0]
        ),
        start_rotation: Optional[float] = Field(
            None,
            title="初始旋转",
            description="粒子初始旋转角度（度）",
            examples=[0.0, 45.0, 90.0]
        ),
        start_color: Optional[List[float]] = Field(
            None,
            title="初始颜色",
            description="粒子初始颜色 [r, g, b, a]",
            examples=[[1.0, 1.0, 1.0, 1.0], [1.0, 0.5, 0.0, 1.0]]
        ),
        gravity_modifier: Optional[float] = Field(
            None,
            title="重力修正",
            description="重力修正系数",
            examples=[0.0, 0.5, 1.0]
        ),
        simulation_space: Optional[str] = Field(
            None,
            title="模拟空间",
            description="模拟空间: Local, World, Custom",
            examples=["Local", "World", "Custom"]
        ),
        simulation_speed: Optional[float] = Field(
            None,
            title="模拟速度",
            description="模拟速度倍率",
            examples=[1.0, 0.5, 2.0]
        ),
        scaling_mode: Optional[str] = Field(
            None,
            title="缩放模式",
            description="缩放模式: Hierarchy, Local, Shape",
            examples=["Hierarchy", "Local", "Shape"]
        ),
        play_on_awake: Optional[bool] = Field(
            None,
            title="唤醒时播放",
            description="是否在唤醒时自动播放"
        ),
        max_particles: Optional[int] = Field(
            None,
            title="最大粒子数",
            description="最大粒子数量",
            examples=[100, 1000, 5000]
        ),
        
        # 发射模块
        emission_enabled: Optional[bool] = Field(
            None,
            title="启用发射",
            description="是否启用发射模块"
        ),
        emission_rate_over_time: Optional[float] = Field(
            None,
            title="时间发射率",
            description="每秒发射粒子数",
            examples=[10.0, 50.0, 100.0]
        ),
        emission_rate_over_distance: Optional[float] = Field(
            None,
            title="距离发射率",
            description="每单位距离发射粒子数",
            examples=[0.0, 5.0, 10.0]
        ),
        
        # 形状模块
        shape_enabled: Optional[bool] = Field(
            None,
            title="启用形状",
            description="是否启用形状模块"
        ),
        shape_type: Optional[str] = Field(
            None,
            title="形状类型",
            description="发射形状类型: Sphere, Hemisphere, Cone, Box, Circle, Edge, Rectangle",
            examples=["Sphere", "Cone", "Box", "Circle"]
        ),
        shape_angle: Optional[float] = Field(
            None,
            title="锥体角度",
            description="锥体发射角度（度）",
            examples=[25.0, 45.0, 90.0]
        ),
        shape_radius: Optional[float] = Field(
            None,
            title="形状半径",
            description="发射形状的半径",
            examples=[0.5, 1.0, 5.0]
        ),
        shape_arc: Optional[float] = Field(
            None,
            title="圆弧角度",
            description="圆形/圆环的弧度角度（度）",
            examples=[360.0, 180.0, 90.0]
        ),
        shape_random_direction: Optional[float] = Field(
            None,
            title="随机方向",
            description="随机方向量 (0-1)",
            examples=[0.0, 0.5, 1.0]
        ),
        
        # 速度模块
        velocity_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="启用生命周期速度",
            description="是否启用生命周期速度模块"
        ),
        velocity_linear: Optional[List[float]] = Field(
            None,
            title="线性速度",
            description="线性速度 [x, y, z]",
            examples=[[0.0, 1.0, 0.0], [1.0, 0.0, 0.0]]
        ),
        velocity_orbital: Optional[List[float]] = Field(
            None,
            title="轨道速度",
            description="轨道速度 [x, y, z]",
            examples=[[0.0, 1.0, 0.0], [0.5, 0.5, 0.0]]
        ),
        
        # 颜色模块
        color_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="启用生命周期颜色",
            description="是否启用生命周期颜色变化"
        ),
        
        # 大小模块
        size_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="启用生命周期大小",
            description="是否启用生命周期大小变化"
        ),
        
        # 旋转模块
        rotation_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="启用生命周期旋转",
            description="是否启用生命周期旋转"
        ),
        rotation_angular_velocity: Optional[float] = Field(
            None,
            title="角速度",
            description="旋转角速度（度/秒）",
            examples=[45.0, 90.0, 180.0]
        ),
        
        # 噪声模块
        noise_enabled: Optional[bool] = Field(
            None,
            title="启用噪声",
            description="是否启用噪声模块"
        ),
        noise_strength: Optional[float] = Field(
            None,
            title="噪声强度",
            description="噪声扰动强度",
            examples=[0.1, 0.5, 1.0]
        ),
        noise_frequency: Optional[float] = Field(
            None,
            title="噪声频率",
            description="噪声频率",
            examples=[0.1, 0.5, 1.0]
        ),
        
        # 碰撞模块
        collision_enabled: Optional[bool] = Field(
            None,
            title="启用碰撞",
            description="是否启用碰撞模块"
        ),
        collision_type: Optional[str] = Field(
            None,
            title="碰撞类型",
            description="碰撞类型: Planes, World",
            examples=["Planes", "World"]
        ),
        collision_dampen: Optional[float] = Field(
            None,
            title="碰撞衰减",
            description="碰撞衰减系数 (0-1)",
            examples=[0.0, 0.5, 1.0]
        ),
        collision_bounce: Optional[float] = Field(
            None,
            title="碰撞反弹",
            description="碰撞反弹系数 (0-1)",
            examples=[0.0, 0.5, 1.0]
        ),
        
        # 渲染模块
        render_mode: Optional[str] = Field(
            None,
            title="渲染模式",
            description="渲染模式: Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh",
            examples=["Billboard", "Stretch", "Mesh"]
        ),
        material: Optional[str] = Field(
            None,
            title="材质路径",
            description="粒子材质资源路径",
            examples=["Assets/Materials/ParticleMaterial.mat"]
        ),
        sorting_layer: Optional[str] = Field(
            None,
            title="排序层",
            description="渲染排序层名称",
            examples=["Default", "Effects", "UI"]
        ),
        sorting_order: Optional[int] = Field(
            None,
            title="排序顺序",
            description="排序层内的顺序",
            examples=[0, 1, 10]
        ),
        
        # 纹理表动画模块
        texture_sheet_animation_enabled: Optional[bool] = Field(
            None,
            title="启用纹理表动画",
            description="是否启用纹理表动画"
        ),
        texture_sheet_tiles: Optional[List[int]] = Field(
            None,
            title="纹理表分块",
            description="纹理表分块数 [x, y]",
            examples=[[2, 2], [4, 4], [8, 8]]
        ),
        texture_sheet_fps: Optional[float] = Field(
            None,
            title="动画帧率",
            description="纹理表动画帧率",
            examples=[30.0, 60.0, 24.0]
        ),
        
        # 拖尾模块
        trails_enabled: Optional[bool] = Field(
            None,
            title="启用拖尾",
            description="是否启用拖尾模块"
        ),
        trails_ratio: Optional[float] = Field(
            None,
            title="拖尾比率",
            description="产生拖尾的粒子比率 (0-1)",
            examples=[0.5, 1.0]
        ),
        trails_lifetime: Optional[float] = Field(
            None,
            title="拖尾生命周期",
            description="拖尾生命周期（秒）",
            examples=[0.5, 1.0, 2.0]
        ),
        
        # 播放控制
        simulate_time: Optional[float] = Field(
            None,
            title="模拟时间",
            description="模拟前进的时间（秒），用于simulate操作",
            examples=[0.5, 1.0, 2.0]
        ),
        with_children: Optional[bool] = Field(
            True,
            title="包含子粒子",
            description="操作是否包含子粒子系统"
        )
    ) -> Dict[str, Any]:
        """
        粒子系统编辑工具,提供完整的粒子系统创建、配置和控制功能。

        支持多种粒子系统操作，适用于：
        - 特效创建：快速创建和配置粒子特效
        - 属性调整：实时调整粒子系统的各种参数
        - 播放控制：精确控制粒子系统的播放、暂停、停止
        - 模拟调试：模拟粒子系统的运行状态

        主要功能模块：
        1. 主模块：持续时间、循环、生命周期、速度、大小、颜色等基础属性
        2. 发射模块：控制粒子发射速率
        3. 形状模块：定义粒子发射形状（球体、锥体、盒子等）
        4. 速度模块：设置粒子的线性和轨道速度
        5. 颜色模块：粒子颜色随生命周期变化
        6. 大小模块：粒子大小随生命周期变化
        7. 旋转模块：粒子旋转效果
        8. 噪声模块：添加噪声扰动
        9. 碰撞模块：粒子与场景碰撞
        10. 渲染模块：控制粒子渲染方式
        11. 纹理表动画：序列帧动画
        12. 拖尾模块：粒子拖尾效果

        操作类型：
        - init_component: 初始化或添加粒子系统组件
        - get_properties: 获取粒子系统当前属性
        - set_properties: 设置粒子系统属性
        - play: 播放粒子系统
        - pause: 暂停粒子系统
        - stop: 停止粒子系统
        - clear: 清除所有粒子
        - simulate: 模拟粒子系统运行
        - restart: 重启粒子系统

        示例用法：
        1. 创建简单的火焰效果:
           {"action": "init_component", "path": "FireEffect", "start_color": [1.0, 0.5, 0.0, 1.0], 
            "start_lifetime": 2.0, "emission_rate_over_time": 50.0, "shape_type": "Cone"}

        2. 播放粒子特效:
           {"action": "play", "path": "FireEffect", "with_children": true}

        3. 调整粒子大小:
           {"action": "set_properties", "path": "FireEffect", "start_size": 1.5, "max_particles": 1000}
        """

        return send_to_unity("edit_particle_system", {
            "action": action,
            "instance_id": instance_id,
            "path": path,
            "duration": duration,
            "looping": looping,
            "prewarm": prewarm,
            "start_delay": start_delay,
            "start_lifetime": start_lifetime,
            "start_speed": start_speed,
            "start_size": start_size,
            "start_rotation": start_rotation,
            "start_color": start_color,
            "gravity_modifier": gravity_modifier,
            "simulation_space": simulation_space,
            "simulation_speed": simulation_speed,
            "scaling_mode": scaling_mode,
            "play_on_awake": play_on_awake,
            "max_particles": max_particles,
            "emission_enabled": emission_enabled,
            "emission_rate_over_time": emission_rate_over_time,
            "emission_rate_over_distance": emission_rate_over_distance,
            "shape_enabled": shape_enabled,
            "shape_type": shape_type,
            "shape_angle": shape_angle,
            "shape_radius": shape_radius,
            "shape_arc": shape_arc,
            "shape_random_direction": shape_random_direction,
            "velocity_over_lifetime_enabled": velocity_over_lifetime_enabled,
            "velocity_linear": velocity_linear,
            "velocity_orbital": velocity_orbital,
            "color_over_lifetime_enabled": color_over_lifetime_enabled,
            "size_over_lifetime_enabled": size_over_lifetime_enabled,
            "rotation_over_lifetime_enabled": rotation_over_lifetime_enabled,
            "rotation_angular_velocity": rotation_angular_velocity,
            "noise_enabled": noise_enabled,
            "noise_strength": noise_strength,
            "noise_frequency": noise_frequency,
            "collision_enabled": collision_enabled,
            "collision_type": collision_type,
            "collision_dampen": collision_dampen,
            "collision_bounce": collision_bounce,
            "render_mode": render_mode,
            "material": material,
            "sorting_layer": sorting_layer,
            "sorting_order": sorting_order,
            "texture_sheet_animation_enabled": texture_sheet_animation_enabled,
            "texture_sheet_tiles": texture_sheet_tiles,
            "texture_sheet_fps": texture_sheet_fps,
            "trails_enabled": trails_enabled,
            "trails_ratio": trails_ratio,
            "trails_lifetime": trails_lifetime,
            "simulate_time": simulate_time,
            "with_children": with_children
        })

