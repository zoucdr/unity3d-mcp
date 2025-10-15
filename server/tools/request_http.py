"""
UnityNetwork request tool，IncludeHTTPRequest、File download、APIInvoke and related features。
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_request_http_tools(mcp: FastMCP):
    @mcp.tool("request_http")
    def request_http(
        ctx: Context,
        action: Annotated[str, Field(
            title="Request operation type",
            description="Network operation to perform: get(GETRequest), post(POSTRequest), put(PUTRequest), delete(DELETERequest), download(Download file), upload(Upload file), ping(Network testing), batch_download(Batch download)",
            examples=["get", "post", "put", "delete", "download", "upload", "ping", "batch_download"]
        )],
        url: Annotated[Optional[str], Field(
            title="RequestURL",
            description="RequestedURLAddress",
            default=None,
            examples=["https://api.github.com/repos/unity3d/unity", "https://httpbin.org/get"]
        )] = None,
        data: Annotated[Optional[Dict[str, Any]], Field(
            title="Request data",
            description="Request data（Used forPOST/PUT，JSONFormat）",
            default=None,
            examples=[{"name": "test", "value": 123}, {"key": "value"}]
        )] = None,
        headers: Annotated[Optional[Dict[str, str]], Field(
            title="Request headers",
            description="Request headers dictionary",
            default=None,
            examples=[{"Content-Type": "application/json", "Authorization": "Bearer token"}]
        )] = None,
        save_path: Annotated[Optional[str], Field(
            title="Save path",
            description="Save path（Used for download，Relative toAssetsOr absolute path）",
            default=None,
            examples=["Assets/Downloads/file.zip", "D:/Downloads/image.png"]
        )] = None,
        file_path: Annotated[Optional[str], Field(
            title="File path",
            description="File path（Used for upload）",
            default=None,
            examples=["Assets/Data/config.json", "D:/Files/document.pdf"]
        )] = None,
        timeout: Annotated[Optional[int], Field(
            title="Timeout",
            description="Timeout（Seconds），Default30Seconds",
            default=30,
            ge=1,
            le=300
        )] = 30,
        method: Annotated[Optional[str], Field(
            title="HTTPMethod",
            description="HTTPMethod（GET, POST, PUT, DELETEEtc.）",
            default=None,
            examples=["GET", "POST", "PUT", "DELETE", "PATCH"]
        )] = None,
        content_type: Annotated[Optional[str], Field(
            title="Content-Type",
            description="Content-Type，Defaultapplication/json",
            default="application/json",
            examples=["application/json", "application/x-www-form-urlencoded", "multipart/form-data"]
        )] = "application/json",
        user_agent: Annotated[Optional[str], Field(
            title="User agent",
            description="User-Agent string",
            default=None,
            examples=["Unity-MCP/1.0", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"]
        )] = None,
        accept_certificates: Annotated[Optional[bool], Field(
            title="Accept certificates",
            description="Whether to accept all certificates（Used for testing）",
            default=False
        )] = False,
        follow_redirects: Annotated[Optional[bool], Field(
            title="Follow redirects",
            description="Whether to follow redirects",
            default=True
        )] = True,
        encoding: Annotated[Optional[str], Field(
            title="Text encoding",
            description="Text encoding，DefaultUTF-8",
            default="UTF-8",
            examples=["UTF-8", "GBK", "ISO-8859-1"]
        )] = "UTF-8",
        form_data: Annotated[Optional[Dict[str, str]], Field(
            title="Form data",
            description="Form data（Key-value pairs）",
            default=None,
            examples=[{"username": "user", "password": "pass"}, {"field1": "value1"}]
        )] = None,
        query_params: Annotated[Optional[Dict[str, str]], Field(
            title="Query parameters",
            description="Query parameters（Key-value pairs）",
            default=None,
            examples=[{"page": "1", "limit": "10"}, {"search": "unity"}]
        )] = None,
        auth_token: Annotated[Optional[str], Field(
            title="Bearer token",
            description="Bearer token（Bearer Token）",
            default=None,
            examples=["eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", "your-api-key"]
        )] = None,
        basic_auth: Annotated[Optional[str], Field(
            title="Basic authentication",
            description="Basic authentication（Username:Password）",
            default=None,
            examples=["username:password", "admin:secret123"]
        )] = None,
        retry_count: Annotated[Optional[int], Field(
            title="Retry count",
            description="Retry count，Default0",
            default=0,
            ge=0,
            le=5
        )] = 0,
        retry_delay: Annotated[Optional[int], Field(
            title="Retry delay",
            description="Retry delay（Seconds），Default1Seconds",
            default=1,
            ge=1,
            le=10
        )] = 1,
        urls: Annotated[Optional[List[str]], Field(
            title="URLList",
            description="URLArray（Used for batch download）",
            default=None,
            examples=[["https://example.com/file1.zip", "https://example.com/file2.zip"]]
        )] = None
    ) -> Dict[str, Any]:
        """UnityNetwork request tool，Used to perform various network operations。（Secondary tool）

        Supports multiple network operations，Suitable for：
        - HTTPRequest：GET、POST、PUT、DELETEAnd standard optionsHTTPMethod
        - File operations：Download and upload files
        - APIInvoke：With externalAPIPerform interactions
        - Network testing：pingAnd connectivity tests
        - Batch operations：Batch download multiple files
        """
        
        return get_common_call_response("request_http")
