"""
Unity Figmamanagement tool，IncludeFigmaImage download、node data fetching and more。
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_figma_manage_tools(mcp: FastMCP):
    @mcp.tool("figma_manage")
    def figma_manage(
        ctx: Context,
        action: Annotated[str, Field(
            title="operation type",
            description="to executeFigmaOperation: download_image(download a single image), fetch_nodes(fetch node data), download_images(batch download images), preview(preview images and returnbase64Encoding)",
            examples=["download_image", "fetch_nodes", "download_images", "preview"]
        )],
        file_key: Annotated[Optional[str], Field(
            title="file key",
            description="Figmafile key",
            default=None,
            examples=["abc123def456", "xyz789uvw012"]
        )] = None,
        node_ids: Annotated[Optional[str], Field(
            title="NodeIDList",
            description="comma separated nodesIDString",
            default=None,
            examples=["1:4,1:5,1:6", "1:4", "123:456,789:012"]
        )] = None,
        node_imgs: Annotated[Optional[str], Field(
            title="node image mapping",
            description="JSONnode name mapping of format，Formatted as{NodeID: File name}。when this parameter is provided，will use the specified filename directly，no need to callFigma APIget node data，improve download efficiency",
            default=None,
            examples=['{"1:4":"image1","1:5":"image2","1:6":"image3"}', '{"123:456":"login_button","789:012":"app_icon"}']
        )] = None,
        root_node_id: Annotated[Optional[str], Field(
            title="Root nodeID",
            description="root node for smart downloadID，scan downloadable children from given node",
            default=None,
            examples=["1:2", "0:1", "123:456"]
        )] = None,
        save_path: Annotated[Optional[str], Field(
            title="save path",
            description="image save path（Relative toAssetsor absolute path）",
            default=None,
            examples=["Assets/Images/Figma", "D:/Downloads/Figma"]
        )] = None,
        format: Annotated[Optional[str], Field(
            title="image format",
            description="image format",
            default="PNG",
            examples=["PNG", "JPG", "SVG"]
        )] = "PNG",
        scale: Annotated[Optional[float], Field(
            title="scale",
            description="image scale",
            default=1.0,
            ge=0.1,
            le=4.0
        )] = 1.0,
        local_json_path: Annotated[Optional[str], Field(
            title="LocalJSONPath",
            description="LocalJSONfile path（Used fordownload_imagesOperation）",
            default=None,
            examples=["Assets/Data/figma_nodes.json", "D:/Data/nodes.json"]
        )] = None,
        auto_convert_sprite: Annotated[Optional[bool], Field(
            title="Auto-convertSprite",
            description="auto convert downloaded images toSpriteFormat",
            default=True
        )] = True,
        include_children: Annotated[Optional[bool], Field(
            title="include child nodes",
            description="include child node data",
            default=False
        )] = False,
        depth: Annotated[Optional[int], Field(
            title="Depth",
            description="depth for node data fetch",
            default=1,
            ge=1,
            le=10
        )] = 1
    ) -> Dict[str, Any]:
        """Unity Figmamanagement tool，for managementFigmaassets and data。（secondary tool）

        supports multipleFigmaOperation，Suitable for：
        - Image download：FromFigmadownload single or batch images
        - node data：FetchFigmanode structure data of the file
        - asset management：auto convert and manage downloaded assets
        - batch processing：efficiently handle multiple nodes
        - smart scan：recursively scan all downloadable image nodes from root
        - image preview：download images and returnbase64Encoding，no need to save files
        
        node parameter description：
        - node_ids: comma separated nodesIDString（Such as"1:4,1:5,1:6"）
        - node_imgs: JSONnode mapping of format（Such as'{"1:4":"image1","1:5":"image2"}'）
        - root_node_id: Root nodeID，for smart scan and download（Such as"1:2"）
        
        previewFeature description：
        - Providefile_keyAndnode_ids（use the first onlyID），download images and returnbase64Encoding
        - Returnedbase64data includes fulldata URLFormat：data:image/png;base64,...
        
        usage：
        1. Provide onlynode_ids: auto invokeFigma APIget node names，then download
        2. Providenode_imgs: download using the specified filename，no extraAPIInvoke，more efficient
        3. Also provide: node_imgsPriority，node_idsAs a supplement
        4. Provideroot_node_id: intelligently scan the node and its children，auto detect images to download
        
        Example - Basic download（auto obtain node names）:
          node_ids="1:4,1:5,1:6"
        
        Example - Efficient download（directly specify filename）:
          node_imgs='{"1:4":"login_button","1:5":"app_icon","1:6":"background"}'
        
        Example - smart scan download:
          root_node_id="1:2"
          
        Example - image preview（Returnbase64）:
          action="preview", file_key="X7pR70jAksb9r7AMNfg3OH", node_ids="1:4"
        """
        return get_common_call_response("figma_manage")
