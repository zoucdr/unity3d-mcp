"""
UnityProject search tool，Includes project asset search。
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
            title="Search target type",
            description="Target type to search: asset(Asset files), script(Script files), scene(Scene files), prefab(Prefab), material(Material), texture(Texture), model(3DModel), shader(Shader), animation(Animation), audio(Audio), dependencies(Dependencies), references(Reference search)",
            examples=["asset", "script", "scene", "prefab", "material", "texture", "model", "shader", "animation", "audio", "dependencies", "references"]
        )],
        query: Annotated[str, Field(
            title="Search query",
            description="Search keywords、Filename pattern or asset path。Whensearch_targetIsdependencies/referencesWhen，This should be a full asset path",
            examples=["Player*", "*.cs", "MainScene", "UI_*", "Assets/Scenes/MainScene.unity", "Assets/Materials/Ground.mat"]
        )],
        folder: Annotated[Optional[str], Field(
            title="Search folder",
            description="Folder path limiting the search scope，Leave empty to search entire project。ForreferencesSearch，This is the scope to search references",
            default=None,
            examples=["Assets/Scripts", "Assets/Prefabs", "Assets/Scenes"]
        )] = None,
        recursive: Annotated[bool, Field(
            title="Recursive search",
            description="FordependenciesSearch：Whether to get indirect dependencies recursively。For basic searches：Whether to search subfolders",
            default=True
        )] = True,
        include_packages: Annotated[bool, Field(
            title="Include package files",
            description="Whether to include in resultsPackagesFiles under the directory",
            default=False
        )] = False,
        case_sensitive: Annotated[bool, Field(
            title="Case sensitive",
            description="Case sensitivity when searching",
            default=False
        )] = False,
        max_results: Annotated[int, Field(
            title="Max results",
            description="Limit on maximum results",
            default=100,
            ge=1,
            le=10000
        )] = 100
    ) -> Dict[str, Any]:
        """UnityProject search tool，Used to search various assets and files in the project。（Secondary tool）

        Supports multiple search types and filters，Suitable for：
        - Quick locate：Find asset by specific name
        - Batch processing：Get files of the same type for batch ops
        - Project cleanup：Find unused or duplicate assets
        - Dependency analysis：Find all dependencies of an asset（dependencies）
        - Reference search：Find which assets reference the given asset（references）

        Search type description：
        - dependencies: Find all dependencies of a given asset（E.g.、Meshes）
        - references: Find all assets referencing a given asset（Reverse dependency lookup）
        - Other types: Search assets by type and name pattern

        """
        
        return get_common_call_response("project_search")
