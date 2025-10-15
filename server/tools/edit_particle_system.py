"""
particle system editing tool
specially forUnitycreation of particle system、configuration、playback control operations
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_particle_system_tools(mcp: FastMCP):
    @mcp.tool("edit_particle_system")
    def edit_particle_system(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="operation type: init_component(initialize component), get_properties(get properties), set_properties(set properties), play(play), pause(pause), stop(stop), clear(clear), simulate(simulate), restart(restart)",
            examples=["init_component", "get_properties", "set_properties", "play", "pause", "stop", "clear", "simulate", "restart"]
        ),
        instance_id: Optional[str] = Field(
            None,
            title="instanceID",
            description="GameObjectinstance ofID",
            examples=["12345", "67890"]
        ),
        path: Optional[str] = Field(
            None,
            title="object path",
            description="GameObjecthierarchy path of",
            examples=["Particle", "Effects/Smoke", "Player/FireEffect"]
        ),
        
        # main module properties
        duration: Optional[float] = Field(
            None,
            title="duration",
            description="particle system duration（seconds）",
            examples=[5.0, 10.0, 2.5]
        ),
        looping: Optional[bool] = Field(
            None,
            title="loop",
            description="loop playback"
        ),
        prewarm: Optional[bool] = Field(
            None,
            title="prewarm",
            description="prewarm（only valid in loop mode）"
        ),
        start_delay: Optional[float] = Field(
            None,
            title="start delay",
            description="start delay（seconds）",
            examples=[0.0, 0.5, 1.0]
        ),
        start_lifetime: Optional[float] = Field(
            None,
            title="particle lifetime",
            description="particle lifetime（seconds）",
            examples=[1.0, 2.0, 5.0]
        ),
        start_speed: Optional[float] = Field(
            None,
            title="initial velocity",
            description="initial speed",
            examples=[5.0, 10.0, 15.0]
        ),
        start_size: Optional[float] = Field(
            None,
            title="initial size",
            description="initial size",
            examples=[0.1, 0.5, 1.0]
        ),
        start_rotation: Optional[float] = Field(
            None,
            title="initial rotation",
            description="initial rotation angle（degrees）",
            examples=[0.0, 45.0, 90.0]
        ),
        start_color: Optional[List[float]] = Field(
            None,
            title="initial color",
            description="initial color [r, g, b, a]",
            examples=[[1.0, 1.0, 1.0, 1.0], [1.0, 0.5, 0.0, 1.0]]
        ),
        gravity_modifier: Optional[float] = Field(
            None,
            title="gravity modifier",
            description="gravity modifier",
            examples=[0.0, 0.5, 1.0]
        ),
        simulation_space: Optional[str] = Field(
            None,
            title="simulation space",
            description="simulation space: Local, World, Custom",
            examples=["Local", "World", "Custom"]
        ),
        simulation_speed: Optional[float] = Field(
            None,
            title="simulation speed",
            description="simulation speed multiplier",
            examples=[1.0, 0.5, 2.0]
        ),
        scaling_mode: Optional[str] = Field(
            None,
            title="scaling mode",
            description="scaling mode: Hierarchy, Local, Shape",
            examples=["Hierarchy", "Local", "Shape"]
        ),
        play_on_awake: Optional[bool] = Field(
            None,
            title="play on awake",
            description="play on awake"
        ),
        max_particles: Optional[int] = Field(
            None,
            title="max particle count",
            description="max particles",
            examples=[100, 1000, 5000]
        ),
        
        # emission module
        emission_enabled: Optional[bool] = Field(
            None,
            title="enable emission",
            description="enable emission module"
        ),
        emission_rate_over_time: Optional[float] = Field(
            None,
            title="time emission rate",
            description="particles per second",
            examples=[10.0, 50.0, 100.0]
        ),
        emission_rate_over_distance: Optional[float] = Field(
            None,
            title="distance emission rate",
            description="emission per distance unit",
            examples=[0.0, 5.0, 10.0]
        ),
        
        # shape module
        shape_enabled: Optional[bool] = Field(
            None,
            title="enable shape",
            description="enable shape module"
        ),
        shape_type: Optional[str] = Field(
            None,
            title="shape type",
            description="emission shape type: Sphere, Hemisphere, Cone, Box, Circle, Edge, Rectangle",
            examples=["Sphere", "Cone", "Box", "Circle"]
        ),
        shape_angle: Optional[float] = Field(
            None,
            title="cone angle",
            description="cone angle（degrees）",
            examples=[25.0, 45.0, 90.0]
        ),
        shape_radius: Optional[float] = Field(
            None,
            title="shape radius",
            description="emission shape radius",
            examples=[0.5, 1.0, 5.0]
        ),
        shape_arc: Optional[float] = Field(
            None,
            title="arc angle",
            description="circle/arc angle（degrees）",
            examples=[360.0, 180.0, 90.0]
        ),
        shape_random_direction: Optional[float] = Field(
            None,
            title="random direction",
            description="random direction amount (0-1)",
            examples=[0.0, 0.5, 1.0]
        ),
        
        # velocity module
        velocity_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="enable velocity over lifetime",
            description="enable velocity over lifetime"
        ),
        velocity_linear: Optional[List[float]] = Field(
            None,
            title="linear velocity",
            description="linear velocity [x, y, z]",
            examples=[[0.0, 1.0, 0.0], [1.0, 0.0, 0.0]]
        ),
        velocity_orbital: Optional[List[float]] = Field(
            None,
            title="orbital velocity",
            description="orbital velocity [x, y, z]",
            examples=[[0.0, 1.0, 0.0], [0.5, 0.5, 0.0]]
        ),
        
        # color module
        color_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="enable color over lifetime",
            description="enable color over lifetime"
        ),
        
        # size module
        size_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="enable size over lifetime",
            description="enable size over lifetime"
        ),
        
        # rotation module
        rotation_over_lifetime_enabled: Optional[bool] = Field(
            None,
            title="enable rotation over lifetime",
            description="enable rotation over lifetime"
        ),
        rotation_angular_velocity: Optional[float] = Field(
            None,
            title="angular speed",
            description="angular velocity（degrees/seconds）",
            examples=[45.0, 90.0, 180.0]
        ),
        
        # noise module
        noise_enabled: Optional[bool] = Field(
            None,
            title="enable noise",
            description="enable noise module"
        ),
        noise_strength: Optional[float] = Field(
            None,
            title="noise intensity",
            description="noise strength",
            examples=[0.1, 0.5, 1.0]
        ),
        noise_frequency: Optional[float] = Field(
            None,
            title="noise frequency",
            description="noise frequency",
            examples=[0.1, 0.5, 1.0]
        ),
        
        # collision module
        collision_enabled: Optional[bool] = Field(
            None,
            title="enable collision",
            description="enable collision module"
        ),
        collision_type: Optional[str] = Field(
            None,
            title="collision type",
            description="collision type: Planes, World",
            examples=["Planes", "World"]
        ),
        collision_dampen: Optional[float] = Field(
            None,
            title="collision damping",
            description="damping coefficient (0-1)",
            examples=[0.0, 0.5, 1.0]
        ),
        collision_bounce: Optional[float] = Field(
            None,
            title="collision bounce",
            description="bounce coefficient (0-1)",
            examples=[0.0, 0.5, 1.0]
        ),
        
        # renderer module
        render_mode: Optional[str] = Field(
            None,
            title="render mode",
            description="render mode: Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh",
            examples=["Billboard", "Stretch", "Mesh"]
        ),
        material: Optional[str] = Field(
            None,
            title="material path",
            description="particle material path",
            examples=["Assets/Materials/ParticleMaterial.mat"]
        ),
        sorting_layer: Optional[str] = Field(
            None,
            title="sorting layer",
            description="render sorting layer name",
            examples=["Default", "Effects", "UI"]
        ),
        sorting_order: Optional[int] = Field(
            None,
            title="sorting order",
            description="order in sorting layer",
            examples=[0, 1, 10]
        ),
        
        # texture sheet module
        texture_sheet_animation_enabled: Optional[bool] = Field(
            None,
            title="enable texture sheet animation",
            description="enable texture sheet animation"
        ),
        texture_sheet_tiles: Optional[List[int]] = Field(
            None,
            title="texture sheet tiles",
            description="texture sheet tiles [x, y]",
            examples=[[2, 2], [4, 4], [8, 8]]
        ),
        texture_sheet_fps: Optional[float] = Field(
            None,
            title="animation frame rate",
            description="texture sheet frame rate",
            examples=[30.0, 60.0, 24.0]
        ),
        
        # trail module
        trails_enabled: Optional[bool] = Field(
            None,
            title="enable trails",
            description="enable trail module"
        ),
        trails_ratio: Optional[float] = Field(
            None,
            title="trail ratio",
            description="trail particle ratio (0-1)",
            examples=[0.5, 1.0]
        ),
        trails_lifetime: Optional[float] = Field(
            None,
            title="trail lifetime",
            description="trail lifetime（seconds）",
            examples=[0.5, 1.0, 2.0]
        ),
        
        # playback control
        simulate_time: Optional[float] = Field(
            None,
            title="simulation time",
            description="simulate forward time（seconds），forsimulateoperation",
            examples=[0.5, 1.0, 2.0]
        ),
        with_children: Optional[bool] = Field(
            True,
            title="include sub particles",
            description="whether to include sub particle systems"
        )
    ) -> Dict[str, Any]:
        """
        particle system editing tool,provide full particle system creation、configuration and control。（secondary tool）

        supports various particle operations，suitable for：
        - effect creation：quickly create and configure effects
        - property tuning：adjust particle system parameters in real time
        - playback control：control particle playback precisely、pause、stop
        - simulation debug：simulate particle system state

        main modules：
        1. main module：duration、loop、lifetime、speed、size、basic properties such as color
        2. emission module：control emission rate
        3. shape module：define emission shape（sphere、cone、box etc）
        4. velocity module：set linear and orbital velocity
        5. color module：particle color changes over lifetime
        6. size module：particle size changes over lifetime
        7. rotation module：rotation effect
        8. noise module：add noise
        9. collision module：particle scene collisions
        10. renderer module：control particle rendering mode
        11. Texture sheet animation：sprite sheet animation
        12. trail module：trail effect

        operation type：
        - init_component: initialize or add particle system component
        - get_properties: get current particle system properties
        - set_properties: set particle system properties
        - play: play particle system
        - pause: pause particle system
        - stop: stop particle system
        - clear: clear all particles
        - simulate: simulate particle system
        - restart: restart particle system

        example usage：
        1. create a simple flame effect:
           {"action": "init_component", "path": "FireEffect", "start_color": [1.0, 0.5, 0.0, 1.0], 
            "start_lifetime": 2.0, "emission_rate_over_time": 50.0, "shape_type": "Cone"}

        2. play particle effect:
           {"action": "play", "path": "FireEffect", "with_children": true}

        3. adjust particle size:
           {"action": "set_properties", "path": "FireEffect", "start_size": 1.5, "max_particles": 1000}
        """

        return get_common_call_response("edit_particle_system")

