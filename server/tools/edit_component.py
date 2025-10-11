"""
Unity组件编辑工具
专门用于获取和设置GameObject组件的属性，支持所有Unity组件类型的反射访问和修改
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
            title="实例ID",
            description="GameObject的实例ID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        path: Annotated[Optional[str], Field(
            title="对象路径",
            description="GameObject的层次结构路径",
            default=None,
            examples=["Player", "Canvas/UI/Button", "Enemy_01"]
        )] = None,
        action: Annotated[Optional[str], Field(
            title="操作类型",
            description="操作类型: get_component_propertys(获取组件属性), set_component_propertys(设置组件属性)",
            default="get_component_propertys",
            examples=["get_component_propertys", "set_component_propertys"]
        )] = "get_component_propertys",
        component_type: Annotated[Optional[str], Field(
            title="组件类型",
            description="组件类型名称（继承自Component的类型名称）",
            default=None,
            examples=["Rigidbody", "BoxCollider", "AudioSource", "Light", "Transform"]
        )] = None,
        properties: Annotated[Optional[Dict[str, Any]], Field(
            title="属性字典",
            description="要设置的属性字典（用于set_component_propertys操作）",
            default=None,
            examples=[
                {"mass": 2.0, "drag": 0.5},
                {"volume": 0.8, "pitch": 1.2},
                {"color": [1.0, 0.0, 0.0, 1.0]}
            ]
        )] = None
    ) -> Dict[str, Any]:
        """Unity组件编辑工具，用于获取和设置GameObject组件的属性。（二级工具）

        使用C#反射机制访问Unity组件的所有可序列化字段和属性，支持读取和修改组件状态。
        这是Unity编辑器Inspector面板功能的编程接口，所有修改支持Undo撤销。

        主要操作：
        - get_component_propertys: 获取组件属性，返回YAML格式数据
        - set_component_propertys: 批量设置组件属性，自动类型转换

        支持组件类型：
        - 物理：Rigidbody, Collider, CharacterController等
        - 渲染：MeshRenderer, SpriteRenderer, ParticleSystem等
        - 光照：Light, LightProbeGroup, ReflectionProbe等
        - 音频：AudioSource, AudioListener等
        - UI：RectTransform, Canvas, Image, Button等
        - 动画：Animator, Animation等
        - 以及所有自定义MonoBehaviour脚本

        数据类型支持：
        - 基本类型：int, float, bool, string
        - Unity类型：Vector2/3/4, Color, Quaternion, Rect, Bounds
        - 引用类型：GameObject, Transform, Material, Texture等
        - 集合类型：Array, List<T>, Dictionary

        属性访问规则：
        - 包含：public字段、[SerializeField]私有字段、可读写的public属性
        - 排除：hideFlags、快捷访问器(rigidbody/camera等)、会创建实例的属性(material/mesh等)

        注意事项：
        - 反射操作有性能开销，避免在每帧调用
        - 确保值类型匹配，Vector3需要3个元素
        - 先使用get查看可用属性作为设置模板
        """

        return get_common_call_response("edit_component")
