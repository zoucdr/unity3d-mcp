"""
Unity Figma管理工具，包含Figma图片下载、节点数据拉取等功能。
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_figma_manage_tools(mcp: FastMCP):
    @mcp.tool("figma_manage")
    def figma_manage(
        ctx: Context,
        action: Annotated[str, Field(
            title="操作类型",
            description="要执行的Figma操作: download_image(下载单张图片), fetch_nodes(拉取节点数据), download_images(批量下载图片), preview(预览图片并返回base64编码)",
            examples=["download_image", "fetch_nodes", "download_images", "preview"]
        )],
        file_key: Annotated[Optional[str], Field(
            title="文件密钥",
            description="Figma文件的密钥",
            default=None,
            examples=["abc123def456", "xyz789uvw012"]
        )] = None,
        node_ids: Annotated[Optional[str], Field(
            title="节点ID列表",
            description="逗号分隔的节点ID字符串",
            default=None,
            examples=["1:4,1:5,1:6", "1:4", "123:456,789:012"]
        )] = None,
        node_imgs: Annotated[Optional[str], Field(
            title="节点图片映射",
            description="JSON格式的节点名称映射，格式为{节点ID: 文件名}。当提供此参数时，将直接使用指定的文件名，无需调用Figma API获取节点数据，提高下载效率",
            default=None,
            examples=['{"1:4":"image1","1:5":"image2","1:6":"image3"}', '{"123:456":"login_button","789:012":"app_icon"}']
        )] = None,
        root_node_id: Annotated[Optional[str], Field(
            title="根节点ID",
            description="智能下载时的根节点ID，用于从指定节点开始扫描所有可下载的子节点",
            default=None,
            examples=["1:2", "0:1", "123:456"]
        )] = None,
        save_path: Annotated[Optional[str], Field(
            title="保存路径",
            description="图片保存路径（相对于Assets或绝对路径）",
            default=None,
            examples=["Assets/Images/Figma", "D:/Downloads/Figma"]
        )] = None,
        format: Annotated[Optional[str], Field(
            title="图片格式",
            description="图片格式",
            default="PNG",
            examples=["PNG", "JPG", "SVG"]
        )] = "PNG",
        scale: Annotated[Optional[float], Field(
            title="缩放比例",
            description="图片缩放比例",
            default=1.0,
            ge=0.1,
            le=4.0
        )] = 1.0,
        local_json_path: Annotated[Optional[str], Field(
            title="本地JSON路径",
            description="本地JSON文件路径（用于download_images操作）",
            default=None,
            examples=["Assets/Data/figma_nodes.json", "D:/Data/nodes.json"]
        )] = None,
        auto_convert_sprite: Annotated[Optional[bool], Field(
            title="自动转换Sprite",
            description="是否自动将下载的图片转换为Sprite格式",
            default=True
        )] = True,
        include_children: Annotated[Optional[bool], Field(
            title="包含子节点",
            description="是否包含子节点数据",
            default=False
        )] = False,
        depth: Annotated[Optional[int], Field(
            title="深度",
            description="节点数据拉取的深度",
            default=1,
            ge=1,
            le=10
        )] = 1
    ) -> Dict[str, Any]:
        """Unity Figma管理工具，用于管理Figma资源和数据。

        支持多种Figma操作，适用于：
        - 图片下载：从Figma下载单张或批量图片
        - 节点数据：拉取Figma文件的节点结构数据
        - 资源管理：自动转换和管理下载的资源
        - 批量处理：高效处理多个节点
        - 智能扫描：从根节点递归扫描所有可下载的图片节点
        - 图片预览：下载图片并返回base64编码，无需保存文件
        
        节点参数说明：
        - node_ids: 逗号分隔的节点ID字符串（如"1:4,1:5,1:6"）
        - node_imgs: JSON格式的节点映射（如'{"1:4":"image1","1:5":"image2"}'）
        - root_node_id: 根节点ID，用于智能扫描下载（如"1:2"）
        
        preview功能说明：
        - 提供file_key和node_ids（只使用第一个ID），下载图片并返回base64编码
        - 返回的base64数据包含完整的data URL格式：data:image/png;base64,...
        
        使用方式：
        1. 仅提供node_ids: 自动调用Figma API获取节点名称，然后下载
        2. 提供node_imgs: 直接使用指定的文件名下载，无需额外API调用，效率更高
        3. 同时提供: node_imgs优先，node_ids作为补充
        4. 提供root_node_id: 智能扫描该节点及其所有子节点，自动识别需要下载的图片
        
        示例 - 基础下载（自动获取节点名称）:
          node_ids="1:4,1:5,1:6"
        
        示例 - 高效下载（直接指定文件名）:
          node_imgs='{"1:4":"login_button","1:5":"app_icon","1:6":"background"}'
        
        示例 - 智能扫描下载:
          root_node_id="1:2"
          
        示例 - 图片预览（返回base64）:
          action="preview", file_key="X7pR70jAksb9r7AMNfg3OH", node_ids="1:4"
        """
        return send_to_unity("figma_manage", {
            "action": action,
            "file_key": file_key,
            "node_ids": node_ids,
            "node_imgs": node_imgs,
            "root_node_id": root_node_id,
            "save_path": save_path,
            "format": format,
            "scale": scale,
            "local_json_path": local_json_path,
            "auto_convert_sprite": auto_convert_sprite,
            "include_children": include_children,
            "depth": depth
        })
