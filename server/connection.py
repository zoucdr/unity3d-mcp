import json
import logging
import time
import requests
from dataclasses import dataclass, field
from typing import Dict, Any, Union, List, Optional
from config import config

# Import JSONDecodeError for older Python versions compatibility
try:
    from json import JSONDecodeError
except ImportError:
    # For Python < 3.5
    JSONDecodeError = ValueError

# Configure logging using settings from config
logging.basicConfig(
    level=getattr(logging, config.log_level),
    format=config.log_format
)
logger = logging.getLogger("unity3d-mcp-server")

@dataclass
class UnityConnection:
    """Manages HTTP connection to the Unity Editor."""
    host: str = config.unity_host
    port: Optional[int] = None  # Currently active port
    failed_ports: Dict[int, float] = field(default_factory=dict)  # Track ports that have failed recently with timestamps
    connection_attempts: int = 0  # Track total connection attempts
    last_connection_time: float = 0  # Track when last connection was made
    last_cleanup_time: float = 0  # Track when failed ports were last cleaned up
    session: requests.Session = field(default_factory=requests.Session)  # HTTP session for connection reuse
    
    def __post_init__(self):
        # 配置 session
        self.session.headers.update({
            'Content-Type': 'application/json',
            'User-Agent': 'Unity-MCP-Client/1.0'
        })

    def connect(self, force_reconnect: bool = False) -> bool:
        """Find an available Unity HTTP server by trying multiple ports."""
        # HTTP 是无状态的，如果有可用端口且不强制重连，直接验证
        if self.port and not force_reconnect:
            if self._is_unity_mcp_server_on_port(self.port):
                logger.debug(f"Reusing existing port {self.port}")
                return True
            else:
                logger.warning(f"Port {self.port} is no longer available")
                self.failed_ports[self.port] = time.time()
                self.port = None
        
        self.connection_attempts += 1
        logger.info(f"Starting port discovery attempt #{self.connection_attempts}")
        
        # 清理过期的失败端口记录
        self._cleanup_expired_failed_ports()
        
        # 获取可尝试的端口列表（排除最近失败的端口）
        available_ports = []
        for port in range(config.unity_port_start, config.unity_port_end + 1):
            if port not in self.failed_ports:
                available_ports.append(port)
        
        # 如果所有端口都失败过，清空失败记录重新开始
        if not available_ports:
            logger.warning("All ports have failed recently, clearing failed ports list and retrying all")
            self.failed_ports.clear()
            available_ports = list(range(config.unity_port_start, config.unity_port_end + 1))
        
        failed_ports_info = {k: f"{(time.time() - v):.1f}s ago" for k, v in self.failed_ports.items()}
        logger.info(f"Trying {len(available_ports)} available ports, failed ports: {failed_ports_info}")
        
        # Use smart port discovery if enabled
        if config.smart_port_discovery:
            # Scan for active Unity MCP HTTP servers
            active_ports = []
            logger.debug("Scanning for active Unity MCP HTTP servers...")
            for port in available_ports:
                if self._is_unity_mcp_server_on_port(port):
                    active_ports.append(port)
            
            if active_ports:
                logger.info(f"Found Unity MCP HTTP servers on ports: {active_ports}")
                # Use the first active port
                self.port = active_ports[0]
                self.failed_ports.pop(self.port, None)
                self.last_connection_time = time.time()
                logger.info(f"Selected port {self.port} for HTTP communication")
                return True
        
        # Traditional sequential port trying
        logger.info(f"Using traditional sequential port discovery on {len(available_ports)} available ports...")
        for port in available_ports:
            if self._is_unity_mcp_server_on_port(port):
                self.port = port
                self.failed_ports.pop(port, None)
                self.last_connection_time = time.time()
                logger.info(f"Selected port {port} for HTTP communication")
                return True
            else:
                self.failed_ports[port] = time.time()
        
        logger.error(f"Failed to find Unity HTTP server on any port in range {config.unity_port_start}-{config.unity_port_end}")
        return False
    
    def _is_unity_mcp_server_on_port(self, port: int) -> bool:
        """Check if there's an active Unity MCP HTTP server on the given port."""
        try:
            url = f"http://{self.host}:{port}"
            logger.debug(f"Pinging {url} with timeout {config.ping_timeout}s")
            
            response = self.session.get(url, timeout=config.ping_timeout)
            if response.status_code == 200:
                try:
                    response_json = response.json()
                    logger.debug(f"Received response from {response_json}")
                    # Check for new pong format
                    if response_json.get("status") == "success" and response_json.get("result", {}).get("message") == "pong":
                        logger.debug(f"Successfully connected to Unity MCP server on {url} (new format)")
                        return True
                except JSONDecodeError:
                    logger.debug(f"Received non-JSON response from {url}, but status was 200. Assuming it's not an MCP server.")
            else:
                logger.debug(f"Ping to {url} returned status code {response.status_code}")

        except requests.exceptions.Timeout:
            logger.debug(f"Ping to http://{self.host}:{port}/ timed out.")
        except requests.exceptions.ConnectionError:
            logger.debug(f"Connection refused on http://{self.host}:{port}/.")
        except Exception as e:
            logger.warning(f"An unexpected error occurred while pinging http://{self.host}:{port}/: {e}")
        
        return False
    
    def disconnect(self):
        """Close the HTTP session."""
        if self.session:
            try:
                self.session.close()
            except Exception as e:
                logger.error(f"Error closing HTTP session: {str(e)}")
            finally:
                self.session = requests.Session()
                # 重新配置 session
                self.session.headers.update({
                    'Content-Type': 'application/json',
                    'User-Agent': 'Unity-MCP-Client/1.0'
                })
    
    def send_command_with_retry(self, command_type: str, cmd: Union[Dict[str, Any], List, None] = None, max_retries: int = 2) -> Dict[str, Any]:
        """Send command with retry mechanism and smart port switching."""
        last_error = None
        
        for attempt in range(max_retries + 1):
            try:
                return self.send_command(command_type, cmd)
            except Exception as e:
                last_error = e
                error_str = str(e).lower()
                logger.warning(f"Command attempt {attempt + 1} failed: {str(e)}")
                
                # 如果不是最后一次尝试，准备重试
                if attempt < max_retries:
                    # 如果是连接问题，标记当前端口为失败并尝试其他端口
                    if ("connection" in error_str or "timeout" in error_str or 
                        "refused" in error_str or "unavailable" in error_str):
                        
                        if self.port:
                            logger.info(f"Marking port {self.port} as failed due to connection issue")
                            self.failed_ports[self.port] = time.time()
                            self.port = None
                        
                        # 尝试连接到不同端口
                        logger.info(f"Attempting to find different port (attempt {attempt + 1})")
                        if not self.connect(force_reconnect=True):
                            logger.warning(f"Could not find any available port")
                            time.sleep(1)
                            continue
                    else:
                        # 非连接问题，稍作等待
                        time.sleep(0.5)
                    
        # 所有重试都失败了
        raise last_error
    
    def _cleanup_expired_failed_ports(self):
        """Clean up expired failed port records."""
        current_time = time.time()
        
        # 检查是否需要清理（避免过于频繁的清理）
        if current_time - self.last_cleanup_time < 30:  # 30秒清理一次
            return
            
        expired_ports = []
        for port, failure_time in self.failed_ports.items():
            if current_time - failure_time > config.port_failure_timeout:
                expired_ports.append(port)
        
        for port in expired_ports:
            self.failed_ports.pop(port)
            logger.debug(f"Removed expired failed port: {port}")
        
        if expired_ports:
            logger.info(f"Cleaned up {len(expired_ports)} expired failed ports: {expired_ports}")
        
        self.last_cleanup_time = current_time
    
    def send_command(self, command_type: str, cmd: Union[Dict[str, Any], List, None] = None) -> Dict[str, Any]:
        """Send a command to Unity via HTTP POST and return its response."""
        if not self.port and not self.connect():
            failed_ports_summary = {k: f"{(time.time() - v):.1f}s ago" for k, v in list(self.failed_ports.items())[:5]}
            raise ConnectionError(f"No Unity HTTP server available. Recent failed ports: {failed_ports_summary}")
        
        url = f"http://{self.host}:{self.port}/"
        
        # Special handling for ping command (使用 GET 请求)
        if command_type == "ping":
            try:
                logger.info(f"Sending ping to {url}")
                response = self.session.get(url, timeout=config.ping_timeout)
                
                if response.status_code == 200:
                    try:
                        data = response.json()
                        response_str = json.dumps(data).lower()
                        if "pong" in response_str or "success" in response_str:
                            logger.info("✅ Ping verification successful")
                            return {"message": "pong"}
                        else:
                            logger.warning(f"Unexpected ping response: {data}")
                            return {"message": "pong", "warning": "Unexpected response format"}
                    except (JSONDecodeError, ValueError):
                        # 尝试检查文本响应
                        if 'pong' in response.text.lower() or 'success' in response.text.lower():
                            logger.info("✅ Ping verification successful (text response)")
                            return {"message": "pong"}
                            
                        logger.warning("Ping response is not valid JSON")
                        return {"message": "pong", "warning": "Response parsing failed"}
                else:
                    raise Exception(f"Ping failed with status code {response.status_code}")
                    
            except requests.exceptions.Timeout:
                logger.error("Ping timeout")
                raise ConnectionError("Ping timeout")
            except requests.exceptions.ConnectionError as e:
                logger.error(f"Ping connection error: {str(e)}")
                raise ConnectionError(f"Connection error during ping: {str(e)}")
            except Exception as e:
                logger.error(f"Ping error: {str(e)}")
                raise ConnectionError(f"Connection verification failed: {str(e)}")
        
        # Normal command handling (使用 POST 请求)
        command = {"type": command_type, "cmd": cmd if cmd is not None else {}}
        try:
            command_json = json.dumps(command, ensure_ascii=False)
            command_size = len(command_json.encode('utf-8'))
            
            if command_size > 1024 * 1024:  # 1MB
                logger.warning(f"Large command detected ({command_size} bytes). This might be slow.")
                
            logger.info(f"Sending HTTP POST to {url}: {command_type} ({command_size} bytes)")
            
            # 设置更长的超时时间和重试次数
            retry_count = 3
            current_retry = 0
            last_error = None
            
            while current_retry < retry_count:
                try:
                    # Send HTTP POST request with increased timeout
                    response = self.session.post(
                        url, 
                        json=command,
                        timeout=config.send_timeout * 2  # 加倍超时时间
                    )
                    
                    # 检查HTTP状态码
                    if response.status_code != 200:
                        logger.error(f"HTTP error: status code {response.status_code}")
                        raise Exception(f"HTTP request failed with status code {response.status_code}")
                    
                    # 解析JSON响应
                    try:
                        result = response.json()
                    except (JSONDecodeError, ValueError) as je:
                        logger.error(f"JSON decode error: {str(je)}")
                        partial_response = response.text[:500] + "..." if len(response.text) > 500 else response.text
                        logger.error(f"Partial response: {partial_response}")
                        raise Exception(f"Invalid JSON response from Unity: {str(je)}")
                    
                    # 检查Unity返回的错误
                    if result.get("status") == "error":
                        error_message = result.get("error") or result.get("message", "Unknown Unity error")
                        logger.error(f"Unity error: {error_message}")
                        raise Exception(error_message)
                    
                    logger.info(f"✅ HTTP request successful, received response")
                    return result.get("result", {})
                    
                except (requests.exceptions.Timeout, requests.exceptions.ConnectionError) as e:
                    current_retry += 1
                    last_error = e
                    logger.warning(f"Request failed (attempt {current_retry}/{retry_count}): {str(e)}")
                    if current_retry < retry_count:
                        logger.info(f"Retrying in 1 second...")
                        time.sleep(1)
                    else:
                        break
            
            # 如果所有重试都失败
            if isinstance(last_error, requests.exceptions.Timeout):
                logger.error(f"HTTP request timeout on port {self.port} after {retry_count} attempts")
                self.failed_ports[self.port] = time.time()
                self.port = None
                raise Exception(f"Request timeout on port {self.port} after {retry_count} attempts")
            elif isinstance(last_error, requests.exceptions.ConnectionError):
                logger.error(f"HTTP connection error on port {self.port}: {str(last_error)}")
                self.failed_ports[self.port] = time.time()
                self.port = None
                raise Exception(f"Connection error on port {self.port}: {str(last_error)}")
            else:
                raise last_error
                
        except Exception as e:
            if not isinstance(e, (requests.exceptions.Timeout, requests.exceptions.ConnectionError)):
                error_str = str(e).lower()
                logger.error(f"Communication error with Unity on port {self.port}: {str(e)}")
                
                # 如果是连接相关错误，标记当前端口为失败
                if ("connection" in error_str or "timeout" in error_str or "refused" in error_str):
                    if self.port:
                        self.failed_ports[self.port] = time.time()
                        logger.info(f"Added port {self.port} to failed ports list")
                        self.port = None
            
            raise Exception(f"Failed to communicate with Unity on port {self.port}: {str(e)}")

