"""
Unity项目搜索工具，包含项目资源搜索功能。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_project_search_tools(mcp: FastMCP):
    @mcp.tool("project_search")
    def project_search(
        ctx: Context,
        search_target: Annotated[str, Field(
            title="搜索目标类型",
            description="要搜索的目标类型: asset(资源文件), script(脚本文件), scene(场景文件), prefab(预制体), material(材质), texture(纹理), model(3D模型), shader(着色器), animation(动画), audio(音频), dependencies(依赖项), references(引用查找)",
            examples=["asset", "script", "scene", "prefab", "material", "texture", "model", "shader", "animation", "audio", "dependencies", "references"]
        )],
        query: Annotated[str, Field(
            title="搜索查询",
            description="搜索关键词、文件名模式或资源路径。当search_target为dependencies/references时，这里应该是完整的资源路径",
            examples=["Player*", "*.cs", "MainScene", "UI_*", "Assets/Scenes/MainScene.unity", "Assets/Materials/Ground.mat"]
        )],
        folder: Annotated[Optional[str], Field(
            title="搜索文件夹",
            description="限制搜索范围的文件夹路径，留空搜索整个项目。对于references搜索，这是查找引用的搜索范围",
            default=None,
            examples=["Assets/Scripts", "Assets/Prefabs", "Assets/Scenes"]
        )] = None,
        recursive: Annotated[bool, Field(
            title="递归搜索",
            description="对于dependencies搜索：是否递归获取所有间接依赖。对于普通搜索：是否搜索子文件夹",
            default=True
        )] = True,
        include_packages: Annotated[bool, Field(
            title="包含包文件",
            description="是否在搜索结果中包含Packages目录下的文件",
            default=False
        )] = False,
        case_sensitive: Annotated[bool, Field(
            title="区分大小写",
            description="搜索时是否区分大小写",
            default=False
        )] = False,
        max_results: Annotated[int, Field(
            title="最大结果数",
            description="限制返回的最大结果数量",
            default=100,
            ge=1,
            le=10000
        )] = 100
    ) -> Dict[str, Any]:
        """Unity项目搜索工具，用于在项目中搜索各种类型的资源和文件。（二级工具）

        支持多种搜索类型和过滤条件，适用于：
        - 快速定位：找到特定名称的资源文件
        - 批量处理：获取同类型文件列表进行批量操作
        - 项目清理：查找未使用或重复的资源
        - 依赖分析：查找资源的所有依赖项（dependencies）
        - 引用查找：查找哪些资源引用了指定资源（references）

        搜索类型说明：
        - dependencies: 查找指定资源依赖的所有其他资源（如场景依赖的材质、网格等）
        - references: 查找引用了指定资源的所有资源（反向依赖查找）
        - 其他类型: 按文件类型和名称模式搜索项目资源

        """
        
        return get_common_call_response("project_search")
