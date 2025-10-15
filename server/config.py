"""
Configuration settings for the Unity MCP Server.
This file contains all configurable args for the server.
"""

from dataclasses import dataclass

@dataclass
class ServerConfig:
    """Main configuration class for the MCP server."""
    
    # Network settings
    unity_host: str = "127.0.0.1"
    unity_port_start: int = 6400
    unity_port_end: int = 6405
    
    # Connection settings
    connection_timeout: float = 120.0  # increase to120seconds timeout，reduce connection issues
    buffer_size: int = 16 * 1024 * 1024  # 16MB buffer
    smart_port_discovery: bool = True   # re enable smart port discovery，align with new port switching logic
    ping_timeout: float = 3.0  # pingper command timeout setting
    connection_retry_delay: float = 1.0  # connection retry delay
    
    # Port switching settings
    port_failure_timeout: float = 60.0  # cooldown after port failure（seconds）
    max_failed_ports: int = 10  # max failed port records
    
    # Logging settings
    log_level: str = "INFO"
    log_format: str = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    
    # Server settings
    max_retries: int = 3
    retry_delay: float = 1.0
    
    # Advanced connection settings
    connection_health_check_interval: float = 30.0  # connection health check interval（seconds）
    auto_port_switching: bool = True  # enable automatic port switching
    port_scan_timeout: float = 1.0  # port scan timeout

# Create a global config instance
config = ServerConfig() 