# Global Unity connection
_unity_connection = None

def get_unity_connection() -> UnityConnection:
    """Retrieve or establish a Unity HTTP connection with automatic port discovery."""
    global _unity_connection
    
    # 如果已存在连接，先验证其可用性
    if _unity_connection is not None:
        try:
            # 验证现有端口是否还可用
            if _unity_connection.port:
                # 尝试ping验证
                result = _unity_connection.send_command("ping")
                logger.debug(f"Reusing existing Unity HTTP connection on port {_unity_connection.port}")
                return _unity_connection
        except Exception as e:
            logger.warning(f"Existing connection validation failed on port {_unity_connection.port}: {str(e)}")
            # 标记当前端口为失败
            if _unity_connection.port:
                _unity_connection.failed_ports[_unity_connection.port] = time.time()
                _unity_connection.port = None
    
    # 如果没有连接或连接失败，创建新连接
    if _unity_connection is None:
        _unity_connection = UnityConnection()
    
    # 尝试找到可用的端口
    max_retries = 3
    for attempt in range(max_retries):
        try:
            logger.info(f"Finding Unity HTTP server (attempt {attempt + 1}/{max_retries})")
            
            if not _unity_connection.connect(force_reconnect=(attempt > 0)):
                failed_ports_summary = {k: f"{(time.time() - v):.1f}s ago" for k, v in list(_unity_connection.failed_ports.items())[:5]}
                failed_ports_info = f" (recent failed ports: {failed_ports_summary})" if failed_ports_summary else ""
                if attempt < max_retries - 1:
                    logger.warning(f"Port discovery attempt {attempt + 1} failed{failed_ports_info}, retrying...")
                    time.sleep(1)
                    continue
                else:
                    raise ConnectionError(f"Could not find Unity HTTP server on any port{failed_ports_info}. Ensure the Unity Editor and MCP HTTP server are running.")
            
            # 验证新连接（使用ping）
            try:
                result = _unity_connection.send_command("ping")
                logger.info(f"Successfully established Unity HTTP connection on port {_unity_connection.port}")
                return _unity_connection
            except Exception as ping_error:
                logger.warning(f"Ping verification failed on port {_unity_connection.port}: {str(ping_error)}")
                # 标记端口为失败并重试
                if _unity_connection.port:
                    _unity_connection.failed_ports[_unity_connection.port] = time.time()
                    _unity_connection.port = None
                raise ping_error
                
        except Exception as e:
            logger.error(f"Connection attempt {attempt + 1} failed: {str(e)}")
            
            if attempt < max_retries - 1:
                time.sleep(1)  # 等待1秒后重试
            else:
                failed_summary = {k: f"{(time.time() - v):.1f}s ago" for k, v in list(_unity_connection.failed_ports.items())[:5]}
                failed_info = f" (recent failed ports: {failed_summary})" if failed_summary else ""
                raise ConnectionError(f"Could not establish Unity HTTP connection after {max_retries} attempts{failed_info}: {str(e)}")
    
    return _unity_connection
