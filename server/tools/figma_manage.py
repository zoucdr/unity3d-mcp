"""
Unity Figma管理工具，包含Figma图片下载、节点数据拉取等功能。

支持的功能：
- 图片下载：批量下载图片
- 节点数据：拉取节点结构数据并保存为JSON
- 智能扫描：自动识别并下载所有需要的图片
- 图片预览：下载图片并返回base64编码
- 资源管理：自动转换为Sprite格式
- 转换规则：获取Figma到Unity UI框架的坐标转换规则
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
            description="要执行的Figma操作: fetch_document(拉取节点数据), download_images(批量下载图片，必须使用node_imgs参数), preview(预览图片并返回base64编码，使用node_id), get_conversion_rules(获取UI框架转换规则)",
            examples=["fetch_document", "download_images", "preview", "get_conversion_rules"]
        )],
        file_key: Annotated[Optional[str], Field(
            title="文件Key",
            description="Figma文件的Key",
            default=None,
            examples=["abc123def456", "xyz789uvw012"]
        )] = None,
        node_imgs: Annotated[Optional[Dict[str, str]], Field(
            title="节点图片映射【必须】",
            description="【必须使用】节点ID到文件名的映射字典，格式为{节点ID: 文件名}。download_images操作必须使用此参数，禁止使用node_id下载图片。此方式无需调用Figma API获取节点数据，大幅提高下载效率",
            default=None,
            examples=[{"1:4":"image1","1:5":"image2","1:6":"image3"}, {"123:456":"login_button","789:012":"app_icon"}]
        )] = None,
        node_id: Annotated[Optional[str], Field(
            title="单个节点ID（仅用于预览）",
            description="单个节点ID，仅用于preview操作。禁止用于download_images操作下载图片，下载图片必须使用node_imgs参数",
            default=None,
            examples=["1:2", "0:1", "123:456"]
        )] = None,
        save_path: Annotated[Optional[str], Field(
            title="保存路径",
            description="图片保存路径（相对于Assets或绝对路径）",
            default=None,
            examples=["Assets/Images/Figma", "D:/Downloads/Figma"]
        )] = None,
        image_format: Annotated[Optional[str], Field(
            title="图片格式",
            description="图片格式: png, jpg, svg, pdf，默认为png",
            default="png",
            examples=["png", "jpg", "svg", "pdf"]
        )] = "png",
        image_scale: Annotated[Optional[float], Field(
            title="图片缩放比例",
            description="图片缩放比例，默认为1",
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
            description="是否包含子节点数据，默认为true",
            default=True
        )] = True,
        ui_framework: Annotated[Optional[str], Field(
            title="UI框架类型",
            description="UI框架类型: ugui, uitoolkit, all（默认为all，返回所有框架的规则）",
            default="all",
            examples=["ugui", "uitoolkit", "all"]
        )] = "all"
    ):
        """Unity Figma管理工具，用于管理Figma资源和数据。

        支持多种Figma操作，适用于：
        - 图片下载：从Figma批量下载图片
        - 节点数据：拉取Figma文件的节点结构数据
        - 图片预览：下载图片并返回base64编码
        - 转换规则：获取UI框架坐标转换规则
        
        节点参数说明（重要）：
        - node_imgs: 【必须使用】字典格式的节点映射（如{"1:4":"image1","1:5":"image2"}），下载图片时必须使用此参数，效率最高
        - node_id: 仅用于单个图片预览(preview操作)，严禁用于下载图片(download_images操作)
        
        preview功能说明：
        - 提供file_key和node_id，下载图片并返回base64编码
        - 返回的base64数据包含完整的data URL格式：data:image/png;base64,...
        
        下载图片功能说明：
        - 【唯一允许的方式】批量高效下载（使用node_imgs直接指定文件名）:
           action="download_images", 
           node_imgs={"1:4":"login_button","1:5":"app_icon","1:6":"background"},
           save_path="Assets/Images/Figma"
           
        - 【严格禁止】使用node_id下载图片:
           ❌ 禁止使用node_id参数执行download_images操作
           
        - 单图片预览（返回base64，这是node_id唯一允许的用途）:
          action="preview", file_key="X7pR70jAksb9r7AMNfg3OH", node_id="1:4"
        
        获取转换规则功能说明：
        - 示例 - 获取所有框架规则:
          action="get_conversion_rules"
        
        - 示例 - 获取UGUI规则:
          action="get_conversion_rules", ui_framework="ugui"
        
        - 示例 - 获取UI Toolkit规则:
          action="get_conversion_rules", ui_framework="uitoolkit"
        
        - 返回数据包含：坐标系说明、转换公式、推荐设置、AI转换提示词
        """
      
        return send_to_unity("figma_manage", {
            "action": action,
            "file_key": file_key,
            "node_id": node_id,
            "node_imgs": node_imgs,
            "save_path": save_path,
            "image_format": image_format,
            "image_scale": image_scale,
            "local_json_path": local_json_path,
            "auto_convert_sprite": auto_convert_sprite,
            "include_children": include_children,
            "ui_framework": ui_framework
        })
