"""
Unitycomponent editing tool
specialized for get and setGameObjectcomponent properties，supports allUnityreflection based access and modification for component types
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_component_tools(mcp: FastMCP):
    @mcp.tool("edit_component")
    def edit_component(
        ctx: Context,
        instance_id: Annotated[Optional[int], Field(
            title="instanceID",
            description="GameObjectinstance ofID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        path: Annotated[Optional[str], Field(
            title="object path",
            description="GameObjecthierarchy path of",
            default=None,
            examples=["Player", "Canvas/UI/Button", "Enemy_01"]
        )] = None,
        action: Annotated[Optional[str], Field(
            title="operation type",
            description="operation type: get_component_propertys(get component properties), set_component_propertys(set component properties)",
            default="get_component_propertys",
            examples=["get_component_propertys", "set_component_propertys"]
        )] = "get_component_propertys",
        component_type: Annotated[Optional[str], Field(
            title="component type",
            description="component type name（inherits fromComponenttype name of）",
            default=None,
            examples=["Rigidbody", "BoxCollider", "AudioSource", "Light", "Transform"]
        )] = None,
        properties: Annotated[Optional[Dict[str, Any]], Field(
            title="property dict",
            description="property dict to set（forset_component_propertysoperation）",
            default=None,
            examples=[
                {"mass": 2.0, "drag": 0.5},
                {"volume": 0.8, "pitch": 1.2},
                {"color": [1.0, 0.0, 0.0, 1.0]}
            ]
        )] = None
    ) -> Dict[str, Any]:
        """Unitycomponent editing tool，used for get and setGameObjectcomponent properties。（secondary tool）

        useC#reflection based accessUnityall serializable fields and properties of a component，support reading and modifying component state。
        this isUnityeditorInspectorprogrammatic interface for inspector panels，all modifications supportedUndoundo。

        main operations：
        - get_component_propertys: get component properties，returnYAMLformatted data
        - set_component_propertys: batch set component properties，automatic type conversion

        supported component types：
        - physics：Rigidbody, Collider, CharacterControlleretc
        - rendering：MeshRenderer, SpriteRenderer, ParticleSystemetc
        - lighting：Light, LightProbeGroup, ReflectionProbeetc
        - audio：AudioSource, AudioListeneretc
        - UI：RectTransform, Canvas, Image, Buttonetc
        - animation：Animator, Animationetc
        - and all customMonoBehaviourscript

        data type support：
        - primitive types：int, float, bool, string
        - Unitytype：Vector2/3/4, Color, Quaternion, Rect, Bounds
        - reference types：GameObject, Transform, Material, Textureetc
        - collection types：Array, List<T>, Dictionary

        property access rules：
        - include：publicfield、[SerializeField]private fields、readable and writablepublicproperty
        - exclude：hideFlags、quick accessor(rigidbody/cameraetc)、properties that create instances(material/meshetc)

        notes：
        - reflection has performance overhead，avoid calling every frame
        - ensure value types match，Vector3need3items
        - use firstgetview available properties as templates
        """

        return get_common_call_response("edit_component")
